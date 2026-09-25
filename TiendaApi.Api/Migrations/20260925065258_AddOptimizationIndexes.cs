using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TiendaApi.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddOptimizationIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_users_Role",
                table: "users",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "IX_productos_CategoriaId",
                table: "productos",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_productos_CategoriaId_Precio",
                table: "productos",
                columns: new[] { "CategoriaId", "Precio" });

            migrationBuilder.CreateIndex(
                name: "IX_productos_CreatedAt",
                table: "productos",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_productos_IsDeleted",
                table: "productos",
                column: "IsDeleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_Role",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_productos_CategoriaId",
                table: "productos");

            migrationBuilder.DropIndex(
                name: "IX_productos_CategoriaId_Precio",
                table: "productos");

            migrationBuilder.DropIndex(
                name: "IX_productos_CreatedAt",
                table: "productos");

            migrationBuilder.DropIndex(
                name: "IX_productos_IsDeleted",
                table: "productos");
        }
    }
}
