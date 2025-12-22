using System.Net.Http.Headers;
using System.Text;

namespace Genius2
{
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System;
    using System.Net.Http;
    using System.Net;
    using System.Collections.Generic;

    public class CheckRouteManagerLicenses
    {
        static string autologicOn = @"AUTOLOGIC_TYPE</ID><VALUE>1</VALUE";
        static string maptypeInfoWare = @"MAP_TYPE</ID><VALUE>1</VALUE>";
        static string allowMapSelection = @"ALLOW_MAP_SELECTION</ID><VALUE>1</VALUE>";
        static string routeManagerLicenseCheckPathRoot = @"\\NLHLNTF1\Shared\Electronica\R_D\RouteManagerLicensesCheck";
        private static string routeManagerLicenseCheckPath;

        static DateTime utcTimeOld = new DateTime(2023, 10, 1);
        private static bool lastResult = true;
        private static int previousScheduleDay = DateTime.Now.DayOfYear;
        private static bool testMode = false;
        private static object licenseLock = new object();


        private static string GetRecipientsNameFromEmail(string emailAddress)
        {
            if (string.IsNullOrEmpty(emailAddress))
            {
                return string.Empty;
            }

            return emailAddress.Split('@').ToList().FirstOrDefault() ?? string.Empty;
        }

        private static void SendMail(string mailAddress, string mailSubject, string mailBody)
        {
            string exchangeServer = @"CHBDSMTP02";
            string smtpUserName = "aebi-schmidt@outlook.com";
            string smtpPassword = "Vakantie2022!";
            int portNumber = 25;
            bool useCredentials = false;

            MailKit.IProtocolLogger protocolLogger = new MailLogger();

            using (protocolLogger)
            {
                MimeKit.MimeMessage message = new MimeKit.MimeMessage();
                message.From.Add(new MimeKit.MailboxAddress("RouteManager license check", "uitele-nl@aebi-schmidt.com"));
                message.To.Add(new MimeKit.MailboxAddress(GetRecipientsNameFromEmail(mailAddress), mailAddress));

                string extraEmail = "john.schrijver@aebi-schmidt.com";
                message.To.Add(new MimeKit.MailboxAddress(GetRecipientsNameFromEmail(extraEmail), extraEmail));

                message.Subject = mailSubject;
                message.Body = new MimeKit.TextPart("plain")
                {
                    Text = mailBody,
                };

                using (MailKit.Net.Smtp.SmtpClient smtpClient = new MailKit.Net.Smtp.SmtpClient(protocolLogger))
                {
                    smtpClient.Connect(exchangeServer, portNumber, MailKit.Security.SecureSocketOptions.Auto);

                    if (useCredentials)
                    {
                        System.Net.NetworkCredential credentials = new System.Net.NetworkCredential(smtpUserName, smtpPassword);
                        smtpClient.Authenticate(credentials);
                    }

                    smtpClient.Send(message);
                    smtpClient.Disconnect(true);
                }
            }
        }

        private static string ConvertCountryToMapInfoware(string country)
        {
            switch (country)
            {
                case "NL":
                case "BE":
                case "LU":
                    return "1383202-5 License map trip Benelux";

                case "DK":
                case "IS":
                case "NO":
                case "FI":
                case "SE":
                    return "1383204-3 License map trip Scandinavie+Ijsland";

                case "IT":
                    return "1383213-2 License map trip Italie";

                case "GB":

                case "IR":
                    return "1383213-2 License map trip UK+IE";

                case "US":
                    return "1383209-8 License map trip USA";

                case "RU":
                    return "n.a. Russia";

                case "LV":
                case "EE":
                case "LT":
                    return "1383210-5 License map trip Balt. staten (EE+LV+LT)";

                case "ES":
                case "PT":
                    return "1383208-9 License map trip Iberia (ES+PT)";

                case "FR":
                    return "1383206-1 License map trip Frankrijk";

                case "DE":
                case "AT":
                case "CH":
                    return "1383205-2 License map trip DE.At,CH (DACH)";

                case "CA":
                    return "1383206-1 License map trip Canada";



                case "PL":
                    return "1383211-4 License map trip Polen";

                case "SK":
                    return "n.a. Slovakia";

                default:
                    return $"Unknown country [{country}] " ;
            }
        }

        private static void CheckFile(string file)
        {
            if (file.Contains("Archive"))
            {
                return;
            }

            if (!file.Contains("S3B11656"))
            {
               ///  return;
            }

            DateTime utcFileTime = File.GetLastAccessTime (file);
            if (utcFileTime > utcTimeOld)
            {
                string rawContent = File.ReadAllText(file);
                string content = rawContent.ToUpper();
                if (content.Contains("LICENSE"))
                {
                    Console.WriteLine("oh fuck");
                }

                int pos1 = 0; // content.IndexOf("<PAT_DATA>");
                int pos2 = content.IndexOf("</PAT_data>");
                if (pos1 >= 0 && pos2 >= 0)
                {
                    content = content.Substring(pos1, pos2 - pos1);
                }

                StringBuilder sb = new StringBuilder("");
                for (int i = 0; i < content.Length; i++)
                {
                    char c = content[i];
                    if (!char.IsWhiteSpace(c))
                    {
                        sb.Append(content[i]);
                    }
                }

                string contentCompressed = sb.ToString();
                string routeManagerCheckFile = Path.Combine(routeManagerLicenseCheckPath, Path.GetFileNameWithoutExtension(file) + ".txt");
                if (contentCompressed.Contains(autologicOn) && 
                    (contentCompressed.Contains(maptypeInfoWare) || contentCompressed.Contains(allowMapSelection))
                   )
                {
                    int posCountry = contentCompressed.IndexOf("<USER_COUNTRY>");
                    string country = ""; // just assume NL when not defined
                    if (posCountry >= 0)
                    {
                        country = contentCompressed.Substring(posCountry + 14, 2);
                    }

                    string lso = GetToken(rawContent, "User_LSOName");
                    string customer = GetToken(rawContent, "CustomerName");
                    string mapVersion = GetToken(rawContent, "InfowareMapVersion");

                    if (!File.Exists(routeManagerCheckFile))
                    {
                        Console.WriteLine($"Routemanager license turned on for {file}");
                        bool test = false;
                        string machine = Path.GetFileNameWithoutExtension(file);
                        string emailTo = "intelliops@aebi-schmidt.com";
                        if (testMode)
                        {
                            emailTo = "john.schrijver@aebi-schmidt.com";
                        }

                        SendMail(emailTo, $"RM license {machine}", $"New RouteManager license for {machine} with Infoware maps. LSO=[{lso}] Customer=[{customer}] Country=[{country}] ");

                    }

                    Dictionary<string, string> dictionary = GetLicenseInfoFile(routeManagerCheckFile);
                    dictionary["COUNTRY_CODE"] = country;
                    dictionary["LSO"] = lso;
                    dictionary["CUSTOMER"] = customer;
                    dictionary["MAPVERSION"] = mapVersion;
                    WriteLicenseInfoFile(routeManagerCheckFile, dictionary);
                }
                else if (File.Exists(routeManagerCheckFile))
                {
                    File.Delete(routeManagerCheckFile);
                }
            }
        }

        private static string GetToken(string contentCompressed, string v)
        {
            int pos1 = contentCompressed.IndexOf("<"+v+">");
            if(pos1<0)
            {
                return "undefined";
            }

            pos1 += v.Length + 2;

            string sub = contentCompressed.Substring(pos1);
            int pos2 = contentCompressed.IndexOf("</" + v + ">");
            if(pos2<0)
            {
                return "undefined";
            }

            string result = contentCompressed.Substring(pos1, pos2 - pos1);
            return result;
        }

        private static void CheckLicenses(string path)
        {
            if (path.Contains("Archive"))
            {
                return;
            }

            Parallel.ForEach(Directory.GetFiles(path, "*.xml"), file =>
            {
                CheckFile(file);
            });

            Parallel.ForEach(Directory.GetDirectories(path), dir =>
            {
                CheckLicenses(dir);
            });


        }

        static private Dictionary<string, string> GetLicenseInfoFile(string fname)
        {
            if (Path.GetExtension(fname).ToLower() != ".txt")
            {
                throw new Exception("License file should be txt");
            }

            Dictionary<string, string> dictionairy = new Dictionary<string, string>();
            if(!File.Exists(fname))
            {
                return dictionairy;
            }

            IEnumerable<string> lines = new List<string>();

            lock (licenseLock)
            {
                 lines= File.ReadLines(fname);
            }

            foreach (var line in lines)
            {
                var parts = line.Split('=');
                if (parts.Count() == 2)
                {
                    dictionairy[parts[0]]=parts[1];
                }
            }

            return dictionairy;
        }


        static private void WriteLicenseInfoFile(string fname, Dictionary<string, string> dictionairy)
        {
            if (Path.GetExtension(fname).ToLower() != ".txt")
            {
                throw new Exception("License file should be txt");
            }

            string content = "";
            foreach (var entry in dictionairy)
            {
                content += entry.Key + "=" + entry.Value + Environment.NewLine;
            }

            lock (licenseLock)
            {
                File.WriteAllText(fname, content);
            }
        }

        class MapCounter
        {
            public int TotalCount { get; set; }
            public Dictionary<string, int> VersionCountDict { get; set; }
        }

        static public HttpResponseMessage Handle(string cmd, string arg)
        {
            testMode = arg == "test";
            bool force = arg == "force";
            bool overview = arg == "overview";

            routeManagerLicenseCheckPath = routeManagerLicenseCheckPathRoot;

            //if (overview)
            {
                if (testMode)
                {
                    routeManagerLicenseCheckPath = Path.Combine(routeManagerLicenseCheckPathRoot, "test");
                }

                bool execute = DateTime.Now.Hour < 4 && previousScheduleDay != DateTime.Now.DayOfYear || testMode || force;

                if (execute)
                {
                    previousScheduleDay = DateTime.Now.DayOfYear;
                    try
                    {
                        string rootPath;
                        if (testMode)
                        {
                            rootPath = @"\\NLHLNTF1\QIS\MachineData\ASCS\Test";
                        }
                        else
                        {
                            rootPath = @"\\NLHLNTF1\QIS\MachineData\ASCS\Prod";
                        }

                        Console.WriteLine("Doing CheckLicenses on " + rootPath);

                        CheckLicenses(rootPath);
                        lastResult = true;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        Console.WriteLine(e.StackTrace);
                        lastResult = false;
                    }
                }
            }

            // Dictionary of number of licenses per map region (BeNeLux, France, DACH, etc)
            Dictionary<string, MapCounter> mapTotals = new Dictionary<string, MapCounter>();

            HttpResponseMessage response = new HttpResponseMessage();
            string content;

            if (overview)
            {
                content = Properties.Resources.licenseoverviewHeader;
                int totalMachines = 0;

                foreach (var f in Directory.GetFiles(routeManagerLicenseCheckPath, "*.txt"))
                {
                    content += "<tr>" + Environment.NewLine;
                   
                    var dictionairy = GetLicenseInfoFile(f);

                    
                    string article = "unknown";
                    string country_code = "??";
                    if (dictionairy.ContainsKey("COUNTRY_CODE"))
                    {
                        country_code = dictionairy["COUNTRY_CODE"];
                        if (country_code != string.Empty)
                        {
                            article = ConvertCountryToMapInfoware(country_code);
                        }
                        else
                        {
                            article = ConvertCountryToMapInfoware("NL"); // if not specified assume NL
                            country_code = "??";
                        }
                    }

                    if(!mapTotals.ContainsKey(article))
                    {
                        mapTotals.Add(article, new MapCounter());
                        mapTotals[article].TotalCount = 1;
                        mapTotals[article].VersionCountDict = new Dictionary<string, int>();
                    }
                    else
                    {
                        mapTotals[article].TotalCount++;
                    }

                    string lso = "";
                    if(dictionairy.ContainsKey("LSO"))
                    {
                        lso = dictionairy["LSO"];
                    }

                    content += $"<td>{lso}</td>";

                    string customer="";
                    if (dictionairy.ContainsKey("CUSTOMER"))
                    {
                        customer = dictionairy["CUSTOMER"];
                    }

                    // Get map version and do a version count per region
                    string mapVersion = string.Empty;
                    {
                        if (dictionairy.ContainsKey("MAPVERSION"))
                        {
                            mapVersion = dictionairy["MAPVERSION"];
                        }

                        if (mapVersion == null || mapVersion == string.Empty)
                        {
                            mapVersion = "undefined";
                        }

                        // We already know that mapTotals[article] exists at this point (see above).
                        if (!mapTotals[article].VersionCountDict.ContainsKey(mapVersion))
                        {
                            mapTotals[article].VersionCountDict[mapVersion] = 1;
                        }
                        else
                        {
                            mapTotals[article].VersionCountDict[mapVersion]++;
                        }
                    }

                    content += $"<td>{customer}</td>";
                    content += "<td>" + Path.GetFileNameWithoutExtension(f) + "</td>";
                    content += "<td>" + mapVersion + "</td>";
                    content += $"<td style=\"background-color:#90ee90\">[{country_code}]{article}</td>";

                    content += "</tr>" + Environment.NewLine;
                    totalMachines++;
                }
                content += Properties.Resources.licenseoverviewFooter;

                string toplines = "";
                toplines += "<h3>Total maps:" + totalMachines.ToString() + "</h3>";
                toplines += Properties.Resources.totalTableHeader;
                foreach(var article in mapTotals)
                {
                    toplines += "<tr>" + Environment.NewLine;
                    toplines += "<td>" + $"<h3>{article.Key}</td>";
                    toplines += "<td></td>";
                    toplines += "<td>" + $"<h3>{article.Value.TotalCount}</td>";
                    toplines += "</tr>" + Environment.NewLine;

                    foreach (var version in article.Value.VersionCountDict)
                    {
                        toplines += "<tr>" + Environment.NewLine;
                        toplines += "<td></td>";
                        toplines += "<td>" + $"{version.Key}</td>";
                        toplines += "<td>" + $"{version.Value}</td>";
                        toplines += "</tr>" + Environment.NewLine;
                    }
                }
                
                toplines += Properties.Resources.totalTableFooter;
                toplines += "<BR><BR>";
                //content = content.Replace("$TOTAL_TABLE", toplines);

                content = content.Replace("#BEFORE_TABLE", toplines);


                content += "</body></html>";
            }
            else
            {
                content = $"CheckRmLicense[{lastResult}]";
            }

            byte[] buffer = ASCIIEncoding.UTF8.GetBytes(content);
            response.Content = new StreamContent(new MemoryStream(buffer));
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
            response.StatusCode = HttpStatusCode.OK;
            response.Content.Headers.ContentLength = buffer.Length;
            return response;
        }

    }
}