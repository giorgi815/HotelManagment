using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class @new : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Admins_ApplicationUserId",
                table: "Admins");

            // Add columns only if they don't already exist (handles partially applied migrations)
            migrationBuilder.Sql(@"IF COL_LENGTH('Managers', 'ApplicationUserId') IS NULL
BEGIN
    ALTER TABLE Managers ADD ApplicationUserId nvarchar(450) NULL;
END");

            migrationBuilder.Sql(@"IF COL_LENGTH('Guests', 'ApplicationId') IS NULL
BEGIN
    ALTER TABLE Guests ADD ApplicationId nvarchar(450) NULL;
END");

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Managers_ApplicationUserId",
                table: "Managers",
                column: "ApplicationUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Guests_ApplicationId",
                table: "Guests",
                column: "ApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Admins_ApplicationUserId",
                table: "Admins",
                column: "ApplicationUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            // Ensure Guests.ApplicationId allows NULL and clean up any invalid values
            migrationBuilder.Sql(@"IF COL_LENGTH('Guests', 'ApplicationId') IS NOT NULL
BEGIN
    ALTER TABLE Guests ALTER COLUMN ApplicationId nvarchar(450) NULL;
END");

            migrationBuilder.Sql(@"
                UPDATE Guests
                SET ApplicationId = NULL
                WHERE ApplicationId IS NOT NULL
                AND ApplicationId NOT IN (SELECT Id FROM AspNetUsers);
            ");

            migrationBuilder.AddForeignKey(
                name: "FK_Guests_AspNetUsers_ApplicationId",
                table: "Guests",
                column: "ApplicationId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Ensure Managers.ApplicationUserId allows NULL and clean up invalid values
            migrationBuilder.Sql(@"IF COL_LENGTH('Managers', 'ApplicationUserId') IS NOT NULL
BEGIN
    ALTER TABLE Managers ALTER COLUMN ApplicationUserId nvarchar(450) NULL;
END");

            migrationBuilder.Sql(@"
                UPDATE Managers
                SET ApplicationUserId = NULL
                WHERE ApplicationUserId IS NOT NULL
                AND ApplicationUserId NOT IN (SELECT Id FROM AspNetUsers);
            ");

            migrationBuilder.AddForeignKey(
                name: "FK_Managers_AspNetUsers_ApplicationUserId",
                table: "Managers",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Guests_AspNetUsers_ApplicationId",
                table: "Guests");

            migrationBuilder.DropForeignKey(
                name: "FK_Managers_AspNetUsers_ApplicationUserId",
                table: "Managers");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_Managers_ApplicationUserId",
                table: "Managers");

            migrationBuilder.DropIndex(
                name: "IX_Guests_ApplicationId",
                table: "Guests");

            migrationBuilder.DropIndex(
                name: "IX_Admins_ApplicationUserId",
                table: "Admins");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "Managers");

            migrationBuilder.DropColumn(
                name: "ApplicationId",
                table: "Guests");

            migrationBuilder.CreateIndex(
                name: "IX_Admins_ApplicationUserId",
                table: "Admins",
                column: "ApplicationUserId");
        }
    }
}
