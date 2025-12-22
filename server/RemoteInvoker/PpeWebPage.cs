using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;

namespace Genius2
{
    public class PpeWebPage
    {
        // Only needed when running from debug in VS. Otherwise, BIN\DEBUG doesn't exist so String.Replace does nothing
        public static string homePath = Environment.CurrentDirectory.ToUpper().Replace(@"BIN\DEBUG","");

        public static string GetFileFromHomePath(string fileName)
        {
            return Path.Combine(homePath, fileName) ;
        }

        private static HttpResponseMessage GetHtml(string fname)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            byte[] buffer = File.ReadAllBytes(GetFileFromHomePath(fname + ".html"));
            response.Content = new StreamContent(new MemoryStream(buffer));
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
            response.StatusCode = HttpStatusCode.OK;
            response.Content.Headers.ContentLength = buffer.Length;
            Console.WriteLine("  =>ok");
            return response;
        }

        private static ImageFormat GetImageFormat(string extension)
        {
            ImageFormat result = null;
            PropertyInfo prop = typeof(ImageFormat).GetProperties().Where(p => p.Name.Equals(extension, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            if (prop != null)
            {
                result = prop.GetValue(prop) as ImageFormat;
            }
            return result;
        }

        private static HttpResponseMessage GetPicture(string fname)
        {
            string filePath = Path.Combine(homePath,fname);
            MemoryStream ms = new MemoryStream();
          
            string extension = Path.GetExtension(fname);
            if (File.Exists(filePath))
            {
                if (!string.IsNullOrWhiteSpace(extension))
                {
                    extension = extension.Substring(extension.IndexOf(".") + 1);
                }
                ImageFormat format = GetImageFormat(extension);
                //If invalid image file is requested the following line wil throw an exception  
                new Bitmap(filePath).Save(ms, format != null ? format as ImageFormat : ImageFormat.Bmp);
            }
        

            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new ByteArrayContent(ms.ToArray());
            response.Content.Headers.ContentType = new MediaTypeHeaderValue(string.Format("image/{0}", Path.GetExtension(fname)));
            return response;
        }

        public static HttpResponseMessage Handle(string cmd, string arg)
        {
            string ext = Path.GetExtension(arg).ToLower();
            string name = Path.GetFileName(arg).ToLower();
            switch(ext)
            {
                case ".png":
                    return GetPicture(name);

                case ".html":
                    return GetHtml(name);
                case "":
                    return GetHtml(name);
                   

            }
           
            return null;

        }
    }
}
