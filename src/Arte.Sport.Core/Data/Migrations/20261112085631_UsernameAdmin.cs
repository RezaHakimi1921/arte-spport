using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arte.Sport.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class UsernameAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_People_Mobile",
                table: "People");

            migrationBuilder.AlterColumn<string>(
                name: "Mobile",
                table: "People",
                type: "character varying(11)",
                maxLength: 11,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(11)",
                oldMaxLength: 11);

            migrationBuilder.AddColumn<bool>(
                name: "IsPlatformAdmin",
                table: "People",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "People",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_People_Mobile",
                table: "People",
                column: "Mobile",
                unique: true,
                filter: "\"Mobile\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_People_Username",
                table: "People",
                column: "Username",
                unique: true,
                filter: "\"Username\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_People_Mobile",
                table: "People");

            migrationBuilder.DropIndex(
                name: "IX_People_Username",
                table: "People");

            migrationBuilder.DropColumn(
                name: "IsPlatformAdmin",
                table: "People");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "People");

            migrationBuilder.AlterColumn<string>(
                name: "Mobile",
                table: "People",
                type: "character varying(11)",
                maxLength: 11,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(11)",
                oldMaxLength: 11,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_People_Mobile",
                table: "People",
                column: "Mobile",
                unique: true);
        }
    }
}
