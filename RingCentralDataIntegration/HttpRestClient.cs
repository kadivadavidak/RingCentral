using System;
using System.Collections.Generic;
using System.Xml;
using Newtonsoft.Json;
using RestSharp;
using Newtonsoft.Json.Linq;
using System.Configuration;
using System.Threading;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace RingCentralDataIntegration
{
    internal class HttpRestClient
    {
        public static Authentication Authentication = new Authentication();
        public static ExtensionList ExtensionList = new ExtensionList();
        public static Stopwatch Stopwatch = new Stopwatch();

        internal static void GetAccessInformation()
        {
            var request = new RestRequest(Method.POST);
            var uri = ConfigurationManager.AppSettings["RingCentralEndpoint"].Replace("v1.0/", "") + "oauth/token";
            var client = new RestClient(uri);
            var token = Authentication.AuthenticationToken;

            request.AddHeader("Authorization", "Basic " + token);
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded;charset=UTF-8");
            request.AddParameter("grant_type", "password");
            request.AddParameter("username", ConfigurationManager.AppSettings["RingCentralUserName"]);
            request.AddParameter("extension", ConfigurationManager.AppSettings["RingCentralExtension"]);
            request.AddParameter("password", ConfigurationManager.AppSettings["RingCentralPassword"]);

            var response = client.Execute(request);

            if (response.StatusCode.ToString() != "OK")
            {
                Thread.Sleep(60000); // One minute
                response = client.Execute(request);
            }

            if (response.Content == "")
            {
                Console.WriteLine(response.ErrorMessage);
                Console.WriteLine("Re-requesting data.");

                response = client.Execute(request);
                token = Authentication.AccessToken;
                request.AddHeader("Authorization", "Bearer " + token);

                response = client.Execute(request);
            }

            dynamic content = JObject.Parse(response.Content);

            Authentication.AccessToken = content.access_token;
            Authentication.TokenType = content.token_type;
            Authentication.ExpiresIn = content.expires_in;
            Authentication.RefreshTokenExpiresIn = content.refresh_token_expires_in;
            Authentication.Scope = content.scope;
            Authentication.OwnerId = content.owner_id;
            Authentication.EndpointId = content.endpoint_id;
        }

        internal static void CheckRateLimit(XmlDocument report)
        {
            var ds = new DataSet();
            var xr = new XmlNodeReader(report);

            ds.ReadXml(xr);

            var rateLimitIndex = ds.Tables[0].Columns.IndexOf("X-Rate-Limit-Limit");
            var rateLimit = Convert.ToInt32(ds.Tables[0].Rows[0].ItemArray[rateLimitIndex]);

            Thread.Sleep(rateLimit == 50 ? 1000 : 2200);

            if (report.InnerXml.Contains("<errorCode>"))
            {
                Console.WriteLine(report.InnerText);
            }

        }

        internal static XmlDocument Extensions(string clientId, bool insertIntoDatabase = true)
        {
            var xmlDoc = new XmlDocument();
            var xmlConcatenation = "<root>";
            var uri = ConfigurationManager.AppSettings["RingCentralEndPoint"] + $"account/{clientId}/extension?page=1&perPage=1000";
            var request = GetReport(uri);
            var numPages = 0;
            var xmlNode = request.GetElementsByTagName("totalPages").Item(0);
            if (xmlNode != null)
            {
                int.TryParse(xmlNode.FirstChild.InnerText, out numPages);
            }

            xmlConcatenation += request.InnerXml;

            if (numPages == 0)
            {
                Console.WriteLine("There was an error getting the number of pages of elements. Please contact the development team for details.");
            }
            else
            {
                for (var i = 2; i <= numPages; i++)
                {
                    uri = ConfigurationManager.AppSettings["RingCentralEndPoint"] + $"account/{clientId}/extension?page={i}&perPage=1000"; // max perPage=1000... Lamo.   

                    request = GetReport(uri);

                    xmlConcatenation += request.InnerXml;
                }
            }

            xmlConcatenation += "</root>";
            xmlDoc.InnerXml = xmlConcatenation;

            if (insertIntoDatabase)
            {
                DataHandler.ExtensionToDatabase(xmlDoc);
            }

            return xmlDoc;
        }

        internal static void SetExtensions(string clientId)
        {
            var xmlDoc = RequestReport(clientId, "extension?"); // Extensions(clientId, false);
            var ds = new DataSet();
            var xr = new XmlNodeReader(xmlDoc);
            var extensionList = new List<string>();

            ds.ReadXml(xr);

            for (var i = 0; i <= ds.Tables[1].Rows.Count - 1; i++)
            {
                if (ds.Tables[1].Rows[i].ItemArray[6].ToString() == "Enabled")
                {
                    extensionList.Add(ds.Tables[1].Rows[i].ItemArray[1].ToString());
                }
            }

            ExtensionList.Extension = extensionList;
        }

        internal static XmlDocument RequestReport(string clientId, string reportUri, bool insertIntoDatabase = true)
        {
            var xmlDoc = new XmlDocument();
            var xmlConcatenation = "<root>";
            var endUri = "page=1&perPage=1000";

            var uri = ConfigurationManager.AppSettings["RingCentralEndPoint"] + $"account/{clientId}/{reportUri}" + endUri;
            var request = GetReport(uri);
            var hasNextPage = (request.GetElementsByTagName("nextPage").Item(0) != null);
            var ds = new DataSet();
            var xr = new XmlNodeReader(request);

            ds.ReadXml(xr);

            if (ds.Tables["records"] != null)
                xmlConcatenation += request.InnerXml;

            if (hasNextPage)
            {
                var i = 1;
                while (hasNextPage)
                {
                    i++;
                    endUri = $"page={i}&perPage=1000";
                    uri = ConfigurationManager.AppSettings["RingCentralEndPoint"] + $"account/{clientId}/{reportUri}" + endUri;

                    request = GetReport(uri);

                    hasNextPage = (request.GetElementsByTagName("nextPage").Item(0) != null);
                    xmlConcatenation += request.InnerXml;
                }
            }

            xmlConcatenation += "</root>";
            xmlDoc.InnerXml = xmlConcatenation;

            return xmlDoc;
        }

        internal static XmlDocument RequestReportByExtension(string clientId, string reportUri, int numberOfExtensions)
        {
            if (ExtensionList.Extension == null || ExtensionList.Extension.Count == 0)
            {
                SetExtensions(clientId);
            }

            var currentExtension = 0;
            var xmlDoc = new XmlDocument();
            var xmlConcatenation = "<root>";
            var extensions = ExtensionList.Extension;
            Debug.Assert(extensions != null, "extensions != null");
            var numIterations = Math.Ceiling((double)extensions.Count / numberOfExtensions);

            for (var i = 0; i < numIterations; i++)
            {
                var extensionArray = new string[numberOfExtensions];

                for (var j = 0; j < numberOfExtensions; j++)
                {
                    extensionArray[j] = extensions.First();
                    extensions.RemoveAt(0);
                }

                var extensionList = string.Join(",", extensionArray);

                var endUri = "page=1&perPage=1000";
                var uri = ConfigurationManager.AppSettings["RingCentralEndPoint"] + $"account/{clientId}/extension/{extensionList}/" + reportUri + endUri;
                var request = GetReport(uri);
                var hasNextPage = (request.GetElementsByTagName("nextPage").Item(0) != null);

                if (reportUri.Contains("presence"))
                    hasNextPage = request.FirstChild.ChildNodes.Count >= 30;

                var ds = new DataSet();
                var xr = new XmlNodeReader(request);

                ds.ReadXml(xr);

                int totalElements = -1;

                // todo: look at this
                if (ds.Tables["paging"]?.Columns["totalElements"] != null)
                    totalElements = Convert.ToInt32(ds.Tables["paging"].Rows[0]["totalElements"]);

                var extTblIndex = ds.Tables.IndexOf("extension"); // TODO: FIX TO BE UNIVERSAL
                if (extTblIndex == -1)
                    extTblIndex = ds.Tables.IndexOf("extensions");

                if (totalElements == -1)
                    totalElements = ds.Tables[extTblIndex].Rows.Count;

                if (totalElements != 0)
                    xmlConcatenation += request.InnerXml;

                if (hasNextPage)
                {
                    var page = 1;
                    
                    while (hasNextPage)
                    {
                        endUri = $"page={page}&perPage=1000";
                        uri = ConfigurationManager.AppSettings["RingCentralEndPoint"] +
                              $"account/{clientId}/extension/{extensionList}/" + reportUri + endUri;

                        request = GetReport(uri);

                        hasNextPage = (request.GetElementsByTagName("nextPage").Item(0) != null);

                        if (reportUri.Contains("presence"))
                            hasNextPage = request.FirstChild.ChildNodes.Count >= 30;

                        xmlConcatenation += request.InnerXml;

                        if (!reportUri.Contains("presence"))
                            page++;
                        
                        if (totalElements < 1000 && page == 1) // todo: && what?? -- for presence
                        {
                            extensionArray = new string[numberOfExtensions];

                            if (extensions.Count == 0)
                            {
                                hasNextPage = false;
                                numIterations = 0;
                                continue;
                            }

                            var k = extensions.Count < numberOfExtensions ? extensions.Count : numberOfExtensions;

                            for (var j = 0; j < k; j++)
                            {
                                extensionArray[j] = extensions.First();
                                extensions.RemoveAt(0);
                            }

                            extensionList = string.Join(",", extensionArray);
                        }
                    }
                }
            }
            
            xmlConcatenation += "</root>";
            xmlDoc.InnerXml = xmlConcatenation;

            return xmlDoc;
        }

        internal static XmlDocument GetReport(string uri)
        {
            var countUnsuccessfull = 1;
            var json = (dynamic)"";
            var xmlDoc = new XmlDocument();

            while (countUnsuccessfull > 0 && countUnsuccessfull <= 3)
            {
                var accessToken = Authentication.AccessToken;

                if (accessToken == null)
                {
                    GetAccessInformation();
                }

                var request = new RestRequest(Method.GET);
                var client = new RestClient(uri);

                request.AddHeader("Authorization", "Bearer " + accessToken);

                var response = client.Execute(request);

                if (response.Content == "")
                {
                    Console.WriteLine(response.ErrorMessage);
                    Console.WriteLine("Re-requesting data.");

                    GetAccessInformation();
                    accessToken = Authentication.AccessToken;
                    request.AddHeader("Authorization", "Bearer " + accessToken);

                    response = client.Execute(request);
                }

                if (response.Headers.First(x => x.Name == "Content-Type").Value.ToString().Contains("multipart/"))
                {
                    var byteArray = Encoding.ASCII.GetBytes(response.Content);
                    var stream = new MemoryStream(byteArray);

                    var content = new StreamContent(stream);

                    content.Headers.ContentType =
                        MediaTypeHeaderValue.Parse(
                            response.Headers.First(x => x.Name == "Content-Type").Value.ToString());

                    var streamProvider = new MultipartMemoryStreamProvider();

                    json = "{\"records\": [";

                    var task = content.ReadAsMultipartAsync(streamProvider).ContinueWith(t =>
                    {
                        foreach (var item in streamProvider.Contents.Skip(1))
                        {
                            json += JObject.Parse(item.ReadAsStringAsync().Result);
                            json += ",";
                        }
                    });

                    Task.WaitAll(task);

                    json = json.Substring(0, json.Length - 1);

                    json += "]}";
                }
                else
                {
                    json = JObject.Parse(response.Content);
                }

                xmlDoc = (XmlDocument) JsonConvert.DeserializeXmlNode(json.ToString(), "page");

                foreach (var header in response.Headers)
                {
                    var node = xmlDoc.CreateNode("element", header.Name, "");
                    var root = xmlDoc.DocumentElement;

                    node.InnerText = header.Value.ToString();
                    root?.PrependChild(node);
                }

                countUnsuccessfull = CheckPull(xmlDoc);
            }

            return xmlDoc;
        }

        internal static int CheckPull(XmlDocument xmlDoc)
        {
            if (xmlDoc.InnerXml.Contains("TokenExpired") || xmlDoc.InnerXml.Contains("TokenInvalid"))
            {
                Console.WriteLine("Access token has expired. Requesting a new token.");
                GetAccessInformation();
                Console.WriteLine("Access token aquired.");
                return 1;
            } else if (xmlDoc.InnerXml.Contains("Request rate exceeded"))
            {
                Console.WriteLine("Request rate has been exceeded. Wating one minute to reset request time frame.");
                Thread.Sleep(60000);
                Console.WriteLine("One minute waited.");
                return 1;
            }
            else if (xmlDoc.InnerXml.Contains("<errorCode>"))
            {
                Console.WriteLine("An error was encountered:");
                Console.WriteLine(xmlDoc.InnerText);
                return 1;
            }
            else
            {
                CheckRateLimit(xmlDoc);
            }

            return 0;
        }

        internal static void Execute(string objectToRun)
        {
            GetAccessInformation();

            var clientId = Authentication.OwnerId;
            var rangeStartDate = DateTime.Today.AddDays(-1);
            var rangeEndDate = DateTime.Today;
            var startDate = rangeStartDate.ToUniversalTime();
            var endDate = rangeEndDate.ToUniversalTime();
            var partialUri = "";

            switch (objectToRun)
            {
                case "Extension":
                    DataHandler.ExtensionToDatabase(RequestReport(clientId, "extension?")); // todo: not setting extension list??
                    break;
                case "PhoneNumber":
                    DataHandler.ExtensionPhoneListToDatabase(RequestReport(clientId, "phone-number?")); // todo: save to database not quite working.
                    break;
                case "Presence":
                    DataHandler.PresenceToDatebase(RequestReportByExtension(clientId, "presence?", 30));
                    break;
                case "CallLog":
                    while (startDate < rangeEndDate)
                    {
                        partialUri =
                            $"call-log?dateFrom={startDate.ToString("yyyy-MM-ddTHH:mm:ssZ")}&dateTo={endDate.ToString("yyyy-MM-ddTHH:mm:ssZ")}&view=Detailed&";
                        DataHandler.CallLogToDatebase(RequestReport(clientId, partialUri));

                        startDate = startDate.AddDays(2);
                        endDate = endDate.AddDays(2);

                        Console.WriteLine($"Beginning pull for {startDate} - {endDate}. Current time: {DateTime.Now}");
                    }
                    break;
                case "MessageStore":
                    while (startDate < rangeEndDate)
                    {
                        partialUri = $"message-store?distinctConversations=true&dateFrom={startDate.ToString("yyyy-MM-ddTHH:mm:ssZ")}&dateTo={endDate.ToString("yyyy-MM-ddTHH:mm:ssZ")}&"; // dateTo={endDate.ToString("yyyy-MM-ddTHH:mm:ssZ")}&
                        DataHandler.MessageListToDatabase(RequestReportByExtension(clientId, partialUri, 1));

                        startDate = startDate.AddDays(2);
                        endDate = endDate.AddDays(2);

                        Console.WriteLine($"Beginning pull for {startDate} - {endDate}. Current time: {DateTime.Now}");
                    }
                    break;
                default:
                    Console.WriteLine($"Object {objectToRun} does not exist. Please enter the name of one of the existing RingCentral Objects.");
                    break;
            }


        }
    }
}