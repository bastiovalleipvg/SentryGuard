using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SentryGuard.Migrations
{
    /// <inheritdoc />
    public partial class SeedActivosDemo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "tb_Activo",
                columns: new[] { "id", "categoria_id", "hostname_ip", "nombre", "organizacion_id" },
                values: new object[,]
                {
                    { 1, 1, "10.10.0.10", "Servidor web de producción", 1 },
                    { 2, 3, "10.10.0.1", "Firewall perimetral", 1 },
                    { 3, 4, "portal.demo.cl", "Portal de clientes", 1 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "tb_Activo",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "tb_Activo",
                keyColumn: "id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "tb_Activo",
                keyColumn: "id",
                keyValue: 3);
        }
    }
}
