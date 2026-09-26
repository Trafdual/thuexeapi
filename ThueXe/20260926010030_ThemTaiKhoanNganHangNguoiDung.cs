using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThueXe.Migrations
{
    /// <inheritdoc />
    public partial class ThemTaiKhoanNganHangNguoiDung : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BankAccount",
                table: "AppUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankName",
                table: "AppUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BankAccount",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "BankName",
                table: "AppUsers");
        }
    }
}
