using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThueXe.Migrations
{
    /// <inheritdoc />
    public partial class ThemNhacGiaoXe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DaNhacGiaoXe",
                table: "booking",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DaNhacGiaoXe",
                table: "booking");
        }
    }
}
