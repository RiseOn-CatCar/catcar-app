namespace CatCar.Contexts.CatalogInventory.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

public sealed class CatalogInventoryDbContextFactory : IDesignTimeDbContextFactory<CatalogInventoryDbContext>
{
    public CatalogInventoryDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CatalogInventoryDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING")
            ?? "Host=localhost;Database=catcar_dev;Username=postgres";
        optionsBuilder.UseNpgsql(connectionString);
        return new CatalogInventoryDbContext(optionsBuilder.Options);
    }
}
