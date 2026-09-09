using Agriloco.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Agriloco1.Services;

public static class ProductionSourceSchema
{
    // Additive setup for existing SQLite prototypes; EnsureCreated handles new databases.
    public static void EnsureCreated(AgrilocoContext db)
    {
        db.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS ProductionIngredientSources (
                Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                FarmId INTEGER NOT NULL,
                ProductionRunId INTEGER NOT NULL,
                ProductionRunIngredientId INTEGER NOT NULL,
                ReceivingLotId INTEGER NULL,
                ReceiptPayload TEXT NOT NULL,
                RecordedAt TEXT NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS IX_ProductionIngredientSources_FarmId_ProductionRunIngredientId
                ON ProductionIngredientSources (FarmId, ProductionRunIngredientId);
            """);
    }
}
