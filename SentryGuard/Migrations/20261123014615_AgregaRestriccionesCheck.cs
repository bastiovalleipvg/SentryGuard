using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SentryGuard.Migrations
{
    /// <inheritdoc />
    public partial class AgregaRestriccionesCheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tb_Incidente_tb_CategoriaIncidente_categoria_id",
                table: "tb_Incidente");

            migrationBuilder.AddColumn<string>(
                name: "descripcion",
                table: "tb_CategoriaIncidente",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "tb_CategoriaIncidente",
                keyColumn: "id",
                keyValue: 1,
                column: "descripcion",
                value: null);

            migrationBuilder.UpdateData(
                table: "tb_CategoriaIncidente",
                keyColumn: "id",
                keyValue: 2,
                column: "descripcion",
                value: null);

            migrationBuilder.UpdateData(
                table: "tb_CategoriaIncidente",
                keyColumn: "id",
                keyValue: 3,
                column: "descripcion",
                value: null);

            migrationBuilder.UpdateData(
                table: "tb_CategoriaIncidente",
                keyColumn: "id",
                keyValue: 4,
                column: "descripcion",
                value: null);

            migrationBuilder.UpdateData(
                table: "tb_CategoriaIncidente",
                keyColumn: "id",
                keyValue: 5,
                column: "descripcion",
                value: null);

            migrationBuilder.UpdateData(
                table: "tb_CategoriaIncidente",
                keyColumn: "id",
                keyValue: 6,
                column: "descripcion",
                value: null);

            migrationBuilder.AddCheckConstraint(
                name: "CK_tb_Usuario_IntentosFallidos",
                table: "tb_Usuario",
                sql: "[intentos_fallidos] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tb_Severidad_HorasSla",
                table: "tb_Severidad",
                sql: "[horas_sla] > 0 AND [horas_sla] <= 720");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tb_PlanServicio_TiempoRespuesta",
                table: "tb_PlanServicio",
                sql: "[tiempo_respuesta_base_h] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tb_Organizacion_RutFormato",
                table: "tb_Organizacion",
                sql: "LEN([rut]) >= 9 AND LEN([rut]) <= 10 AND [rut] LIKE '%-%'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tb_Incidente_FechaCierre",
                table: "tb_Incidente",
                sql: "[fecha_cierre] IS NULL OR [fecha_cierre] >= [fecha_creacion]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tb_Incidente_TituloMinimo",
                table: "tb_Incidente",
                sql: "LEN(LTRIM(RTRIM([titulo]))) >= 5");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tb_ContratoSLA_Detalle_HorasSla",
                table: "tb_ContratoSLA_Detalle",
                sql: "[horas_sla] > 0 AND [horas_sla] <= 720");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tb_ContratoSLA_Vigencia",
                table: "tb_ContratoSLA",
                sql: "[vigente_hasta] IS NULL OR [vigente_hasta] > [vigente_desde]");

            migrationBuilder.AddForeignKey(
                name: "FK_tb_Incidente_tb_CategoriaIncidente_categoria_id",
                table: "tb_Incidente",
                column: "categoria_id",
                principalTable: "tb_CategoriaIncidente",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tb_Incidente_tb_CategoriaIncidente_categoria_id",
                table: "tb_Incidente");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tb_Usuario_IntentosFallidos",
                table: "tb_Usuario");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tb_Severidad_HorasSla",
                table: "tb_Severidad");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tb_PlanServicio_TiempoRespuesta",
                table: "tb_PlanServicio");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tb_Organizacion_RutFormato",
                table: "tb_Organizacion");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tb_Incidente_FechaCierre",
                table: "tb_Incidente");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tb_Incidente_TituloMinimo",
                table: "tb_Incidente");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tb_ContratoSLA_Detalle_HorasSla",
                table: "tb_ContratoSLA_Detalle");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tb_ContratoSLA_Vigencia",
                table: "tb_ContratoSLA");

            migrationBuilder.DropColumn(
                name: "descripcion",
                table: "tb_CategoriaIncidente");

            migrationBuilder.AddForeignKey(
                name: "FK_tb_Incidente_tb_CategoriaIncidente_categoria_id",
                table: "tb_Incidente",
                column: "categoria_id",
                principalTable: "tb_CategoriaIncidente",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
