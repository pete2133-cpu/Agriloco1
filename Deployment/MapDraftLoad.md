# Farm map load schema diagnostic

Unity requests GET /api/UnityMap/draft?farmId=1 with Authorization: Bearer <in-memory access token>.
FarmMapWrite authorizes the member and farm ownership, then UnityMapController.GetDraft calls GetSavedMap.
The shared draft/published reader materializes FarmMapFeatures including nullable LabelLayoutJson.

Observed production: /farm returns 200 and includes Pathway definition 96 (Pumpkin Path); /published returns 500.
The earlier successful published snapshot contained 30 features and did not expose LabelLayoutJson.
Production exception and schema confirmation are still required: the generic 500 response cannot prove its cause.

A disposable SQL Server regression reproduces farm=200, authenticated draft=500, published=500 when
LabelLayoutJson is missing, with SqlException 207: Invalid column name 'LabelLayoutJson'.
Applying existing migration 20260913153926_AddMapLabelLayout restores draft/published=200 and leaves
all feature IDs, ordered point IDs and exact coordinates unchanged. Pathway remains present. Anonymous draft=401.

Run CheckMapSchema.sql read-only in Azure SQL, or obtain the server exception from App Service logs.
If the column and migration are missing and the exception matches, use AddMapLabelLayout.sql,
generated from the EXISTING SqlServerAgrilocoContext migration. Review and apply to the correct database.
It only adds one nullable nvarchar(max) column and records the migration; it does not reset maps or change geometry.
Do not apply the Down migration. Do not create a duplicate migration. If schema/history disagree, investigate first.
No SQL has been applied to Azure by this task.

If confirmed, the already-deployed backend needs this database migration; another ASP.NET publish is not needed
for this schema-only correction. No production controller or Unity code was changed in this diagnostic task.
After applying, test authenticated GET /api/UnityMap/draft?farmId=1 and public /published again before editing.

Validation: dotnet build Agriloco1.csproj --configuration Release
Test: dotnet run --project Tools/MapSaveRegression --configuration Release -- --draft-schema-regression
The test ONLY uses a unique disposable LocalDB database; its deliberate schema fault never targets Azure.
