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


namespace RemoteInvoker
{
    class WebServiceFun
    {
        // see: https://docs.microsoft.com/en-us/aspnet/web-api/overview/advanced/http-message-handlers
        public class ApiKeyHandler : DelegatingHandler
        {
            public ApiKeyHandler()
            {

            }


            private async Task<HttpResponseMessage> GetFile(string file,string mime)
            {
                file = @"c:/pgad"+ file;
                HttpResponseMessage response = new HttpResponseMessage();
                byte[] buffer = File.ReadAllBytes(file);
                response.Content = new StreamContent(new MemoryStream(buffer));
                response.Content.Headers.ContentType = new MediaTypeHeaderValue(mime);
                response.StatusCode = HttpStatusCode.OK;
                response.Content.Headers.ContentLength = buffer.Length;
                return response;
            }

            private  Task<HttpResponseMessage> ProcessLine(string line)
            {
                if(line=="/" || line=="/index.html")
                {
                    return GetFile("/index.html","text/html");
                }

                if (line.StartsWith("/images/") || line.StartsWith("/assets/"))
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

                    }
                }

                return null;                
            }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                string query = request.RequestUri.Query;

                var response = ProcessLine(request.RequestUri.LocalPath);
                if(response!=null)
                {
                    return response;
                }

                if (query == string.Empty)
                {
                    //request.RequestUri = new Uri("https://pgad.dsea.nl/index.html");
                    
                }
                return base.SendAsync(request, cancellationToken);

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
