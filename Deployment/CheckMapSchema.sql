-- Read-only diagnostics. Run against the Agriloco Azure SQL database.
SELECT DB_NAME() AS DatabaseName,
       COL_LENGTH(N'dbo.FarmMapFeatures', N'LabelLayoutJson') AS LabelLayoutColumnLength;
-- NULL means absent (or insufficient metadata permissions); -1 means nvarchar(max).
SELECT MigrationId, ProductVersion FROM dbo.__EFMigrationsHistory
WHERE MigrationId IN (N'20260909151613_InitialSqlServer', N'20260913153926_AddMapLabelLayout')
ORDER BY MigrationId;
SELECT m.Id, m.Status,
       (SELECT COUNT_BIG(*) FROM dbo.FarmMapFeatures f WHERE f.FarmMapId=m.Id) AS FeatureCount,
       (SELECT COUNT_BIG(*) FROM dbo.FarmMapFeaturePoints p
        JOIN dbo.FarmMapFeatures f ON f.Id=p.FarmMapFeatureId WHERE f.FarmMapId=m.Id) AS PointCount
FROM dbo.FarmMaps m WHERE m.FarmId=1;
