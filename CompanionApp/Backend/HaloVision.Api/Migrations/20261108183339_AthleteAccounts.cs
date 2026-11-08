using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HaloVision.Api.Migrations
{
    /// <inheritdoc />
    public partial class AthleteAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_athlete_profile_AspNetUsers_GuideId",
                table: "athlete_profile");

            migrationBuilder.DropIndex(
                name: "IX_athlete_profile_GuideId",
                table: "athlete_profile");

            migrationBuilder.DropColumn(
                name: "GuideId",
                table: "pairing_code");

            migrationBuilder.RenameColumn(
                name: "GuideId",
                table: "athlete_profile",
                newName: "UserId");

            migrationBuilder.AlterColumn<int>(
                name: "VisionLossType",
                table: "athlete_profile",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<int>(
                name: "SurfaceMode",
                table: "athlete_profile",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<int>(
                name: "SkillLevel",
                table: "athlete_profile",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<int>(
                name: "Severity",
                table: "athlete_profile",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<int>(
                name: "CueMode",
                table: "athlete_profile",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<int>(
                name: "ColorPalette",
                table: "athlete_profile",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<int>(
                name: "AffectedEye",
                table: "athlete_profile",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.CreateIndex(
                name: "IX_athlete_profile_UserId",
                table: "athlete_profile",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_athlete_profile_AspNetUsers_UserId",
                table: "athlete_profile",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_athlete_profile_AspNetUsers_UserId",
                table: "athlete_profile");

            migrationBuilder.DropIndex(
                name: "IX_athlete_profile_UserId",
                table: "athlete_profile");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "athlete_profile",
                newName: "GuideId");

            migrationBuilder.AddColumn<string>(
                name: "GuideId",
                table: "pairing_code",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "VisionLossType",
                table: "athlete_profile",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<string>(
                name: "SurfaceMode",
                table: "athlete_profile",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<string>(
                name: "SkillLevel",
                table: "athlete_profile",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<string>(
                name: "Severity",
                table: "athlete_profile",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<string>(
                name: "CueMode",
                table: "athlete_profile",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<string>(
                name: "ColorPalette",
                table: "athlete_profile",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<string>(
                name: "AffectedEye",
                table: "athlete_profile",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.CreateIndex(
                name: "IX_athlete_profile_GuideId",
                table: "athlete_profile",
                column: "GuideId");

            migrationBuilder.AddForeignKey(
                name: "FK_athlete_profile_AspNetUsers_GuideId",
                table: "athlete_profile",
                column: "GuideId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
