using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SentryGuard.Migrations
{
    /// <inheritdoc />
    public partial class StaffInternoSinOrganizacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "tb_Usuario",
                keyColumn: "id",
                keyValue: 2,
                column: "organizacion_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "tb_Usuario",
                keyColumn: "id",
                keyValue: 3,
                column: "organizacion_id",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "tb_Usuario",
                keyColumn: "id",
                keyValue: 2,
                column: "organizacion_id",
                value: 1);

            migrationBuilder.UpdateData(
                table: "tb_Usuario",
                keyColumn: "id",
                keyValue: 3,
                column: "organizacion_id",
                value: 1);
        }
    }
}
