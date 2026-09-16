BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260913153926_AddMapLabelLayout'
)
BEGIN
    ALTER TABLE [FarmMapFeatures] ADD [LabelLayoutJson] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260913153926_AddMapLabelLayout'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260913153926_AddMapLabelLayout', N'8.0.23');
END;
GO

COMMIT;
GO

