# Agriloco first Azure deployment review

## A. Exact cause of the Visual Studio 404

Observed IIS Express process 40772 running with `C:\Users\pete2\source\repos\sept 9\.vs\Agriloco1\config\applicationhost.config`, site Agriloco1. That configuration's site entry (lines 158–164) binds HTTPS port 44389 and sets physicalPath to `C:\Users\pete2\source\repos\sept 9`. A verified HTTPS request to `https://localhost:44389/viewer/?farmId=1` returned 404. That older repository has no `wwwroot/viewer/index.html`, and its Program.cs has static files but no UseDefaultFiles call. The running process therefore is not the integrated deployment repository. No old-project files were modified.

Agriloco-Deploy contains the complete 17-file export under `wwwroot/viewer` and calls UseDefaultFiles before Unity static-file middleware. No nested export directory exists.

## B–C. Changes and exact Visual Studio project

Open `C:\Users\pete2\source\repos\Agriloco-Deploy\Agriloco1.sln`, whose project is `C:\Users\pete2\source\repos\Agriloco-Deploy\Agriloco1.csproj`. Stop the old sept 9 debugging session first so it releases port 44389. Set Agriloco1 as startup project, select IIS Express and run. Then open `https://localhost:44389/viewer/?farmId=1`.

One application configuration file changed: `Properties/launchSettings.json`. Its IIS Express profile now sets `Application__PublicBaseUrl=https://localhost:44389` so server-side API calls in that profile agree with its actual HTTPS binding. The Project profile remains HTTP localhost:5227. This supplemental profile correction is not the cause of the old repository's 404. Production configuration and Unity source/build files were not changed. This report is the only added review document.

## D. Correct-repository runtime test

Ran the exact Release assembly from Agriloco-Deploy with temporary Development/SQLite environment settings at `https://localhost:55634`. Browser URL: `https://localhost:55634/viewer/?farmId=1`.

Real Farm 1 (Wheelbarrow Orchards) loaded. Actual browser GETs to `/api/UnityMap/farm?farmId=1` and `/api/UnityMap/published?farmId=1` returned HTTP 200, including refreshes. The farm response has 80 definition items; the published response has 58 features. Browser console confirmed the 921×568 basemap loaded and 58 published features rendered. No synthetic data was used. Existing geolocation timeout/shader diagnostics remain nonblocking; no GPS redesign was attempted. No new signup or email was sent. Temporary validation server stopped afterward; the tested port is not a new permanent Visual Studio binding.

## E. Fresh production publish

Output: `.codex-build/azure-readiness/publish`.

Restore, Release build and fresh Release publish passed. The fresh compilation reports the three existing warnings (two CS8601 in FarmControllers, one CS1998 in ProductionRunDetails), no errors. Checked Agriloco1.dll, deps/runtimeconfig files, Windows IIS web.config, appsettings.Production.json, all 17 Unity files, and Farm 1's basemap. Viewer hashes match source web assets.

Verified no local .db/.sqlite databases, appsettings.json, appsettings.Development.json, secrets.json, .codex-build, .git, .vs, or temporary log directories are inside publish. Scanned published text/config assets for the configured local SMTP password and API-key values without printing those values; no matches were found. Secrets must be supplied at hosting time. The publish retains existing public legacy static assets, including wwwroot/Unity and older unreferenced upload images; none were deleted during this review.

## F. Resources to create later (none created now)

For the smallest change from this Windows/IIS project, use a resource group, Windows App Service plan supporting .NET 8 (for example Basic B1 for an initial low-volume test), a Windows Web App with Code/.NET 8/64-bit runtime, an Azure SQL logical server, and one empty Azure SQL database in the same chosen region. Select capacity/backup retention against the actual budget before creating billable resources. No custom-domain resources or DNS changes are needed for the initial azurewebsites.net test.

Use a separate schema-deployment identity and a runtime database identity. Managed identity is preferable: enable the Web App's system-assigned identity, set an Entra administrator on the SQL server, create a contained database user for that Web App identity, and grant only the necessary dbo table read/write permissions (initially db_datareader/db_datawriter if finer grants have not yet been defined). The runtime identity needs no schema-alter rights. Give the schema operator the necessary DDL rights separately. After schema creation, an Entra administrator can provision the system-assigned identity in the target database with `CREATE USER [<web-app-identity-name>] FROM EXTERNAL PROVIDER; ALTER ROLE db_datareader ADD MEMBER [<web-app-identity-name>]; ALTER ROLE db_datawriter ADD MEMBER [<web-app-identity-name>];` (substitute the actual unique identity name; use its object ID to resolve ambiguous names). Configure SQL network access for the chosen App Service route and migration workstation; use explicit rules or private networking rather than assuming SQL is reachable.

## G. Exact App Service configuration

These names are App Service **application settings**. Replace angle-bracket placeholders with chosen resource values; do not copy literal placeholders.

| Name | Value |
| --- | --- |
| ASPNETCORE_ENVIRONMENT | Production |
| Database__Provider | SqlServer |
| ConnectionStrings__Default | `Server=tcp:<sql-server>.database.windows.net,1433;Initial Catalog=<database>;Authentication=Active Directory Managed Identity;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;` for the system-assigned identity above |
| Application__PublicBaseUrl | `https://<actual-app-service-hostname>` with no path/query (use the hostname shown by Azure, not an invented domain) |
| AllowedHosts | `<actual-app-service-hostname>` without scheme or port |
| Email__Host | smtp.gmail.com (or the actual configured SMTP service) |
| Email__Port | 587 |
| Email__User | secret-configured SMTP account |
| Email__AppPassword | secret-configured app password; never place it in source/publish/report |
| Email__From | configured sender address |

A secret SQL username/password connection string is an alternative only if SQL authentication is selected; do not use a server administrator as the app account. Do not also configure a conflicting App Service connection string named Default. Double underscores map to hierarchical ASP.NET configuration: [Microsoft configuration guidance](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-8.0).

Set HTTPS Only on and TLS minimum 1.2 or higher, and use the .NET 8 Windows runtime. No custom startup command is required: the generated web.config runs Agriloco1.dll. Always On is useful where supported for the hosted worker. Do not deploy Development settings or local ASPNETCORE_URLS/localhost bindings.

Use an extracted code/ZIP deployment and leave WEBSITE_RUN_FROM_PACKAGE unset. The existing upload page writes to wwwroot/uploads; run-from-package makes wwwroot read-only ([Microsoft documentation](https://learn.microsoft.com/en-us/azure/app-service/deploy-run-package)). Preserve/back up uploaded files across later deployments. Blob storage would require a separate application change and is not a prerequisite for this initial Windows plan.

Windows IIS integration supplies the forwarded scheme. If Linux App Service is selected instead, configure ASPNETCORE_FORWARDEDHEADERS_ENABLED=true behind the managed App Service proxy, use the .NET 8 stack and `dotnet Agriloco1.dll`, and verify forwarded HTTPS behavior and upload storage before launch. Do not enable arbitrary proxy trust for a directly exposed backend. No CORS rule is needed for the same-origin Viewer. No current source usage of Google:GeocodingApiKey was found, so it is not a demonstrated requirement for this Viewer launch. If adding the custom domain later, update PublicBaseUrl and AllowedHosts only after binding/testing HTTPS; no code domain change is needed.

## H. Empty Azure SQL schema procedure

1. Create a SQL database with **Data source: None** (not Sample, Backup, or an imported SQLite database). Use a deliberate database name and verify the target server/database before applying anything. Azure SQL creation reference: [Microsoft](https://learn.microsoft.com/en-us/azure/azure-sql/database/single-database-create-quickstart?view=azuresql).
2. Schema is `SqlServerAgrilocoContext`, migration `20260909151613_InitialSqlServer`. Do not use the historical SQLite AgrilocoContext migration, EnsureCreated, or a SQLite SQL dump.
3. Already generated locally, without database access: `.codex-build/azure-readiness/production-schema.sql`. EF reports no pending model changes. Reproduce from the deployment repository with:

```powershell
dotnet restore Agriloco1.csproj
dotnet build Agriloco1.csproj -c Release --no-restore
dotnet ef migrations script --context SqlServerAgrilocoContext --idempotent --configuration Release --no-build --output .codex-build/azure-readiness/production-schema.sql
```

4. Review the script, then later open it in SSMS connected directly to the new Azure SQL database using the schema-deployment identity and execute the full script. SSMS handles its GO batch separators. Confirm `SELECT DB_NAME()` first. The script creates schema, indexes, five availability-channel seeds, and the migration-history row transactionally. It does not create the Azure resource or copy Farm 1. No SQL script was executed against SQL Server during this pass.
5. Verify one history row for 20260909151613_InitialSqlServer, five channel IDs/codes, expected tables/indexes, and normal runtime-user read/write permissions. Confirm runtime startup does not attempt schema creation. Apply Farm 1 data separately only after the following plan is reviewed. Idempotent-script guidance: [EF Core documentation](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying).

## I. Safe Farm 1 migration plan (not executed)

**User-selected source:** `C:\Users\pete2\source\repos\Agriloco-Deploy\agriloco.db`. Do not merge sept 9. Its definitions 39, 40 and 66 have Available statuses where this selected source has Unavailable; it also has two extra channel links and an enabled link that the selected source has disabled. Those differences were disclosed and the tested deployment source was explicitly selected.

Freeze source writes and take a consistent SQLite backup immediately before the future transfer. Import through a reviewed, parameterized conversion tool into the schema-created target in one transaction; do not replay SQLite DDL or use CSV coercion without typed validation. Refuse a destination containing unexpected Farm 1 IDs/data. Use explicit column lists, SQL Server IDENTITY_INSERT ON/OFF per identity table (one at a time), keep IDs and nulls, preserve timestamps as UTC-intended values with their original precision, convert SQLite integer booleans to bit, and validate SQL string lengths/numeric ranges. Seed rows are reconciled, not duplicated.

| Table | Required selection and count in chosen source |
| --- | --- |
| AvailabilityChannels | IDs 1–5 are already migration-seeded: PickYourOwn, FarmStore, Wholesale, RetailPartner, FarmersMarket. Verify ID AND Code match; reuse those rows. Review any non-key metadata differences before updating them. |
| Farms | Id=1: 1 row, preserving ID, name, IsActive, coordinates, map URL and timestamps. |
| Definitions | 0 currently required: all 80 FarmDefinitions have NULL DefinitionId. If source changes before transfer, recursively include referenced master definitions/parents. |
| FarmDefinitions | FarmId=1: 80 rows, all currently active/public. Preserve ParentFarmDefinitionId, type, status, sort order and other fields. All non-null parents resolve within this set. Insert in parent-first order if constraints require it. |
| FarmDefinitionChannels | Links to those definitions: 3 rows (IDs 1–3), all disabled. Preserve IsEnabled=false; do not invent enabled availability channels. |
| FarmMaps | Minimum Viewer: published map Id=2, FarmId=1. Recommended to retain editor continuity too: include draft Id=1, total 2 rows. Preserve status and publication/update timestamps. |
| FarmMapFeatures | Minimum: 58 rows where FarmMapId=2. With the existing draft: 116 total. Preserve IDs, geometry/label/style fields, FarmDefinitionId, LabelSourceFarmDefinitionId and custom labels. Zero label/definition sentinel values must retain their meaning. All positive references resolve. |
| FarmMapFeaturePoints | Minimum: 116 ordered points belonging to the 58 published features. With draft: 232 total. Preserve IDs, FarmMapFeatureId, PointOrder and normalized coordinates exactly. |

Import order: reconcile channels, Farms, referenced master Definitions if any, FarmDefinitions, channel links, FarmMaps, features, then points. Verify identity counters after explicit-ID insertion so subsequent editor writes cannot collide. Do not copy SQLite sqlite_sequence or its migration history to SQL Server.

The public Viewer obtains crops/varieties and availability from FarmDefinitions, not legacy Crops. This source has one legacy Crop, zero MapCells and zero FarmMapLayouts; none is used by the two Viewer endpoints. A broader legacy website migration can include that crop separately if required, but it is not a dependency for the first Viewer test. Inventory/production/history tables are not Viewer dependencies. Do not automatically migrate member passwords, subscribers, or customer contact data for this map smoke test; operator access and existing subscriptions need their own deliberate plan. Production signup starts with empty subscription tables unless explicitly migrated later.

Before commit, validate expected row counts, all parents/channel/feature/point/label references, the uniqueness of FarmId+Status maps used by the endpoint, ID mappings, and the unchanged disabled channels/statuses. Roll back on any mismatch. After import, compare the two API JSON results against the selected SQLite baseline semantically (allow only expected timestamp formatting differences), and verify the uploaded basemap hash before opening the public Viewer. No data export/import or upload was performed now.

## J. Farm 1 files

Required image: `wwwroot/uploads/farms/1/basemap.png`, referenced as `/uploads/farms/1/basemap.png`; 1,052,655 bytes; SHA-256 `5D924777A6CC75E6EDE80EFF4365F02A8FFD2281EB471E560A698876D5530A32`. It is already present in this publish. Preserve path/case and verify the same checksum on the future host. Farm 1's older basemap.jpeg and the other farms' upload images are not dependencies of the selected current map. The Viewer export's complete Build and TemplateData assets must accompany index.html. Its fonts are embedded; no separate StreamingAssets was generated. No additional external icon file dependency was found for the current public Viewer rendering.

## K–L. Genuine blockers and next action

The local 404 is explained; static hosting, the schema artifact and publish are ready for review. Azure SQL connectivity/schema application and the selected Farm 1 import are still unperformed prerequisites for a real hosted map test.

**Public exposure blocker:** UnityMapController's POST draft and POST publish actions accept a farm ID without caller authentication/ownership checks. Program.cs has no authentication middleware or fallback authorization policy, and those endpoints have no Authorize protection. Anonymous callers could overwrite/publish farm maps if this complete application were exposed openly. Do not mistake a functioning public read-only viewer for a protected farmer backend. No mutation was sent to prove this and no authentication redesign was made. Either protect farmer write/admin routes with authorization before public access, or make the initial Azure test explicitly access-restricted to designated testers while that work is completed. A fully open public launch must resolve it.

Recommended next action: open the correct solution and review this launch-profile change, the generated schema script, production settings and selected-source migration plan. Address or approve a restricted-test approach for the write-route exposure before authorizing the next controlled Azure provisioning/schema/data-transfer step. SMTP credentials, successful hosted mail, phone GPS and real hosted HTTPS behavior still require their intended environment tests; missing GPS-to-map-marker code is not a basic deployment blocker.

No Git operations, cloud deployment, DNS changes, or old-repository/Unity edits were performed. Local startup retained its existing additive SQLite behavior; no production migration state was changed.