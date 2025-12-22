using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Genius2
{
    public static class Schematics
    {
        public static HttpResponseMessage Handle(string cmd, string arg)
        {
            string outputTxt = @"c:\temp\output.txt";
            string outputPdf = @"c:\temp\output.pdf";
            if (File.Exists(outputTxt))
            {
                File.Delete(outputTxt);
            }
            if (File.Exists(outputPdf))
            {
                File.Delete(outputPdf);
            }

            Console.Beep(1000, 100);
            Console.Write(cmd + ": " + arg);
            string result = "";
            using (Process process = new Process())
            {
                string homePath = Environment.CurrentDirectory.ToUpper();
                string path = Path.Combine(homePath,"DiagramCreator.exe");
                process.StartInfo.FileName = Path.GetFileName(path);
                process.StartInfo.WorkingDirectory = Path.GetDirectoryName(path);
                process.StartInfo.Arguments = arg + " " + cmd;
                process.Start();
                process.WaitForExit();

                if (File.Exists(outputTxt))
                {
                    result = File.ReadAllText(outputTxt);
                }
                else
                {
                    if (!File.Exists(outputPdf))
                    {
                        throw new Exception("Can not find:" + outputPdf);
                    }
                }

                HttpResponseMessage response = new HttpResponseMessage();

                if (result != "")
                {
                    response.Content = new StringContent("<div>" + result.Replace(Environment.NewLine, "<br>") + "</div>");
                    response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
                    response.StatusCode = HttpStatusCode.OK;
                    Console.WriteLine("  =>nok");
                    return response;
                }
                else
                {
                    if (cmd == "CreateSchematics")
                    {
                        try
                        {
                            //SapInterfaces.SapUploader.UploadPdf(arg, outputPdf, "ELECTRIC_SCHEME");
                        }
                        catch
                        {

                        }
                    }


                    byte[] buffer = File.ReadAllBytes(outputPdf);
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
        }
    }
}
