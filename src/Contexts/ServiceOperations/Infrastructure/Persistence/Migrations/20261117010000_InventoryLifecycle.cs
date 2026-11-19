using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CatCar.Contexts.ServiceOperations.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ServiceOperationsDbContext))]
[Migration("20261117010000_InventoryLifecycle")]
public sealed class InventoryLifecycle : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(name: "approved_budget_id", schema: "service_operations", table: "work_orders", type: "uuid", nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "inventory_reservation_failed_at", schema: "service_operations", table: "work_orders", type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<string>(name: "inventory_reservation_failure_code", schema: "service_operations", table: "work_orders", type: "text", nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "status_entered_at", schema: "service_operations", table: "work_orders", type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "execution_started_at", schema: "service_operations", table: "work_orders", type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<double>(name: "diagnosis_duration_seconds", schema: "service_operations", table: "work_orders", type: "double precision", nullable: true);
        migrationBuilder.AddColumn<double>(name: "execution_duration_seconds", schema: "service_operations", table: "work_orders", type: "double precision", nullable: true);
        migrationBuilder.AddColumn<double>(name: "finalization_duration_seconds", schema: "service_operations", table: "work_orders", type: "double precision", nullable: true);
        migrationBuilder.AddColumn<bool>(name: "has_complete_stage_history", schema: "service_operations", table: "work_orders", type: "boolean", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<DateTime>(name: "approved_at", schema: "service_operations", table: "budgets", type: "timestamp with time zone", nullable: true);
        migrationBuilder.Sql("""
            UPDATE service_operations.work_orders AS w
            SET approved_budget_id = b.id
            FROM service_operations.budgets AS b
            WHERE b.id = w.active_budget_id AND b.work_order_id = w.id AND b.status = 'Approved';
            UPDATE service_operations.budgets AS b
            SET approved_at = w.budget_approved_at
            FROM service_operations.work_orders AS w
            WHERE b.id = w.active_budget_id AND b.work_order_id = w.id AND b.status = 'Approved'
              AND w.budget_approved_at IS NOT NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (var column in new[] { "approved_budget_id", "inventory_reservation_failed_at", "inventory_reservation_failure_code", "status_entered_at", "execution_started_at", "diagnosis_duration_seconds", "execution_duration_seconds", "finalization_duration_seconds", "has_complete_stage_history" })
            migrationBuilder.DropColumn(name: column, schema: "service_operations", table: "work_orders");
        migrationBuilder.DropColumn(name: "approved_at", schema: "service_operations", table: "budgets");
    }
}
