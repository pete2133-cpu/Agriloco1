IF OBJECT_ID(N'dbo.RowLabels', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RowLabels
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RowLabels PRIMARY KEY,
        FarmId INT NOT NULL,
        CropId INT NULL,
        AnchorX INT NOT NULL,
        AnchorY INT NOT NULL,
        LabelText NVARCHAR(200) NOT NULL DEFAULT(N''),
        Availability NVARCHAR(50) NOT NULL DEFAULT(N'Available'),
        CreatedAt DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME()),
        UpdatedAt DATETIME2 NULL
    );

    -- FK to Farms
    ALTER TABLE dbo.RowLabels
    ADD CONSTRAINT FK_RowLabels_Farms_FarmId
        FOREIGN KEY (FarmId) REFERENCES dbo.Farms(Id)
        ON DELETE CASCADE;

    -- FK to Crops (nullable)
    ALTER TABLE dbo.RowLabels
    ADD CONSTRAINT FK_RowLabels_Crops_CropId
        FOREIGN KEY (CropId) REFERENCES dbo.Crops(Id)
        ON DELETE SET NULL;

    CREATE INDEX IX_RowLabels_Farm_Anchor
        ON dbo.RowLabels(FarmId, AnchorX, AnchorY);
END