namespace Genius2
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Linq;

    public static class ViewXml
    {     

        public static HttpResponseMessage Handle(string cmd, string arg)
        {                 
            string xmlPath = @"\\NLHLNTF1\Data\QIS\MachineData\ASCS\Prod\";
            xmlPath += arg.Substring(0, 5);
            xmlPath += @"\";
            xmlPath += arg + ".xml";

            string content = File.ReadAllText(xmlPath);




            byte[] buffer = ASCIIEncoding.UTF8.GetBytes(content);
            HttpResponseMessage response = new HttpResponseMessage();
            response.Content = new StreamContent(new MemoryStream(buffer));
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/xml");
            response.StatusCode = HttpStatusCode.OK;
            response.Content.Headers.ContentLength = buffer.Length;
            Console.WriteLine("  =>ok");
            return response;

        }
    }
}
