using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agriloco1.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class AddMapLabelLayout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LabelLayoutJson",
                table: "FarmMapFeatures",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LabelLayoutJson",
                table: "FarmMapFeatures");
        }
    }
}
