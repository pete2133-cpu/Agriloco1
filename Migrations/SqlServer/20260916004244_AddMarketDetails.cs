using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agriloco1.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class AddMarketDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MarketDetails",
                columns: table => new
                {
                    FarmDefinitionId = table.Column<int>(type: "int", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Image = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    ImageContentType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketDetails", x => x.FarmDefinitionId);
                    table.ForeignKey(
                        name: "FK_MarketDetails_FarmDefinitions_FarmDefinitionId",
                        column: x => x.FarmDefinitionId,
                        principalTable: "FarmDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MarketSellingOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FarmDefinitionId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    SellQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    SellUnit = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    PackageType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Sku = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Barcode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TrackInventory = table.Column<bool>(type: "bit", nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    StockUnit = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    StockQuantityPerSale = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketSellingOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketSellingOptions_MarketDetails_FarmDefinitionId",
                        column: x => x.FarmDefinitionId,
                        principalTable: "MarketDetails",
                        principalColumn: "FarmDefinitionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarketSellingOptions_FarmDefinitionId",
                table: "MarketSellingOptions",
                column: "FarmDefinitionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarketSellingOptions");

            migrationBuilder.DropTable(
                name: "MarketDetails");
        }
    }
}
