# Focused public-launch audit

September 9, 2026. Scope: obvious public-launch exposures, not a general penetration test or authentication redesign. No Git operations, Azure resources/deployment, remote schema changes, DNS changes or real test emails.

## A. Public functionality

The public Viewer, farm/published map GETs, public farm/search/catalog/palette/legacy map data, Member/farm signup and both availability subscription routes remain anonymous. The requested PUBLIC READ category below includes explicitly intended public signup POSTs. Farm contacts deliberately listed for customers remain public; private Member contact records do not.

## B. Farmer management

The existing Member cookie and Member.FarmId ownership check now cover GET and POST handlers throughout /Farmer, except public Login/Register and the antiforgery-protected sign-out page. All 27 management pages returned 401 anonymously. Farm-specific pages verify active Member-to-Farm ownership before their handler executes. Pages without farm data still require active membership. Crop create/update/byFarm and draft reads are protected. Crop updates resolve the crop's actual farm from its ID, rather than trusting a supplied farm ID. Member GETs require the active signed-in Member's own ID. The harvest-detail handler now checks the record's farm for GET and POST, closing a separate record-ID bypass.

Internal farmer HTTP calls forward the current cookie to the configured trusted application origin, with cookie storage/redirects disabled. This preserves GridPainter and legacy crop editing. No admin or multi-farm role was invented.

## C. Development tools

EmailTest config-check/send, Alerts debug list/enqueue, SearchAnalytics and global catalog mutation tools are unavailable outside Development (404), including /Admin/CropCatalog. There is no production admin grant model to authorize global catalog editing safely. Development tools also require login; EmailTest send is now POST with antiforgery validation. The former GET send route cannot send mail. Swagger remains Development-only. WeatherForecast and CropSearchControllers source files are excluded from compilation; /WeatherForecast is absent. WeatherForecast retains a defensive DevelopmentOnly attribute despite being excluded.

## D. Email

The arbitrary-recipient test sender and subscription debug listing/enqueue were the clear exposures; their production entry points are disabled. Legitimate availability subscription behavior and the working SMTP/notification/queue implementation were preserved. Legacy subscribe-crop now validates a single email address, rejecting multiple recipients/display-name input, matching the intent of the existing newer signup validation. Crop availability and Dashboard availability changes require ownership before notification code can execute. No real test email was sent; tests used example.invalid recipients in isolated databases and loopback SMTP configuration. Actual hosted SMTP delivery is not certified by this audit.

## E. Secret scan

Local appsettings.json contains nonempty SMTP password and geocoding-key settings. Values were never printed. The file remains available for local development and excluded from publish, alongside appsettings.Development.json. Production settings come from App Service/environment configuration. Scanned source/configuration for credential literals and settings, without using Git to determine tracked status or inspect history. Scanned all **163 final published files**, including decompressed Brotli/GZip content, for the known local secret values in UTF-8 and UTF-16: zero matches. No development settings, SQLite databases, private tools or build staging directories were published. This does not claim that unknown secrets or prior repository history were exhaustively audited; keep local secret configuration out of commits.

## F. Legacy password migration

Tools/PrepareMemberMigration creates a fresh sanitized logical SQLite copy from a read-only consistent source transaction. It uses the application's MemberPasswords hasher and verifies each conversion in memory; no plaintext credential is inserted into the destination or journal. Existing marked hashes are retained. Unsupported legacy formats fail closed before output creation. It refuses an existing/same destination. It neither starts the app nor connects to SQL Server/email. Failure logs omit exception details that could contain data.

One real legacy Member was converted in a separate proof copy. Every business row/ID matched apart from PasswordHash/PasswordSalt; legacy password bytes were absent from destination bytes. Original development credentials were unchanged. A separately converted synthetic Member successfully logged into the running website. New public registration also stores a marked hash. Import the converted BLOB bytes into SQL Server, not the legacy source password. See AZURE-DEPLOYMENT-HANDOFF.md for the offline command and import boundaries.

## G. DTO/privacy findings

No password/hash/salt is returned by the inspected public API DTOs. Member email/alternate email/phone/username lookups are now self-only. Public crop projections no longer populate private notes or internal inventory source, external IDs, quantities, status or sync timestamps; the protected byFarm projection retains management data. The public farm page no longer populates those internal fields either. Anonymous Unity farm/published responses filter hidden/inactive definitions, hidden ancestors and their referenced features/labels; the active owning farmer retains private definition access. Anonymous draft reads are protected. Intentional published geometry, public farm contact/address data and customer availability information remain available.

## H. Production behavior

The actual published application started in Production using SqlServer settings and a deliberately unreachable local SQL endpoint: startup did not connect to/apply schema. Development exceptions and Swagger were unavailable. A deliberately failing SQL-backed GET returned only a generic 500 JSON message, without stack/connection details. HTTPS redirection and HSTS remain configured. HSTS excludes localhost by ASP.NET default; a custom-Host attempt correctly failed TLS name validation, so no TLS bypass was used. Hosted HSTS/redirect validation is explicitly in the handoff. Windows IIS scheme forwarding and persistent keys/uploads must be confirmed on the actual host. Production SQLite remains disallowed.

## I. Validation

- Restore passed. Final Release build/publish passed, zero errors; three existing compile warnings on a full build (two nullable assignments and one async-without-await inventory handler).
- 66 initial HTTP assertions passed: public endpoints/signup; all 27 management pages denied anonymously; private APIs denied; converted login; authorized management reads; second-farm/member rejection; crop create; private crop values absent from public JSON.
- Additional checks passed: foreign harvest GET/POST with own farmId returned 404; foreign crop update returned 403; authenticated Razor crop save returned 200/Saved; logout revoked access.
- Final privacy checks: hidden definition/features omitted anonymously and retained for owner; valid legacy signup 200, multiple-recipient signup 400; final Farm 1 returned 80 public definitions and 58 published features.
- Anonymous real WebGL rendered 58 features and a 921x568 basemap. All **17 Viewer files** match final publish byte-for-byte.
- Production checks: all tested debug/catalog/analytics/Admin routes 404; Swagger/WeatherForecast 404; anonymous management/member/map writes 401; /viewer/ 200; deliberate SQL failure generic 500. Production route tests used no functioning database and do not substitute for Azure SQL data smoke tests.
- SQL Server model reports no pending changes; fresh idempotent schema script generated locally, never applied remotely.
- Offline migration negative checks passed: unsupported salted credentials failed before output creation; an existing/same destination was refused without changing its hash.
- Audit runtime data, sanitized proof copy, HTTP scripts/results, logs, schema and final publish are in .codex-build/launch-audit. Normal bin/obj artifacts changed. Those are excluded local artifacts, not source to deploy or commit.

## J–L. Recommendation and next action

**GO for controlled first Azure deployment/validation; NO-GO for immediate agriloco.ca cutover.** No remaining obvious anonymous-management/debug/credential blocker was identified within this focused audit. Complete actual SQL schema/import/identity/network, persistent upload and login-key, hosted HTTPS, default-host and cellular phone acceptance checks before DNS. If farmer Unity Editor saving is required, its separate production build must derive the API origin from the authenticated website and launch with the authorized farm (Farm 1: ?farmId=1); standalone Unity does not inherit browser cookies. That separate Editor work was not changed or certified here.

The next action is review in Visual Studio, confirm subscription/region/resource names/budget, then separately authorize **resource group creation**. The ordered 12-step handoff is in AZURE-DEPLOYMENT-HANDOFF.md. No commit or deployment was performed.

## Reachable API inventory

Generated from the running application's Swagger endpoint after protections were added; excluded source controllers are not counted. 42 method/route combinations:

| Method | Route | Classification | Gate/intent |
| --- | --- | --- | --- |
| POST | `/api/Alerts/subscribe-crop` | PUBLIC READ | Intended anonymous signup |
| GET | `/api/Alerts/debug/list` | DEBUG/DEVELOPMENT | 404 outside Development; login required in Development |
| POST | `/api/Alerts/debug/enqueue` | DEBUG/DEVELOPMENT | 404 outside Development; login required in Development |
| POST | `/api/AvailabilitySubscriptions` | PUBLIC READ | Intended anonymous signup |
| GET | `/api/CropCatalog/categories` | PUBLIC READ | Intended anonymous customer data |
| GET | `/api/CropCatalog/varieties` | PUBLIC READ | Intended anonymous customer data |
| POST | `/api/CropCatalog/category` | DEBUG/DEVELOPMENT | 404 outside Development; login required in Development |
| POST | `/api/CropCatalog/variety` | DEBUG/DEVELOPMENT | 404 outside Development; login required in Development |
| GET | `/api/CropCatalogAliases` | PUBLIC READ | Intended anonymous customer data |
| POST | `/api/CropCatalogAliases` | DEBUG/DEVELOPMENT | 404 outside Development; login required in Development |
| GET | `/api/Crops/categories` | PUBLIC READ | Intended anonymous customer data |
| GET | `/api/Crops/varieties` | PUBLIC READ | Intended anonymous customer data |
| POST | `/api/Crops` | AUTHENTICATED FARM MANAGEMENT | Active Member/Farm ownership; unsafe requests also require same-origin Origin |
| GET | `/api/Crops/{id}` | PUBLIC READ | Intended anonymous customer data |
| GET | `/api/Crops/public` | PUBLIC READ | Intended anonymous customer data |
| GET | `/api/Crops/byFarm/{farmId}` | AUTHENTICATED FARM MANAGEMENT | Active Member/Farm ownership; unsafe requests also require same-origin Origin |
| PUT | `/api/Crops/{id}/availability` | AUTHENTICATED FARM MANAGEMENT | Active Member/Farm ownership; unsafe requests also require same-origin Origin |
| PUT | `/api/Crops/{id}/details` | AUTHENTICATED FARM MANAGEMENT | Active Member/Farm ownership; unsafe requests also require same-origin Origin |
| GET | `/api/EmailTest/config-check` | DEBUG/DEVELOPMENT | 404 outside Development; login required in Development |
| POST | `/api/EmailTest/send` | DEBUG/DEVELOPMENT | 404 outside Development; login required in Development |
| POST | `/api/farms/{id}/geocode-address` | AUTHENTICATED FARM MANAGEMENT | Active Member/Farm ownership; unsafe requests also require same-origin Origin |
| POST | `/api/Farms/register_minimal` | PUBLIC READ | Intended anonymous signup |
| GET | `/api/Farms/public` | PUBLIC READ | Intended anonymous customer data |
| GET | `/api/Farms/{id}/public_single` | PUBLIC READ | Intended anonymous customer data |
| GET | `/api/Farms/{id}/map-layout` | PUBLIC READ | Intended anonymous customer data |
| POST | `/api/Farms/{id}/map-layout` | AUTHENTICATED FARM MANAGEMENT | Active Member/Farm ownership; unsafe requests also require same-origin Origin |
| GET | `/api/Farms/{id}/map-image` | PUBLIC READ | Intended anonymous customer data |
| GET | `/api/Map/cells` | PUBLIC READ | Intended anonymous customer data |
| POST | `/api/Map/cells` | AUTHENTICATED FARM MANAGEMENT | Active Member/Farm ownership; unsafe requests also require same-origin Origin |
| DELETE | `/api/Map/cells` | AUTHENTICATED FARM MANAGEMENT | Active Member/Farm ownership; unsafe requests also require same-origin Origin |
| POST | `/api/member/register` | PUBLIC READ | Intended anonymous signup |
| GET | `/api/member/{id}` | AUTHENTICATED FARM MANAGEMENT | Self-only active Member; no other-member contact lookup |
| GET | `/api/member/byusername/{username}` | AUTHENTICATED FARM MANAGEMENT | Self-only active Member; no other-member contact lookup |
| GET | `/api/Palette` | PUBLIC READ | Intended anonymous customer data |
| GET | `/api/SearchAnalytics/term` | DEBUG/DEVELOPMENT | 404 outside Development; login required in Development |
| GET | `/api/SearchAnalytics/top` | DEBUG/DEVELOPMENT | 404 outside Development; login required in Development |
| GET | `/api/UnityMap/farm` | PUBLIC READ | Intended anonymous customer data |
| GET | `/api/UnityMap/draft` | AUTHENTICATED FARM MANAGEMENT | Active Member/Farm ownership; unsafe requests also require same-origin Origin |
| POST | `/api/UnityMap/draft` | AUTHENTICATED FARM MANAGEMENT | Active Member/Farm ownership; unsafe requests also require same-origin Origin |
| POST | `/api/UnityMap/publish` | AUTHENTICATED FARM MANAGEMENT | Active Member/Farm ownership; unsafe requests also require same-origin Origin |
| GET | `/api/UnityMap/published` | PUBLIC READ | Intended anonymous customer data |
| GET | `/api/UnityMap/cells` | PUBLIC READ | Intended anonymous customer data |

## Reachable Razor page/handler inventory

Shared layouts/imports are not routes. Pages/Dashboard is excluded by the project. The disabled /Map/FarmMap and /Farmer/AddMapCell pages have no write behavior.

| Page | Handlers (Async suffix omitted) | Classification |
| --- | --- | --- |
| `/Admin/CropCatalog` | OnGet, OnPost | DEBUG/DEVELOPMENT |
| `/Farmer/Add` | OnGet, OnPostCreate, OnPostEdit, OnPostDelete | AUTHENTICATED FARM MANAGEMENT |
| `/Farmer/AddCropAsset` | OnGet, OnPost | AUTHENTICATED FARM MANAGEMENT |
| `/Farmer/AddMapCell` | OnGet | AUTHENTICATED FARM MANAGEMENT |
| `/Farmer/Crops` | OnGet, OnPost | AUTHENTICATED FARM MANAGEMENT |
| `/Farmer/Dashboard` | OnGet, OnPostStatus, OnPostChannel, OnPostPublic, OnPostBasemap | AUTHENTICATED FARM MANAGEMENT |
| `/Farmer/GridPainter` | OnGet, OnPost | AUTHENTICATED FARM MANAGEMENT |
| `/Farmer/Inventory/FinishedProducts` | OnGet | AUTHENTICATED FARM MANAGEMENT |
| `/Farmer/Inventory/HarvestLabel` | OnGet | AUTHENTICATED FARM MANAGEMENT |
| `/Farmer/Inventory/HarvestLotDetails` | OnGet, OnPost | AUTHENTICATED FARM MANAGEMENT |
| `/Farmer/Inventory/HarvestLots` | OnGet, OnPostCreate, OnPostToggleStatus, OnPostDelete | AUTHENTICATED FARM MANAGEMENT |
| `/Farmer/Inventory/Index` | OnGet | AUTHENTICATED FARM MANAGEMENT |
| `/Farmer/Inventory/InventoryItemDetails` | OnGet, OnPostSaveItem, OnPostAddPackage, OnPostTogglePackage, OnPostDeletePackage | AUTHENTICATED FARM MANAGEMENT |
| `/Farmer/Inventory/InventoryItems` | OnGet, OnPostCreate, OnPostToggleActive, OnPostDelete | AUTHENTICATED FARM MANAGEMENT |
| `/Farmer/Inventory/Packages` | OnGet | AUTHENTICATED FARM MANAGEMENT |
| `/Farmer/Inventory/ProcessRecords` | OnGet | AUTHENTICATED FARM MANAGEMENT |
| `/Farmer/Inventory/ProductionRunDetails` | OnGet, OnPostLoadRecipe, OnPostSaveSource, OnPostSaveHeader, OnPostSaveIngredient, OnPostResetScale, OnPostStartBatch, OnPostSaveProgress, OnPostCompleteBatch, OnPostCancelBatch, OnPostAddWorkEntry, OnPostAddWorker, OnPostUpdateWorker, OnPostAddCheck, OnPostAddObservationValue | AUTHENTICATED FARM MANAGEMENT |
| `/Farmer/Inventory/ProductionRuns` | OnGet, OnPostCreate, OnPostDelete | AUTHENTICATED FARM MANAGEMENT |
| `/Farmer/Inventory/ReceivingLabel` | OnGet | AUTHENTICATED FARM MANAGEMENT |
| `/Farmer/Inventory/ReceivingLotDetails` | OnGet, OnPostSave, OnPostAddCustomField, OnPostDeleteCustomField | AUTHENTICATED FARM MANAGEMENT |
| `/Farmer/Inventory/ReceivingLots` | OnGet, OnPostCreate, OnPostDelete | AUTHENTICATED FARM MANAGEMENT |
| `/Farmer/Inventory/RecipeDetails` | OnGet, OnPostSaveRecipe, OnPostAddIngredient, OnPostDeleteIngredient, OnPostMoveIngredientUp, OnPostMoveIngredientDown | AUTHENTICATED FARM MANAGEMENT |
| `/Farmer/Inventory/Recipes` | OnGet, OnPostCreate, OnPostToggleActive, OnPostDelete | AUTHENTICATED FARM MANAGEMENT |
| `/Farmer/Inventory/SupplierDetails` | OnGet, OnPost | AUTHENTICATED FARM MANAGEMENT |
| `/Farmer/Inventory/Suppliers` | OnGet, OnPostCreate, OnPostToggleActive, OnPostDelete | AUTHENTICATED FARM MANAGEMENT |
| `/Farmer/Inventory/Units` | OnGet | AUTHENTICATED FARM MANAGEMENT |
| `/Farmer/Login` | OnPost | PUBLIC READ |
| `/Farmer/Logout` | OnPost | AUTHENTICATED FARM MANAGEMENT |
| `/Farmer/ManageCropAssets` | OnGet, OnPostSaveRow | AUTHENTICATED FARM MANAGEMENT |
| `/Farmer/MapImage` | OnGet, OnPost | AUTHENTICATED FARM MANAGEMENT |
| `/Farmer/Register` | OnGet, OnPost | PUBLIC READ |
| `/Map/FarmMap` | OnGet | PUBLIC READ |
| `/Public/Farm` | OnGet | PUBLIC READ |
| `/Search/Crops` | OnGet | PUBLIC READ |

Logout is safe without an authenticated cookie: its POST is antiforgery-protected and only clears the caller's cookie; it exposes no farm data. Global /Farmer protection includes newly added future management pages by convention. Public registration/login are explicit exceptions.

## Source files changed in this audit

- `Agriloco1.csproj`
- `Program.cs`
- `Security/FarmMapWriteAttribute.cs`
- `Security/DevelopmentOnlyAttribute.cs`
- `Security/MemberApiHandler.cs`
- `Controllers/AlertsController.cs`
- `Controllers/CropCatalogController.cs`
- `Controllers/CropCatelogAliasesController.cs`
- `Controllers/CropsController.cs`
- `Controllers/EmailTestController.cs`
- `Controllers/MembersController.cs`
- `Controllers/SearchAnalyticsController.cs`
- `Controllers/UnityMapController.cs`
- `Controllers/WeatherForecastController.cs`
- `Pages/Admin/CropCatalog.cshtml.cs`
- `Pages/Farmer/Crops.cshtml.cs`
- `Pages/Farmer/Register.cshtml.cs`
- `Pages/Farmer/AddCropAsset.cshtml.cs`
- `Pages/Farmer/Inventory/HarvestLotDetails.cshtml.cs`
- `Pages/Public/Farm.cshtml.cs`
- `Tools/PrepareMemberMigration/PrepareMemberMigration.csproj`
- `Tools/PrepareMemberMigration/Program.cs`
- `AZURE-DEPLOYMENT-HANDOFF.md`
- `PUBLIC-LAUNCH-AUDIT.md`

Automatic approval review rejected removing a defensive attribute from excluded WeatherForecast source during cleanup. The protection was retained and all requested work continued; nothing remains blocked by that rejection.
