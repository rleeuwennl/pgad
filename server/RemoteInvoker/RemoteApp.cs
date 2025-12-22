namespace RemoteInvoker
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Timers;
    using System.Windows.Forms;

    class AscsApp
    {
        public string status = "no status";
        public Point lastClick = new Point(0, 0);
        public string controlPanel = "??";
        int lastRequestTickCount;

        public AscsApp()
        {
            UpdateLastRequest();
        }

        public void UpdateLastRequest()
        {
            lastRequestTickCount = Environment.TickCount;
        }

        public bool Expired()
        {
            return (Environment.TickCount - lastRequestTickCount) > 60000;
        }

    };

    class RemoteApp
    {
        private System.Timers.Timer timer;
        ConcurrentDictionary<string, Process> processList;
        ConcurrentDictionary<string, AscsApp> ascsList = new ConcurrentDictionary<string, AscsApp>();

        DateTime lastQtWriteTime = new DateTime(1970, 5, 31, 7, 0, 0);
        DateTime lastDnWriteTime = new DateTime(1970, 5, 31, 7, 0, 0);

        public RemoteApp()
        {
            List<Process> processToKill = new List<Process>();
            foreach (var process in Process.GetProcesses())
            {
                if (process.ProcessName.Contains("ASCS"))
                {
                    processToKill.Add(process);
                }
            }

            foreach (var process in processToKill)
            {
                //process.StartInfo.WorkingDirectory
                try
                {
                    process.Kill();
                }
                catch
                {

                }
            }

            timer = new System.Timers.Timer(1);
            timer.Elapsed += OnTimer;
            timer.Start();
        }

        private void OnTimer(object sender, ElapsedEventArgs e)
        {
            string serialNumberToKill = "";
            foreach (var ascs in ascsList)
            {
                string serialNumber = ascs.Key;
                if (ascs.Value.Expired())
                {
                    serialNumberToKill = serialNumber;
                }
            }

            ascsList.TryRemove(serialNumberToKill, out var _);

            if (serialNumberToKill != "")
            {
                Process process = GetProcess(serialNumberToKill);
                if (process != null)
                {
                    try
                    {
                        process.Kill();
                        process.WaitForExit(5000);
                        string dir = GetDirectoryOfSerialNumber(serialNumberToKill);
                        Directory.Delete(dir, true);
                    }
                    catch (Exception exception)
                    {

                    }
                }
            }
        }

        /// <summary>
        /// Uploads the configuration via the XmlUploader.
        /// </summary>
        /// <param name="filePathXmlUploader">The file path of the XML uploader.</param>
        private bool UploadConfiguration(string xml, string serialNumber, string version)
        {

            string saveCurrentDirectory = Environment.CurrentDirectory;
            string serialDirectrory = GetDirectoryOfSerialNumber(serialNumber);
            Directory.CreateDirectory(serialDirectrory);
            string xmlFile = Path.Combine(GetDirectoryOfSerialNumber(serialNumber), serialNumber + ".xml");
            File.WriteAllText(xmlFile, xml);
            //Environment.CurrentDirectory = GetDirectoryOfSerialNumber(serialNumber);
            string filePathXmlUploader = Path.Combine(serialDirectrory, @"SpreadingXmlUploader.exe");

            StringBuilder commandLineOptions = new StringBuilder();

            commandLineOptions.Append(@"/standalone ");
            commandLineOptions.Append(string.Format(CultureInfo.InvariantCulture, "/Config=\"{0}\" ", xmlFile));
            ProcessStartInfo processStartInfo = new ProcessStartInfo()
            {
                Arguments = commandLineOptions.ToString(),
                FileName = filePathXmlUploader,
                WindowStyle = ProcessWindowStyle.Normal,
                WorkingDirectory = serialDirectrory
            };

            Process xmlUploaderProcess = new Process
            {
                StartInfo = processStartInfo,
            };
            xmlUploaderProcess.Start();

            // Environment.CurrentDirectory = saveCurrentDirectory;
            bool stat = false;
            int retry = 100;
            while (!xmlUploaderProcess.HasExited && (retry >= 0))
            {
                Thread.Sleep(500);
                stat = !stat;
                retry--;
                SetStatus(serialNumber, "Downloading " + serialNumber + ".xml" + (stat ? "......" : "..............."));
            }

            if (retry < 0)
            {
                xmlUploaderProcess.Kill();
            }
            return true;
        }

        private IntPtr GetWindowHandle(string title)
        {
            Process process = GetProcess(title);
            if (process == null)
            {
                return IntPtr.Zero;
            }
            return process.MainWindowHandle;
        }

        private void UpdateProcessList()
        {
            ConcurrentDictionary<string, Process> newProcessList = new ConcurrentDictionary<string, Process>();
            if (this.processList != null)
            {
                foreach (var kvp in this.processList)
                {
                    if (!kvp.Value.HasExited)
                    {
                        newProcessList.TryAdd(kvp.Key, kvp.Value);
                    }
                    else
                    {
                        if (ascsList.TryGetValue(kvp.Key, out var ascsApp))
                        {
                            ascsApp.status = "Ascs ended.";
                        }
                    }
                }
            }
            this.processList = newProcessList;
        }

        private Process GetProcess(string title)
        {

            this.UpdateProcessList();


            Process process;
            if (processList.TryGetValue(title, out process))
            {
                return process;
            }
            else
            {
                return null;
            }
        }


        private void CopyFolder(string serial, DirectoryInfo source, DirectoryInfo target, bool overwiteFiles = true)
        {
            SetStatus(serial, "Copying " + source.FullName);
            bool sourceExist = Directory.Exists(source.FullName);
            if (!sourceExist) return;
            if (!target.Exists) target.Create();


            try
            {
                Parallel.ForEach(source.GetDirectories(), (sourceChildDirectory) =>
                    CopyFolder(serial, sourceChildDirectory, new DirectoryInfo(Path.Combine(target.FullName, sourceChildDirectory.Name))));

                Parallel.ForEach(source.GetFiles(), sourceFile =>
                    sourceFile.CopyTo(Path.Combine(target.FullName, sourceFile.Name), overwiteFiles));
            }
            catch
            {
                SetStatus(serial, "Error copying " + source.FullName);
            }
        }

        private string GetDirectoryOfSerialNumber(string serial)
        {
            return Path.Combine(@"c:\temp", serial);
        }

        private void SetStatus(string serial, string status)
        {
            this.GetAscsApp(serial).status = status;
        }

        public bool CreateApplication(string xml)
        {
            string contentOrg = xml.ToUpper();
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < contentOrg.Length; i++)
            {
                char c = contentOrg[i];
                if (!char.IsWhiteSpace(c))
                {
                    sb.Append(contentOrg[i]);
                }
            }
            string content = sb.ToString();

            string search = "<ID>MACHINE_SERIAL_NR</ID>";
            int pos1 = content.IndexOf(search);
            string s = content.Substring(pos1 + search.Length);

            search = "<VALUE>";
            pos1 = s.IndexOf(search);
            int pos2 = s.IndexOf("</VALUE>");
            string serialNumber = s.Substring(pos1 + search.Length, pos2 - (pos1 + search.Length));
            IntPtr mainWindowHandle = GetWindowHandle(serialNumber);
            search = "<CONTROLPANELVERSION>PACKAGE\\";
            pos1 = content.IndexOf(search);
            pos2 = content.IndexOf("\\VERSION.XML</CONTROLPANELVERSION>");
            string version = content.Substring(pos1 + search.Length, pos2 - (pos1 + search.Length));

            string tagControlPanelType = "<CONTR_SYSTEM>";
            int indexOfControlType = content.IndexOf(tagControlPanelType);
            string controlType = "ES";
            if (indexOfControlType >= 0)
            {
                controlType = content.Substring(indexOfControlType + tagControlPanelType.Length, 2);
            }

            bool smartCareBox = content.IndexOf(@"<ID>MACHINE_TYPE</ID><VALUE>100</VALUE>") >= 0;

            if (mainWindowHandle == IntPtr.Zero)
            {
                SetStatus(serialNumber, "Start copy xml");

                string applicationDir = GetDirectoryOfSerialNumber(serialNumber);
                if (!smartCareBox)
                {
                    CopyFolder(serialNumber, new DirectoryInfo(@"\\NLHLNTF1\Data\F_data\ASCS\Software\Desktop\ASCS\" + version), new DirectoryInfo(applicationDir));
                }
                else
                {
                    CopyFolder(serialNumber, new DirectoryInfo(@"\\NLHLNTF1\Data\F_data\ASCS\Software\Desktop\ASCS_SmartCareBox\" + version), new DirectoryInfo(applicationDir));
                }

                if (!UploadConfiguration(xml, serialNumber, version))
                {
                    return false;
                }

                this.GetAscsApp(serialNumber).controlPanel = controlType;
                string saveCurrentDirectory = Environment.CurrentDirectory;
                ProcessStartInfo processStartInfo = new ProcessStartInfo();


                string startApplication;


                if (smartCareBox)
                {
                    startApplication = "ASCS.Spreading.SmartCareMain.exe";
                }
                else
                {
                    startApplication = "ascs.exe";
                }


                Environment.CurrentDirectory = GetDirectoryOfSerialNumber(serialNumber);
                processStartInfo.FileName = startApplication;
                processStartInfo.Arguments = "/nosound /standalone /scale=70 /" + controlType + @" /AutoLogicFolder=c:\Kishonti /ContextMenu /NoEventSuppress /MenuAccessPassCode=3198 /NoUsb /NoSafeBoot /NoPortThrow /NoSerialPort /KeyboardEmulatedVelocityAndGPS /SetGPSToRouteStart /ClearStandAloneIcon";
                processStartInfo.WindowStyle = ProcessWindowStyle.Normal;


                Process applicationProcess = new Process();
                applicationProcess.StartInfo = processStartInfo;
                applicationProcess.Start();
                applicationProcess.WaitForInputIdle();

                bool stat = false;

                while (mainWindowHandle == IntPtr.Zero)
                {
                    Process p = Process.GetProcessById(applicationProcess.Id);
                    if (p != null)
                    {
                        if (p.MainWindowHandle != IntPtr.Zero)
                        {
                            string text = WindowsInterop.GetText(p.MainWindowHandle);
                            if (text == "ASCS_UI_Simulator")
                            {
                                p = Process.GetProcessById(applicationProcess.Id);
                                mainWindowHandle = p.MainWindowHandle;
                            }
                        }
                    }
                    System.Threading.Thread.Sleep(500);
                    SetStatus(serialNumber, "Starting ASCS " + (stat ? "........" : "..............................."));
                    stat = !stat;
                }

                this.processList.TryAdd(serialNumber, applicationProcess);
                this.UpdateProcessList();

                Environment.CurrentDirectory = saveCurrentDirectory;


                ClickAtPoint(serialNumber, 10, 10, 1); // just a dummy click
                ClickAtPoint(serialNumber, 10, 10, 0); // just a dummy click
            }
            return true;
        }

        public bool StartFlexigoApplication()
        {
            string serialNumber = "flexigo";

            this.GetAscsApp(serialNumber).controlPanel = "EP";
            string saveCurrentDirectory = Environment.CurrentDirectory;

            Process ascsProcess = null;
            Process flexigoProcess = null;

            foreach (var p in Process.GetProcesses())
            {
                if (p.ProcessName.ToLower().Contains("ascs.spreading.main_nc"))
                {
                    ascsProcess = p;
                }

                if (p.ProcessName.ToLower().Contains("eflexigo"))
                {
                    flexigoProcess = p;
                }
            }


            DateTime newQtTime = File.GetLastWriteTime(@"\\nlhlntf1\shared\Electronica\r_d\buildoutput\flexigo-spreader\qtdeploy.zip");
            DateTime newDnTime = File.GetLastWriteTime(@"\\nlhlntf1\shared\Electronica\r_d\buildoutput\flexigo-spreader\dotnet.zip");

            if (newDnTime > this.lastDnWriteTime || newQtTime > this.lastQtWriteTime)
            {
                if (ascsProcess != null)
                {
                    ascsProcess.Kill();
                    ascsProcess = null;

                }

                if (flexigoProcess != null)
                {
                    flexigoProcess.Kill();
                    flexigoProcess = null;
                }

                this.lastDnWriteTime = newDnTime;
                this.lastQtWriteTime = newQtTime;

                SetStatus(serialNumber, "Copying deployments.......");
                Process copyProcess = new Process();
                copyProcess.StartInfo.UseShellExecute = true;
                copyProcess.StartInfo.FileName = Path.Combine(Environment.SystemDirectory, "xcopy.exe");
                copyProcess.StartInfo.Arguments = @"\\nlhlntf1\shared\Electronica\r_d\buildoutput\flexigo-spreader C:\flexigo-spreader /E /I /Y";
                copyProcess.Start();
                copyProcess.WaitForExit();

                SetStatus(serialNumber, "Untar QT deployment.......");
                Process unzipQtDeployProcess = new Process();
                unzipQtDeployProcess.StartInfo.UseShellExecute = true;
                unzipQtDeployProcess.StartInfo.FileName = Path.Combine(Environment.SystemDirectory, "tar.exe");
                unzipQtDeployProcess.StartInfo.WorkingDirectory = @"C:\flexigo-spreader";
                unzipQtDeployProcess.StartInfo.Arguments = @"-xf qtdeploy.zip";
                unzipQtDeployProcess.Start();
                unzipQtDeployProcess.WaitForExit();


                SetStatus(serialNumber, "Untar dotnet deployment.......");
                Process unzipDotNetDeployProcess = new Process();
                unzipDotNetDeployProcess.StartInfo.UseShellExecute = true;
                unzipDotNetDeployProcess.StartInfo.FileName = Path.Combine(Environment.SystemDirectory, "tar.exe");
                unzipDotNetDeployProcess.StartInfo.WorkingDirectory = @"C:\flexigo-spreader";
                unzipDotNetDeployProcess.StartInfo.Arguments = @"-xf dotnet.zip";
                unzipDotNetDeployProcess.Start();
                unzipDotNetDeployProcess.WaitForExit();
            }

            if (ascsProcess == null)
            {
                SetStatus(serialNumber, "Start dotnet BL.......");
                ascsProcess = new Process();
                ascsProcess.StartInfo.UseShellExecute = true;
                ascsProcess.StartInfo.WorkingDirectory = @"C:\flexigo-spreader\dotnet";
                ascsProcess.StartInfo.FileName = "ASCS.Spreading.Main_NC.exe";
                ascsProcess.StartInfo.Arguments = @"/standalone /NoPortThrow /KeyboardEmulatedVelocityAndGPS /SetGPSToRouteStart  ";
                ascsProcess.Start();
            }

            System.Threading.Thread.Sleep(2000);

            if (flexigoProcess == null)
            {
                SetStatus(serialNumber, "Start QT eFlexigoL.......");
                flexigoProcess = new Process();
                flexigoProcess.StartInfo.UseShellExecute = true;
                flexigoProcess.StartInfo.WorkingDirectory = @"C:\flexigo-spreader\qtdeploy";
                flexigoProcess.StartInfo.FileName = "eFlexigo.exe";
                flexigoProcess.StartInfo.Arguments = @"/fitscreen /tilespath=C:/flexigo-spreader/qtdeploy/tiles";
                flexigoProcess.Start();
            }

            Process applicationProcess = flexigoProcess;

            IntPtr mainWindowHandle = IntPtr.Zero;

            bool stat = false;

            while (mainWindowHandle == IntPtr.Zero)
            {
                Process p = Process.GetProcessById(applicationProcess.Id);
                if (p != null)
                {
                    if (p.MainWindowHandle != IntPtr.Zero)
                    {
                        string text = WindowsInterop.GetText(p.MainWindowHandle);
                        if (text == "eFlexigo")
                        {
                            p = Process.GetProcessById(applicationProcess.Id);
                            mainWindowHandle = p.MainWindowHandle;
                        }
                    }
                }
                System.Threading.Thread.Sleep(500);
                SetStatus(serialNumber, "Starting ASCS " + (stat ? "........" : "..............................."));
                stat = !stat;
            }

            this.processList.TryAdd(serialNumber, applicationProcess);
            this.UpdateProcessList();

            Environment.CurrentDirectory = saveCurrentDirectory;


            ClickAtPoint(serialNumber, 10, 10, 1); // just a dummy click
            ClickAtPoint(serialNumber, 10, 10, 0); // just a dummy click

            return true;
        }

        private static unsafe Bitmap ReplaceColor(Bitmap source,
                                   Color toReplace,
                                   Color replacement)
        {
            const int pixelSize = 4; // 32 bits per pixel

            Bitmap target = new Bitmap(
              source.Width,
              source.Height,
              PixelFormat.Format32bppArgb);

            BitmapData sourceData = null, targetData = null;

            try
            {
                sourceData = source.LockBits(
                  new Rectangle(0, 0, source.Width, source.Height),
                  ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

                targetData = target.LockBits(
                  new Rectangle(0, 0, target.Width, target.Height),
                  ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

                for (int y = 0; y < source.Height; ++y)
                {
                    byte* sourceRow = (byte*)sourceData.Scan0 + (y * sourceData.Stride);
                    byte* targetRow = (byte*)targetData.Scan0 + (y * targetData.Stride);

                    for (int x = 0; x < source.Width; ++x)
                    {
                        byte b = sourceRow[x * pixelSize + 0];
                        byte g = sourceRow[x * pixelSize + 1];
                        byte r = sourceRow[x * pixelSize + 2];
                        byte a = sourceRow[x * pixelSize + 3];

                        if (toReplace.R == r && toReplace.G == g && toReplace.B == b)
                        {
                            r = replacement.R;
                            g = replacement.G;
                            b = replacement.B;
                        }

                        targetRow[x * pixelSize + 0] = b;
                        targetRow[x * pixelSize + 1] = g;
                        targetRow[x * pixelSize + 2] = r;
                        targetRow[x * pixelSize + 3] = a;
                    }
                }
            }
            finally
            {
                if (sourceData != null)
                    source.UnlockBits(sourceData);

                if (targetData != null)
                    target.UnlockBits(targetData);
            }

            return target;
        }

        private Image CaptureWindow(AscsApp ascsApp, IntPtr handle)
        {
            // get te hDC of the target window
            IntPtr hdcSrc = WindowsInterop.GetWindowDC(handle);

            if (hdcSrc == IntPtr.Zero)
            {
                return null;
            }

            // get the size
            WindowsInterop.RECT windowRect = new WindowsInterop.RECT();

            WindowsInterop.GetWindowRect(handle, ref windowRect);

            int width = windowRect.right - windowRect.left;
            int height = windowRect.bottom - windowRect.top;

            // create a device context we can copy to
            IntPtr hdcDest = WindowsInterop.CreateCompatibleDC(hdcSrc);

            // create a bitmap we can copy it to,
            // using GetDeviceCaps to get the width/height
            IntPtr hBitmap = WindowsInterop.CreateCompatibleBitmap(hdcSrc, width, height);
            Image img = null;

            if (hBitmap != null)
            {
                // select the bitmap object
                IntPtr hOld = WindowsInterop.SelectObject(hdcDest, hBitmap);
                bool esConfiguration = ascsApp.controlPanel == "ES";


                // bitblt over
                WindowsInterop.BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, WindowsInterop.SRCCOPY);

                using (var graphics = Graphics.FromHdc(hdcDest))
                {
                    if (!esConfiguration)
                    {
                        // remove title stuff
                        graphics.FillRectangle(Brushes.White, new Rectangle(0, 0, width, 30));
                        graphics.FillRectangle(Brushes.White, new Rectangle(0, 0, 3, height));
                    }

                    graphics.DrawLine(Pens.Red, new Point(ascsApp.lastClick.X - 5, ascsApp.lastClick.Y), new Point(ascsApp.lastClick.X + 5, ascsApp.lastClick.Y));
                    graphics.DrawLine(Pens.Red, new Point(ascsApp.lastClick.X, ascsApp.lastClick.Y - 5), new Point(ascsApp.lastClick.X, ascsApp.lastClick.Y + 5));

                }

                // restore selection
                WindowsInterop.SelectObject(hdcDest, hOld);
                WindowsInterop.DeleteDC(hdcDest);
                WindowsInterop.ReleaseDC(handle, hdcSrc);

                Bitmap bitmap = Bitmap.FromHbitmap(hBitmap);
                if (esConfiguration)
                {
                    Color oldColor = bitmap.GetPixel(0, 0);
                    bitmap = ReplaceColor(Bitmap.FromHbitmap(hBitmap), oldColor, Color.White);
                }
                img = bitmap;
                WindowsInterop.DeleteObject(hBitmap);
            }
            return img;
        }

        private static Image DrawText(string text, Font fontOptional = null, Color? textColorOptional = null, Color? backColorOptional = null, Size? minSizeOptional = null)
        {
            Font font = Control.DefaultFont;
            if (fontOptional != null)
                font = fontOptional;

            Color textColor = Color.Black;
            if (textColorOptional != null)
                textColor = (Color)textColorOptional;

            Color backColor = Color.White;
            if (backColorOptional != null)
                backColor = (Color)backColorOptional;

            Size minSize = Size.Empty;
            if (minSizeOptional != null)
                minSize = (Size)minSizeOptional;

            //first, create a dummy bitmap just to get a graphics object
            SizeF textSize;
            using (Image img = new Bitmap(1, 1))
            {
                using (Graphics drawing = Graphics.FromImage(img))
                {
                    //measure the string to see how big the image needs to be
                    textSize = drawing.MeasureString(text, font);
                    if (!minSize.IsEmpty)
                    {
                        textSize.Width = textSize.Width > minSize.Width ? textSize.Width : minSize.Width;
                        textSize.Height = textSize.Height > minSize.Height ? textSize.Height : minSize.Height;
                    }
                }
            }

            //create a new image of the right size
            Image retImg = new Bitmap((int)textSize.Width, (int)textSize.Height);
            using (var drawing = Graphics.FromImage(retImg))
            {
                //paint the background
                drawing.Clear(backColor);

                //create a brush for the text
                using (Brush textBrush = new SolidBrush(textColor))
                {
                    drawing.DrawString(text, font, textBrush, 0, 0);
                    drawing.Save();
                }
            }
            return retImg;
        }

        /// <summary>
        /// Creates an Image object of the application.
        /// </summary>
        /// <returns></returns>
        private Image CaptureAppScreen(string serialNumber)
        {
            AscsApp ascsApp = this.GetAscsApp(serialNumber);
            ascsApp.UpdateLastRequest();
            IntPtr mainWindowHandle = GetWindowHandle(serialNumber);

            if (mainWindowHandle == IntPtr.Zero)
            {
                if (ascsList.ContainsKey(serialNumber))
                {
                    return DrawText(ascsList[serialNumber].status, new Font("Arial", 16, FontStyle.Bold));
                }
                else
                {
                    return DrawText("No application started", new Font("Arial", 16, FontStyle.Bold));
                }
            }
            Image result = null;
            try
            {
                result = CaptureWindow(ascsApp, mainWindowHandle);

                if (result == null)
                {
                    // its dying
                    Process process = GetProcess(serialNumber);
                    process.Kill();
                }
            }
            catch
            {
                result = null;
            }

            if (result == null)
            {
                result = DrawText("Machine " + serialNumber + " has exited", new Font("Arial", 16, FontStyle.Bold));
            }
            return result;
        }

        private AscsApp GetAscsApp(string serial)
        {
            if (!ascsList.ContainsKey(serial))
            {
                AscsApp ascsApp = new AscsApp();
                ascsList.TryAdd(serial, ascsApp);
            }
            return ascsList[serial];
        }

        public HttpResponseMessage ClickAtPoint(string serialNumber, int xs, int ys, int mouseKind)
        {
            if (serialNumber != null)
            {
                IntPtr mainWindowHandle = GetWindowHandle(serialNumber);
                AscsApp ascsApp = GetAscsApp(serialNumber);
                lock (ascsApp)
                {
                    ascsApp.lastClick = new Point(xs, ys);
                    WindowsInterop.RECT rect = new WindowsInterop.RECT();
                    WindowsInterop.GetWindowRect(mainWindowHandle, ref rect);

                    Point win_coords = new Point(xs + rect.left, ys + rect.top);
                    Point ctrl_coords = win_coords;

                    Console.WriteLine(xs.ToString() + "," + ys.ToString() + "  " + mouseKind.ToString());

                    IntPtr curParent;
                    IntPtr ctrl_handle = mainWindowHandle;
                    do
                    {
                        curParent = ctrl_handle;
                        WindowsInterop.ScreenToClient(curParent, ref win_coords);
                        ctrl_handle = WindowsInterop.ChildWindowFromPoint(curParent, win_coords);
                    } while (ctrl_handle != IntPtr.Zero);

                    //Now you have the ultimate child in curParent and the coords in win_coords.
                    WindowsInterop.ScreenToClient(curParent, ref ctrl_coords);

                    int lParam = WindowsInterop.MAKELPARAM(ctrl_coords.X, ctrl_coords.Y);

                    if (mouseKind == 1)
                    {
                        WindowsInterop.SendMessage(curParent, WindowsInterop.WM_LBUTTONDOWN, (IntPtr)WindowsInterop.MK_LBUTTON, (IntPtr)lParam);
                    }
                    else
                    {
                        WindowsInterop.SendMessage(curParent, WindowsInterop.WM_LBUTTONUP, (IntPtr)0, (IntPtr)lParam);
                    }
                }
            }

            string content = "some text";
            var response = new HttpResponseMessage();
            response.Content = new StringContent(content);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");

            return response;
        }


        private void SendKey(IntPtr mainWindowHandle, Keys key)
        {
            const uint WM_KEYDOWN = 0x0100;
            const uint WM_KEYUP = 0x0101;

            //Thread.Sleep(50);
            WindowsInterop.PostMessage(mainWindowHandle, WM_KEYDOWN, (IntPtr)(key), (IntPtr)(0));
            WindowsInterop.PostMessage(mainWindowHandle, WM_KEYUP, (IntPtr)(key), (IntPtr)(0));
            // Thread.Sleep(50);
        }

        public HttpResponseMessage HandleKey(string serialNumber, string key)
        {
            string content = "some text";
            IntPtr mainWindowHandle = GetWindowHandle(serialNumber);

            if (mainWindowHandle != IntPtr.Zero)
            {

                WindowsInterop.SetForegroundWindow(mainWindowHandle);

                switch (key)
                {
                    case "ArrowUp":
                        SendKey(mainWindowHandle, Keys.Up);
                        break;
                    case "ArrowLeft":
                        SendKey(mainWindowHandle, Keys.Left);
                        break;
                    case "ArrowRight":
                        SendKey(mainWindowHandle, Keys.Right);
                        break;
                    case "ArrowDown":
                        SendKey(mainWindowHandle, Keys.Down);
                        break;
                }
            }

            var response = new HttpResponseMessage();
            response.Content = new StringContent(content);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
            return response;
        }

        public static void ExecuteCommand(string command, string workingFolder)
        {
            int ExitCode;
            ProcessStartInfo ProcessInfo;
            Process process;

            ProcessInfo = new ProcessStartInfo("cmd.exe", "/c " + command);
            ProcessInfo.CreateNoWindow = true;
            ProcessInfo.UseShellExecute = false;
            ProcessInfo.WorkingDirectory = workingFolder;
            // *** Redirect the output ***
            ProcessInfo.RedirectStandardError = true;
            ProcessInfo.RedirectStandardOutput = true;

            process = Process.Start(ProcessInfo);
            process.WaitForExit();

            // *** Read the streams ***
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            ExitCode = process.ExitCode;


            process.Close();
        }




        public HttpResponseMessage StartFlexigo()
        {
            StartFlexigoApplication();
            string content = "some text";
            var response = new HttpResponseMessage();
            response.Content = new StringContent(content);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
            return response;
        }

        public HttpResponseMessage Start(string xml)
        {
            this.CreateApplication(xml);
            string content = "some text";
            var response = new HttpResponseMessage();
            response.Content = new StringContent(content);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
            return response;
        }

        public HttpResponseMessage Wheel(string serial, string delta)
        {
            IntPtr mainWindowHandle = GetWindowHandle(serial);
            AscsApp ascsApp = this.GetAscsApp(serial);
            WindowsInterop.RECT rect = new WindowsInterop.RECT();
            WindowsInterop.GetWindowRect(mainWindowHandle, ref rect);
            int xs = ascsApp.lastClick.X;
            int ys = ascsApp.lastClick.Y;
            Point win_coords = new Point(xs + rect.left, ys + rect.top);
            Point ctrl_coords = win_coords;


            IntPtr curParent;
            IntPtr ctrl_handle = mainWindowHandle;
            do
            {
                curParent = ctrl_handle;
                WindowsInterop.ScreenToClient(curParent, ref win_coords);
                ctrl_handle = WindowsInterop.ChildWindowFromPoint(curParent, win_coords);
            } while (ctrl_handle != IntPtr.Zero);

            //Now you have the ultimate child in curParent and the coords in win_coords.
            WindowsInterop.ScreenToClient(curParent, ref ctrl_coords);

            if (curParent != IntPtr.Zero)
            {
                int direction = int.Parse(delta);
                int deltaVal = (int)(120 * direction);
                IntPtr wParam = (IntPtr)(((deltaVal << 16) | (ushort)0));
                int lParam = WindowsInterop.MAKELPARAM(ctrl_coords.X, ctrl_coords.Y);
                WindowsInterop.PostMessage(curParent, (int)WindowsInterop.WM_MOUSEWHEEL, wParam, (IntPtr)lParam);
            }

            string content = "some text";
            var response = new HttpResponseMessage();
            response.Content = new StringContent(content);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
            return response;
        }

        public HttpResponseMessage GetScreen(string serialNumber)
        {
            int posCount = serialNumber.IndexOf(':');
            serialNumber = serialNumber.Substring(0, posCount);
            HttpResponseMessage response = new HttpResponseMessage();
            using (Image image = CaptureAppScreen(serialNumber))
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    image.Save(memoryStream, ImageFormat.Jpeg);
                    //image.Save("screen", ImageFormat.Jpeg);
                    response.Content = new ByteArrayContent(memoryStream.ToArray());
                }
            }
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            response.StatusCode = HttpStatusCode.OK;

            return response;
        }
    }
}
