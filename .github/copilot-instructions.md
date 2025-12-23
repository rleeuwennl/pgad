<!-- Copilot / AI agent instructions for the PGAD repo -->
# Copilot Instructions — PGAD

Purpose
- Help contributors quickly understand this repo's structure, key workflows, and editing constraints so AI agents can be productive immediately.

High-level architecture
- Root: a static website (HTML/CSS/JS) served from the repository root (see [index.html](index.html)).
- `server/`: a .NET Web API/self-hosting service (solution: [server/PpeWebService.sln](server/PpeWebService.sln)). The service exposes endpoints under `/api/PpeWebService` (see [server/README.md](server/README.md)).
- `certificate/`: local SSL artifacts used by the service; certificate operations are documented in [server/README.md](server/README.md).

Where to look (examples)
- UI entry: [index.html](index.html)
- Front-end assets: `assets/css/`, `assets/js/`, `assets/sass/` (example SCSS: [assets/sass/main.scss](assets/sass/main.scss))
- Server code & solution: [server/PpeWebService.sln](server/PpeWebService.sln) and the `server/` subfolders (source and packages).
- Project notes: [readme.txt](readme.txt) and [server/README.md](server/README.md)

Build / run guidance (discoverable steps)
- Static site: open `index.html` in a browser or serve the root directory with a static HTTP server.
- Server: open `server/PpeWebService.sln` in Visual Studio (recommended) and build/run. The repo includes a `packages/` folder, so a NuGet restore may be unnecessary; if missing, run NuGet restore or use Visual Studio's restore feature.
- SSL/certificates: follow the exact commands in [server/README.md](server/README.md) for creating/importing PFX and applying `netsh` bindings. These steps require Windows admin privileges and access to `c:/pgad/certificate`.

Project-specific conventions and patterns
- Static assets stay under `assets/` and are referenced by relative paths in `index.html`.
- SASS sources live in `assets/sass/` and are compiled into `assets/css/` (no automatic build script present in repo).
- Server uses ASP.NET Web API self-hosting (older Web API packages present under `server/packages/`). Expect classic .NET tooling (Visual Studio / msbuild), not dotnet CLI for modern .NET Core projects.
- API surface: requests use query parameters (example from README): `...?cmd=WebPage&arg=main` — be cautious changing dispatch logic without end-to-end tests.

Integration points & external dependencies
- External domain/cert: `dsea.nl` (certificate notes in `server/README.md`).
- The server exposes an HTTP API consumed by the front end or other apps — do not change routes without verifying callers.
- The repository contains third-party libraries under `server/packages/` (NuGet). Maintain that folder when editing server projects unless you intentionally migrate to package restore.

Secrets and safety
- `server/README.md` and `certificate/` contain sensitive details (thumbprints, example passwords). Treat them as secrets — do not commit new credentials. If you find credentials in plaintext, flag them and suggest moving to a secure store.

Tests and CI
- There are no discoverable automated tests or CI configs in the root (except a `server/bitbucket-pipelines.yml` inside `server/`). Expect manual verification when changing behavior.

When making changes
- For front-end edits: modify files under `assets/` and validate by loading `index.html` locally.
- For server edits: open `server/PpeWebService.sln`, build, and run locally. Use the URLs shown in `server/README.md` to validate endpoints.
- Avoid modifying `server/packages/` unless you update build instructions and verify the solution still builds on CI/dev machines.

If unsure, ask the maintainer for:
- which Visual Studio version to target for `PpeWebService.sln`.
- whether it's safe to replace the certificate workflow with automated tooling.

Useful links in this repo
- [index.html](index.html)
- [readme.txt](readme.txt)
- [server/README.md](server/README.md)
- [server/PpeWebService.sln](server/PpeWebService.sln)

End.
