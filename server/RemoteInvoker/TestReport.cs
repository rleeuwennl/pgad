using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Genius2
{
    public static class TestReport
    {
        public static HttpResponseMessage Handle(string cmd, string arg)
        {
            try
            {
                string machineType = arg.Substring(0, 5);
                string testReport = @"\\NLHLNTF1\Data\QIS\MachineData\ASCS\Prod\" + machineType + @"\Reports\" + arg + ".pdf";
                Console.Beep(1000, 100);
                Console.Write(cmd + ": " + arg);
                string result = "";
                HttpResponseMessage response = new HttpResponseMessage();

                if (!File.Exists(testReport))
                {
                    result = "Can not find " + testReport;
                    response.Content = new StringContent("<div>" + result.Replace(Environment.NewLine, "<br>") + "</div>");
                    response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
                    response.StatusCode = HttpStatusCode.OK;
                    Console.WriteLine("  =>nok");
                    return response;
                }
                else
                {
                    byte[] buffer = File.ReadAllBytes(testReport);
                    response.Content = new StreamContent(new MemoryStream(buffer));
                    response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                    response.StatusCode = HttpStatusCode.OK;
                    response.Content.Headers.ContentLength = buffer.Length;
                    ContentDispositionHeaderValue contentDisposition = null;
                    if (ContentDispositionHeaderValue.TryParse("inline; filename=" + arg + ".pdf", out contentDisposition))
                    {
                        response.Content.Headers.ContentDisposition = contentDisposition;
                    }
                }
                Console.WriteLine("  =>ok");
                return response;
            }
            catch(Exception e)
            {
                string content = "Failed to retrieve testreport"+Environment.NewLine+e.Message+Environment.NewLine+e.StackTrace;
                var response = new HttpResponseMessage();
                response.Content = new StringContent(content);
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
                return response;
            }

        }
    }
}
