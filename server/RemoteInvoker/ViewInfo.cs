using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Xml;
using System.Xml.XPath;

namespace Genius2
{
    public static class ViewInfo
    {
        public static bool AutologicInfowareEnabled = false;
        public static bool AutologicKishontiEnabled = false;
        public static bool AutologicAllowMapSelection = false;
        public static string AutologicMaptripMapVersion = string.Empty;


        // gets a node's text, check for null, if null return an empty string
        private static string GetNodeText(XPathNavigator nav ,string xpath)
        {
            XPathNavigator navigator = nav.SelectSingleNode(xpath);

            if (navigator == null)
            {
                return string.Empty;
            }

            return navigator.Value;
        }
    
        private static void GetAutologicStuff(XPathNavigator nav)
        {
            string xPathAutologicTypeOldXmlType = "/Machine_data/PAT_data/PAT/Blocks/Block[fields[Id='4']]/EepromFields/Data[fields[Id='CONTROLPANEL_AUTOLOGIC_TYPE']]/fields/Value";
            string xPathAutologicType = "/Machine_data/PAT_data/PAT/Blocks/Block[fields[Id='4']]/SubBlocks/SubBlock[fields[Id='CONTROLPANEL']]/EepromFields/Data[fields[Id='_AUTOLOGIC_TYPE']]/fields/Value";
            string xPathMaptripMapVersion = "/Machine_data/PST_data/PST/fields/InfowareMapVersion";
            string xPathAutologicMapType = "/Machine_data/PAT_data/PAT/Blocks/Block[fields[Id='4']]/SubBlocks/SubBlock[fields[Id='AUTOLOGIC']]/EepromFields/Data[fields[Id='_MAP_TYPE']]/fields/Value";
            string xPathAutologicAllowMapSelection = "/Machine_data/PAT_data/PAT/Blocks/Block[fields[Id='4']]/SubBlocks/SubBlock[fields[Id='AUTOLOGIC']]/EepromFields/Data[fields[Id='_ALLOW_MAP_SELECTION']]/fields/Value";

            bool autologicEnabled = false;
            AutologicInfowareEnabled = false;
            AutologicKishontiEnabled = false;

            // Determine whether Autologic is enabled in the configuration
            // by getting AUTOLOGIC_TYPE
            // 0 = Autologic not used
            // 1 = Autologic is used
            XPathNavigator navigatorPath = nav.SelectSingleNode(xPathAutologicType);
            if (navigatorPath == null)
            {
                navigatorPath = nav.SelectSingleNode(xPathAutologicTypeOldXmlType);
            }

            if (navigatorPath != null)
            {
                autologicEnabled = navigatorPath.Value.Equals("1", StringComparison.OrdinalIgnoreCase);
            }

            if (autologicEnabled)
            {
                // Determine which map to use
                // If AUTOLOGIC_MAP_TYPE = 0 : Use Kishonti maps
                // If AUTOLOGIC_MAP_TYPE = 1 : Use Infoware maps
                // If AUTOLOGIC_ALLOW_MAP_SELECTION = 1: Use both Kishonti and Infoware maps

                // Get AUTOLOGIC_MAP_TYPE
                // 0 (or not found, for older configurations) = use Kishonti
                // 1 = use Infoware
                navigatorPath = nav.SelectSingleNode(xPathAutologicMapType);
                if (navigatorPath != null)
                {
                    AutologicInfowareEnabled = navigatorPath.Value.Equals("1", StringComparison.OrdinalIgnoreCase);
                    AutologicKishontiEnabled = !AutologicInfowareEnabled;
                }
                else
                {
                    AutologicKishontiEnabled = true;
                }

                // Get AUTOLOGIC_ALLOW_MAP_SELECTION
                // 0 = no manual selection possible: Only 1 map can be used (the one which is selected bij AUTOLOGIC_MAP_TYPE)
                // 1 = manual selection is possible: Both maps should be used.
                navigatorPath = nav.SelectSingleNode(xPathAutologicAllowMapSelection);
                if (navigatorPath != null)
                {
                    bool enableBoth = navigatorPath.Value.Equals("1", StringComparison.OrdinalIgnoreCase);
                    if (enableBoth)
                    {
                        AutologicAllowMapSelection = true;
                        AutologicInfowareEnabled = true;
                        AutologicKishontiEnabled = true;
                    }
                }

                if (AutologicInfowareEnabled)
                {
                    navigatorPath = nav.SelectSingleNode(xPathMaptripMapVersion);
                    if (navigatorPath != null)
                    {
                        AutologicMaptripMapVersion = navigatorPath.Value;
                    }
                }
            }
        }

        public static HttpResponseMessage Handle(string cmd, string arg)
        {
            XPathNavigator nav;
            string machineType = arg.Substring(0, 5);
            string xpathMachineSerialNumberXml3 = "/Machine_data/PAT_data/PAT/Blocks/Block[fields[Id='0']]/EepromFields/Data[fields[Id='MACHINE_SERIAL_NR']]/fields/Value";
            string xpathMachineTypeFromPAT = "/Machine_data/PAT_data/PAT/Blocks/Block[fields[Id='0']]/EepromFields/Data[fields[Id='MACHINE_TYPE_DESCRIPTION']]/fields/Value";
            string xpathMachineTypeXml3 = "/Machine_data/Configuration_2008_data/Configuration/fields/Mach_Description";
            string xpathCustomerNameFromPAT3 = "/Machine_data/PAT_data/PAT/Blocks/Block[fields[Id='0']]/EepromFields/Data[fields[Id='MACHINE_CUSTOMER_NAME']]/fields/Value";
            string xpathCustomerNameXml3 = "/Machine_data/Configuration_2008_data/Configuration/fields/User_Name";
            string xPathXmlModelNameXml3 = "/Machine_data/Configuration_2008_data/Configuration/fields/XML_ModelName";
            string xpathCustomerNameXml2 = "/Machine_data/Configuration_data/Handleiding/fields/EindgebruikerNaam";
            string xpathControlPanelTypeFromPAT = "/Machine_data/PAT_data/PAT/Blocks/Block[fields[Id='0']]/EepromFields/Data[fields[Id='MACHINE_CONTROL_TYPE']]/fields/Value";
            string xpathControlPanelTypeXml3 = "/Machine_data/Configuration_2008_data/Configuration/fields/Contr_System";
            string xpathControlPanelTypeXml2 = "/Machine_data/Configuration_data/Handleiding/fields/BesturingssysteemType";
            string xpathControlPanelVersionRelativeFilePath = "/Machine_data/PST_data/PST/fields/ControlPanelVersion";
            string xpathJobControllerVersionRelativeFilePath = "/Machine_data/PST_data/PST/fields/JobControllerVersion";
            string xpathJobExtensionModuleVersionRelativeFilePath = "/Machine_data/PST_data/PST/fields/ExtensionModuleVersion";
            string xpathChanges = "/Machine_data/XML_Changes/Change";
            string xpathLastModified = "/Machine_data/PAT_data/PAT/Blocks/Block[fields[Id='0']]/EepromFields/Data[fields[Id='CONFIGURATION_TIMESTAMP']]/fields/Value";
            string xPathAdjustedEOL = "/Machine_data/EOL_data/EOL/SubBlocks/SubBlock[fields[Id='TestInfo']]/EepromFields/Data[fields[Id='Test Time']]/fields/Value";
            string xPathAdjustedDateAET = "/Machine_data/AET_data/AET/fields/Date";
            string xPathAdjustedTimeAET = "/Machine_data/AET_data/AET/fields/Time";

            string xmlPath = @"\\NLHLNTF1\Data\QIS\MachineData\ASCS\Prod\";
            xmlPath += arg.Substring(0, 5);
            xmlPath += @"\";
            xmlPath += arg + ".xml";

            string serialNumber;
            string cpVersion;
            string jcVersion;
            string machineTypeXml;
            string customerName;
            string cpType;

            HttpResponseMessage response = new HttpResponseMessage();

            if (!File.Exists(xmlPath))
            {
                string result = "Could not find " + xmlPath;
                response.Content = new StringContent("<div>" + result.Replace(Environment.NewLine, "<br>") + "</div>");
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
                response.StatusCode = HttpStatusCode.OK;
                Console.WriteLine("  =>nok");
                return response;
            }

            using (XmlReader xmlReader = XmlReader.Create(xmlPath))
            {
                XPathDocument doc = new XPathDocument(xmlReader);
                nav = doc.CreateNavigator();

                serialNumber = GetNodeText(nav,xpathMachineSerialNumberXml3);
                cpVersion = GetNodeText(nav, xpathControlPanelVersionRelativeFilePath).Split('\\')[1];
                jcVersion=GetNodeText(nav, xpathJobControllerVersionRelativeFilePath).Split('\\')[1];
                machineTypeXml = GetNodeText(nav, xpathMachineTypeXml3);
                customerName = GetNodeText(nav, xpathCustomerNameXml3);
                cpType = GetNodeText(nav, xpathControlPanelTypeXml3);
                GetAutologicStuff(nav);
            }

            string content =File.ReadAllText( PpeWebPage.GetFileFromHomePath("info.html"));

            content = content.Replace("{machine}", serialNumber);
            content = content.Replace("{cpVersion}", cpVersion);
            content = content.Replace("{jcVersion}", jcVersion);
            content = content.Replace("{machineType}", machineTypeXml);
            content = content.Replace("{customerName}", customerName);
            content = content.Replace("{cpType}", cpType);

            if (AutologicInfowareEnabled || AutologicKishontiEnabled)
            {
                content = content.Replace("{autologicVisible}", "block");

                string allowMapSelection = AutologicAllowMapSelection ? "Yes" : "No";
                content = content.Replace("{autologicAllowMapSelection}", allowMapSelection);

                string mapProvider = AutologicInfowareEnabled ? "Infoware Maptrip" : "Kishonti";
                content = content.Replace("{autologicMapProvider}", mapProvider);

                string mapVersion = (AutologicInfowareEnabled && AutologicMaptripMapVersion != string.Empty)
                    ? AutologicMaptripMapVersion
                    : "-";
                content = content.Replace("{autologicMapVersion}", mapVersion);
            }
            else
            {
                content = content.Replace("{autologicVisible}", "none");
            }

            byte[] buffer = ASCIIEncoding.UTF8.GetBytes(content);
            response.Content = new StreamContent(new MemoryStream(buffer));
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
            response.StatusCode = HttpStatusCode.OK;
            response.Content.Headers.ContentLength = buffer.Length;
            Console.WriteLine("  =>ok");
            return response;


        }
    }
}
