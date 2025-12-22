using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using Genius2;

namespace RemoteInvoker
{
    public class PpeWebServiceController : ApiController
    {
        static RemoteApp remoteApp = null;
        static object theLock = new object();

        PpeWebServiceController()
        {
            if (remoteApp == null)
            {
                remoteApp = new RemoteApp();
            }
        }

        // http://machines/api/PpeWebService?cmd=CreateSchematics&arg=S3B12915
        public HttpResponseMessage GetDocument(string cmd, string arg)
        {           
            string result = "Unknown result";
            try
            {
                switch (cmd)
                {
                    case "CheckRouteManagerLicenses":
                        Console.WriteLine($"{DateTime.Now.ToString()}: Received request {cmd} {arg}");
                        return CheckRouteManagerLicenses.Handle(cmd, arg);

                    case "CreateSchematics":
                        Console.WriteLine($"{DateTime.Now.ToString()}: Received request {cmd} {arg}");
                        return Schematics.Handle(cmd, arg);

                    case "ViewSchematics":
                        Console.WriteLine($"{DateTime.Now.ToString()}: Received request {cmd} {arg}");
                        return Schematics.Handle(cmd, arg);

                    case "ViewOpManual":
                        Console.WriteLine($"{DateTime.Now.ToString()}: Received request {cmd} {arg}");
                        return OpManual.Handle(cmd, arg);

                    case "ViewSpareParts":
                        Console.WriteLine($"{DateTime.Now.ToString()}: Received request {cmd} {arg}");
                        return ViewSpareParts.Handle(cmd, arg);

                    case "GeneratePst3":
                        Console.WriteLine($"{DateTime.Now.ToString()}: Received request {cmd} {arg}");
                        return Pst3Generator.Handle(cmd, arg);

                    case "ViewTestReport":
                        Console.WriteLine($"{DateTime.Now.ToString()}: Received request {cmd} {arg}");
                        return TestReport.Handle(cmd, arg);

                    case "ViewInfo": Console.WriteLine($"{DateTime.Now.ToString()}: Received request {cmd} {arg}");
                        return ViewInfo.Handle(cmd, arg);

                    case "ViewBom":Console.WriteLine($"{DateTime.Now.ToString()}: Received request {cmd} {arg}");
                        return Bom.Handle(cmd, arg);

                    case "ViewXml":
                        Console.WriteLine($"{DateTime.Now.ToString()}: Received request {cmd} {arg}");
                        return ViewXml.Handle(cmd, arg);

                    case "WebPage":
                        Console.WriteLine($"{DateTime.Now.ToString()}: Received request {cmd} {arg}");
                        return PpeWebPage.Handle(cmd, arg);

                    case "Screen":
                        return remoteApp.GetScreen(arg);

                    case "Page":
                        Console.WriteLine($"{DateTime.Now.ToString()}: Received request {cmd} {arg}");
                        return this.GetPage(arg);

                    case "Image":
                        Console.WriteLine($"{DateTime.Now.ToString()}: Received request {cmd} {arg}");
                        return this.GetImage(arg);

                    default:

                        throw new Exception("Unknown PpeWebService cmd:" + cmd);
                }
            }

            catch (Exception e)
            {
                result = e.Message + Environment.NewLine + e.StackTrace;
            }

            HttpResponseMessage response = new HttpResponseMessage();
            response.Content = new StringContent("<div>" + result.Replace(Environment.NewLine, "<br>") + "</div>");
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
            response.StatusCode = HttpStatusCode.OK;
            Console.WriteLine("  =>nok");
            return response;
        }

        public HttpResponseMessage GetDocument(string cmd, string serial, string x, string y, string clickKind)
        {
            string result = "Unknown result";
            try
            {
                switch (cmd)
                {
                    case "Click":
                        {
                            int xVal = (int)(float.Parse(x, CultureInfo.InvariantCulture) + 0.5);
                            int yVal = (int)(float.Parse(y, CultureInfo.InvariantCulture) + 0.5);
                            int clickKindVal = int.Parse(clickKind, CultureInfo.InvariantCulture);
                            return remoteApp.ClickAtPoint(serial, xVal, yVal, clickKindVal);
                        }

                    default:

                        throw new Exception("Unknown PpeWebService cmd:" + cmd);
                }
            }

            catch (Exception e)
            {
                result = e.Message + Environment.NewLine + e.StackTrace;
            }

            HttpResponseMessage response = new HttpResponseMessage();
            response.Content = new StringContent("<div>" + result.Replace(Environment.NewLine, "<br>") + "</div>");
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
            response.StatusCode = HttpStatusCode.OK;
            Console.WriteLine("  =>nok");
            return response;
        }


        public HttpResponseMessage GetDocument(string cmd, string serial, string dir)
        {
            string result = "Unknown result";
            try
            {
                switch (cmd)
                {

                    case "Wheel":
                        {
                            return remoteApp.Wheel(serial, dir);
                        }

                    default:

                        throw new Exception("Unknown PpeWebService cmd:" + cmd);
                }
            }

            catch (Exception e)
            {
                result = e.Message + Environment.NewLine + e.StackTrace;
            }

            HttpResponseMessage response = new HttpResponseMessage();
            response.Content = new StringContent("<div>" + result.Replace(Environment.NewLine, "<br>") + "</div>");
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
            response.StatusCode = HttpStatusCode.OK;
            Console.WriteLine("  =>nok");
            return response;
        }


        [HttpPost]
        public async Task<HttpResponseMessage> PostData(string cmd)
        {
            string result = "Invalid cmd="+cmd;
            try
            {
                switch (cmd)
                {
                    case "uploadar3":
                        {
                            var content = Request.Content;
                            string ar3Data = await content.ReadAsStringAsync();
                            string[] lines = ar3Data.Split('\n');
                            foreach(var line in lines)
                            {
                                if(line.StartsWith("RouteID:"))
                                {
                                    string[] args = line.Split(':');
                                    if(args.Length==2)
                                    {
                                        string routeID = args[1].Trim();
                                        File.WriteAllText(Path.Combine(@"C:\Kishonti\Routes", routeID + ".ar3"),ar3Data);
                                        File.WriteAllText(Path.Combine(@"C:\maptrip\Routes", routeID + ".ar3"), ar3Data);
                                    }
                                }

                            }                            
                        }
                        break;

                    case "startserial":
                        {

                            var content = Request.Content;
                            string serial = await content.ReadAsStringAsync();
                            string prefix = serial.Substring(0, 5);
                            if (serial == "flexigo")
                            {
                                return remoteApp.StartFlexigo();
                            }
                            else
                            {
                                string xmlData = File.ReadAllText($@"\\NLHLNTF1\Data\QIS\MachineData\ASCS\Prod\{prefix}\{serial}.xml");
                                return remoteApp.Start(xmlData);
                            }
                        }

                    case "keydown":
                        {
                            var content = Request.Content;
                            string[] args= (await content.ReadAsStringAsync()).Split(',');
                            if(args.Length==2)
                            {
                                string serial = args[0];
                                string key = args[1];

                                remoteApp.HandleKey(serial, key);
                            }
                        }
                        break;
                }
            }

            catch(Exception e)
            {
                result = e.Message;
            }


          
            var response = new HttpResponseMessage();
            response.Content = new StringContent(result);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
            return response;
        }

        public HttpResponseMessage GetPage(string pagina)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            byte[] buffer = File.ReadAllBytes(@"..\..\" + pagina + ".html");
            response.Content = new StreamContent(new MemoryStream(buffer));
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
            response.StatusCode = HttpStatusCode.OK;
            response.Content.Headers.ContentLength = buffer.Length;
            Console.WriteLine("  =>ok");
            return response;
        }

        private HttpResponseMessage GetImage(string imageFile)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            using (Image image = Image.FromFile(@"..\..\" + imageFile))
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    image.Save(memoryStream, ImageFormat.Jpeg);
                    response.Content = new ByteArrayContent(memoryStream.ToArray());
                }
            }

            response.Content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            response.StatusCode = HttpStatusCode.OK;
            return response;
        }
    }
}
