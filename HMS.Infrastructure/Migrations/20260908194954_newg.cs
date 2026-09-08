using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class newg : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Guests_AspNetUsers_ApplicationId",
                table: "Guests");

            migrationBuilder.RenameColumn(
                name: "ApplicationId",
                table: "Guests",
                newName: "ApplicationUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Guests_ApplicationId",
                table: "Guests",
                newName: "IX_Guests_ApplicationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Guests_AspNetUsers_ApplicationUserId",
                table: "Guests",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Guests_AspNetUsers_ApplicationUserId",
                table: "Guests");

            migrationBuilder.RenameColumn(
                name: "ApplicationUserId",
                table: "Guests",
                newName: "ApplicationId");

            migrationBuilder.RenameIndex(
                name: "IX_Guests_ApplicationUserId",
                table: "Guests",
                newName: "IX_Guests_ApplicationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Guests_AspNetUsers_ApplicationId",
                table: "Guests",
                column: "ApplicationId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
