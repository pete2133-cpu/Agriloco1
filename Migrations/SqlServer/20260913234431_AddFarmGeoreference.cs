using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agriloco1.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class AddFarmGeoreference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FarmGeoreferences",
                columns: table => new
                {
                    FarmId = table.Column<int>(type: "int", nullable: false),
                    MapImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MapImageUploadedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PointsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Revision = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FarmGeoreferences", x => x.FarmId);
                    table.ForeignKey(
                        name: "FK_FarmGeoreferences_Farms_FarmId",
                        column: x => x.FarmId,
                        principalTable: "Farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FarmGeoreferences");
        }
    }
}
