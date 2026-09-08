using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SentryGuard.Migrations
{
    /// <inheritdoc />
    public partial class InicialSentryGuard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tb_CategoriaActivo",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_CategoriaActivo", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tb_EstadoIncidente",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_EstadoIncidente", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tb_PlanServicio",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    tiempo_respuesta_base_h = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_PlanServicio", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tb_Rol",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_Rol", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tb_Severidad",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    horas_sla = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_Severidad", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tb_Organizacion",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    razon_social = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    rut = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    plan_contratado_id = table.Column<int>(type: "int", nullable: false),
                    fecha_alta = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_Organizacion", x => x.id);
                    table.ForeignKey(
                        name: "FK_tb_Organizacion_tb_PlanServicio_plan_contratado_id",
                        column: x => x.plan_contratado_id,
                        principalTable: "tb_PlanServicio",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tb_Activo",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    organizacion_id = table.Column<int>(type: "int", nullable: false),
                    nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    hostname_ip = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    categoria_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_Activo", x => x.id);
                    table.ForeignKey(
                        name: "FK_tb_Activo_tb_CategoriaActivo_categoria_id",
                        column: x => x.categoria_id,
                        principalTable: "tb_CategoriaActivo",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tb_Activo_tb_Organizacion_organizacion_id",
                        column: x => x.organizacion_id,
                        principalTable: "tb_Organizacion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tb_ContratoSLA",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    organizacion_id = table.Column<int>(type: "int", nullable: false),
                    vigente_desde = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    vigente_hasta = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_ContratoSLA", x => x.id);
                    table.ForeignKey(
                        name: "FK_tb_ContratoSLA_tb_Organizacion_organizacion_id",
                        column: x => x.organizacion_id,
                        principalTable: "tb_Organizacion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tb_Usuario",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    password_hash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    rol_id = table.Column<int>(type: "int", nullable: false),
                    organizacion_id = table.Column<int>(type: "int", nullable: true),
                    activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    intentos_fallidos = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    bloqueado_hasta = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_Usuario", x => x.id);
                    table.ForeignKey(
                        name: "FK_tb_Usuario_tb_Organizacion_organizacion_id",
                        column: x => x.organizacion_id,
                        principalTable: "tb_Organizacion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tb_Usuario_tb_Rol_rol_id",
                        column: x => x.rol_id,
                        principalTable: "tb_Rol",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tb_ContratoSLA_Detalle",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    contrato_id = table.Column<int>(type: "int", nullable: false),
                    severidad_id = table.Column<int>(type: "int", nullable: false),
                    horas_sla = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_ContratoSLA_Detalle", x => x.id);
                    table.ForeignKey(
                        name: "FK_tb_ContratoSLA_Detalle_tb_ContratoSLA_contrato_id",
                        column: x => x.contrato_id,
                        principalTable: "tb_ContratoSLA",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tb_ContratoSLA_Detalle_tb_Severidad_severidad_id",
                        column: x => x.severidad_id,
                        principalTable: "tb_Severidad",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tb_Incidente",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    organizacion_id = table.Column<int>(type: "int", nullable: false),
                    activo_id = table.Column<int>(type: "int", nullable: false),
                    titulo = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    severidad_id = table.Column<int>(type: "int", nullable: false),
                    estado_id = table.Column<int>(type: "int", nullable: false),
                    analista_asignado_id = table.Column<int>(type: "int", nullable: true),
                    fecha_creacion = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    fecha_cierre = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_Incidente", x => x.id);
                    table.ForeignKey(
                        name: "FK_tb_Incidente_tb_Activo_activo_id",
                        column: x => x.activo_id,
                        principalTable: "tb_Activo",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tb_Incidente_tb_EstadoIncidente_estado_id",
                        column: x => x.estado_id,
                        principalTable: "tb_EstadoIncidente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tb_Incidente_tb_Organizacion_organizacion_id",
                        column: x => x.organizacion_id,
                        principalTable: "tb_Organizacion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tb_Incidente_tb_Severidad_severidad_id",
                        column: x => x.severidad_id,
                        principalTable: "tb_Severidad",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tb_Incidente_tb_Usuario_analista_asignado_id",
                        column: x => x.analista_asignado_id,
                        principalTable: "tb_Usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tb_LogAuditoria",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    usuario_id = table.Column<int>(type: "int", nullable: true),
                    accion = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ip_origen = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    timestamp = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_LogAuditoria", x => x.id);
                    table.ForeignKey(
                        name: "FK_tb_LogAuditoria_tb_Usuario_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "tb_Usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "tb_RefreshToken",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    usuario_id = table.Column<int>(type: "int", nullable: false),
                    token_hash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    fecha_emision = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    fecha_expira = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    revocado = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_RefreshToken", x => x.id);
                    table.ForeignKey(
                        name: "FK_tb_RefreshToken_tb_Usuario_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "tb_Usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tb_BitacoraIncidente",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    incidente_id = table.Column<int>(type: "int", nullable: false),
                    usuario_id = table.Column<int>(type: "int", nullable: false),
                    accion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    comentario = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    timestamp = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_BitacoraIncidente", x => x.id);
                    table.ForeignKey(
                        name: "FK_tb_BitacoraIncidente_tb_Incidente_incidente_id",
                        column: x => x.incidente_id,
                        principalTable: "tb_Incidente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tb_BitacoraIncidente_tb_Usuario_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "tb_Usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tb_EvidenciaIncidente",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    incidente_id = table.Column<int>(type: "int", nullable: false),
                    nombre_archivo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ruta_almacenamiento = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    subido_por_id = table.Column<int>(type: "int", nullable: false),
                    fecha = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_EvidenciaIncidente", x => x.id);
                    table.ForeignKey(
                        name: "FK_tb_EvidenciaIncidente_tb_Incidente_incidente_id",
                        column: x => x.incidente_id,
                        principalTable: "tb_Incidente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tb_EvidenciaIncidente_tb_Usuario_subido_por_id",
                        column: x => x.subido_por_id,
                        principalTable: "tb_Usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tb_Activo_categoria_id",
                table: "tb_Activo",
                column: "categoria_id");

            migrationBuilder.CreateIndex(
                name: "IX_tb_Activo_organizacion_id",
                table: "tb_Activo",
                column: "organizacion_id");

            migrationBuilder.CreateIndex(
                name: "IX_tb_BitacoraIncidente_incidente_id",
                table: "tb_BitacoraIncidente",
                column: "incidente_id");

            migrationBuilder.CreateIndex(
                name: "IX_tb_BitacoraIncidente_timestamp",
                table: "tb_BitacoraIncidente",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_tb_BitacoraIncidente_usuario_id",
                table: "tb_BitacoraIncidente",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_tb_CategoriaActivo_nombre",
                table: "tb_CategoriaActivo",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tb_ContratoSLA_organizacion_id",
                table: "tb_ContratoSLA",
                column: "organizacion_id",
                unique: true,
                filter: "[vigente_hasta] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_tb_ContratoSLA_Detalle_contrato_id_severidad_id",
                table: "tb_ContratoSLA_Detalle",
                columns: new[] { "contrato_id", "severidad_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tb_ContratoSLA_Detalle_severidad_id",
                table: "tb_ContratoSLA_Detalle",
                column: "severidad_id");

            migrationBuilder.CreateIndex(
                name: "IX_tb_EstadoIncidente_nombre",
                table: "tb_EstadoIncidente",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tb_EvidenciaIncidente_incidente_id",
                table: "tb_EvidenciaIncidente",
                column: "incidente_id");

            migrationBuilder.CreateIndex(
                name: "IX_tb_EvidenciaIncidente_subido_por_id",
                table: "tb_EvidenciaIncidente",
                column: "subido_por_id");

            migrationBuilder.CreateIndex(
                name: "IX_tb_Incidente_activo_id",
                table: "tb_Incidente",
                column: "activo_id");

            migrationBuilder.CreateIndex(
                name: "IX_tb_Incidente_analista_asignado_id",
                table: "tb_Incidente",
                column: "analista_asignado_id");

            migrationBuilder.CreateIndex(
                name: "IX_tb_Incidente_estado_id",
                table: "tb_Incidente",
                column: "estado_id");

            migrationBuilder.CreateIndex(
                name: "IX_tb_Incidente_fecha_creacion",
                table: "tb_Incidente",
                column: "fecha_creacion");

            migrationBuilder.CreateIndex(
                name: "IX_tb_Incidente_organizacion_id",
                table: "tb_Incidente",
                column: "organizacion_id");

            migrationBuilder.CreateIndex(
                name: "IX_tb_Incidente_severidad_id",
                table: "tb_Incidente",
                column: "severidad_id");

            migrationBuilder.CreateIndex(
                name: "IX_tb_LogAuditoria_timestamp",
                table: "tb_LogAuditoria",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_tb_LogAuditoria_usuario_id",
                table: "tb_LogAuditoria",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_tb_Organizacion_plan_contratado_id",
                table: "tb_Organizacion",
                column: "plan_contratado_id");

            migrationBuilder.CreateIndex(
                name: "IX_tb_Organizacion_rut",
                table: "tb_Organizacion",
                column: "rut",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tb_PlanServicio_nombre",
                table: "tb_PlanServicio",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tb_RefreshToken_fecha_expira",
                table: "tb_RefreshToken",
                column: "fecha_expira");

            migrationBuilder.CreateIndex(
                name: "IX_tb_RefreshToken_usuario_id",
                table: "tb_RefreshToken",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_tb_Rol_nombre",
                table: "tb_Rol",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tb_Severidad_nombre",
                table: "tb_Severidad",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tb_Usuario_email",
                table: "tb_Usuario",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tb_Usuario_organizacion_id",
                table: "tb_Usuario",
                column: "organizacion_id");

            migrationBuilder.CreateIndex(
                name: "IX_tb_Usuario_rol_id",
                table: "tb_Usuario",
                column: "rol_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tb_BitacoraIncidente");

            migrationBuilder.DropTable(
                name: "tb_ContratoSLA_Detalle");

            migrationBuilder.DropTable(
                name: "tb_EvidenciaIncidente");

            migrationBuilder.DropTable(
                name: "tb_LogAuditoria");

            migrationBuilder.DropTable(
                name: "tb_RefreshToken");

            migrationBuilder.DropTable(
                name: "tb_ContratoSLA");

            migrationBuilder.DropTable(
                name: "tb_Incidente");

            migrationBuilder.DropTable(
                name: "tb_Activo");

            migrationBuilder.DropTable(
                name: "tb_EstadoIncidente");

            migrationBuilder.DropTable(
                name: "tb_Severidad");

            migrationBuilder.DropTable(
                name: "tb_Usuario");

            migrationBuilder.DropTable(
                name: "tb_CategoriaActivo");

            migrationBuilder.DropTable(
                name: "tb_Organizacion");

            migrationBuilder.DropTable(
                name: "tb_Rol");

            migrationBuilder.DropTable(
                name: "tb_PlanServicio");
        }
    }
}
