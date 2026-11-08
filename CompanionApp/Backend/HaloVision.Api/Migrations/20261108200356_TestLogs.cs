using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HaloVision.Api.Migrations
{
    /// <inheritdoc />
    public partial class TestLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "test_log",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeviceId = table.Column<int>(type: "INTEGER", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Location = table.Column<string>(type: "TEXT", nullable: false),
                    ParticipantCode = table.Column<string>(type: "TEXT", nullable: false),
                    Lux = table.Column<float>(type: "REAL", nullable: false),
                    AirTempC = table.Column<float>(type: "REAL", nullable: false),
                    DeviceTempC = table.Column<float>(type: "REAL", nullable: false),
                    BatteryStartPct = table.Column<int>(type: "INTEGER", nullable: false),
                    BatteryEndPct = table.Column<int>(type: "INTEGER", nullable: false),
                    AvgFps = table.Column<float>(type: "REAL", nullable: false),
                    LatencyMs = table.Column<float>(type: "REAL", nullable: false),
                    TrackingLossCount = table.Column<int>(type: "INTEGER", nullable: false),
                    SampledIoU = table.Column<float>(type: "REAL", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_test_log", x => x.Id);
                    table.ForeignKey(
                        name: "FK_test_log_device_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "device",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_test_log_DeviceId",
                table: "test_log",
                column: "DeviceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "test_log");
        }
    }
}
