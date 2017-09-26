using System;
using System.Text;
using System.Configuration;

namespace RingCentralDataIntegration
{
    internal class Authentication
    {
        private string authenticationToken;
        public string AuthenticationToken
        {
            get
            {
                var appKey = ConfigurationManager.AppSettings["RingCentralAppKey"];
                var appSecret = ConfigurationManager.AppSettings["RingCentralAppSecret"];
                var authenticationPair = appKey + ":" + appSecret;
                var authenticationAscii = Encoding.ASCII.GetBytes(authenticationPair);
                var token = Convert.ToBase64String(authenticationAscii);

                return token;
            }
        }
        public string AccessToken { get; set; }
        public string TokenType { get; set; }
        public string ExpiresIn { get; set; }
        public string RefreshToken { get; set; }
        public string RefreshTokenExpiresIn { get; set; }
        public string Scope { get; set; }
        public string OwnerId { get; set; }
        public string EndpointId { get; set; }
    }
}
