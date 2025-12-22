using Genius2;
using System;
using System.Diagnostics;
using System.IO;

namespace RemoteInvoker
{
    public class GenerateHtmlPages
    {   
        public static void GenerateVersionInfoPage()
        {
            try
            {
                foreach (var htmlpage in Directory.GetFiles(@"\\nlhlntf1\shared\electronica\r_d\buildoutput\GeneratedHtmlData", "*.html"))
                {
                    Console.WriteLine($" Retrieving {htmlpage}");
                    string homePath = PpeWebPage.homePath;
                    File.Copy(htmlpage, Path.Combine(homePath, Path.GetFileName(htmlpage)), true);
                }
            }
            catch
            {

            }
        }
    }
}
