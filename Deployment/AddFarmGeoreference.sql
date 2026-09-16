BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260913234431_AddFarmGeoreference'
)
BEGIN
    CREATE TABLE [FarmGeoreferences] (
        [FarmId] int NOT NULL,
        [MapImageUrl] nvarchar(max) NULL,
        [MapImageUploadedAt] datetime2 NULL,
        [PointsJson] nvarchar(max) NOT NULL,
        [Revision] uniqueidentifier NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_FarmGeoreferences] PRIMARY KEY ([FarmId]),
        CONSTRAINT [FK_FarmGeoreferences_Farms_FarmId] FOREIGN KEY ([FarmId]) REFERENCES [Farms] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260913234431_AddFarmGeoreference'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260913234431_AddFarmGeoreference', N'8.0.23');
END;
GO

COMMIT;
GO

