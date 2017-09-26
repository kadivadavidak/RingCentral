using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Linq;
using System.Xml;
using System.Collections.Generic;

namespace RingCentralDataIntegration
{
    internal class DataHandler
    {
        internal static SqlConnection Connection()
        {
            var dbUserName = ConfigurationManager.AppSettings["SQLServerUserName"];
            var dbPassword = ConfigurationManager.AppSettings["SQLServerPassword"];
            string connetionString = ConfigurationManager.ConnectionStrings["partialConnectString"] + $";User ID={dbUserName};Password={dbPassword}";

            return new SqlConnection(connetionString);
        }

        internal static void CallLogToDatebase(XmlDocument response)
        {
            if (response.InnerText == "")
                return;

            var connection = Connection();
            var ds = new DataSet();
            var table = new DataTable();
            var dateAccessed = DateTime.Now;
            var xr = new XmlNodeReader(response);

            ds.ReadXml(xr);

            table.Columns.Add("Uri");
            table.Columns.Add("Id");
            table.Columns.Add("SessionId");
            table.Columns.Add("StartTime");
            table.Columns.Add("Duration");
            table.Columns.Add("Type");
            table.Columns.Add("Direction");
            table.Columns.Add("Action");
            table.Columns.Add("Result");
            table.Columns.Add("Transport");
            table.Columns.Add("LastModifiedTime");
            table.Columns.Add("DateAccessed");

            var tblIndex = ds.Tables.IndexOf("records");

            if (tblIndex == -1)
            {
                tblIndex = ds.Tables.IndexOf("record");
            }

            for (var i = 0; i <= ds.Tables[tblIndex].Rows.Count - 1; i++)
            {
                var row = table.NewRow();
                var rows = ds.Tables[tblIndex].Rows[i];

                row[0] = rows["uri"];
                row[1] = rows["id"];
                row[2] = rows["sessionId"];
                row[3] = rows["StartTime"];
                row[4] = rows["duration"];
                row[5] = rows["type"];
                row[6] = rows["direction"];
                row[7] = rows["action"];
                row[8] = rows["result"];
                row[9] = rows["transport"];
                row[10] = rows["lastModifiedTime"];
                row[11] = dateAccessed;

                table.Rows.Add(row);
            }

            using (var bulkCopy = new SqlBulkCopy(connection))
            {
                connection.Open();

                foreach (DataColumn column in table.Columns)
                {
                    bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                }

                bulkCopy.BulkCopyTimeout = 600;
                bulkCopy.DestinationTableName = "ringcentral.CallLog";
                bulkCopy.BatchSize = 10000;
                bulkCopy.WriteToServer(table);

                connection.Close();
            }

            CallLegToDatebase(ds);
        }

        internal static void CallLegToDatebase(DataSet ds)
        {
            var connection = Connection();
            var table = new DataTable();
            var dateAccessed = DateTime.Now;

            table.Columns.Add("CallLogId");
            table.Columns.Add("LegId");
            table.Columns.Add("StartTime");
            table.Columns.Add("Duration");
            table.Columns.Add("Type");
            table.Columns.Add("Direction");
            table.Columns.Add("Action");
            table.Columns.Add("Result");
            table.Columns.Add("Transport");
            table.Columns.Add("LegType");
            table.Columns.Add("ExtensionId");
            table.Columns.Add("ToName");
            table.Columns.Add("ToPhoneNumber");
            table.Columns.Add("ToLocation");
            table.Columns.Add("FromName");
            table.Columns.Add("FromPhoneNumber");
            table.Columns.Add("FromLocation");
            table.Columns.Add("MessageId");
            table.Columns.Add("MessageType");
            table.Columns.Add("DateAccessed");

            var tblIndex = ds.Tables.IndexOf("legs");
            var recordTblIndex = ds.Tables.IndexOf("records");
            var toTblIndex = ds.Tables.IndexOf("to");
            var fromTblIndex = ds.Tables.IndexOf("from");
            var messageTblIndex = ds.Tables.IndexOf("message");
            var extensionTblIndex = ds.Tables.IndexOf("extension");
            var recordsId = 0;

            for (var i = 0; i <= ds.Tables[tblIndex].Rows.Count - 1; i++)
            {
                var row = table.NewRow();
                var columns = ds.Tables[tblIndex].Columns;
                var rows = ds.Tables[tblIndex].Rows[i];
                
                recordsId = Convert.IsDBNull(ds.Tables[fromTblIndex].Rows[i]["records_Id"]) ? recordsId : Convert.ToInt32(ds.Tables[fromTblIndex].Rows[i]["records_Id"]);
                var legId = Convert.ToInt32(ds.Tables[tblIndex].Rows[i]["legs_Id"]);
                var records = ds.Tables[recordTblIndex].Select("records_Id = " + rows.ItemArray[columns.IndexOf("records_Id")]);

                row[0] = records[0]["id"];
                row[1] = rows["legs_Id"];
                row[2] = rows["startTime"];
                row[3] = rows["duration"];
                row[4] = rows["type"];
                row[5] = rows["direction"];
                row[6] = rows["action"];
                row[7] = rows["result"];
                row[8] = rows["transport"];
                row[9] = rows["legType"];

                var extensions = ds.Tables[extensionTblIndex].Select("legs_Id = " + legId);
                if (extensions.Length > 0)
                {
                    row[10] = extensions[0]["id"];
                }

                var callTo = ds.Tables[toTblIndex].Select("legs_id = " + legId);
                if (callTo.Length > 0)
                {
                    row[11] = callTo[0]["name"].ToString().Replace("'", "''");
                    row[12] = callTo[0]["phoneNumber"];
                    row[13] = callTo[0]["location"].ToString().Replace("'", "''");
                }

                var callFrom = ds.Tables[fromTblIndex].Select("legs_id = " + legId);
                if (callFrom.Length > 0)
                {
                    row[14] = callFrom[0]["name"].ToString().Replace("'", "''");
                    row[15] = callFrom[0]["phoneNumber"];
                    row[16] = callFrom[0]["location"].ToString().Replace("'", "''");
                }
                
                if (messageTblIndex != -1)
                {
                    var messages = ds.Tables[messageTblIndex].Select("legs_id = " + legId);
                    if (messages.Length > 0)
                    {
                        row[17] = messages[0]["id"];
                        row[18] = messages[0]["type"];
                    }
                }
                

                row[19] = dateAccessed;

                table.Rows.Add(row);
            }

            using (var bulkCopy = new SqlBulkCopy(connection))
            {
                connection.Open();

                foreach (DataColumn column in table.Columns)
                {
                    bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                }

                bulkCopy.BulkCopyTimeout = 600;
                bulkCopy.DestinationTableName = "ringcentral.CallLeg";
                bulkCopy.BatchSize = 10000;
                bulkCopy.WriteToServer(table);

                connection.Close();
            }
        }

        internal static void PresenceToDatebase(XmlDocument response)
        {
            var ds = new DataSet();
            var table = new DataTable();
            var xr = new XmlNodeReader(response);
            var dateAccessed = DateTime.Now;

            ds.ReadXml(xr);

            var extensionTblIndex = ds.Tables.IndexOf("extension");
            var tblIndex = ds.Tables.IndexOf("records");

            if (tblIndex == -1)
            {
                tblIndex = ds.Tables.IndexOf("page");
            }

            table.Columns.Add("Uri");
            table.Columns.Add("PresenceStatus");
            table.Columns.Add("TelephonyStatus");
            table.Columns.Add("UserStatus");
            table.Columns.Add("DndStatus");
            table.Columns.Add("Message");
            table.Columns.Add("ExtensionId");
            table.Columns.Add("Extension");
            table.Columns.Add("DateAccessed");

            for (var i = 0; i <= ds.Tables[tblIndex].Rows.Count - 1; i++)
            {
                var row = table.NewRow();
                var extensionColumns = ds.Tables[extensionTblIndex].Columns;
                var rows = ds.Tables[tblIndex].Rows[i];

                row[0] = rows["uri"];
                row[1] = rows["presenceStatus"];

                row[2] = (ds.Tables[tblIndex].Columns["telephonyStatus"] != null) ? ds.Tables[tblIndex].Rows[i]["telephonyStatus"] : typeof(DBNull);
                row[3] = (ds.Tables[tblIndex].Columns["userStatus"] != null) ? ds.Tables[tblIndex].Rows[i]["userStatus"] : typeof(DBNull);
                row[4] = (ds.Tables[tblIndex].Columns["dndStatus"] != null) ? ds.Tables[tblIndex].Rows[i]["dndStatus"] : typeof(DBNull);
                row[5] = (ds.Tables[tblIndex].Columns["message"] != null) ? ds.Tables[tblIndex].Rows[i]["message"] : typeof(DBNull);

                var extensionDataSet = ds.Tables[extensionTblIndex].Select("records_Id = " + ds.Tables[tblIndex].Rows[i].ItemArray[ds.Tables[tblIndex].Columns.IndexOf("records_Id")]);

                if (extensionDataSet.Length > 0)
                {
                    row[6] = extensionDataSet[0][extensionColumns.IndexOf("id")];
                    row[7] = extensionDataSet[0][extensionColumns.IndexOf("extensionNumber")];
                }

                row[8] = dateAccessed;

                table.Rows.Add(row);
            }

            using (var connection = Connection())
            {
                connection.Open();

                using (var bulkCopy = new SqlBulkCopy(connection))
                {

                    foreach (DataColumn column in table.Columns)
                    {
                        bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                    }

                    bulkCopy.BulkCopyTimeout = 600;
                    bulkCopy.DestinationTableName = "ringcentral.Presence";
                    bulkCopy.BatchSize = 10000;
                    bulkCopy.WriteToServer(table);
                }

                connection.Close();
            }
        }

        internal static void ExtensionToDatabase(XmlDocument response)
        {
            var ds = new DataSet();
            var xr = new XmlNodeReader(response);
            var dateAccessed = DateTime.Now;
            var table = new DataTable();

            ds.ReadXml(xr);

            table.Columns.Add("Uri");
            table.Columns.Add("Id");
            table.Columns.Add("ExtensionNumber");
            table.Columns.Add("ContactFirstName");
            table.Columns.Add("ContactLastName");
            table.Columns.Add("ContactCompany");
            table.Columns.Add("ContactEmail");
            table.Columns.Add("ContactBusinessPhone");
            table.Columns.Add("Name");
            table.Columns.Add("Type");
            table.Columns.Add("Status");
            table.Columns.Add("Admin");
            table.Columns.Add("IntCalling");
            table.Columns.Add("DateAccessed");

            for (var i = 0; i <= ds.Tables[1].Rows.Count - 1; i++)
            {
                var row = table.NewRow();
                var columns = ds.Tables["records"].Columns;
                var permissionsColumns = ds.Tables["permissions"].Columns;
                var recordIdColIndex = Convert.ToInt32(ds.Tables["records"].Rows[i].ItemArray[columns.IndexOf("records_Id")]);
                var contacts = ds.Tables["contact"].Select("records_Id = " + recordIdColIndex);
                var permissions = ds.Tables["permissions"].Select("records_Id = " + recordIdColIndex);
                var admin = new DataRow[0];
                var intCalling = new DataRow[0];
                var paging = ds.Tables["paging"].Rows[0];

                if (permissions.Length > 0)
                {
                    var permId = Convert.ToInt32(permissions[0][permissionsColumns.IndexOf("permissions_Id")]);
                    admin = ds.Tables["admin"].Select("permissions_Id = " + permId);
                    intCalling = ds.Tables["internationalCalling"].Select("permissions_Id = " + permId);
                }

                row[0] = ds.Tables["records"].Rows[i]["uri"];
                row[1] = ds.Tables["records"].Rows[i]["id"];
                row[2] = ds.Tables["records"].Rows[i]["extensionNumber"];

                if (contacts.Length > 0)
                {
                    row[3] = contacts[0]["firstName"];
                    row[4] = contacts[0]["lastName"];
                    row[5] = contacts[0]["company"];
                    row[6] = contacts[0]["email"];
                    row[7] = contacts[0]["businessPhone"];
                }

                row[8] = ds.Tables["records"].Rows[i]["name"];
                row[9] = ds.Tables["records"].Rows[i]["type"];
                row[10] = ds.Tables["records"].Rows[i]["status"];

                if (permissions.Length > 0)
                {
                    row[11] = admin[0]["enabled"];
                    row[12] = intCalling[0]["enabled"];
                }

                row[13] = dateAccessed;

                table.Rows.Add(row);
            }

            using (var connection = Connection())
            {
                connection.Open();

                using (var bulkCopy = new SqlBulkCopy(connection))
                {
                    foreach (DataColumn column in table.Columns)
                    {
                        bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                    }

                    bulkCopy.BulkCopyTimeout = 600;
                    bulkCopy.DestinationTableName = "ringcentral.Extension";
                    bulkCopy.BatchSize = 10000;
                    bulkCopy.WriteToServer(table);
                }

                connection.Close();
            }
        }
        internal static void ExtensionPhoneListToDatabase(XmlDocument response)
        {
            var ds = new DataSet();
            var xr = new XmlNodeReader(response);
            var table = new DataTable();
            var dateAccessed = DateTime.Now;

            ds.ReadXml(xr);

            var extTblIndex = ds.Tables.IndexOf("extension");
            var featTblIndex = ds.Tables.IndexOf("features");
            var tblIndex = ds.Tables.IndexOf("records");

            if (tblIndex == -1)
            {
                tblIndex = ds.Tables.IndexOf("record");
            }

            table.Columns.Add("Uri");
            table.Columns.Add("Id");
            table.Columns.Add("ExtensionNumber");
            table.Columns.Add("PhoneNumber");
            table.Columns.Add("PaymentType");
            table.Columns.Add("Location");
            table.Columns.Add("Type");
            table.Columns.Add("UsageType");
            table.Columns.Add("Status");
            table.Columns.Add("Features");
            table.Columns.Add("DateAccessed");

            for (var i = 0; i <= ds.Tables[1].Rows.Count - 1; i++)
            {
                var row = table.NewRow();
                var columns = ds.Tables[tblIndex].Columns;
                var rows = ds.Tables[tblIndex].Rows[i];
                var recordIdColIndex = Convert.ToInt32(rows.ItemArray[columns.IndexOf("records_Id")]);

                if (extTblIndex != -1)
                {
                    var extensions = ds.Tables[extTblIndex].Select("records_Id = " + recordIdColIndex);

                    if (extensions.Length > 0)
                    {
                        row[2] = extensions[0]["extensionNumber"];
                    }
                }

                if (featTblIndex != -1)
                {
                    var featureTable = ds.Tables[featTblIndex].Select("records_Id = " + recordIdColIndex);
                    var featureList = from feat in featureTable select feat["features_Text"];

                    if (featureTable.Length > 0)
                    {
                        row[9] = string.Join("|", featureList);
                    }
                }

                row[0] = rows["uri"];
                row[1] = rows["id"];
                row[3] = rows["phoneNumber"];
                row[4] = rows["paymentType"];
                row[5] = rows["location"];
                row[6] = rows["type"];
                row[7] = rows["usageType"];
                row[8] = rows["status"];
                row[10] = dateAccessed;

                table.Rows.Add(row);
            }

            using (var connection = Connection())
            {
                connection.Open();

                using (var bulkCopy = new SqlBulkCopy(connection))
                {

                    foreach (DataColumn column in table.Columns)
                    {
                        bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                    }

                    bulkCopy.BulkCopyTimeout = 600;
                    bulkCopy.DestinationTableName = "ringcentral.PhoneNumber";
                    bulkCopy.BatchSize = 10000;
                    bulkCopy.WriteToServer(table);
                }

                connection.Close();
            }
        }

        internal static void MessageListToDatabase(XmlDocument response)
        {
            if (response.InnerText == "")
                return;

            var connection = Connection();
            var ds = new DataSet();
            var table = new DataTable();
            var xr = new XmlNodeReader(response);
            var dateAccessed = DateTime.Now;

            ds.ReadXml(xr);

            var tblIndex = ds.Tables.IndexOf("records");
            if (tblIndex == -1)
                tblIndex = ds.Tables.IndexOf("record");

            var toTblIndex = ds.Tables.IndexOf("to");
            var fromTblIndex = ds.Tables.IndexOf("from");
            var attachmentsTblIndex = ds.Tables.IndexOf("attachments");
            var conversationTblIndex = ds.Tables.IndexOf("conversation");

            table.Columns.Add("Uri");
            table.Columns.Add("Id");
            table.Columns.Add("Type");
            table.Columns.Add("CreationTime");
            table.Columns.Add("ReadStatus");
            table.Columns.Add("Priority");
            table.Columns.Add("Direction");
            table.Columns.Add("Availability");
            table.Columns.Add("Subject");
            table.Columns.Add("MessageStatus");
            table.Columns.Add("SmsSendingAttemptsCount");
            table.Columns.Add("ConversationId");
            table.Columns.Add("FaxResolution");
            table.Columns.Add("FaxPageCount");
            table.Columns.Add("LastModifiedTime");
            table.Columns.Add("CoverIndex");
            table.Columns.Add("CoverPageText");
            table.Columns.Add("VmTranscriptionStatus");
            table.Columns.Add("ToPhoneNumber");
            table.Columns.Add("ToMessageStatus");
            table.Columns.Add("toExtensionNumber");
            table.Columns.Add("ToName");
            table.Columns.Add("ToLocation");
            table.Columns.Add("FromExtensionNumber");
            table.Columns.Add("FromName");
            table.Columns.Add("FromPhoneNumber");
            table.Columns.Add("FromLocation");
            table.Columns.Add("AttachmentId");
            table.Columns.Add("AttachmentUri");
            table.Columns.Add("AttachmentType");
            table.Columns.Add("AttachmentContentType");
            table.Columns.Add("AttachmentVmDuration");
            table.Columns.Add("ConversationUri");
            table.Columns.Add("DateAccessed");

            for (var i = 0; i < ds.Tables[tblIndex].Rows.Count; i++)
            {
                var row = table.NewRow();
                var columns = ds.Tables[tblIndex].Columns;
                var toColumns = ds.Tables[toTblIndex].Columns;
                var fromColumns = ds.Tables[fromTblIndex].Columns;
                var recordId = Convert.ToInt32(ds.Tables[tblIndex].Rows[i].ItemArray[columns.IndexOf("records_Id")]);
                var to = ds.Tables[toTblIndex].Select("records_Id = " + recordId);
                var from = ds.Tables[fromTblIndex].Select("records_Id = " + recordId);
                var attachment = ds.Tables[attachmentsTblIndex].Select("records_Id = " + recordId);
                var tableDef = ds.Tables[tblIndex];

                if (conversationTblIndex != -1)
                {
                    var conversation = ds.Tables[conversationTblIndex].Select("records_Id = " + recordId);
                    if(conversation.Length > 0)
                    {
                        row[32] = conversation[0]["uri"];
                    }
                }

                row[0] = tableDef.Rows[i]["uri"];
                row[1] = tableDef.Rows[i]["id"];
                row[2] = tableDef.Rows[i]["type"];
                row[3] = tableDef.Rows[i]["creationTime"];
                row[4] = tableDef.Rows[i]["readStatus"];
                row[5] = tableDef.Rows[i]["priority"];
                row[6] = tableDef.Rows[i]["direction"];
                row[7] = tableDef.Rows[i]["availability"];
                row[8] = (tableDef.Columns["subject"] != null) ? tableDef.Rows[i]["subject"] : typeof(DBNull);
                row[9] = tableDef.Rows[i]["messageStatus"];
                row[10] = (tableDef.Columns["smsSendingAttemptCount"] != null) ? tableDef.Rows[i]["smsSendingAttemptCount"] : typeof(DBNull);
                row[11] = (tableDef.Columns["conversationId"] != null) ? tableDef.Rows[i]["conversationId"] : typeof(DBNull);
                row[12] = (tableDef.Columns["faxResolution"] != null) ? tableDef.Rows[i]["faxResolution"] : typeof(DBNull);
                row[13] = (tableDef.Columns["faxPageCount"] != null) ? tableDef.Rows[i]["faxPageCount"] : typeof(DBNull);
                row[14] = tableDef.Rows[i]["lastModifiedTime"];
                row[15] = (tableDef.Columns["coverIndex"] != null) ? tableDef.Rows[i]["coverIndex"] : typeof(DBNull);
                row[16] = (tableDef.Columns["coverPageText"] != null) ? tableDef.Rows[i]["coverPageText"] : typeof(DBNull);
                row[17] = tableDef.Rows[i]["vmTranscriptionStatus"];

                if (to.Length > 0)
                {
                    row[18] = to[0]["phoneNumber"];
                    row[19] = (toColumns["messageStatus"] != null) ? to[0]["messageStatus"] : typeof(DBNull);
                    row[20] = (toColumns["extensionNumber"] != null) ? to[0]["extensionNumber"] : typeof(DBNull);
                    row[21] = to[0][toColumns.IndexOf("name")];
                    row[22] = to[0][toColumns.IndexOf("location")];
                }

                if (from.Length > 0)
                {
                    row[23] = (fromColumns["extensionNumber"] != null) ? from[0]["extensionNumber"] : typeof(DBNull);
                    row[24] = (fromColumns["name"] != null) ? from[0]["name"] : typeof(DBNull);
                    row[25] = from[0][fromColumns.IndexOf("phoneNumber")];
                    row[26] = from[0][fromColumns.IndexOf("location")];
                }

                if (attachment.Length > 0)
                {
                    row[27] = attachment[0]["id"];
                    row[28] = attachment[0]["uri"];
                    row[29] = attachment[0]["type"];
                    row[30] = attachment[0]["contentType"];
                    row[31] = attachment[0]["vmDuration"];
                }

                row[33] = dateAccessed;

                table.Rows.Add(row);
            }

            using (var bulkCopy = new SqlBulkCopy(connection))
            {
                connection.Open();

                foreach (DataColumn column in table.Columns)
                {
                    bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                }

                bulkCopy.BulkCopyTimeout = 600;
                bulkCopy.DestinationTableName = "ringcentral.MessageStore";
                bulkCopy.BatchSize = 10000;
                bulkCopy.WriteToServer(table);

                connection.Close();
            }
        }

        internal static void AddColumnToTable(string tableName, string columnName)
        {
            try
            {
                var sql = $"ALTER TABLE {tableName} ADD {columnName} varchar(255);";
                var connection = Connection();
                var command = new SqlCommand(sql, connection);

                connection.Open();

                command.ExecuteNonQuery();

                connection.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("There was a problem adding column to database.\n\nERROR: " + e);
                throw;
            }
        }

        internal static List<string> TableColumns(string tableName)
        {
            try
            {
                var ds = new DataSet();
                var sql = $"SELECT TOP 1 * FROM {tableName} T";
                var connection = Connection();
                var command = new SqlCommand(sql, connection);
                var columns = new List<string>();

                connection.Open();

                var adpter = new SqlDataAdapter(command);
                adpter.Fill(ds);
                var values = ds.Tables[0];

                foreach (var column in values.Columns)
                {
                    columns.Add(column.ToString());
                }

                connection.Close();
                adpter.Dispose();

                return columns;
            }
            catch (Exception e)
            {
                Console.WriteLine("There was a problem geting list of columns from database.\n\nERROR: " + e);
                throw;
            }
        }
    }
}
