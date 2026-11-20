using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CatCar.Contexts.ServiceOperations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "service_operations");

            migrationBuilder.CreateTable(
                name: "customers",
                schema: "service_operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_number = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    document_type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vehicles",
                schema: "service_operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plate = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    brand = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    model = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    manufacture_year = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "work_order_sagas",
                schema: "service_operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    budget_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    state = table.Column<string>(type: "text", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_order_sagas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "work_orders",
                schema: "service_operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    initial_description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    active_budget_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_budget_id = table.Column<Guid>(type: "uuid", nullable: true),
                    inventory_reservation_failed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    inventory_reservation_failure_code = table.Column<string>(type: "text", nullable: true),
                    status_entered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    execution_started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    diagnosis_duration_seconds = table.Column<double>(type: "double precision", nullable: true),
                    execution_duration_seconds = table.Column<double>(type: "double precision", nullable: true),
                    finalization_duration_seconds = table.Column<double>(type: "double precision", nullable: true),
                    has_complete_stage_history = table.Column<bool>(type: "boolean", nullable: false),
                    opened_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    diagnosis_started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    budget_approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    delivered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_orders", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "work_order_requested_parts",
                schema: "service_operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    inventory_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_order_requested_parts", x => x.id);
                    table.ForeignKey(
                        name: "FK_work_order_requested_parts_work_orders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalSchema: "service_operations",
                        principalTable: "work_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "work_order_requested_services",
                schema: "service_operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cataloged_service_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_order_requested_services", x => x.id);
                    table.ForeignKey(
                        name: "FK_work_order_requested_services_work_orders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalSchema: "service_operations",
                        principalTable: "work_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_customers_document_number",
                schema: "service_operations",
                table: "customers",
                column: "document_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vehicles_customer_id",
                schema: "service_operations",
                table: "vehicles",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_vehicles_plate",
                schema: "service_operations",
                table: "vehicles",
                column: "plate",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_work_order_requested_parts_WorkOrderId",
                schema: "service_operations",
                table: "work_order_requested_parts",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_work_order_requested_services_WorkOrderId",
                schema: "service_operations",
                table: "work_order_requested_services",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_work_orders_customer_id",
                schema: "service_operations",
                table: "work_orders",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_orders_status",
                schema: "service_operations",
                table: "work_orders",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_work_orders_vehicle_id",
                schema: "service_operations",
                table: "work_orders",
                column: "vehicle_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customers",
                schema: "service_operations");

            migrationBuilder.DropTable(
                name: "vehicles",
                schema: "service_operations");

            migrationBuilder.DropTable(
                name: "work_order_requested_parts",
                schema: "service_operations");

            migrationBuilder.DropTable(
                name: "work_order_requested_services",
                schema: "service_operations");

            migrationBuilder.DropTable(
                name: "work_order_sagas",
                schema: "service_operations");

            migrationBuilder.DropTable(
                name: "work_orders",
                schema: "service_operations");
        }
    }
}
