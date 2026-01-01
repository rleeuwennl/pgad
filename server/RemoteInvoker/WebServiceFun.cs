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

using System.Drawing;



using System.Drawing.Imaging;
using System.IO;
using System.Web;

namespace RemoteInvoker
{
    class WebServiceFun
    {
        // see: https://docs.microsoft.com/en-us/aspnet/web-api/overview/advanced/http-message-handlers
        public class ApiKeyHandler : DelegatingHandler
        {
            private int vistitors = 0;
            public ApiKeyHandler()
            {

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

                    if (path == @"\" && Path.GetExtension(line) == ".html")
                    {
                        if (isFragmentRequest)
                        {
                            return GetFile(line, "text/html");
                        }
                        // Serve shell (index.html) so client-side loader can inject fragment
                        return GetFile("/index.html", "text/html");
                    }

                    if (line.StartsWith("/images/") || line.StartsWith("/assets/") || line.StartsWith("/pdf/") || line.StartsWith("/html/"))
                    {
                        string ext = Path.GetExtension(line);

                        switch (ext)
                        {
                            case ".html": return GetFile(line, "text/html");
                            case ".css": return GetFile(line, "text/css");
                            case ".js": return GetFile(line, "text/jscript");
                            case ".txt": return GetFile(line, "text/html");
                            case ".ico": return GetFile(line, "image/x-icon");
                            case ".jpg": return GetFile(line, "image/jpeg");
                            case ".png": return GetFile(line, "image/png");
                            case ".woff2": return GetFile(line, "font/woff2");
                            case ".pdf": return GetFile(line, "application/pdf");
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
