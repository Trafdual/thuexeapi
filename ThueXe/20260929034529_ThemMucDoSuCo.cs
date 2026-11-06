using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThueXe.Migrations
{
    /// <inheritdoc />
    public partial class ThemMucDoSuCo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Incidents",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "KHAC");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "Incidents");
        }
    }
}
