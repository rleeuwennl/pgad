using System;
using System.Threading;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Web;

// see: https://docs.microsoft.com/en-us/aspnet/web-api/overview/advanced/http-message-handlers
public class RequestHandler : DelegatingHandler
{
    private int vistitors = 0;
    // Simple in-memory token store (valid tokens)
    private static HashSet<string> validTokens = new HashSet<string>();
    private static readonly string ADMIN_USERNAME = "pgad";
    private static readonly string ADMIN_PASSWORD = "JezusIsKoning!"; // Change this!


    public RequestHandler()
    {

    }

    /// <summary>
    /// Check if request has valid authorization token
    /// </summary>
    private bool IsAuthorized(HttpRequestMessage request)
    {
        IEnumerable<string> authHeaders;
        if (request.Headers.TryGetValues("X-Auth-Token", out authHeaders))
        {
            var token = authHeaders.FirstOrDefault();
            return !string.IsNullOrEmpty(token) && validTokens.Contains(token);
        }
        return false;
    }

    private HttpResponseMessage GetHtml(string filename)
    {
        if (!System.IO.File.Exists(filename))
        {
            return null;
        }

        string result = System.IO.File.ReadAllText(filename);
        result = result.Replace("[visitors]", string.Format("{0:000000}", vistitors));

        HttpResponseMessage response = new HttpResponseMessage();
        response.Content = new StringContent(result);
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
        response.StatusCode = HttpStatusCode.OK;
        return response;
    }

    private string CreateDefaultLiturgieJson()
    {
        var jsonObject = new JObject
        {
            { "youtubeInsluit", "" },
            { "pdfFile", "" }
        };
        return jsonObject.ToString(Newtonsoft.Json.Formatting.Indented);
    }

    private async Task<HttpResponseMessage> GetFile(string file, string mime)
    {

        file = @"c:/pgad" + file;

        if (!System.IO.File.Exists(file))
        {
            // Create default JSON for liturgie files if they don't exist
            if (Path.GetExtension(file) == ".json" && file.Contains("liturgie"))
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(file));
                    var defaultJson = CreateDefaultLiturgieJson();
                    File.WriteAllText(file, defaultJson);
                    Console.WriteLine("Created default liturgie JSON: " + file);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error creating default JSON: " + ex.Message);
                    return null;
                }
            }
            else
            {
                //return null;
            }
        }

        if (Path.GetExtension(file) == ".html")
        {
            return GetHtml(file);
        }

        HttpResponseMessage response = new HttpResponseMessage();
        byte[] buffer = File.ReadAllBytes(file);
        response.Content = new StreamContent(new MemoryStream(buffer));
        response.Content.Headers.ContentType = new MediaTypeHeaderValue(mime);
        response.StatusCode = HttpStatusCode.OK;
        response.Content.Headers.ContentLength = buffer.Length;
        return response;
    }

    private Task<HttpResponseMessage> RemovePdf(HttpRequestMessage request)
    {
        if (!IsAuthorized(request))
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        }

        try
        {
            var json = request.Content.ReadAsStringAsync().Result;
            var jsonObj = JObject.Parse(json);
            var filename = jsonObj["filename"]?.ToString();

            if (!string.IsNullOrEmpty(filename))
            {
                var jsonFilename = filename.Replace(".html", ".json");
                var jsonPath = @"c:/pgad/liturgie/json/" + jsonFilename;

                if (File.Exists(jsonPath))
                {
                    var jsonContent = File.ReadAllText(jsonPath);
                    var jsonData = JObject.Parse(jsonContent);
                    var currentPdfPath = jsonData["pdfFile"]?.ToString();

                    // Remove the PDF file if it exists
                    if (!string.IsNullOrEmpty(currentPdfPath))
                    {
                        var pdfFileToDelete = @"c:/pgad" + currentPdfPath;
                        if (File.Exists(pdfFileToDelete))
                        {
                            File.Delete(pdfFileToDelete);
                            Console.WriteLine("Deleted PDF file: " + pdfFileToDelete);
                        }
                    }

                    // Clear the pdfFile property in JSON
                    jsonData["pdfFile"] = "";
                    File.WriteAllText(jsonPath, jsonData.ToString(Newtonsoft.Json.Formatting.Indented));
                    Console.WriteLine("Removed PDF reference for " + filename);

                    var response = new HttpResponseMessage();
                    response.Content = new StringContent("{\"success\":true}");
                    response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    return Task.FromResult(response);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("PDF removal error: " + ex.Message);
        }
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest));
    }

    private Task<HttpResponseMessage> UploadPdf(HttpRequestMessage request)
    {
        if (!IsAuthorized(request))
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        }

        try
        {
            var provider = new System.Net.Http.MultipartMemoryStreamProvider();
            request.Content.ReadAsMultipartAsync(provider).Wait();

            string filename = "";
            string pdfFilename = "";
            byte[] fileData = null;

            foreach (var content in provider.Contents)
            {
                var name = content.Headers.ContentDisposition.Name.Trim('\"');
                if (name == "filename")
                {
                    filename = content.ReadAsStringAsync().Result;
                }
                else if (name == "pdfFile")
                {
                    pdfFilename = content.Headers.ContentDisposition.FileName.Trim('\"');
                    fileData = content.ReadAsByteArrayAsync().Result;
                }
            }

            if (!string.IsNullOrEmpty(filename) && fileData != null)
            {
                // Save PDF file
                Directory.CreateDirectory(@"c:/pgad/liturgie/pdf");
                File.WriteAllBytes(@"c:/pgad/liturgie/pdf/" + pdfFilename, fileData);

                // Update JSON file to reference the PDF
                var jsonFilename = filename.Replace(".html", ".json");
                var jsonPath = @"c:/pgad/liturgie/json/" + jsonFilename;

                if (File.Exists(jsonPath))
                {
                    var jsonContent = File.ReadAllText(jsonPath);
                    var jsonData = JObject.Parse(jsonContent);

                    jsonData["pdfFile"] = "/liturgie/pdf/" + pdfFilename;

                    File.WriteAllText(jsonPath, jsonData.ToString(Newtonsoft.Json.Formatting.Indented));
                }

                Console.WriteLine("Uploaded PDF: " + pdfFilename + " for " + filename);

                var response = new HttpResponseMessage();
                response.Content = new StringContent("{\"success\":true,\"pdfFilename\":\"" + pdfFilename + "\"}");
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                return Task.FromResult(response);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("PDF upload error: " + ex.Message);
        }
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest));
    }

    private Task<HttpResponseMessage> UpdateYoutubeInsluit(HttpRequestMessage request)
    {
        if (!IsAuthorized(request))
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        }

        try
        {
            var json = request.Content.ReadAsStringAsync().Result;
            var jsonObj = JObject.Parse(json);

            var filename = jsonObj["filename"]?.ToString();
            var youtubeInsluit = jsonObj["youtubeInsluit"]?.ToString();

            if (!string.IsNullOrEmpty(filename))
            {
                // Extract base filename without .html extension
                var jsonFilename = filename.Replace(".html", ".json");
                var jsonPath = @"c:/pgad/liturgie/json/" + jsonFilename;

                if (File.Exists(jsonPath))
                {
                    var jsonContent = File.ReadAllText(jsonPath);
                    var jsonData = JObject.Parse(jsonContent);

                    jsonData["youtubeInsluit"] = youtubeInsluit;

                    File.WriteAllText(jsonPath, jsonData.ToString(Newtonsoft.Json.Formatting.Indented));
                    Console.WriteLine("Updated YouTube Insluit in: " + jsonFilename);

                    var response = new HttpResponseMessage();
                    response.Content = new StringContent("{\"success\":true}");
                    response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    return Task.FromResult(response);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Insluit update error: " + ex.Message);
        }
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest));
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string query = request.RequestUri.Query;

        Console.Write(System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " =>");
        Console.Write("Request:" + request.RequestUri.LocalPath);
        var response = ProcessRequest(request);

        Console.WriteLine(response != null ? " [OK]" : " [NOK]");

        if (response != null)
        {
            return response;
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }

    private Task<HttpResponseMessage> HandleSitemap(HttpRequestMessage request)
    {
        try
        {
            var sitemapXml = GenerateSitemapXml();
            var response = new HttpResponseMessage();
            response.Content = new StringContent(sitemapXml, System.Text.Encoding.UTF8, "application/xml");
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/xml");
            return Task.FromResult(response);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Sitemap generation error: " + ex.Message);
            // Return a basic valid sitemap on error
            var fallbackSitemap = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<urlset xmlns=""http://www.sitemaps.org/schemas/sitemap/0.9"">
  <url>
    <loc>https://pgad.dsea.nl/</loc>
    <lastmod>" + DateTime.Now.ToString("yyyy-MM-dd") + @"</lastmod>
    <changefreq>weekly</changefreq>
    <priority>1.0</priority>
  </url>
</urlset>";
            var response = new HttpResponseMessage();
            response.Content = new StringContent(fallbackSitemap, System.Text.Encoding.UTF8, "application/xml");
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/xml");
            return Task.FromResult(response);
        }
    }

    private string GenerateSitemapXml()
    {
        var sitemapBuilder = new System.Text.StringBuilder();
        sitemapBuilder.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sitemapBuilder.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

        // Add homepage
        sitemapBuilder.AppendLine("  <url>");
        sitemapBuilder.AppendLine("    <loc>https://pgad.dsea.nl/</loc>");
        sitemapBuilder.AppendLine("    <lastmod>" + DateTime.Now.ToString("yyyy-MM-dd") + "</lastmod>");
        sitemapBuilder.AppendLine("    <changefreq>weekly</changefreq>");
        sitemapBuilder.AppendLine("    <priority>1.0</priority>");
        sitemapBuilder.AppendLine("  </url>");

        // Add all HTML pages from html/ directory
        var htmlDir = @"c:/pgad/html";
        if (Directory.Exists(htmlDir))
        {
            var htmlFiles = Directory.GetFiles(htmlDir, "*.html");
            foreach (var htmlFile in htmlFiles.OrderBy(f => f))
            {
                var fileName = Path.GetFileName(htmlFile);

                // Skip Google verification files and other non-content files
                if (fileName.StartsWith("google") || fileName.Contains("verification"))
                    continue;

                // Validate filename (should not contain special characters that break XML)
                if (string.IsNullOrWhiteSpace(fileName) || fileName.Contains("<") || fileName.Contains(">"))
                    continue;

                var lastModified = File.GetLastWriteTime(htmlFile).ToString("yyyy-MM-dd");
                var priority = GetPagePriority(fileName);
                var changeFreq = GetChangeFrequency(fileName);

                sitemapBuilder.AppendLine("  <url>");
                sitemapBuilder.AppendLine("    <loc>https://pgad.dsea.nl/html/" + fileName + "</loc>");
                sitemapBuilder.AppendLine("    <lastmod>" + lastModified + "</lastmod>");
                sitemapBuilder.AppendLine("    <changefreq>" + changeFreq + "</changefreq>");
                sitemapBuilder.AppendLine("    <priority>" + priority.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "</priority>");
                sitemapBuilder.AppendLine("  </url>");
            }
        }

        sitemapBuilder.AppendLine("</urlset>");
        return sitemapBuilder.ToString();
    }

    private double GetPagePriority(string fileName)
    {
        // Define priorities based on page importance
        var highPriorityPages = new[] { "contact.html", "missie-visie.html" };
        var mediumPriorityPages = new[] { "kerkenraad.html", "moderamen.html", "pastoraalteam.html", "diaconaat.html", "cantorij.html", "over-vieren.html", "zending.html", "voorgangers.html" };
        var lowPriorityPages = new[] { "cvk-bergroting-en-jaarrekeningen.html", "decleratie-formulier.html", "kerkbalans.html", "tarieven-en-betalingen.html", "verhuur-aanhanger.html", "hans-de-booij.html" };

        if (highPriorityPages.Contains(fileName)) return 0.9;
        if (mediumPriorityPages.Contains(fileName)) return 0.8;
        if (lowPriorityPages.Contains(fileName)) return 0.5;

        return 0.7; // Default priority
    }

    private string GetChangeFrequency(string fileName)
    {
        // Define change frequencies based on content type
        var yearlyPages = new[] { "cvk-bergroting-en-jaarrekeningen.html", "decleratie-formulier.html", "kerkbalans.html", "tarieven-en-betalingen.html", "verhuur-aanhanger.html", "gebouw-galluskerk.html", "gebouw-martinikerk.html", "perspective.html", "beleidsplan-cvk.html", "college-van-kerkrentmeesters.html" };

        if (yearlyPages.Contains(fileName)) return "yearly";
        return "monthly"; // Default to monthly for most pages
    }

    private Task<HttpResponseMessage> HandleRootIndex(HttpRequestMessage request)
    {
        string countersFile = "counters.txt";
        if (System.IO.File.Exists(countersFile))
        {
            string s = System.IO.File.ReadAllText(countersFile);
            vistitors = int.Parse(s) + 1;
            Console.WriteLine($"{vistitors} visitors");
            Console.Beep(2000, 100);
        }

        System.IO.File.WriteAllText(countersFile, vistitors.ToString());

        return GetFile("/index.html", "text/html");
    }

    private Task<HttpResponseMessage> HandleLogout(HttpRequestMessage request)
    {
        IEnumerable<string> authHeaders;
        if (request.Headers.TryGetValues("X-Auth-Token", out authHeaders))
        {
            var token = authHeaders.FirstOrDefault();
            validTokens.Remove(token);
        }
        var response = new HttpResponseMessage();
        response.Content = new StringContent("{\"success\":true}");
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        return Task.FromResult(response);
    }

    private Task<HttpResponseMessage> HandleAuthorization(HttpRequestMessage request)
    {
        try
        {
            var json = request.Content.ReadAsStringAsync().Result;
            // Simple JSON parsing for username and password
            var userMatch = System.Text.RegularExpressions.Regex.Match(json, "\"username\"\\s*:\\s*\"([^\"]+)\"");
            var passMatch = System.Text.RegularExpressions.Regex.Match(json, "\"password\"\\s*:\\s*\"([^\"]+)\"");

            var username = userMatch.Success ? userMatch.Groups[1].Value : "";
            var password = passMatch.Success ? passMatch.Groups[1].Value : "";

            if (username == ADMIN_USERNAME && password == ADMIN_PASSWORD)
            {
                var token = Guid.NewGuid().ToString();
                validTokens.Add(token);
                var response = new HttpResponseMessage();
                response.Content = new StringContent("{\"success\":true,\"token\":\"" + token + "\"}");
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                Console.WriteLine("Login successful for: " + username);
                return Task.FromResult(response);
            }
            Console.WriteLine("Login failed - invalid credentials");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Login error: " + ex.Message);
        }
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
    }

    private Task<HttpResponseMessage> ProcessRequest(HttpRequestMessage request)
    {
        try
        {
            string line = request.RequestUri.LocalPath;

            string path = Path.GetDirectoryName(line);

            // Handle authorization endpoints
            if (line == "/api/auth/login")
            {
                return HandleAuthorization(request);
            }

            if (line == "/api/auth/logout")
            {
                return HandleLogout(request);
            }

            // Update YouTube insluit code in liturgie JSON file
            if (line == "/api/liturgie/update-insluit" && request.Method == HttpMethod.Post)
            {
                return UpdateYoutubeInsluit(request);
            }

            // Upload PDF for liturgie
            if (line == "/api/liturgie/upload-pdf" && request.Method == HttpMethod.Post)
            {
                return UploadPdf(request);
            }

            if (line == "/api/liturgie/remove-pdf" && request.Method == HttpMethod.Post)
            {
                return RemovePdf(request);

            }

            // For authorized requests, add to console output
            if (IsAuthorized(request))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(" [AUTHORIZED]");
                Console.ResetColor();
            }

            if (line == "/")
            {
                return HandleRootIndex(request);
            }

            // Handle sitemap.xml request (with or without trailing slash)
            if (line == "/sitemap.xml" || line == "/sitemap.xml/")
            {
                return HandleSitemap(request);
            }

            // Handle robots.txt request (with or without trailing slash)
            if (line == "/robots.txt" || line == "/robots.txt/")
            {
                return GetFile("/robots.txt", "text/plain");
            }

            // Let ACME challenge files be served from /.well-known/acme-challenge/
            if (line.StartsWith("/.well-known/acme-challenge/"))
            {
                return GetFile(line, "text/plain");
            }

            // Allow serving HTML verification files from root (Google Search Console, Bing, etc.)
            if (Path.GetExtension(line) == ".html" && !line.StartsWith("/html/"))
            {
                // Check if file exists in root for verification files
                string filePath = @"c:/pgad" + line;
                if (System.IO.File.Exists(filePath))
                {
                    return GetFile(line, "text/html");
                }
            }

            // All .html requests: serve the fragment only when explicitly asked; otherwise serve shell
            if (Path.GetExtension(line) == ".html")
            {
                bool isFragmentRequest = request.Headers.Contains("X-Fragment-Request");

                if (isFragmentRequest)
                {
                    return GetFile(line, "text/html");
                }

                // Serve shell (index.html) so client-side loader can inject fragment
                //return GetFile("/index.html", "text/html");
                return GetFile(line, "text/html");
            }

            if (line.StartsWith("/images/") || line.StartsWith("/assets/") || line.StartsWith("/pdf/") || line.StartsWith("/html/") || line.StartsWith("/json/") || line.StartsWith("/liturgie/"))
            {
                string ext = Path.GetExtension(line);

                switch (ext)
                {
                    case ".css": return GetFile(line, "text/css");
                    case ".js": return GetFile(line, "text/jscript");
                    case ".txt": return GetFile(line, "text/plain");
                    case ".xml": return GetFile(line, "application/xml");
                    case ".ico": return GetFile(line, "image/x-icon");
                    case ".jpg": return GetFile(line, "image/jpeg");
                    case ".png": return GetFile(line, "image/png");
                    case ".woff2": return GetFile(line, "font/woff2");
                    case ".pdf": return GetFile(line, "application/pdf");
                    case ".json": return GetFile(line, "application/json");
                }
            }

        }
        catch (Exception e)
        {
            Console.WriteLine($"FAILURE {e.Message} \r\n {e.StackTrace}");
            Console.Beep(500, 1000);
        }

        return null;
    }
}

