using System;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Collections.Specialized;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Greenshot.Plugin;
using System.IO;
using System.Net.Http.Headers;
using GreenshotPlugin.Core;

namespace GreenshotPlugins.ERPNext
{
    
    class ERPNextUtils
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(ERPNextUtils));

        public static HttpClient GetHttpClient(string accessToken)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("User-Agent", "GreenShot");
            client.Timeout = TimeSpan.FromSeconds(25);

            Log.DebugFormat("Built HttpClient with access token", accessToken);

            if (accessToken != String.Empty)
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
            }

            return client;
        }

        public static string FormatInstanceURL(string instanceURL)
        {
            if (!instanceURL.EndsWith("/"))
            {
                instanceURL = $"{instanceURL}/";
            }

            if (!instanceURL.StartsWith("http"))
            {
                instanceURL = $"https://{instanceURL}";
            }

            return instanceURL;
        }

        public static string GetAuthorizationUrl(string instanceURL, string clientID, string state, string redirectURI)
        {
            instanceURL = FormatInstanceURL(instanceURL);

            var apiEndpoint = $"{instanceURL}api/method/frappe.integrations.oauth2.authorize";

            NameValueCollection queryString = HttpUtility.ParseQueryString(string.Empty);
            queryString.Add("client_id", clientID);
            queryString.Add("state", state);
            queryString.Add("response_type", "code");
            queryString.Add("redirect_uri", redirectURI);

            return $"{apiEndpoint}?{queryString}&scope=openid all";
        }

        public static string GetTokenUrl(string instanceURL)
        {
            instanceURL = FormatInstanceURL(instanceURL);

            return $"{instanceURL}api/method/frappe.integrations.oauth2.get_token";
        }

        private static Random random = new Random();

        /**
         * Credit to Wai Ha Lee and dtb on Stack Overflow
         * https://stackoverflow.com/a/1344242
         * 
         * Insecure; random enough for me to not care, though.
         */
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static async Task<Dictionary<string, string>> GetFrappeOAuthTokenResponse(string instanceURL, string clientID, string redirectURI, string code)
        {
            var client = GetHttpClient(String.Empty);

            var tokenUrl = GetTokenUrl(instanceURL);
            var payload = new Dictionary<string, string>
            {
                { "client_id", clientID },
                { "code", code},
                { "redirect_uri", redirectURI },
                { "grant_type", "authorization_code" }
            };

            var response = await client.PostAsync(tokenUrl, new FormUrlEncodedContent(payload));
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<Dictionary<string, string>>(responseBody);
        }

        public static async Task<Dictionary<string, string>> RefreshOAuthToken(string instanceURL, string clientID, string redirectURI, string refreshToken)
        {
            var client = GetHttpClient(String.Empty);

            var tokenUrl = GetTokenUrl(instanceURL);
            Log.DebugFormat("Refreshing ERPNext OAuth token with token URL {0}", tokenUrl);

            var payload = new Dictionary<string, string>
            {
                { "client_id", clientID },
                { "refresh_token", refreshToken },
                { "redirect_uri", redirectURI },
                { "grant_type", "refresh_token" }
            };

            var response = await client.PostAsync(tokenUrl, new FormUrlEncodedContent(payload));
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<Dictionary<string, string>>(responseBody);
        }

        public static async Task<bool> AddTagToFile(string instanceURL, string accessToken, string doctype, string docname, string tag)
        {
            instanceURL = FormatInstanceURL(instanceURL);
            var client = GetHttpClient(accessToken);

            var payload = new MultipartFormDataContent{
                { new StringContent(tag), "tag"  },
                { new StringContent(doctype), "dt" },
                { new StringContent(docname), "dn" },
            };

            var response = await client.PostAsync($"{instanceURL}api/method/frappe.desk.doctype.tag.tag.add_tag", payload);
            response.EnsureSuccessStatusCode();

            return true;
        }

        public static async Task<Dictionary<string, Dictionary<string, string>>> UploadImage(string instanceURL, string accessToken, string fileName, ISurface image, SurfaceOutputSettings outputSettings)
        {
            Log.DebugFormat("Uploading image to ERPNext {0}", fileName);

            instanceURL = FormatInstanceURL(instanceURL);

            var client = GetHttpClient(accessToken);
            var mimeType = "image/" + outputSettings.Format;

            try
            {
                using (var stream = new MemoryStream())
                {
                    ImageOutput.SaveToStream(image, stream, outputSettings);
                    stream.Flush();
                    stream.Seek(0, SeekOrigin.Begin);

                    var content = new StreamContent(stream);
                    content.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
                    content.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                    {
                        Name = "file",
                        FileName = fileName
                    };

                    var payload = new MultipartFormDataContent{
                        // payload
                        { new StringContent("Home/Attachments"), "folder"  },
                        { new StringContent("1"), "is_private" },
                        { new StringContent(fileName), "file_name" },

                        // file
                        { content, "file", fileName },
                     };

                    var response = await client.PostAsync($"{instanceURL}api/method/upload_file", payload);
                    response.EnsureSuccessStatusCode();

                    var responseBody = await response.Content.ReadAsStringAsync();

                    return JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(responseBody);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Upload to ERPNext gave an exception: ", ex);
                throw ex;
            }

            return null;
        }

        // NOTE: This is for Frappe 12. Will need to be changed in Frappe 13.
        public static string BuildDocURL(string instanceURL, string doctype, string docname)
        {
            string baseURL = FormatInstanceURL(instanceURL);

            return $"{baseURL}desk#Form/{doctype}/{docname}";
        }
    }
}
