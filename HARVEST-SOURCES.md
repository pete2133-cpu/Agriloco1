# Harvest source selection at creation

The initial harvest form now selects Crop > Variety > Row/location from the current farm's saved dashboard Product, Variety and Location records. No database migration or Unity change.

## Behavior

- Active saved records are available regardless of public visibility or availability status, since crops can be harvested before being offered for sale.
- Variety choices depend on the crop; row choices depend on the crop and variety. Changing a parent clears dependent selections.
- A saved variety/row is required when choices exist. A crop without saved varieties or locations can still be harvested without inventing details.
- One row per harvest lot keeps its quantity attributable to that row. Save a separate lot for another row.
- The server validates the selected record IDs and relationships within the selected farm. Invalid, archived, cross-farm or mismatched selections are rejected.
- CropName, VarietyName and Location are saved at creation in the existing harvest fields. They are historical name snapshots, not new foreign-key columns; later dashboard renames do not rewrite earlier harvests.
- Date, quantity and selections remain on validation errors. Search includes location. Farm context is preserved through list actions, details links and the return from details.
- Existing harvest records and later editing remain available. No inventory calculations, APIs, email or Unity code changed.

The current saved Golden Delicious locations are Row 21, Row 22 and Row 39. Row 23 was not added or substituted.

## Verification

- 15 checks against a SQLite backup passed: valid creation, stored names/location, quantity/date/unit/farm, lot numbering, required row/variety, retained inputs on errors, mismatched row, cross-farm IDs, invalid quantity, stale/archived sources, private/unavailable sources, no-child crops and cycle protection. All fixture changes rolled back.
- Browser test against that disposable copy saved Apple / Golden Delicious / Row 21 / 12.5 kg directly from the first form; the new list entry immediately contained the row.
- Browser verified that changing crops resets variety/row, and reviewed the form at desktop and 390px width.
- Build passed with zero errors; a full compilation reports the same two pre-existing nullable warnings in FarmControllers.cs. Incremental verification also passed.
- The working database was not edited by tests. No email was sent.

## Test in Visual Studio

1. Stop debugging, rebuild C:\Users\pete2\source\repos\ag3\Agriloco1.sln, then press F5.
2. Open Inventory & production > Harvests.
3. Choose Apple, then Golden Delicious. Choose one of its saved rows.
4. Enter quantity, unit and date; click Save harvest.
5. Confirm the new entry already shows the crop, variety and location. The Details page is not required for initial source selection.
6. Try another crop and confirm its own varieties/locations appear. Old harvest records remain unchanged.

## Files changed

- Pages/Farmer/Inventory/HarvestLots.cshtml
- Pages/Farmer/Inventory/HarvestLots.cshtml.cs
- Pages/Farmer/Inventory/HarvestLotDetails.cshtml
- Pages/Farmer/Inventory/HarvestLotDetails.cshtml.cs
- Services/HarvestSourceCatalog.cs
- wwwroot/js/harvest-sources.js
- wwwroot/css/v1.css
- HARVEST-SOURCES.md