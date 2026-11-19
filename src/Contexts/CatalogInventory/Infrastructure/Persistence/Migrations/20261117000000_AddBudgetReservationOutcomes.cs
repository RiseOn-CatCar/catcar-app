using CatCar.Contexts.CatalogInventory.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace CatCar.Contexts.CatalogInventory.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CatalogInventoryDbContext))]
[Migration("20261117000000_AddBudgetReservationOutcomes")]
public sealed class AddBudgetReservationOutcomes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(name: "budget_reservation_outcomes", schema: "catalog_inventory", columns: table => new
        {
            budget_id = table.Column<Guid>(type: "uuid", nullable: false),
            work_order_id = table.Column<Guid>(type: "uuid", nullable: false),
            outcome = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
            failure_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
            resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            correlation_id = table.Column<Guid>(type: "uuid", nullable: false),
            created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
            updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
        }, constraints: table => table.PrimaryKey("pk_budget_reservation_outcomes", x => x.budget_id));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.DropTable(name: "budget_reservation_outcomes", schema: "catalog_inventory");
}
