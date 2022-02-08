using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;

namespace GreenshotPlugins.ERPNext
{
    public class ERPNextAPIController : ApiController
    {
        [HttpGet]
        public HttpResponseMessage Index(string code, string state)
        {
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            response.Content = new StringContent("You can close this now.");
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");

            Globals.LastOAuthAuthCode = code;
            Globals.LastOAuthState = state;

            return response;
        }
    }
}
