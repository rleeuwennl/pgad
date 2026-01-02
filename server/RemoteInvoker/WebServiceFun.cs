using System;
using System.ServiceModel;
using System.Web.Http.SelfHost;
using System.Web.Http;
using System.Security.Principal;
using System.Threading;
using System.ServiceModel.Channels;
using System.Web.Http.SelfHost.Channels;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;

using System.Drawing;



using System.Drawing.Imaging;
using System.IO;
using System.Web;
using System.Collections.Generic;
using System.Linq;

namespace RemoteInvoker
{
    class WebServiceFun
    {
        // Simple in-memory token store (valid tokens)
        private static HashSet<string> validTokens = new HashSet<string>();
        private static readonly string ADMIN_USERNAME = "admin";
        private static readonly string ADMIN_PASSWORD = "pgad2026"; // Change this!

        // see: https://docs.microsoft.com/en-us/aspnet/web-api/overview/advanced/http-message-handlers
        public class ApiKeyHandler : DelegatingHandler
        {
            private int vistitors = 0;
            public ApiKeyHandler()
            {

            }

            /// <summary>
            /// Check if request has valid authorization token
            /// </summary>
            private bool IsAuthorized(HttpRequestMessage request)
            {
                IEnumerable<string> authHeaders;
                if (request.Headers.TryGetValues("X-Auth-Token", out authHeaders))
                {
                    var token = authHeaders.FirstOrDefault();
                    return !string.IsNullOrEmpty(token) && validTokens.Contains(token);
                }
                return false;
            }

            private HttpResponseMessage GetHtml(string filename)
            {
                if(!System.IO.File.Exists(filename))
                {
                    return null;
                }

                string result = System.IO.File.ReadAllText(filename);
                result = result.Replace("[visitors]", string.Format("{0:000000}", vistitors));

                HttpResponseMessage response = new HttpResponseMessage();
                response.Content = new StringContent(result);
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
                response.StatusCode = HttpStatusCode.OK;
                return response;

            }


            private async Task<HttpResponseMessage> GetFile(string file, string mime)
            {

                file = @"c:/pgad" + file;

                if (!System.IO.File.Exists(file))
                {
                    return null;
                }

                if (Path.GetExtension(file) == ".html")
                {
                    return GetHtml(file);
                }

                HttpResponseMessage response = new HttpResponseMessage();
                byte[] buffer = File.ReadAllBytes(file);
                response.Content = new StreamContent(new MemoryStream(buffer));
                response.Content.Headers.ContentType = new MediaTypeHeaderValue(mime);
                response.StatusCode = HttpStatusCode.OK;
                response.Content.Headers.ContentLength = buffer.Length;
                return response;
            }

            private Task<HttpResponseMessage> ProcessLine(HttpRequestMessage request)
            {
                try
                {
                    string line = request.RequestUri.LocalPath;
                    bool isFragmentRequest = request.Headers.Contains("X-Fragment-Request");
                    string path = Path.GetDirectoryName(line);

                    // Handle authorization endpoints
                    if (line == "/api/auth/login")
                    {
                        try
                        {
                            var json = request.Content.ReadAsStringAsync().Result;
                            // Simple JSON parsing for username and password
                            var userMatch = System.Text.RegularExpressions.Regex.Match(json, "\"username\"\\s*:\\s*\"([^\"]+)\"");
                            var passMatch = System.Text.RegularExpressions.Regex.Match(json, "\"password\"\\s*:\\s*\"([^\"]+)\"");
                            
                            var username = userMatch.Success ? userMatch.Groups[1].Value : "";
                            var password = passMatch.Success ? passMatch.Groups[1].Value : "";

                            if (username == ADMIN_USERNAME && password == ADMIN_PASSWORD)
                            {
                                var token = Guid.NewGuid().ToString();
                                validTokens.Add(token);
                                var response = new HttpResponseMessage();
                                response.Content = new StringContent("{\"success\":true,\"token\":\"" + token + "\"}");
                                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                                Console.WriteLine("Login successful for: " + username);
                                return Task.FromResult(response);
                            }
                            Console.WriteLine("Login failed - invalid credentials");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Login error: " + ex.Message);
                        }
                        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
                    }

                    if (line == "/api/auth/logout")
                    {
                        IEnumerable<string> authHeaders;
                        if (request.Headers.TryGetValues("X-Auth-Token", out authHeaders))
                        {
                            var token = authHeaders.FirstOrDefault();
                            validTokens.Remove(token);
                        }
                        var response = new HttpResponseMessage();
                        response.Content = new StringContent("{\"success\":true}");
                        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                        return Task.FromResult(response);
                    }

                    // Update YouTube link in liturgie JSON file
                    if (line == "/api/liturgie/update-youtube" && request.Method == HttpMethod.Post)
                    {
                        if (!IsAuthorized(request))
                        {
                            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
                        }

                        try
                        {
                            var json = request.Content.ReadAsStringAsync().Result;
                            var jsonObj = JObject.Parse(json);
                            
                            var filename = jsonObj["filename"]?.ToString();
                            var youtubeUrl = jsonObj["youtubeUrl"]?.ToString();
                            var youtubeInsluit = jsonObj["youtubeInsluit"]?.ToString();
                            
                            if (!string.IsNullOrEmpty(filename) && !string.IsNullOrEmpty(youtubeUrl))
                            {
                                // Extract base filename without .html extension
                                var jsonFilename = filename.Replace(".html", ".json");
                                var jsonPath = @"c:/pgad/json/" + jsonFilename;
                                
                                if (File.Exists(jsonPath))
                                {
                                    var jsonContent = File.ReadAllText(jsonPath);
                                    var jsonData = JObject.Parse(jsonContent);
                                    
                                    jsonData["youtubeUrl"] = youtubeUrl;
                                    if (!string.IsNullOrEmpty(youtubeInsluit))
                                    {
                                        jsonData["youtubeInsluit"] = youtubeInsluit;
                                    }
                                    
                                    File.WriteAllText(jsonPath, jsonData.ToString(Newtonsoft.Json.Formatting.Indented));
                                    Console.WriteLine("Updated YouTube URL in: " + jsonFilename);

                                    var response = new HttpResponseMessage();
                                    response.Content = new StringContent("{\"success\":true}");
                                    response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                                    return Task.FromResult(response);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("YouTube update error: " + ex.Message);
                        }
                        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest));
                    }

                    // Update YouTube insluit code in liturgie JSON file
                    if (line == "/api/liturgie/update-insluit" && request.Method == HttpMethod.Post)
                    {
                        if (!IsAuthorized(request))
                        {
                            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
                        }

                        try
                        {
                            var json = request.Content.ReadAsStringAsync().Result;
                            var jsonObj = JObject.Parse(json);
                            
                            var filename = jsonObj["filename"]?.ToString();
                            var youtubeInsluit = jsonObj["youtubeInsluit"]?.ToString();
                            
                            if (!string.IsNullOrEmpty(filename) && !string.IsNullOrEmpty(youtubeInsluit))
                            {
                                // Extract base filename without .html extension
                                var jsonFilename = filename.Replace(".html", ".json");
                                var jsonPath = @"c:/pgad/json/" + jsonFilename;
                                
                                if (File.Exists(jsonPath))
                                {
                                    var jsonContent = File.ReadAllText(jsonPath);
                                    var jsonData = JObject.Parse(jsonContent);
                                    
                                    jsonData["youtubeInsluit"] = youtubeInsluit;
                                    
                                    File.WriteAllText(jsonPath, jsonData.ToString(Newtonsoft.Json.Formatting.Indented));
                                    Console.WriteLine("Updated YouTube Insluit in: " + jsonFilename);

                                    var response = new HttpResponseMessage();
                                    response.Content = new StringContent("{\"success\":true}");
                                    response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                                    return Task.FromResult(response);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Insluit update error: " + ex.Message);
                        }
                        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest));
                    }

                    // Upload PDF for liturgie
                    if (line == "/api/liturgie/upload-pdf" && request.Method == HttpMethod.Post)
                    {
                        if (!IsAuthorized(request))
                        {
                            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
                        }

                        try
                        {
                            var provider = new System.Net.Http.MultipartMemoryStreamProvider();
                            request.Content.ReadAsMultipartAsync(provider).Wait();

                            string filename = "";
                            string pdfFilename = "";
                            byte[] fileData = null;

                            foreach (var content in provider.Contents)
                            {
                                var name = content.Headers.ContentDisposition.Name.Trim('\"');
                                if (name == "filename")
                                {
                                    filename = content.ReadAsStringAsync().Result;
                                }
                                else if (name == "pdfFile")
                                {
                                    pdfFilename = content.Headers.ContentDisposition.FileName.Trim('\"');
                                    fileData = content.ReadAsByteArrayAsync().Result;
                                }
                            }

                            if (!string.IsNullOrEmpty(filename) && fileData != null)
                            {
                                // Save PDF file
                                Directory.CreateDirectory(@"c:/pgad/pdf");
                                File.WriteAllBytes(@"c:/pgad/pdf/" + pdfFilename, fileData);

                                // Update JSON file to reference the PDF
                                var jsonFilename = filename.Replace(".html", ".json");
                                var jsonPath = @"c:/pgad/json/" + jsonFilename;
                                
                                if (File.Exists(jsonPath))
                                {
                                    var jsonContent = File.ReadAllText(jsonPath);
                                    var jsonData = JObject.Parse(jsonContent);
                                    
                                    jsonData["pdfFile"] = "/pdf/" + pdfFilename;
                                    
                                    File.WriteAllText(jsonPath, jsonData.ToString(Newtonsoft.Json.Formatting.Indented));
                                }

                                Console.WriteLine("Uploaded PDF: " + pdfFilename + " for " + filename);

                                var response = new HttpResponseMessage();
                                response.Content = new StringContent("{\"success\":true,\"pdfFilename\":\"" + pdfFilename + "\"}");
                                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                                return Task.FromResult(response);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("PDF upload error: " + ex.Message);
                        }
                        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest));
                    }

                    // For authorized requests, add to console output
                    if (IsAuthorized(request))
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write(" [AUTHORIZED]");
                        Console.ResetColor();
                    }

                    if (line == "/")
                    {
                        string countersFile = "counters.txt";
                        if (System.IO.File.Exists(countersFile))
                        {
                            string s = System.IO.File.ReadAllText(countersFile);
                            vistitors = int.Parse(s) + 1;
                            Console.WriteLine($"{vistitors} visitors");
                            Console.Beep(2000, 100);
                        }

                        System.IO.File.WriteAllText(countersFile, vistitors.ToString());

                        return GetFile("/index.html", "text/html");
                    }

                    // All .html requests: serve the fragment only when explicitly asked; otherwise serve shell
                    if (Path.GetExtension(line) == ".html")
                    {
                        if (isFragmentRequest)
                        {
                            return GetFile(line, "text/html");
                        }

                        // Serve shell (index.html) so client-side loader can inject fragment
                        return GetFile("/index.html", "text/html");
                    }

                    if (line.StartsWith("/images/") || line.StartsWith("/assets/") || line.StartsWith("/pdf/") || line.StartsWith("/html/") || line.StartsWith("/json/"))
                    {
                        string ext = Path.GetExtension(line);

                        switch (ext)
                        {
                            case ".css": return GetFile(line, "text/css");
                            case ".js": return GetFile(line, "text/jscript");
                            case ".txt": return GetFile(line, "text/html");
                            case ".ico": return GetFile(line, "image/x-icon");
                            case ".jpg": return GetFile(line, "image/jpeg");
                            case ".png": return GetFile(line, "image/png");
                            case ".woff2": return GetFile(line, "font/woff2");
                            case ".pdf": return GetFile(line, "application/pdf");
                            case ".json": return GetFile(line, "application/json");
                        }
                    }

                }
                catch(Exception e)
                {
                    Console.WriteLine($"FAILURE {e.Message} \r\n {e.StackTrace}");
                    Console.Beep(500, 1000);
                }

                return null;
            }

            string GetIp()
            {
                string ip = System.Web.HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
                if (string.IsNullOrEmpty(ip))
                {
                    ip = System.Web.HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];
                }
                return ip;
            }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {

                string query = request.RequestUri.Query;

                Console.Write(System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") +" =>");
                Console.Write("Request:"+ request.RequestUri.LocalPath);

                var response = ProcessLine(request);



                Console.WriteLine(response!=null?" [OK]": " [NOK]");


                if (response != null)
                {
                    return response;
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));

            }
        }

        internal class EExtendHttpSelfHostConfigurationpublic : HttpSelfHostConfiguration
        {
            public EExtendHttpSelfHostConfigurationpublic(Uri baseAddress) : base(baseAddress)
            {
            }
            protected override BindingParameterCollection OnConfigureBinding(HttpBinding httpBinding)
            {
                httpBinding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Certificate;
                httpBinding.Security.Mode = HttpBindingSecurityMode.Transport;
                httpBinding.MaxReceivedMessageSize = long.MaxValue;
                this.MaxBufferSize = int.MaxValue;
                this.MaxReceivedMessageSize = long.MaxValue;

                return base.OnConfigureBinding(httpBinding);
            }
        }

        public static void StartWebService()
        {
            /*
             * using the following example:
             * https://www.dotnetcurry.com/aspnet/896/self-host-aspnet-webapi-without-iis
             * use http://localhost/api/Contacts to retrieve xml with contacts
             */

            Uri baseAddres;
            baseAddres = new Uri("https://localhost:443");

            // Set up server configuration
            HttpSelfHostConfiguration config = new HttpSelfHostConfiguration(baseAddres);
            config.MaxReceivedMessageSize = 2147483647;
            config.Routes.MapHttpRoute(
              name: "DefaultApi",
              routeTemplate: "api/{controller}/{id}",
              defaults: new { id = RouteParameter.Optional }
            );
            config.MessageHandlers.Add(new ApiKeyHandler());


            // Create server
            var server = new HttpSelfHostServer(config);
            // Start listening
            server.OpenAsync().Wait();
            Console.WriteLine("Web API Self hosted on " + baseAddres);
        }
    }
}
