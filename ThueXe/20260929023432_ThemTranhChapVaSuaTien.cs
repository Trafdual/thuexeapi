using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThueXe.Migrations
{
    /// <inheritdoc />
    public partial class ThemTranhChapVaSuaTien : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FailNote",
                table: "Payouts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DisputeOpenedAt",
                table: "booking",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisputeOpenedBy",
                table: "booking",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisputeReason",
                table: "booking",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DisputeResolvedBy",
                table: "booking",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExtendRequestedAt",
                table: "booking",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ExtendRequestedEndDate",
                table: "booking",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreDisputeStatus",
                table: "booking",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReadyToSettleAt",
                table: "booking",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IncidentCount",
                table: "AppUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsLocked",
                table: "AppUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LockReason",
                table: "AppUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Incidents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingId = table.Column<long>(type: "bigint", nullable: false),
                    ReportedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incidents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Incidents_booking_BookingId",
                        column: x => x.BookingId,
                        principalTable: "booking",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IncidentPhotos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IncidentId = table.Column<long>(type: "bigint", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentPhotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncidentPhotos_Incidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "Incidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IncidentPhotos_IncidentId",
                table: "IncidentPhotos",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_BookingId",
                table: "Incidents",
                column: "BookingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IncidentPhotos");

            migrationBuilder.DropTable(
                name: "Incidents");

            migrationBuilder.DropColumn(
                name: "FailNote",
                table: "Payouts");

            migrationBuilder.DropColumn(
                name: "DisputeOpenedAt",
                table: "booking");

            migrationBuilder.DropColumn(
                name: "DisputeOpenedBy",
                table: "booking");

            migrationBuilder.DropColumn(
                name: "DisputeReason",
                table: "booking");

            migrationBuilder.DropColumn(
                name: "DisputeResolvedBy",
                table: "booking");

            migrationBuilder.DropColumn(
                name: "ExtendRequestedAt",
                table: "booking");

            migrationBuilder.DropColumn(
                name: "ExtendRequestedEndDate",
                table: "booking");

            migrationBuilder.DropColumn(
                name: "PreDisputeStatus",
                table: "booking");

            migrationBuilder.DropColumn(
                name: "ReadyToSettleAt",
                table: "booking");

            migrationBuilder.DropColumn(
                name: "IncidentCount",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "IsLocked",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "LockReason",
                table: "AppUsers");
        }
    }
}
