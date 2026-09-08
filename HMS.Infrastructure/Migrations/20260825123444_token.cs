using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class token : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Converting primary key from Guid to int requires creating a new table
            // Create a temporary table with int identity Id and an OldId column to preserve original GUIDs
            migrationBuilder.CreateTable(
                name: "RefreshTokens_Temp",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Token = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    OldId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens_Temp", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Temp_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Copy data from old table into the temp table, preserving the old Guid on OldId
            migrationBuilder.Sql(@"
                INSERT INTO RefreshTokens_Temp (Token, UserId, ExpiresAt, CreatedAt, RevokedAt, OldId)
                SELECT Token, UserId, ExpiresAt, CreatedAt, RevokedAt, Id FROM RefreshTokens;
            ");

            // Drop the old table and rename temp to original name
            migrationBuilder.DropTable(name: "RefreshTokens");
            migrationBuilder.RenameTable(name: "RefreshTokens_Temp", newName: "RefreshTokens");

            // Recreate index on UserId for the new table
            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Recreate the old table with GUID Id and restore data from the int-keyed table
            migrationBuilder.CreateTable(
                name: "RefreshTokens_Old",
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
                    table.PrimaryKey("PK_RefreshTokens_Old", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Old_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Insert data back, using OldId if preserved, otherwise generate new GUIDs
            migrationBuilder.Sql(@"
                INSERT INTO RefreshTokens_Old (Id, Token, UserId, ExpiresAt, CreatedAt, RevokedAt)
                SELECT COALESCE(OldId, NEWID()), Token, UserId, ExpiresAt, CreatedAt, RevokedAt FROM RefreshTokens;
            ");

            // Drop the int-keyed table and rename the old table back to original name
            migrationBuilder.DropTable(name: "RefreshTokens");
            migrationBuilder.RenameTable(name: "RefreshTokens_Old", newName: "RefreshTokens");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");
        }
    }
}
