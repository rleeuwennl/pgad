using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Genius2
{
    public static class Bom
    {
        public static HttpResponseMessage Handle(string cmd, string arg)
        {
/*            
            var content = SapInterfaces.SapInterface.GetBom(arg, Properties.Resources.BomHeader, Properties.Resources.BomFooter);
            HttpResponseMessage response = new HttpResponseMessage();
            byte[] buffer = ASCIIEncoding.UTF8.GetBytes(content);
            response.Content = new StreamContent(new MemoryStream(buffer));
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
            response.StatusCode = HttpStatusCode.OK;
            response.Content.Headers.ContentLength = buffer.Length;
*/
            HttpResponseMessage response = null;
            return response;
        }
    }
}
