using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SentryGuard.Migrations
{
    /// <inheritdoc />
    public partial class AuditoriaCategoriaTransicionesVistas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "entidad",
                table: "tb_LogAuditoria",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "entidad_id",
                table: "tb_LogAuditoria",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "valores_anteriores",
                table: "tb_LogAuditoria",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "valores_nuevos",
                table: "tb_LogAuditoria",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "categoria_id",
                table: "tb_Incidente",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "creado_por_id",
                table: "tb_Incidente",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "fecha_modificacion",
                table: "tb_Incidente",
                type: "datetime2(0)",
                precision: 0,
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<int>(
                name: "modificado_por_id",
                table: "tb_Incidente",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "creado_por_id",
                table: "tb_Activo",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "fecha_creacion",
                table: "tb_Activo",
                type: "datetime2(0)",
                precision: 0,
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<DateTime>(
                name: "fecha_modificacion",
                table: "tb_Activo",
                type: "datetime2(0)",
                precision: 0,
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<int>(
                name: "modificado_por_id",
                table: "tb_Activo",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "tb_CategoriaIncidente",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_CategoriaIncidente", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tb_TransicionEstado",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    estado_origen_id = table.Column<int>(type: "int", nullable: false),
                    estado_destino_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_TransicionEstado", x => x.id);
                    table.CheckConstraint("CK_tb_TransicionEstado_OrigenDistintoDestino", "[estado_origen_id] <> [estado_destino_id]");
                    table.ForeignKey(
                        name: "FK_tb_TransicionEstado_tb_EstadoIncidente_estado_destino_id",
                        column: x => x.estado_destino_id,
                        principalTable: "tb_EstadoIncidente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tb_TransicionEstado_tb_EstadoIncidente_estado_origen_id",
                        column: x => x.estado_origen_id,
                        principalTable: "tb_EstadoIncidente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "tb_Activo",
                keyColumn: "id",
                keyValue: 1,
                columns: new[] { "creado_por_id", "fecha_creacion", "fecha_modificacion", "modificado_por_id" },
                values: new object[] { null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null });

            migrationBuilder.UpdateData(
                table: "tb_Activo",
                keyColumn: "id",
                keyValue: 2,
                columns: new[] { "creado_por_id", "fecha_creacion", "fecha_modificacion", "modificado_por_id" },
                values: new object[] { null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null });

            migrationBuilder.UpdateData(
                table: "tb_Activo",
                keyColumn: "id",
                keyValue: 3,
                columns: new[] { "creado_por_id", "fecha_creacion", "fecha_modificacion", "modificado_por_id" },
                values: new object[] { null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null });

            migrationBuilder.InsertData(
                table: "tb_CategoriaIncidente",
                columns: new[] { "id", "nombre" },
                values: new object[,]
                {
                    { 1, "Phishing" },
                    { 2, "Malware" },
                    { 3, "Acceso no autorizado" },
                    { 4, "Denegación de servicio" },
                    { 5, "Fuga de datos" },
                    { 6, "Otro" }
                });

            migrationBuilder.InsertData(
                table: "tb_TransicionEstado",
                columns: new[] { "id", "estado_destino_id", "estado_origen_id" },
                values: new object[,]
                {
                    { 1, 2, 1 },
                    { 2, 3, 1 },
                    { 3, 4, 1 },
                    { 4, 5, 1 },
                    { 5, 3, 2 },
                    { 6, 4, 2 },
                    { 7, 5, 2 },
                    { 8, 2, 3 },
                    { 9, 4, 3 },
                    { 10, 5, 3 },
                    { 11, 2, 4 },
                    { 12, 5, 4 },
                    { 13, 1, 5 },
                    { 14, 2, 5 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_tb_LogAuditoria_entidad_entidad_id",
                table: "tb_LogAuditoria",
                columns: new[] { "entidad", "entidad_id" });

            migrationBuilder.CreateIndex(
                name: "IX_tb_Incidente_categoria_id",
                table: "tb_Incidente",
                column: "categoria_id");

            migrationBuilder.CreateIndex(
                name: "IX_tb_Incidente_creado_por_id",
                table: "tb_Incidente",
                column: "creado_por_id");

            migrationBuilder.CreateIndex(
                name: "IX_tb_Incidente_modificado_por_id",
                table: "tb_Incidente",
                column: "modificado_por_id");

            migrationBuilder.CreateIndex(
                name: "IX_tb_Activo_creado_por_id",
                table: "tb_Activo",
                column: "creado_por_id");

            migrationBuilder.CreateIndex(
                name: "IX_tb_Activo_modificado_por_id",
                table: "tb_Activo",
                column: "modificado_por_id");

            migrationBuilder.CreateIndex(
                name: "IX_tb_CategoriaIncidente_nombre",
                table: "tb_CategoriaIncidente",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tb_TransicionEstado_estado_destino_id",
                table: "tb_TransicionEstado",
                column: "estado_destino_id");

            migrationBuilder.CreateIndex(
                name: "IX_tb_TransicionEstado_estado_origen_id_estado_destino_id",
                table: "tb_TransicionEstado",
                columns: new[] { "estado_origen_id", "estado_destino_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_tb_Activo_tb_Usuario_creado_por_id",
                table: "tb_Activo",
                column: "creado_por_id",
                principalTable: "tb_Usuario",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tb_Activo_tb_Usuario_modificado_por_id",
                table: "tb_Activo",
                column: "modificado_por_id",
                principalTable: "tb_Usuario",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tb_Incidente_tb_CategoriaIncidente_categoria_id",
                table: "tb_Incidente",
                column: "categoria_id",
                principalTable: "tb_CategoriaIncidente",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tb_Incidente_tb_Usuario_creado_por_id",
                table: "tb_Incidente",
                column: "creado_por_id",
                principalTable: "tb_Usuario",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tb_Incidente_tb_Usuario_modificado_por_id",
                table: "tb_Incidente",
                column: "modificado_por_id",
                principalTable: "tb_Usuario",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            // --- Auditoría: los incidentes existentes parten con la última fecha conocida como modificación ---
            migrationBuilder.Sql("UPDATE tb_Incidente SET fecha_modificacion = COALESCE(fecha_cierre, fecha_creacion);");

            // --- Restricciones de integridad en la base (tercer nivel de validación) ---
            migrationBuilder.Sql(@"ALTER TABLE tb_Incidente ADD CONSTRAINT CK_tb_Incidente_FechaCierre
                CHECK (fecha_cierre IS NULL OR fecha_cierre >= fecha_creacion);");

            // El incidente solo puede apuntar a un activo de su misma organización (OWASP API1 también en la BD)
            migrationBuilder.Sql(@"ALTER TABLE tb_Activo ADD CONSTRAINT UQ_tb_Activo_Id_Organizacion UNIQUE (id, organizacion_id);");
            migrationBuilder.Sql(@"ALTER TABLE tb_Incidente ADD CONSTRAINT FK_tb_Incidente_Activo_Organizacion
                FOREIGN KEY (activo_id, organizacion_id) REFERENCES tb_Activo (id, organizacion_id);");

            // --- Vistas para reportes y dashboard ---
            migrationBuilder.Sql(@"
CREATE VIEW vw_CumplimientoSla AS
SELECT
    i.id AS IncidenteId,
    i.organizacion_id AS OrganizacionId,
    i.severidad_id AS SeveridadId,
    i.estado_id AS EstadoId,
    i.fecha_creacion AS FechaCreacion,
    CAST(COALESCE(d.horas_sla, s.horas_sla) AS DECIMAL(12,2)) AS HorasSla,
    CAST(DATEDIFF(SECOND, i.fecha_creacion, COALESCE(i.fecha_cierre, SYSUTCDATETIME())) / 3600.0 AS DECIMAL(12,2)) AS HorasTranscurridas,
    CAST(CASE WHEN DATEDIFF(SECOND, i.fecha_creacion, COALESCE(i.fecha_cierre, SYSUTCDATETIME())) / 3600.0
                   <= COALESCE(d.horas_sla, s.horas_sla) THEN 1 ELSE 0 END AS BIT) AS CumpleSla
FROM tb_Incidente i
JOIN tb_Severidad s ON s.id = i.severidad_id
OUTER APPLY (
    SELECT TOP 1 det.horas_sla
    FROM tb_ContratoSLA c
    JOIN tb_ContratoSLA_Detalle det ON det.contrato_id = c.id
    WHERE c.organizacion_id = i.organizacion_id
      AND det.severidad_id = i.severidad_id
      AND c.vigente_desde <= i.fecha_creacion
      AND (c.vigente_hasta IS NULL OR c.vigente_hasta >= i.fecha_creacion)
    ORDER BY c.vigente_desde DESC
) d;");

            migrationBuilder.Sql(@"
CREATE VIEW vw_IncidenteDetalle AS
SELECT
    i.id AS Id,
    i.titulo AS Titulo,
    i.organizacion_id AS OrganizacionId,
    o.razon_social AS Organizacion,
    i.activo_id AS ActivoId,
    a.nombre AS Activo,
    ca.nombre AS CategoriaActivo,
    i.categoria_id AS CategoriaIncidenteId,
    ci.nombre AS CategoriaIncidente,
    i.severidad_id AS SeveridadId,
    s.nombre AS Severidad,
    i.estado_id AS EstadoId,
    e.nombre AS Estado,
    i.analista_asignado_id AS AnalistaAsignadoId,
    u.nombre AS Analista,
    i.fecha_creacion AS FechaCreacion,
    i.fecha_cierre AS FechaCierre,
    CAST(CASE WHEN i.fecha_cierre IS NULL THEN NULL
              ELSE DATEDIFF(SECOND, i.fecha_creacion, i.fecha_cierre) / 3600.0 END AS DECIMAL(12,2)) AS HorasResolucion
FROM tb_Incidente i
JOIN tb_Organizacion o ON o.id = i.organizacion_id
JOIN tb_Activo a ON a.id = i.activo_id
JOIN tb_CategoriaActivo ca ON ca.id = a.categoria_id
JOIN tb_Severidad s ON s.id = i.severidad_id
JOIN tb_EstadoIncidente e ON e.id = i.estado_id
LEFT JOIN tb_CategoriaIncidente ci ON ci.id = i.categoria_id
LEFT JOIN tb_Usuario u ON u.id = i.analista_asignado_id;");

            migrationBuilder.Sql(@"
CREATE VIEW vw_IndicadoresMensuales AS
SELECT
    YEAR(i.fecha_creacion) AS Anio,
    MONTH(i.fecha_creacion) AS Mes,
    i.severidad_id AS SeveridadId,
    s.nombre AS Severidad,
    COUNT(*) AS TotalIncidentes,
    SUM(CASE WHEN i.fecha_cierre IS NOT NULL THEN 1 ELSE 0 END) AS Resueltos,
    CAST(AVG(CASE WHEN i.fecha_cierre IS NULL THEN NULL
                  ELSE DATEDIFF(SECOND, i.fecha_creacion, i.fecha_cierre) / 3600.0 END) AS DECIMAL(12,2)) AS PromedioHorasResolucion,
    SUM(CASE WHEN c.CumpleSla = 1 THEN 1 ELSE 0 END) AS CumplenSla
FROM tb_Incidente i
JOIN tb_Severidad s ON s.id = i.severidad_id
JOIN vw_CumplimientoSla c ON c.IncidenteId = i.id
GROUP BY YEAR(i.fecha_creacion), MONTH(i.fecha_creacion), i.severidad_id, s.nombre;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_IndicadoresMensuales;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_IncidenteDetalle;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_CumplimientoSla;");
            migrationBuilder.Sql("ALTER TABLE tb_Incidente DROP CONSTRAINT FK_tb_Incidente_Activo_Organizacion;");
            migrationBuilder.Sql("ALTER TABLE tb_Activo DROP CONSTRAINT UQ_tb_Activo_Id_Organizacion;");
            migrationBuilder.Sql("ALTER TABLE tb_Incidente DROP CONSTRAINT CK_tb_Incidente_FechaCierre;");
            migrationBuilder.DropForeignKey(
                name: "FK_tb_Activo_tb_Usuario_creado_por_id",
                table: "tb_Activo");

            migrationBuilder.DropForeignKey(
                name: "FK_tb_Activo_tb_Usuario_modificado_por_id",
                table: "tb_Activo");

            migrationBuilder.DropForeignKey(
                name: "FK_tb_Incidente_tb_CategoriaIncidente_categoria_id",
                table: "tb_Incidente");

            migrationBuilder.DropForeignKey(
                name: "FK_tb_Incidente_tb_Usuario_creado_por_id",
                table: "tb_Incidente");

            migrationBuilder.DropForeignKey(
                name: "FK_tb_Incidente_tb_Usuario_modificado_por_id",
                table: "tb_Incidente");

            migrationBuilder.DropTable(
                name: "tb_CategoriaIncidente");

            migrationBuilder.DropTable(
                name: "tb_TransicionEstado");

            migrationBuilder.DropIndex(
                name: "IX_tb_LogAuditoria_entidad_entidad_id",
                table: "tb_LogAuditoria");

            migrationBuilder.DropIndex(
                name: "IX_tb_Incidente_categoria_id",
                table: "tb_Incidente");

            migrationBuilder.DropIndex(
                name: "IX_tb_Incidente_creado_por_id",
                table: "tb_Incidente");

            migrationBuilder.DropIndex(
                name: "IX_tb_Incidente_modificado_por_id",
                table: "tb_Incidente");

            migrationBuilder.DropIndex(
                name: "IX_tb_Activo_creado_por_id",
                table: "tb_Activo");

            migrationBuilder.DropIndex(
                name: "IX_tb_Activo_modificado_por_id",
                table: "tb_Activo");

            migrationBuilder.DropColumn(
                name: "entidad",
                table: "tb_LogAuditoria");

            migrationBuilder.DropColumn(
                name: "entidad_id",
                table: "tb_LogAuditoria");

            migrationBuilder.DropColumn(
                name: "valores_anteriores",
                table: "tb_LogAuditoria");

            migrationBuilder.DropColumn(
                name: "valores_nuevos",
                table: "tb_LogAuditoria");

            migrationBuilder.DropColumn(
                name: "categoria_id",
                table: "tb_Incidente");

            migrationBuilder.DropColumn(
                name: "creado_por_id",
                table: "tb_Incidente");

            migrationBuilder.DropColumn(
                name: "fecha_modificacion",
                table: "tb_Incidente");

            migrationBuilder.DropColumn(
                name: "modificado_por_id",
                table: "tb_Incidente");

            migrationBuilder.DropColumn(
                name: "creado_por_id",
                table: "tb_Activo");

            migrationBuilder.DropColumn(
                name: "fecha_creacion",
                table: "tb_Activo");

            migrationBuilder.DropColumn(
                name: "fecha_modificacion",
                table: "tb_Activo");

            migrationBuilder.DropColumn(
                name: "modificado_por_id",
                table: "tb_Activo");
        }
    }
}
