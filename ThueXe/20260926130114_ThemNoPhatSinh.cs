using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThueXe.Migrations
{
    /// <inheritdoc />
    public partial class ThemNoPhatSinh : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "DebtAmount",
                table: "booking",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DebtPaidAt",
                table: "booking",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DebtAmount",
                table: "booking");

            migrationBuilder.DropColumn(
                name: "DebtPaidAt",
                table: "booking");
        }
    }
}
