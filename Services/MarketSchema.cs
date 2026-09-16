using Agriloco.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Agriloco1.Services;

// Legacy local SQLite uses additive startup DDL. SQL Server uses reviewed migrations only.
public static class MarketSchema
{
    public static void EnsureCreated(AgrilocoContext db)
    {
        db.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS MarketDetails (
                FarmDefinitionId INTEGER NOT NULL PRIMARY KEY REFERENCES FarmDefinitions(Id) ON DELETE CASCADE,
                DisplayName TEXT NULL, Description TEXT NULL, Category TEXT NULL,
                Image BLOB NULL, ImageContentType TEXT NULL);
            CREATE TABLE IF NOT EXISTS MarketSellingOptions (
                Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                FarmDefinitionId INTEGER NOT NULL REFERENCES MarketDetails(FarmDefinitionId) ON DELETE CASCADE,
                Name TEXT NULL, Price TEXT NULL, Currency TEXT NOT NULL,
                SellQuantity TEXT NULL, SellUnit TEXT NULL, PackageType TEXT NULL,
                Sku TEXT NULL, Barcode TEXT NULL, TrackInventory INTEGER NOT NULL,
                IsPublic INTEGER NOT NULL, SortOrder INTEGER NOT NULL,
                StockUnit TEXT NULL, StockQuantityPerSale TEXT NULL);
            CREATE INDEX IF NOT EXISTS IX_MarketSellingOptions_FarmDefinitionId ON MarketSellingOptions(FarmDefinitionId);
            """);
    }
}
