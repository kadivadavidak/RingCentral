using System.Net;
using System.Net.Mail;

namespace RingCentralDataIntegration
{
    class SmtpHandler
    {
        internal static void SendMessage(string subject, string body)
        {
            var email = new MailMessage("donotreply@domain.com", "devemail@domain.com")
            {
                Subject = subject,
                Body = body
            };

            using (var client = new SmtpClient())
            {
                client.Port = 25;
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.UseDefaultCredentials = false;
                client.Host = "SMTOP Server Address";
                client.EnableSsl = true;
                client.Credentials = new NetworkCredential("Username",
                    "Password");

                client.Send(email);
            }
        }
    }
}
