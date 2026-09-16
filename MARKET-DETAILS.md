# Optional Market Details

## Changed project files

- `Models/MarketDetails.cs` (new)
- `Data/AgrilocoContext.cs`
- `Services/MarketSchema.cs` (new)
- `Program.cs`
- `Pages/Farmer/Dashboard.cshtml`
- `Pages/Farmer/MarketDetails.cshtml` (new)
- `Pages/Farmer/MarketDetails.cshtml.cs` (new)
- `Pages/Farmer/_MarketOption.cshtml` (new)
- `wwwroot/css/market-details.css` (new)
- `wwwroot/js/market-details.js` (new)
- `Controllers/UnityMapController.cs`
- `wwwroot/viewer/index.html`
- `wwwroot/viewer/viewer.css`
- `wwwroot/viewer/viewer.js`
- `Migrations/SqlServer/20260916004244_AddMarketDetails.cs` (new)
- `Migrations/SqlServer/20260916004244_AddMarketDetails.Designer.cs` (new)
- `Migrations/SqlServer/SqlServerAgrilocoContextModelSnapshot.cs`
- `MARKET-DETAILS.md` (this file)

Local validation artifacts (not application source) are under `.codex-build/market-details/`, including the HTTP regression script, isolated SQLite database, logs and publish output. The generated incremental SQL review artifact is `.codex-build/market/AddMarketDetails.sql`.

## Data and behavior

- `MarketDetails.FarmDefinitionId` is both its primary key and a foreign key to the existing farm item. No second catalog is created. Display name, description, category and image are optional overrides.
- Market inclusion still comes exclusively from an enabled `FarmStore` channel assignment, subject to the existing active/public item and ancestor rules. Details and selling options are not required. External `FarmersMarket` assignments are not on-farm store assignments.
- Availability remains `FarmDefinition.Status`; no status is stored in either new table.
- `MarketSellingOptions` has stable, Agriloco-owned integer IDs, an item foreign key, optional name/price/quantity/unit/package, currency (CAD default), separate private SKU/barcode fields, visibility, sort order and an inventory-tracking preference.
- `StockUnit` and `StockQuantityPerSale` are nullable future conversion metadata. They are not inferred from selling units and are not currently edited or consumed. No stock deduction or inventory synchronization occurs. Existing inventory items/packages have independent identities; this change does not pretend there is an existing link to farm definitions.
- Future provider mappings can reference a selling option ID with provider/external item/external variant IDs without replacing Agriloco IDs. No provider dependency or empty provider-mapping table was added.
- Optional category is a listing override, not a new category-management system. Blank category uses the existing crop root; blank name uses the existing item name. An existing linked master-definition description remains a fallback.
- Public APIs explicitly project only customer fields. SKU, barcode, tracking, stock conversion and internal notes are never returned. The image route checks current farm, visibility and FarmStore assignment, including for signed-in users.
- Uploaded PNG/JPEG/WebP files (maximum 5 MB, signature-checked) are stored in the database, not ephemeral App Service files. Replacing/removing an image does not affect the basemap. Public images use a separate, no-store route.
- Editor GET, POST and preview-image handlers use existing Member farm ownership protection. POST also requires same-origin and Razor antiforgery validation. Submitted selling-option IDs must belong to the selected farm item.

## Manual SQL Server deployment

No migration or deployment has been applied remotely.

1. Back up the target database and confirm the currently deployed migration is `20260913234431_AddFarmGeoreference` (or that all migrations through it have been applied). Do not run the older initial schema against an existing database without its established baseline/history.
2. From the deployment project, build and generate a reviewable upgrade script (these commands do not connect to Azure):

   ```powershell
   dotnet build Agriloco1.csproj -c Release
   dotnet ef migrations script 20260913234431_AddFarmGeoreference 20260916004244_AddMarketDetails --context SqlServerAgrilocoContext --configuration Release --no-build --idempotent --output AddMarketDetails.sql
   ```

3. Review `AddMarketDetails.sql`. It should create only `MarketDetails`, `MarketSellingOptions`, their foreign keys/index and migration-history entry. Apply it yourself to the intended SQL Server database through your normal approved database tooling. Existing farm rows are not updated or seeded by this migration.
4. Publish the updated ASP.NET application **after** adding the tables. The new Market query expects them. Existing items need no enrichment/backfill to keep appearing.
5. Smoke-test a FarmStore-only item, edit/save optional details, check the anonymous Market, then switch back to Farm Map. Confirm the new CSS/JS versions load. No Unity rebuild, map republish, DNS or POS setup is needed.

Local SQLite development follows the project's existing additive startup-DDL convention through `MarketSchema`; production does not run that helper. Do not apply the legacy SQLite EF migration chain to SQL Server.

If an application rollback is necessary, keep the new additive tables and roll back the binaries. Dropping these tables would delete the optional details/images/options.

## Validation performed

- Release build, JavaScript syntax checks, SQL Server incremental idempotent script generation and Release publish.
- Real HTTP GET/POST checks against an isolated SQLite fixture, including zero-detail listing, descriptions, image upload/read, multiple price/unit/package options, stable option IDs, invalid prices, unknown/foreign IDs, anonymous/ownership/Origin/antiforgery rejections and safe public projection.
- Existing Dashboard FarmStore and status handlers exercised. FarmStore removal also denies public image access. Second-farm empty Market returned successfully with no cross-farm data.
- Desktop and 390px editor/public layouts, add/remove-option controls, mobile save, category filtering, empty state and return to map checked in the browser.
- Simulated GPS watch callbacks through the unchanged viewer: position and accuracy circle, movement while in Market, return to map, stop/restart and permission error. No physical on-farm GPS test was performed.
- Crop/Variety/Today's Availability selections persisted across Market navigation. Original dashboard form blocks and backend handlers remained unchanged.

Test data, images, memberships and GPS reference points used for validation were confined to `.codex-build/market-details/test.db`; production data was not changed. The temporary browser GPS harness was removed before publish.
