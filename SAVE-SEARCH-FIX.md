# Dashboard saving and database search fix

## Changes

- Dashboard hidden booleans now submit explicit true/false strings. Previously Razor rendered value="value", which could not bind to the save handler. Both sales-method and public-visibility controls are corrected.
- Website search now reads active/public Product and Variety records from FarmDefinitions as well as legacy Crops. No tables, migrations, API routes or DTO contracts changed.
- Varieties use their product ancestor as the crop name. Apple/apples and partial variety-name searches work. Location rows are not presented as separate foods.
- Hidden/inactive ancestors suppress their children. Legacy duplicates cannot override a dashboard entry's visibility or status. Legacy-only entries remain searchable.
- Search sales-method filters use each entry's own saved channel settings (no new inheritance rule). Available uses the same four statuses as the dashboard. Coming Soon has its own filter.
- Public farm food listings use the same reader as search. Existing map rendering still uses the original legacy data/IDs. Definition IDs are never passed to legacy email subscription endpoints; existing matching legacy crop IDs retain their existing email controls. This does not add email subscription support for definition-only entries.

## Verification

All mutation tests used a consistent SQLite backup of the working database. The working database was not changed by these tests.

- Actual GET/POST/reload tests: Pick Your Own, Farm Store and Wholesale each saved on and off (six checks). Public visibility saved off and on (two checks). Test states restored.
- 12 reader regression checks passed: ancestor naming, all varieties, duplicate handling, dashboard status precedence, location exclusion, plural/partial matching, availability semantics, private/inactive entries, hidden ancestors, enabled/disabled sales channels. Fixture mutations rolled back.
- Apple and apples each returned 20 listings: the product plus 19 varieties. Honeycrisp returned one. Gala partial matching also matches Golden Gala. Coming Soon returned the current matching entry. The public farm page includes the varieties.
- Final build succeeded with zero errors and the two existing nullable warnings in FarmControllers.cs.
- No status-change notifications or email requests were submitted. No Unity files were touched. Nothing pushed.

## Test in Visual Studio

1. Stop debugging, rebuild C:\Users\pete2\source\repos\ag3\Agriloco1.sln, then press F5.
2. In Farmer Dashboard, toggle Pick Your Own, Farm Store or Wholesale on a row. Reload and confirm the selection remains; toggle it off and reload again.
3. Search Apple, apples, Honeycrisp or another variety. Keep Availability set to Any to include unavailable and coming-soon varieties.
4. Choose a sales method to see entries with that method saved. Open a result and confirm the farm page shows its varieties.

## Files

- Pages/Farmer/Dashboard.cshtml
- Pages/Search/Crops.cshtml
- Pages/Search/Crops.cshtml.cs
- Pages/Public/Farm.cshtml
- Pages/Public/Farm.cshtml.cs
- Services/PublicFoodCatalog.cs
- SAVE-SEARCH-FIX.md