# Agriloco website V1 cleanup

Branch: v1/website-cleanup. Local only; nothing pushed or deployed.

## What changed

- Shared typography, green/neutral palette, spacing, buttons, input styling, focus states and responsive tables. No external UI framework or build tool is required.
- Public navigation focuses on finding food. Member navigation groups availability, crops/products, inventory/production and the public farm page. Existing secondary tools remain accessible.
- Dashboard availability stays above collapsed basemap setup. Status/sales/visibility controls are labeled on phones; location rows are visually secondary. Upload timestamp renders correctly and the file path is no longer prominent.
- Record Activity opens a navigation page for the existing harvest, receiving and production workflows. The previously broken Open Map destination is replaced by View public farm, which includes the existing ASP.NET map. No Unity export was changed.
- Search has a prominent filter form and scannable cards showing crop, variety, availability, buying method, farm name/address and a farm link. Empty and connection-failure messages are clearer.
- Public farm pages use consistent contact details, food listings and availability badges while retaining the map and email controls.
- Crop/product forms use plainer labels. On phones, the add/edit form appears before the long existing-product list.
- Inventory landing, records and finished-products pages now guide users to existing workflows. Shared inventory navigation, compact forms and horizontally scrollable tables apply across the existing list/detail pages.
- Visual Studio launch profiles open Search/Crops instead of Swagger. Existing ports and environments are preserved.

## Small presentation-support code change

Pages/Search/Crops.cshtml.cs now uses the current website origin for its existing API calls, allowing the project profile and IIS Express to work without a separate server on port 5227. A read-only farm metadata query supplies names/addresses to result cards. Network/JSON errors produce a readable message. API routes, contracts, result filtering and database models are unchanged.

## Deliberately unchanged

Unity, map geometry, API controllers/DTOs, database/schema, inventory calculations and transactions, recipes/traceability logic, authentication, GPS and email delivery. Older disabled or empty specialist pages such as the obsolete Map/FarmMap viewer and standalone Units/Packages were not rebuilt. Existing stock-item workflows provide units/packaging access. Search continues to use its existing crop source; it has not been merged with the dashboard data model.

The existing uncommitted agriloco.db change was preserved and excluded from the commit. No application records were edited during verification.

## Verification

- Final command: dotnet build Agriloco1.csproj --no-restore --nologo -v:q -o bin/V1Final
- Result: zero errors; two existing nullable warnings in Controllers/FarmControllers.cs (lines 85 and 115).
- Standard output was locked by an already-running app, so final verification used a separate output directory. Stop debugging before rebuilding in Visual Studio.
- Local preview at http://localhost:5240 used a disposable database copy, not the working database.
- 16 GET checks succeeded: search (including a no-match query), dashboard, public farm, add product, search listings, registration, inventory overview/items, receiving, harvests, production, recipes, suppliers, records and finished products.
- Browser checks at desktop and 390px width covered dashboard, search, public farm, add/edit product and receiving. Confirmed basemap expansion and corrected mobile field overflow.
- Existing member asp-for bindings, handler names and auto-submit hooks were compared with the baseline and preserved.
- No POST transactions, registration, uploads or notification requests were submitted. End-to-end saving, validation after submission, inventory transactions, live email, actual IIS Express launch and every detail-record combination remain for user testing. External map tile availability is not guaranteed by these checks.

## Test from Visual Studio

1. Open C:\Users\pete2\source\repos\ag3\Agriloco1.sln (the ag3 working folder, branch v1/website-cleanup).
2. Stop any existing debugging session, then build the solution.
3. Choose IIS Express or the Agriloco1 project profile and press F5. Both launch the public search page. The project profile remains http://localhost:5227; IIS Express retains its existing configured ports.
4. Search for Apple or a variety. Check availability and buying-method filters, clear filters using the empty-state link, and open a farm result.
5. Open Farmer dashboard. Verify availability comes first, then expand Farm basemap setup near the bottom. Review labels and layout on a narrow browser window.
6. Open Add crop or product. Check the existing create/edit fields and that the form appears first on mobile.
7. Open Inventory & production, Receiving, Harvests, Production, Recipes, Suppliers, Records and Finished products. Check navigation, forms and table scrolling.
8. Test saves or uploads only with changes you intend to make. Existing availability actions can trigger the existing email behavior; this cleanup does not change that behavior.

## Every file changed
- Pages/Farmer/Add.cshtml
- Pages/Farmer/Crops.cshtml
- Pages/Farmer/Dashboard.cshtml
- Pages/Farmer/Inventory/FinishedProducts.cshtml
- Pages/Farmer/Inventory/HarvestLotDetails.cshtml
- Pages/Farmer/Inventory/HarvestLots.cshtml
- Pages/Farmer/Inventory/Index.cshtml
- Pages/Farmer/Inventory/InventoryItemDetails.cshtml
- Pages/Farmer/Inventory/InventoryItems.cshtml
- Pages/Farmer/Inventory/ProcessRecords.cshtml
- Pages/Farmer/Inventory/ProductionRunDetails.cshtml
- Pages/Farmer/Inventory/ProductionRuns.cshtml
- Pages/Farmer/Inventory/ReceivingLotDetails.cshtml
- Pages/Farmer/Inventory/ReceivingLots.cshtml
- Pages/Farmer/Inventory/RecipeDetails.cshtml
- Pages/Farmer/Inventory/Recipes.cshtml
- Pages/Farmer/Inventory/SupplierDetails.cshtml
- Pages/Farmer/Inventory/Suppliers.cshtml
- Pages/Farmer/Register.cshtml
- Pages/Public/Farm.cshtml
- Pages/Search/Crops.cshtml
- Pages/Search/Crops.cshtml.cs
- Pages/Shared/_InventoryNav.cshtml
- Pages/Shared/_Layout.cshtml
- Properties/launchSettings.json
- V1-CLEANUP.md
- wwwroot/css/v1.css
- wwwroot/js/v1.js
