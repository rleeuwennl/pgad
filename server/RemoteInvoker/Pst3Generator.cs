using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Genius2.Properties;

namespace Genius2
{
    class Pst3Generator
    {
        public static HttpResponseMessage Handle(string cmd, string arg)
        {
            if(arg.Length!=8)
            {
                throw new Exception("Invalid serialnumber: "+arg);
            }
            string xmlPath = @"\\NLHLNTF1\Data\QIS\MachineData\ASCS\Prod\";
            xmlPath += arg.Substring(0, 5);
            xmlPath += @"\";
            xmlPath += arg + ".xml";

            string pst3file = @"c:\temp\output.pst3";

            string pstArgs = Resources.pst3builder;
            if(arg.Substring(0,3).ToLower()=="scb")
            {
                pstArgs = pstArgs.Replace(@"/JC_dir=\\NLHLNTF1\Data\F_data\ASCS\Software\JobController ", "");
            }
            else if (arg.Contains("SK660"))
            {
                pstArgs += @" /pincode=3030";
            }
            pstArgs = pstArgs.Replace("@xmlpath", xmlPath);
            pstArgs = pstArgs.Replace("@pst3file", pst3file);

            if (File.Exists(pst3file))
            {
                File.Delete(pst3file);
            }

            string pst3generatorSrcPath = @"\\NLHLNTF1\data\F_data\ASCS\PST3Generator";
            string pst3generatorDstPath = @"C:\projects\PST3Generator";
            string[] files = { "PST3Generator.exe", "ASCS.Lib.Common.dll", "PST3Parser_standalone.dll" };
            foreach (var f in files)
            {
                string src = Path.Combine(pst3generatorSrcPath, f);
                string dst = Path.Combine(pst3generatorDstPath, f);
                try
                {
                    // copy when source is newer
                    if (File.GetLastWriteTime(src) > File.GetLastWriteTime(dst))
                    {
                        File.Copy(src, dst, true);
                    }
                }
                catch(Exception e)
                {

                }
            }

            Console.Beep(1000, 100);
            Console.Write(cmd + ": " + arg);
            using (Process process = new Process())
            {
                string path = @"C:\projects\PST3Generator\PST3Generator.exe";
                process.StartInfo.FileName = Path.GetFileName(path);
                process.StartInfo.WorkingDirectory = Path.GetDirectoryName(path);
                process.StartInfo.Arguments = pstArgs;
                process.Start();
                process.WaitForExit();


                if (!File.Exists(pst3file))
                {
                    throw new Exception("Can not find:" + pst3file+" exitcode="+process.ExitCode.ToString());
                }


                HttpResponseMessage response = new HttpResponseMessage();


                byte[] buffer = File.ReadAllBytes(pst3file);
                response.Content = new StreamContent(new MemoryStream(buffer));

                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                //   response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                response.StatusCode = HttpStatusCode.OK;
                response.Content.Headers.ContentLength = buffer.Length;
                ContentDispositionHeaderValue contentDisposition = null;
                if (ContentDispositionHeaderValue.TryParse("inline; filename=" + arg + ".pst3", out contentDisposition))
                {
                    response.Content.Headers.ContentDisposition = contentDisposition;
                }

                Console.WriteLine("  =>ok");
                return response;
            }
        }

    }
}