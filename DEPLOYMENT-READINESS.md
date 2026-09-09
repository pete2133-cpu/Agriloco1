# Agriloco backend deployment preparation

No Azure resources or DNS were changed. Unity-Map is a separate repository and was not edited.

## Production configuration

Set these hosting environment variables before starting the published application:

| Setting | Value |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `Database__Provider` | `SqlServer` |
| `ConnectionStrings__Default` | Azure SQL connection string, with `Encrypt=True;TrustServerCertificate=False` and the selected credentials/identity |
| `Application__PublicBaseUrl` | Actual HTTPS origin, e.g. `https://your-host.azurewebsites.net` (no path, query, or fragment) |
| `AllowedHosts` | Actual hostname(s), separated by semicolons |
| `Email__Host` / `Email__Port` | SMTP host/port; production defaults retain Gmail/587 |
| `Email__User` / `Email__AppPassword` / `Email__From` | Existing working SMTP values, supplied through hosting secrets |
| `Google__GeocodingApiKey` | Production geocoding key, if geocoding is used |

Production uses HSTS and HTTPS redirection. On Azure Linux App Service, set `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` so the platform's TLS termination is recognized before redirection. This setting trusts forwarded headers: use it only behind the managed proxy with direct backend access restricted. IIS integration handles forwarded scheme headers on Windows. For a different reverse proxy, configure its known proxy addresses explicitly. See [Microsoft's proxy hosting guidance](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-8.0).

Server-side API calls and the legacy alert worker use the configured public origin. Browser API calls should stay same-origin; `/viewer/` requires no cross-origin CORS policy. Development retains SQLite and the development launch URL, configurable through `Application__PublicBaseUrl` for other launch profiles.

Local `appsettings.json` and `appsettings.Development.json` are excluded from publish so the working local SMTP credential is preserved without shipping it. SQLite files and `.codex-build` scratch outputs are also excluded. Configure production SMTP values before testing alerts. The dashboard alert matching, transition detection, deduplication, failure handling, and recurring subscriptions were not changed.

## Database preparation (manual, before launch)

The compiled historical migration belongs to `AgrilocoContext` and is SQLite-only and incomplete for today's model. Do not apply it to production, or apply it over the existing EnsureCreated SQLite database.

Production uses `SqlServerAgrilocoContext` to keep its migration history independent. Its full current model includes email subscriptions, production-source records, five seeded default availability channels, and decimal(18,6) quantities.

**Unresolved launch blocker:** Windows Application Control blocked EF from loading the compiled application (`0x800711C7`), including after elevated rebuild/scaffolding retries. No final SQL Server migration or model snapshot was generated. Complete the following locally in your trusted Visual Studio developer terminal before deploying:

```powershell
dotnet ef migrations add InitialSqlServer --context SqlServerAgrilocoContext --output-dir Migrations/SqlServer --configuration Release
```

Generate a reviewable SQL script using:

```powershell
dotnet ef migrations script --context SqlServerAgrilocoContext --idempotent --configuration Release --output .codex-build/production-schema.sql
```

Review and apply that script to an empty Azure SQL database using your normal database administration workflow before starting the app. The app deliberately does not create or alter production schema at startup. Future model changes require a new SQL Server migration and schema review. Existing SQLite data needs a separately planned export/import with preserved IDs and relationship checks; the initial migration creates schema, not farm/customer data.

## Unity-Map follow-up (separate repository)

1. Replace every hard-coded localhost API URL, including serialized scene/prefab/Inspector values. In WebGL, derive the origin from `Application.absoluteURL` (for example `new Uri(Application.absoluteURL).GetLeftPart(UriPartial.Authority)`) and append root API paths. Keep any Editor-only development URL separate.
2. Parse a positive `farmId` from the page query string and pass it to every farm-specific request. The exact smoke-test entry URL is `https://YOUR-HOST/viewer/?farmId=1`. Do not drop `?farmId=1` in links or reloads; show an error if it is absent/invalid.
3. Load live farm/status data from `/api/UnityMap/farm?farmId=1` and customer-viewer geometry from `/api/UnityMap/published?farmId=1`. Do not use the draft endpoint for the customer viewer. Farm 1 and a published map must exist in the destination database.
4. Resolve returned relative map/image/icon URLs against the same origin, not `/viewer/`. Check path casing on Linux (`wwwroot/Icons` currently uses an uppercase I).
5. Build a fresh WebGL viewer. Copy the complete export (`index.html`, `Build`, `TemplateData`, and any `StreamingAssets`) into backend `wwwroot/viewer/` before publishing. Keep build asset URLs relative so they resolve below `/viewer/`. Do not substitute the existing legacy `wwwroot/Unity` export without rebuilding its API settings.
6. Use uncompressed assets or native gzip/Brotli output with decompression fallback disabled. Backend static hosting supports `.wasm`, `.js`, and `.data`, including `.gz`/`.br` with the corresponding MIME type and Content-Encoding. Threaded builds need a separate COOP/COEP review; this pass does not enable cross-origin isolation globally.
7. In a browser, verify `/viewer/?farmId=1`, asset responses, the two API calls, map images/icons, and subscription behavior over HTTPS with no localhost requests or mixed-content failures.

`UseDefaultFiles` serves `/viewer/index.html` at `/viewer/` once the export is supplied and preserves the query string. No Unity build is supplied by this backend pass.

## Validation and remaining launch work

Restore, Release build, and Release publish passed with zero errors. Build reports three warnings in existing code: two CS8601 warnings in FarmControllers and one CS1998 warning in ProductionRunDetails. Publish output is `.codex-build/deploy-readiness/publish`. EF generation remains blocked by Windows Application Control. Runtime smoke tests were not completed; no SQL Server database was contacted and no real emails were sent.

SQL Server migration generation/review/application, actual database connectivity, SMTP inbox delivery with hosting secrets, and a rebuilt Unity browser run must still be verified before launch. Existing uploaded media under `wwwroot/uploads` also needs durable storage/backup planning before real usage. This focused pass is not an authentication/authorization audit.
