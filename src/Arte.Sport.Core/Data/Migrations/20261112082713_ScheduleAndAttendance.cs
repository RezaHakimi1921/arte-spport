using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arte.Sport.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class ScheduleAndAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CancelUntilMinutes",
                table: "Branches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "MemberCanCancel",
                table: "Branches",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Attendances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CorrectionReason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RecordedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attendances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Bookings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CancelledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Inbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientPersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Body = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Seen = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inbox", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PlaceName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    CoachPersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StaffPunches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    InAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    OutAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CloseReason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffPunches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StaffShifts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    Weekday = table.Column<int>(type: "integer", nullable: false),
                    StartsAt = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    EndsAt = table.Column<TimeOnly>(type: "time without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffShifts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_SessionId_MemberProfileId",
                table: "Attendances",
                columns: new[] { "SessionId", "MemberProfileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_SessionId_MemberProfileId",
                table: "Bookings",
                columns: new[] { "SessionId", "MemberProfileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Inbox_BranchId_RecipientPersonId_Seen",
                table: "Inbox",
                columns: new[] { "BranchId", "RecipientPersonId", "Seen" });

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_BranchId_StartsAt",
                table: "Sessions",
                columns: new[] { "BranchId", "StartsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StaffPunches_BranchId_PersonId_InAt",
                table: "StaffPunches",
                columns: new[] { "BranchId", "PersonId", "InAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StaffShifts_BranchId_PersonId_Weekday",
                table: "StaffShifts",
                columns: new[] { "BranchId", "PersonId", "Weekday" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Attendances");

            migrationBuilder.DropTable(
                name: "Bookings");

            migrationBuilder.DropTable(
                name: "Inbox");

            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropTable(
                name: "StaffPunches");

            migrationBuilder.DropTable(
                name: "StaffShifts");

            migrationBuilder.DropColumn(
                name: "CancelUntilMinutes",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "MemberCanCancel",
                table: "Branches");
        }
    }
}
