# SEO Verification Checklist for pgad.dsea.nl

## ✅ What's Working:
1. **robots.txt** - ✓ Accessible at https://pgad.dsea.nl/robots.txt
2. **sitemap.xml** - ✓ Dynamically generated at https://pgad.dsea.nl/sitemap.xml
3. **index.html** - ✓ Updated with all SEO meta tags and structured data

## ⚠️ CRITICAL: Server Restart Required

**The .NET backend service MUST be restarted for changes to take effect!**

The index.html file has been updated locally but the running server may be serving a cached version.

### Steps to Restart the Backend:

1. **Close the running WebServer.exe** (if running)
2. **Rebuild the solution:**
   - Open `c:\pgad\server\WebServer.sln` in Visual Studio
   - Build → Rebuild Solution
3. **Run the server:**
   - Press F5 or Start Debugging
   - Ensure it starts on https://localhost:443/

### Verify Changes:

After restart, check these URLs:

1. **Main Page Meta Tags:**
   ```
   https://pgad.dsea.nl/
   ```
   - Look in page source (Ctrl+U) for:
     - `<title>` tag with keywords
     - `<meta name="description">`
     - `<meta name="robots" content="index, follow">`
     - Schema.org structured data in `<script type="application/ld+json">`

2. **Robots.txt:**
   ```
   https://pgad.dsea.nl/robots.txt
   ```
   - Should show plain text with sitemap reference

3. **Sitemap:**
   ```
   https://pgad.dsea.nl/sitemap.xml
   ```
   - Should show XML with all 35+ pages

## SEO Improvements Implemented:

### Meta Tags (in index.html)
- ✅ Enhanced page title with location keywords
- ✅ Meta description (160 characters)
- ✅ Meta keywords
- ✅ Robots meta tag (index, follow)
- ✅ Canonical URL
- ✅ Viewport for mobile

### Social Media (OG Tags)
- ✅ Open Graph tags (Facebook, LinkedIn)
- ✅ Twitter card tags
- ✅ Social media image references

### Structured Data (Schema.org)
- ✅ Church/Organization schema
- ✅ Address schema with postal details
- ✅ Service offerings schema
- ✅ Event schema for services
- ✅ Contact point schema

### Technical SEO
- ✅ Dynamic sitemap generation
- ✅ robots.txt with sitemap reference
- ✅ Resource preloading (CSS, JS)
- ✅ DNS prefetching
- ✅ Descriptive alt text for images
- ✅ Proper heading hierarchy

### Content SEO
- ✅ Alt text for all service images
- ✅ Descriptions for each service
- ✅ Location and time information in listings
- ✅ Semantic HTML structure

## Next Steps After Server Restart:

1. **Submit to Google Search Console:**
   - https://search.google.com/search-console
   - Add property: https://pgad.dsea.nl
   - Submit sitemap: https://pgad.dsea.nl/sitemap.xml
   - Request indexing

2. **Check Google Search Results:**
   - In Google, search: "site:pgad.dsea.nl"
   - Should show indexed pages

3. **Monitor SEO Performance:**
   - Google Search Console → Performance
   - Check impressions, clicks, CTR
   - View search queries bringing traffic

4. **Validate with Tools:**
   - Google Mobile-Friendly Test
   - PageSpeed Insights
   - Lighthouse (Chrome DevTools)
   - Schema.org Validator

## File Locations:

- Local files: `c:\pgad\index.html`, `c:\pgad\robots.txt`
- Server code: `c:\pgad\server\WebServer\RequestHandler.cs`
- Live URL: https://pgad.dsea.nl/

---

**Status:** Awaiting server restart to deploy changes to production
**Last Updated:** 2026-03-10
