# Agriloco first Azure deployment handoff

Prepared September 9, 2026. No Azure resource, deployment, SQL application or DNS action has been performed. This document supersedes the anonymous-write blocker statements in the earlier readiness reviews. See `PUBLIC-LAUNCH-AUDIT.md` for the current route inventory and evidence.

**Recommendation: GO for a controlled first Azure deployment and validation. Do not connect agriloco.ca until steps 9–10 pass.** Real Azure SQL connectivity, persistent upload behavior, hosted HTTPS/HSTS and phone loading remain hosting acceptance checks. The standalone Unity Editor still needs its separate same-origin integration before it can save through production browser login; this does not prevent the public Viewer deployment.

The exact next action is to review these working-directory changes in Visual Studio, select/confirm the Azure subscription, region, names and budget, then separately authorize creation of the resource group in step 1. No Git operation was performed by this audit.

## Deployment sequence — execute only after authorization

1. **Resource group.** Create a dedicated production resource group in the selected region. Proposed name: `rg-agriloco-prod`; Canada Central is a reasonable location to confirm for this Toronto-based project. Record the actual subscription and region. Do not infer that an existing resource group or resource name is available.

2. **Azure SQL server/database.** Create a logical server and empty database in that region. Configure its Microsoft Entra administrator and narrow network access for the migration workstation and chosen App Service connectivity. Keep the schema operator separate from the application runtime identity. Choose database capacity/backup retention against the approved budget; no billable resources have been selected or created by this audit.

3. **Production schema.** Review `.codex-build/launch-audit/production-schema.sql`, then apply it to the new database as the schema operator. It is the idempotent SQL Server EF migration script, generated locally. Confirm `__EFMigrationsHistory` contains the initial SQL Server migration and required tables exist. The application does not apply migrations, EnsureCreated or SQLite startup DDL in Production. Do not grant runtime schema-alter permissions.

4. **Safe Farm 1 data transfer.** Freeze writes while preparing the actual migration snapshot. Use **Agriloco-Deploy**, never `sept 9`. Run the offline conversion utility below to a new private staging file, not the original database and not the publish folder. Import only the agreed Farm 1 data from that sanitized copy, with a reviewed parameterized data-transfer process in a target transaction. Do not export plaintext credentials to CSV, SQL literals or logs.

   Reconcile the five AvailabilityChannels by Code against migration seeds; do not duplicate them. Preserve Farm 1; its 80 FarmDefinitions (all DefinitionId values currently null), three disabled channel links, two FarmMaps (draft 1/published 2), 116 features and 232 ordered points across both maps. Published-only counts are 58 features/116 points. Preserve parent IDs, statuses, visibility, style, labels, coordinates and timestamps. Include the one existing Farm 1 Member with its **converted PasswordHash bytes**, empty PasswordSalt, original Member.Id/FarmId and IsActive so farmer login works. Include the existing legacy Crop if retaining the legacy crop-management workflow. Inventory and subscriber/contact history require an explicit separate selection; the utility copies them to private staging for fidelity but does not authorize importing every table.

   Use table order: master channels/Definitions if referenced, Farms, Members, parent-first FarmDefinitions, channel links, FarmMaps, features, points, and selected legacy Crops. Map SQLite BLOB to SQL Server varbinary and preserve hashes exactly. Use controlled IDENTITY_INSERT for explicit IDs and reseed appropriately. Do not copy SQLite internal tables or SQLite migration history. Validate row counts, foreign references and Member-to-Farm ownership before committing; roll back on mismatch. Keep plaintext source backups private and local; do not upload them to Azure SQL or App Service.

5. **Basemap/uploads.** Stage `wwwroot/uploads/farms/1/basemap.png` at the identical path, preserving URL case. Expected size: 1,052,655 bytes; SHA-256: `5D924777A6CC75E6EDE80EFF4365F02A8FFD2281EB471E560A698876D5530A32`. It is included in the validated publish. Transfer only selected additional uploads, and establish backup/persistence for future farmer uploads. Do not publish a source/database backup as static content.

6. **App Service.** Create a Windows .NET 8 Code Web App and suitably sized App Service plan in the same resource group/region, using the actual available app name. Record the exact default hostname assigned by Azure. Windows/IIS is the smallest hosting change for this project. Enable HTTPS Only, minimum TLS 1.2 or higher, and Always On where supported. Use extracted deployment with writable persistent upload storage: do not enable read-only run-from-package while uploads write beneath wwwroot. Preserve ASP.NET Data Protection keys across restarts; verify cookie continuity and upload persistence on restart. See [Microsoft's ASP.NET Core/SQL deployment tutorial](https://learn.microsoft.com/en-us/azure/app-service/tutorial-dotnetcore-sqldb-app).

7. **Identity, application settings and secrets.** Enable the Web App's system-assigned managed identity. As the SQL Entra administrator, create its contained database user and grant required data read/write permissions, not DDL. Configure the following App Service **application settings**, replacing placeholders with actual values. Do not create a conflicting connection-string entry named Default. Follow [Microsoft's managed identity SQL guidance](https://learn.microsoft.com/en-us/azure/app-service/tutorial-connect-msi-sql-database).

   | Setting | Value |
   | --- | --- |
   | ASPNETCORE_ENVIRONMENT | Production |
   | Database__Provider | SqlServer |
   | ConnectionStrings__Default | `Server=tcp:<sql-server>.database.windows.net,1433;Initial Catalog=<database>;Authentication=Active Directory Managed Identity;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;` |
   | Application__PublicBaseUrl | `https://<actual-default-app-hostname>`; origin only, no path/query |
   | AllowedHosts | `<actual-default-app-hostname>`; hostname only |
   | Email__Host | Actual SMTP host |
   | Email__Port | Actual SMTP port, currently 587 |
   | Email__User | SMTP account, supplied privately |
   | Email__AppPassword | SMTP app password supplied through protected App Service configuration/Key Vault reference |
   | Email__From | Configured sender |
   | Google__GeocodingApiKey | Only if farmer address geocoding is required; supply privately |

   IIS integration handles the forwarded HTTPS scheme on Windows. If the hosting choice changes to Linux/proxy hosting, revisit the trusted-forwarded-header configuration in DEPLOYMENT-READINESS.md before launch. Do not blindly trust arbitrary forwarded headers. Same-origin login cookies require HTTPS. No API keys should be placed in Unity. The published app contains no local development secrets.

8. **Release deployment.** Deploy only the contents of `.codex-build/launch-audit/publish`, with Agriloco1.dll/web.config at the deployment root, not an extra parent directory. If source changes after this audit, repeat restore/build/publish and the exclusions/hash scan. Do not deploy the repository, Tools, `.codex-build` parent directory, local settings, or any SQLite file. Preserve selected persistent uploads during deployment. Confirm the deployed file hashes and all 17 Viewer assets.

9. **Default-host validation.** First use the assigned `azurewebsites.net` hostname. Check anonymous `/viewer/?farmId=1`, `/api/UnityMap/farm?farmId=1`, `/api/UnityMap/published?farmId=1`, public farm/search, and basemap. Compare published geometry against the selected baseline. Confirm anonymous management/draft/write requests return 401; debug/email-test routes return 404; authenticated wrong-farm requests return 403; and the migrated Member can log in with the existing password. Exercise writes on an explicitly disposable test farm, never by clearing Farm 1's production map. Verify SQL runtime rights, restart/login continuity, uploads, HTTPS redirection and HSTS. Exercise legitimate signup with a controlled address. A live availability email test requires separate explicit authorization; none was sent during this audit.

10. **Cellular phone.** Turn Wi-Fi off and open `https://<actual-default-app-hostname>/viewer/?farmId=1` in a private browser session. Confirm basemap, 58 published features, crop selection/availability, readable text and signup flow. Confirm no localhost request or browser certificate warning. Record the phone/browser result before DNS cutover.

11. **Only then connect agriloco.ca.** Separately authorize DNS changes. Add the desired apex/www names in App Service using the exact TXT verification and A/CNAME values shown for the actual app; do not guess IPs or verification IDs. Choose the canonical hostname and update Application__PublicBaseUrl and AllowedHosts after domain validation/binding. Retain the default hostname only if still needed for testing. See [Microsoft's custom-domain/certificate workflow](https://learn.microsoft.com/en-us/azure/app-service/tutorial-secure-domain-certificate).

12. **Custom-domain HTTPS.** Obtain/bind an eligible App Service managed certificate or approved certificate, verify HTTPS and renewal requirements for each hostname, and repeat phone, anonymous Viewer, login, management-denial and cookie-origin tests on the custom domain. Verify HTTP redirects to HTTPS and HSTS on the hosted domain. Keep the default host and rollback data available until acceptance. See [Microsoft's current certificate requirements](https://learn.microsoft.com/en-us/azure/app-service/configure-ssl-certificate).

## Offline password preparation

From the repository directory, choose a **new** destination in a private local staging directory:

```powershell
dotnet run --project Tools/PrepareMemberMigration/PrepareMemberMigration.csproj -c Release -- "C:\Users\pete2\source\repos\Agriloco-Deploy\agriloco.db" "<private-staging-directory>\farm1-sanitized.sqlite"
```

Replace the placeholder before running. Never reuse the destination path or use the original path as the destination. The utility opens the source read-only, takes a consistent read transaction, uses the application's MemberPasswords implementation, verifies each conversion in memory, and inserts only hashed credentials into a fresh logical copy. Unknown/empty legacy formats fail closed before output creation. Existing marked hashes remain unchanged. It never starts the web application, sends email, or connects to SQL Server. Logs contain counts only, never credential values or database exception details. A nonzero exit means **do not import the output**.

The local proof copy is `.codex-build/launch-audit/sanitized.db`: all business rows/IDs matched, one legacy Member was converted, the original credential bytes were absent from the destination, and the original database was unchanged. This is evidence, not an automatically approved production snapshot. Regenerate from the frozen source at migration time. Treat the staging copy as private because it still contains member/customer/business information and password hashes. The utility is excluded from the website build/publish.
