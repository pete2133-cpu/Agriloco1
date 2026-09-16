# First Azure deployment — access blocked before creation

The user authorized this deployment, but explicitly required stopping before resource creation if Azure tooling/authenticated access was unavailable. That condition was met. Nothing was created or deployed in Azure; no DNS or custom-domain work was performed.

## What was verified locally

- Azure CLI was not on PATH and was absent from its standard Windows install locations.
- Azure PowerShell Az.Accounts/Get-AzContext was unavailable.
- Visual Studio Community 2022 is installed, but vswhere found no installation with the Azure workload.
- No callable Azure connector/authenticated Azure management context was available. No subscription or credentials were guessed. No token/cache extraction or tooling installation was attempted.
- Release build and publish passed: zero errors, three existing warnings.
- Fresh publish: `.codex-build/first-azure-deployment/publish`.
- Ready-to-upload archive: `.codex-build/first-azure-deployment/agriloco-release.zip`. The archive contains publish contents at its root, not an extra directory.
- All 17 Viewer files match source. Application DLL, web.config and required Farm 1 basemap are present.
- No SQLite files/development configuration/known local secret values were found in publish, including decompressed Brotli/GZip assets.
- SQL schema script exists: `.codex-build/launch-audit/production-schema.sql` (30,458 bytes).
- Password conversion utility and Farm 1 import plan exist. The utility creates a sanitized SQLite staging copy; it does not itself import data into Azure SQL.
- Current read-only source counts: Farm 1 = 1, definitions = 80, channel links = 3, published features = 58, published points = 116, Members = 1.
- Basemap: `wwwroot/uploads/farms/1/basemap.png`, 1,052,655 bytes, SHA-256 `5D924777A6CC75E6EDE80EFF4365F02A8FFD2281EB471E560A698876D5530A32`.
- These are working-directory validations; commit status was not independently verified. No Git operation was used.

## Exact manual Azure Portal setup

These are instructions for the user, not actions already performed. Use your actual subscription; do not send passwords, tokens, connection-string passwords or publish profiles into chat. Confirm Portal-displayed costs and availability before creating resources.

1. Sign in to `https://portal.azure.com`. Open **Subscriptions**, select the intended subscription and verify you can create resources. Use the appropriate directory if more than one appears.
2. Open **Resource groups → Create**. Select that subscription. Name: **rg-agriloco-prod**. Region: **Canada Central**, provided your subscription supports the required services there. Select **Review + create → Create**. If the name already belongs to an unrelated group, use a new Agriloco-specific group instead; do not reuse unrelated resources.
3. Open **SQL databases → Create**. Select the new group. Database name: **agriloco-v1**. Under Server, choose **Create new**, with an available globally unique name beginning **sql-agriloco-v1-**, in **Canada Central**. Configure **Microsoft Entra authentication** and set your authorized account/group as the server's Entra administrator. Under **Compute + storage → Configure database**, choose the small **Basic DTU** tier if available and appropriate for the displayed cost; do not accept a larger default tier without reviewing it. Use **Data source: None** so the database starts empty. Keep encryption enabled. On Networking, enable only the access needed for your migration workstation; add its client IP, not an all-address firewall rule. Finish **Review + create → Create**. See [Microsoft's database creation steps](https://learn.microsoft.com/en-us/azure/azure-sql/database/single-database-create-quickstart?view=azuresql).
4. Open **App Services → Create → Web App**. Use the same subscription/group/region. Choose an available name beginning **agriloco-v1-**, **Publish: Code**, **Runtime: .NET 8**, **Operating system: Windows**. Create a dedicated plan named **asp-agriloco-v1**, initially **Basic B1** if available and affordable. Leave source-control deployment disconnected. Create the app, then immediately **Stop** it while preparing schema/data/settings. In its Overview, copy the exact assigned default hostname; do not construct or guess it. See [Microsoft's App Service creation guidance](https://learn.microsoft.com/en-ca/azure/app-service/quickstart-dotnetcore).
5. In the Web App, open **Identity → System assigned → On → Save**. Record the identity's Object/Principal ID. Under Configuration/General settings enable **HTTPS Only**, **Always On**, and minimum TLS 1.2 or higher. Do not enable `WEBSITE_RUN_FROM_PACKAGE`: this V1 uses extracted App Service content so the existing upload code can write files. Do not add a custom domain.
6. In SQL server **Networking**, add narrow rules for the app's listed possible outbound IP addresses if using public SQL connectivity, plus the migration workstation IP. Do not open the server to all IP addresses. A private-network alternative needs its own network setup; do not assume managed identity grants network access.
7. In the database's **Query editor** sign in with the Entra administrator, or use SSMS with Microsoft Entra MFA. Apply the reviewed schema script to this empty database. If the Portal editor cannot execute the script's batch separators, run the file in SSMS rather than rewriting the migration. Then provision the Web App identity in the **application database**, substituting the actual unique app identity name:

   ```sql
   CREATE USER [<actual-web-app-identity-name>] FROM EXTERNAL PROVIDER;
   ALTER ROLE db_datareader ADD MEMBER [<actual-web-app-identity-name>];
   ALTER ROLE db_datawriter ADD MEMBER [<actual-web-app-identity-name>];
   ```

   Do not grant the runtime identity schema-alter/admin permissions. Do not guess around identity-resolution errors; resolve the correct tenant/object first. See [Microsoft's SQL Entra guidance](https://learn.microsoft.com/en-us/azure/azure-sql/database/authentication-aad-overview?view=azuresql).
8. **Pause before data import/deployment if authenticated tooling still is not available.** Report only subscription name/ID, group, region, SQL server/database name, Web App name/default hostname and identity Object ID. The controlled Farm 1 import still needs to be executed and verified against Azure SQL. Portal **Import database** expects a BACPAC, not the sanitized SQLite file. Do not upload the original or sanitized SQLite database to App Service, and do not create synthetic Farm 1 data.

## Remaining steps after authenticated access and Farm 1 import

Follow AZURE-DEPLOYMENT-HANDOFF.md for the ID-preserving, parameterized Farm 1 transfer. Regenerate a private sanitized snapshot from the frozen real source using Tools/PrepareMemberMigration. Import the selected real rows and hashed Member BLOBs; validate relationships/counts before committing. Do not copy all private inventory/subscriber tables merely because the staging copy contains them.

In Web App **Settings → Environment variables** (or Configuration → Application settings), configure:

| Name | Value/handling |
| --- | --- |
| ASPNETCORE_ENVIRONMENT | Production |
| Database__Provider | SqlServer |
| ConnectionStrings__Default | Managed-identity SQL connection string from the handoff, using actual server/database |
| Application__PublicBaseUrl | HTTPS origin using the exact assigned Azure hostname |
| AllowedHosts | That exact hostname, without scheme/path |
| Email__Host, Email__Port, Email__User, Email__AppPassword, Email__From | Actual SMTP configuration, entered privately; required for sending alerts, not a reason to expose secrets in the package |
| Google__GeocodingApiKey | Only if farmer address geocoding is required; not required for public Viewer startup |

Do not add a conflicting App Service connection-string entry named Default. Do not use a SQL administrator account for the app. Keep secure authentication/ownership code unchanged.

After successful schema/data/settings preparation, deploy the prepared ZIP through **Development Tools → Advanced Tools → Go → Tools → Zip Push Deploy**. Upload the ZIP and confirm success, then start the Web App. This extracts the package into App Service content, including `/uploads/farms/1/basemap.png`. See [Microsoft's Windows Kudu ZIP workflow](https://learn.microsoft.com/en-us/azure/app-service/deploy-zip). Uploads remain in writable App Service content for V1, **not separate Azure Blob storage**; deployments/restore/deletion can affect them, so verify restart persistence and arrange backups.

Verify application startup, both public UnityMap APIs, `/viewer/?farmId=1`, basemap, Brotli/WASM/data requests, same-origin network requests, anonymous write denial and migrated Member login on the real hostname. No Azure smoke test has yet run. `/` may need an explicit landing route check; do not treat only the static Viewer URL as proof of the whole application. Do not send unnecessary emails.

## A–O status

| Item | Result |
| --- | --- |
| A. Subscription/group/region | None used; proposed dedicated group rg-agriloco-prod / Canada Central, uncreated and unverified against subscription availability |
| B. SQL resources | None created |
| C. Schema | Local script verified; not applied remotely |
| D. Farm 1 migration | Source counts verified; no Azure import |
| E. App Service | None created |
| F. Settings | Names/values plan known; none configured remotely |
| G. Deployment | Fresh publish and ZIP ready; not deployed |
| H. Public Azure URL | Not available; no hostname invented |
| I. Viewer URL | Pending actual hostname; final path must be /viewer/?farmId=1 |
| J. API tests | Azure tests not run |
| K. Authentication tests | Azure tests not run; prior local audit passed |
| L. Uploads | Basemap verified in publish; no cloud upload; V1 plan uses App Service content |
| M. Limits | Missing Azure tools/context, pending import/deployment/remote validation; standalone Unity Editor remains a separate integration |
| N. Phone test | After Azure tests pass, turn Wi-Fi off, open the actual HTTPS Viewer URL in a private tab, confirm Wheelbarrow Orchards/basemap/features and selections; report phone/browser result without changing DNS |
| O. Domain recommendation | **NO-GO for connecting agriloco.ca now**: no deployed/tested Azure environment exists |

No application source/configuration, original database, unrelated Azure resources, DNS or Git history was changed in this step. Only this status document and local build/publish/package artifacts were created or refreshed.
