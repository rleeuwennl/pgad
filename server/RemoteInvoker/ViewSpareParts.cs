using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Genius2
{
    public static class ViewSpareParts
    {
        public static HttpResponseMessage Handle(string cmd, string arg)
        {
            Console.Beep(1000, 100);
            Console.Write(cmd + ": " + arg);
            string result = "No manual found for " + arg;

            string machineType = arg.Substring(0, 5);
            //string manualsPath = @"\\chbdntf1\ManualsHolten\Manuals with Serial Number\" + machineType.Substring(0, 3) + @"\" + machineType;
            string manualsPath = @"c:\temp\chbdntf1\ManualsHolten\Manuals with Serial Number\" + machineType.Substring(0, 3) + @"\" + machineType;

            string filter = arg + "-SparePartsList_NL*.pdf";

            var files = new string[0];
            try
            {
                Directory.GetFiles(manualsPath, filter);
            }
            catch (Exception e)
            {
                result = "Failed to retrieve manual for " + arg + Environment.NewLine + e.Message + Environment.NewLine + e.StackTrace;
            }


            HttpResponseMessage response = new HttpResponseMessage();


            if (files.Length < 1)
            {
                response.Content = new StringContent("<div>" + result.Replace(Environment.NewLine, "<br>") + "</div>");
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
                response.StatusCode = HttpStatusCode.OK;
                Console.WriteLine("  =>nok");
                return response;
            }



            byte[] buffer = File.ReadAllBytes(files[0]);
            response.Content = new StreamContent(new MemoryStream(buffer));
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            response.StatusCode = HttpStatusCode.OK;
            response.Content.Headers.ContentLength = buffer.Length;
            ContentDispositionHeaderValue contentDisposition = null;
            if (ContentDispositionHeaderValue.TryParse("inline; filename=" + Path.GetFileName(files[0]) + ".pdf", out contentDisposition))
            {
                response.Content.Headers.ContentDisposition = contentDisposition;
            }

            Console.WriteLine("  =>ok");
            return response;

        }
    }
}
