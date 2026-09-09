using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Agriloco.Api.Data;

// A separate context keeps production migrations independent of legacy SQLite history.
public sealed class SqlServerAgrilocoContext : AgrilocoContext
{
    public SqlServerAgrilocoContext(DbContextOptions<SqlServerAgrilocoContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Agriloco.Api.Models.FarmMapLayout>().HasIndex(x => x.FarmId).IsUnique();
        modelBuilder.Entity<Agriloco1.Models.Inventory.AvailabilityChannel>().Property(x => x.Code).HasMaxLength(100);
        modelBuilder.Entity<Agriloco1.Models.Inventory.AvailabilityChannel>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<Agriloco1.Models.Inventory.FarmDefinitionChannel>()
            .HasIndex(x => new { x.FarmDefinitionId, x.AvailabilityChannelId }).IsUnique();
        var createdAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var channels = new[]
        {
            ("Pick Your Own", "PickYourOwn", "Available for customers to harvest directly at the farm."),
            ("Farm Store", "FarmStore", "Available through the farm store or farm gate."),
            ("Wholesale", "Wholesale", "Available for wholesale purchase."),
            ("Retail Partner", "RetailPartner", "Available through third-party retail partners."),
            ("Farmers Market", "FarmersMarket", "Available through farmers markets.")
        };
        modelBuilder.Entity<Agriloco1.Models.Inventory.AvailabilityChannel>().HasData(channels.Select((channel, index) =>
            new Agriloco1.Models.Inventory.AvailabilityChannel
            {
                Id = index + 1, Name = channel.Item1, Code = channel.Item2, Description = channel.Item3,
                IsActive = true, SortOrder = (index + 1) * 10, CreatedAt = createdAt
            }));
        foreach (var property in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetProperties()))
        {
            if ((Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType) == typeof(decimal))
            {
                property.SetPrecision(18);
                property.SetScale(6);
            }
        }
    }
}

public sealed class SqlServerAgrilocoContextFactory : IDesignTimeDbContextFactory<SqlServerAgrilocoContext>
{
    public SqlServerAgrilocoContext CreateDbContext(string[] args)
    {
        // Scaffolding/scripts do not connect. Database updates require an explicit connection.
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Server=localhost;Database=Agriloco;Integrated Security=True;Encrypt=True";
        return new(new DbContextOptionsBuilder<SqlServerAgrilocoContext>()
            .UseSqlServer(connection).Options);
    }
}
