using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SentryGuard.Migrations
{
    /// <inheritdoc />
    public partial class SeedCatalogosYUsuariosPrueba : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "tb_CategoriaActivo",
                columns: new[] { "id", "nombre" },
                values: new object[,]
                {
                    { 1, "Servidor" },
                    { 2, "Endpoint" },
                    { 3, "Red" },
                    { 4, "Aplicación" },
                    { 5, "Nube" }
                });

            migrationBuilder.InsertData(
                table: "tb_EstadoIncidente",
                columns: new[] { "id", "nombre" },
                values: new object[,]
                {
                    { 1, "Abierto" },
                    { 2, "En progreso" },
                    { 3, "Escalado" },
                    { 4, "Resuelto" },
                    { 5, "Cerrado" }
                });

            migrationBuilder.InsertData(
                table: "tb_PlanServicio",
                columns: new[] { "id", "descripcion", "nombre", "tiempo_respuesta_base_h" },
                values: new object[,]
                {
                    { 1, "Plan de servicio básico", "Básico", 24m },
                    { 2, "Plan de servicio estándar", "Estándar", 12m },
                    { 3, "Plan de servicio premium", "Premium", 4m }
                });

            migrationBuilder.InsertData(
                table: "tb_Rol",
                columns: new[] { "id", "nombre" },
                values: new object[,]
                {
                    { 1, "Administrador" },
                    { 2, "Supervisor" },
                    { 3, "Analista" },
                    { 4, "Cliente" }
                });

            migrationBuilder.InsertData(
                table: "tb_Severidad",
                columns: new[] { "id", "horas_sla", "nombre" },
                values: new object[,]
                {
                    { 1, 24m, "Baja" },
                    { 2, 12m, "Media" },
                    { 3, 4m, "Alta" },
                    { 4, 1m, "Crítica" }
                });

            migrationBuilder.InsertData(
                table: "tb_Organizacion",
                columns: new[] { "id", "fecha_alta", "plan_contratado_id", "razon_social", "rut" },
                values: new object[] { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, "Empresa Demo SpA", "76123456-7" });

            migrationBuilder.InsertData(
                table: "tb_Usuario",
                columns: new[] { "id", "activo", "bloqueado_hasta", "email", "nombre", "organizacion_id", "password_hash", "rol_id" },
                values: new object[,]
                {
                    { 1, true, null, "admin@sentryguard.cl", "Administrador SentryGuard", null, "AQAAAAIAAYagAAAAEA7tnp5gva8xrGxWDTthC+lZOlvP37Z6w0rYJiGbxYzLWfHz5fvYXt7Usi6skqdweQ==", 1 },
                    { 2, true, null, "supervisor@sentryguard.cl", "Supervisor Demo", 1, "AQAAAAIAAYagAAAAEA7tnp5gva8xrGxWDTthC+lZOlvP37Z6w0rYJiGbxYzLWfHz5fvYXt7Usi6skqdweQ==", 2 },
                    { 3, true, null, "analista@sentryguard.cl", "Analista Demo", 1, "AQAAAAIAAYagAAAAEA7tnp5gva8xrGxWDTthC+lZOlvP37Z6w0rYJiGbxYzLWfHz5fvYXt7Usi6skqdweQ==", 3 },
                    { 4, true, null, "cliente@sentryguard.cl", "Cliente Demo", 1, "AQAAAAIAAYagAAAAEA7tnp5gva8xrGxWDTthC+lZOlvP37Z6w0rYJiGbxYzLWfHz5fvYXt7Usi6skqdweQ==", 4 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "tb_CategoriaActivo",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "tb_CategoriaActivo",
                keyColumn: "id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "tb_CategoriaActivo",
                keyColumn: "id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "tb_CategoriaActivo",
                keyColumn: "id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "tb_CategoriaActivo",
                keyColumn: "id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "tb_EstadoIncidente",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "tb_EstadoIncidente",
                keyColumn: "id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "tb_EstadoIncidente",
                keyColumn: "id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "tb_EstadoIncidente",
                keyColumn: "id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "tb_EstadoIncidente",
                keyColumn: "id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "tb_PlanServicio",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "tb_PlanServicio",
                keyColumn: "id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "tb_Severidad",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "tb_Severidad",
                keyColumn: "id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "tb_Severidad",
                keyColumn: "id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "tb_Severidad",
                keyColumn: "id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "tb_Usuario",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "tb_Usuario",
                keyColumn: "id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "tb_Usuario",
                keyColumn: "id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "tb_Usuario",
                keyColumn: "id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "tb_Organizacion",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "tb_Rol",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "tb_Rol",
                keyColumn: "id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "tb_Rol",
                keyColumn: "id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "tb_Rol",
                keyColumn: "id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "tb_PlanServicio",
                keyColumn: "id",
                keyValue: 2);
        }
    }
}
