using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GreenshotPlugins.ERPNext
{
    public static class Globals
    {
        public static string LastOAuthAuthCode { get; set; }
        public static string LastOAuthState { get; set; }
        public static string LastGeneratedOAuthState { get; set; }
        public const string OAuthRedirectUri = "http://localhost:5057";
    }
}
