<!-- Copilot / AI agent instructions for the PGAD repo -->
# Copilot Instructions — PGAD

Purpose
- Help contributors quickly understand this repo's structure, key workflows, and editing constraints so AI agents can be productive immediately.

## Architecture Overview

**PGAD** is a church website (Protestantse Gemeente Angerlo-Doesburg) with:
1. **Static frontend**: HTML/CSS/JS at root; content loaded dynamically via `data-fragment` links (see [assets/js/main.js](assets/js/main.js)).
2. **Self-hosted .NET API**: [server/WebServer.sln](server/WebServer.sln) (targets .NET 4.7.2) serves requests at `/api/` endpoints.
3. **Content modules**: `html/` (pages), `liturgie/` (worship schedules with JSON metadata), `pdf/` (documents).
4. **Authorization**: Simple token system (see [SIMPLE_AUTH.md](SIMPLE_AUTH.md)) — password-protected edits via `X-Auth-Token` header.

## Key Components & Data Flow

**Frontend Navigation** ([index.html](index.html))
- Menu links with `data-fragment="html/page.html"` trigger dynamic page loads.
- [assets/js/main.js](assets/js/main.js) intercepts clicks, fetches fragment HTML via GET, injects into `#main` div.
- No full page reload — SPA-like behavior for navigation.

**Server Routing** ([server/WebServer/RequestHandler.cs](server/WebServer/RequestHandler.cs))
- Handles file serving (HTML, CSS, JS, images, PDFs) from `c:/pgad/`.
- Exposes `/api/auth/*` (login/logout), `/api/liturgie/*` (worship schedule edits).
- Authorization checked via `IsAuthorized()` helper; valid tokens stored in-memory.

**Liturgie Editor** ([assets/js/liturgie-editor.js](assets/js/liturgie-editor.js))
- Admin-only feature: POST to `/api/liturgie/update-insluit`, `/api/liturgie/upload-pdf`, `/api/liturgie/remove-pdf`.
- Reads/writes JSON from `liturgie/json/` and PDFs from `liturgie/pdf/`.
- Requires valid auth token; requires form-data with `Content-Type: application/x-www-form-urlencoded` (check usage).

## Project-Specific Conventions

1. **File serving**: Paths are absolute (`c:/pgad/...`) on Windows; always validate existence before reading.
2. **HTML fragments**: All reusable pages live in `html/` and are loaded client-side via `data-fragment` attribute (not server-side rendering).
3. **Authorization**: Password in [server/WebServer/RequestHandler.cs](server/WebServer/RequestHandler.cs) line ~18 (`ADMIN_PASSWORD`). Tokens are UUIDs stored in static `validTokens` HashSet (in-memory, lost on restart).
4. **SASS build**: Sources in `assets/sass/main.scss` → compiled CSS in `assets/css/main.css`. No automatic build script; compile manually or use a tool.
5. **SSL/HTTPS**: Certificates in `certificate/`; certificate hash and `netsh` binding commands in [server/README.md](server/README.md). Requires Windows admin privileges.

## Workflow: Running Locally

**Static site only:**
```
Open index.html in browser or serve root with `python -m http.server 8000` (or similar).
```

**With server (HTTPS required):**
1. Set up SSL certificate: follow [server/README.md](server/README.md) (import PFX, run `netsh` commands as admin).
2. Open [server/WebServer.sln](server/WebServer.sln) in Visual Studio.
3. Restore NuGet packages if needed (`packages/` folder may contain local copies).
4. Build and run; server listens on `https://localhost:443/` (or port specified in [server/WebServer/Program.cs](server/WebServer/Program.cs)).
5. Test: `https://nlhlelec01.aebi-schmidt.com/api/PpeWebService?cmd=WebPage&arg=main` (or local IP).

## Common Tasks

**Add a new page:**
- Create `html/my-page.html` (fragments only, no `<html>` wrapper).
- Add menu link to [index.html](index.html): `<a href="#" data-fragment="html/my-page.html">Link Text</a>`.
- No server restart needed.

**Edit liturgy/worship schedule:**
- Place JSON in `liturgie/json/`, HTML in `liturgie/html/`, PDF in `liturgie/pdf/`.
- Admin must be authorized (Ctrl+Shift+L) to edit via editor UI.
- Server validates authorization before allowing updates.

**Update API endpoint:**
- Modify [server/WebServer/RequestHandler.cs](server/WebServer/RequestHandler.cs) or add new handler methods.
- Test authorization flow: mock `X-Auth-Token` header in requests.
- Do not change `/api/auth/*` routes without updating [assets/js/simpleauth.js](assets/js/simpleauth.js).

## Secrets & Safety

⚠️ **Credentials in plaintext:** [server/README.md](server/README.md) and [server/WebServer/RequestHandler.cs](server/WebServer/RequestHandler.cs) contain passwords and certificate hashes. Do NOT commit new credentials; flag and escalate to maintainer.

## Testing & Debugging

- **No automated tests present.** Verify changes manually:
  - Front-end: load `index.html` in browser, test navigation and form submissions.
  - Server: run locally, check console output for `[OK]` / `[AUTHORIZED]` tags.
  - Auth flow: use browser DevTools to inspect `X-Auth-Token` header in requests.

## External Dependencies

- **Domain/SSL**: `dsea.nl` (certificate renewal every year ~March; coordinate with IT).
- **Third-party NuGet**: `AspNetWebApi.SelfHost`, `MailKit`, `BouncyCastle` (in `server/packages/`).

## Useful Links

- [index.html](index.html) — entry point
- [server/WebServer.sln](server/WebServer.sln) — .NET solution
- [server/README.md](server/README.md) — SSL/certificate setup
- [SIMPLE_AUTH.md](SIMPLE_AUTH.md) — authorization details
- [assets/js/main.js](assets/js/main.js) — SPA navigation logic
- [server/WebServer/RequestHandler.cs](server/WebServer/RequestHandler.cs) — API routing
