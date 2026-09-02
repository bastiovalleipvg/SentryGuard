
USE master;
GO

/* ============================================================
   1. CREACIÓN DE LA BASE DE DATOS
   ============================================================ */

IF DB_ID(N'SentryGuardDB') IS NULL
BEGIN
    CREATE DATABASE SentryGuardDB;
END
GO

USE SentryGuardDB;
GO


/* ============================================================
   2. TABLA: tb_Rol
   Catálogo de roles del sistema
   ============================================================ */

CREATE TABLE tb_Rol
(
    id              INT IDENTITY(1,1) NOT NULL,
    nombre          NVARCHAR(50) NOT NULL,

    CONSTRAINT PK_tb_Rol
        PRIMARY KEY (id),

    CONSTRAINT UQ_tb_Rol_Nombre
        UNIQUE (nombre)
);
GO


/* ============================================================
   3. TABLA: tb_PlanServicio
   Catálogo de planes contratados
   ============================================================ */

CREATE TABLE tb_PlanServicio
(
    id                          INT IDENTITY(1,1) NOT NULL,
    nombre                      NVARCHAR(50) NOT NULL,
    descripcion                 NVARCHAR(500) NULL,
    tiempo_respuesta_base_h     DECIMAL(10,2) NOT NULL,

    CONSTRAINT PK_tb_PlanServicio
        PRIMARY KEY (id),

    CONSTRAINT UQ_tb_PlanServicio_Nombre
        UNIQUE (nombre),

    CONSTRAINT CK_tb_PlanServicio_TiempoRespuesta
        CHECK (tiempo_respuesta_base_h > 0)
);
GO


/* ============================================================
   4. TABLA: tb_CategoriaActivo
   Catálogo de tipos de activos tecnológicos
   ============================================================ */

CREATE TABLE tb_CategoriaActivo
(
    id              INT IDENTITY(1,1) NOT NULL,
    nombre          NVARCHAR(50) NOT NULL,

    CONSTRAINT PK_tb_CategoriaActivo
        PRIMARY KEY (id),

    CONSTRAINT UQ_tb_CategoriaActivo_Nombre
        UNIQUE (nombre)
);
GO


/* ============================================================
   5. TABLA: tb_Severidad
   Catálogo de severidades — fuente única de horas_sla por defecto
   ============================================================ */

CREATE TABLE tb_Severidad
(
    id              INT IDENTITY(1,1) NOT NULL,
    nombre          NVARCHAR(50) NOT NULL,
    horas_sla       DECIMAL(10,2) NOT NULL,

    CONSTRAINT PK_tb_Severidad
        PRIMARY KEY (id),

    CONSTRAINT UQ_tb_Severidad_Nombre
        UNIQUE (nombre),

    CONSTRAINT CK_tb_Severidad_HorasSLA
        CHECK (horas_sla > 0)
);
GO


/* ============================================================
   6. TABLA: tb_EstadoIncidente
   Catálogo de estados del ciclo de vida
   ============================================================ */

CREATE TABLE tb_EstadoIncidente
(
    id              INT IDENTITY(1,1) NOT NULL,
    nombre          NVARCHAR(50) NOT NULL,

    CONSTRAINT PK_tb_EstadoIncidente
        PRIMARY KEY (id),

    CONSTRAINT UQ_tb_EstadoIncidente_Nombre
        UNIQUE (nombre)
);
GO


/* ============================================================
   7. TABLA: tb_Organizacion
   Empresas cliente del servicio MSSP
   ============================================================ */

CREATE TABLE tb_Organizacion
(
    id                  INT IDENTITY(1,1) NOT NULL,
    razon_social        NVARCHAR(200) NOT NULL,
    rut                 VARCHAR(10) NOT NULL,   -- formato NNNNNNNN-X, sin puntos
    plan_contratado_id  INT NOT NULL,

    fecha_alta          DATETIME2(0) NOT NULL
                        CONSTRAINT DF_tb_Organizacion_FechaAlta
                        DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_tb_Organizacion
        PRIMARY KEY (id),

    CONSTRAINT UQ_tb_Organizacion_RUT
        UNIQUE (rut),

    CONSTRAINT FK_tb_Organizacion_tb_PlanServicio
        FOREIGN KEY (plan_contratado_id)
        REFERENCES tb_PlanServicio(id)
);
GO


/* ============================================================
   8. TABLA: tb_ContratoSLA
   Cabecera del SLA vigente por organización.
   ============================================================ */

CREATE TABLE tb_ContratoSLA
(
    id                  INT IDENTITY(1,1) NOT NULL,
    organizacion_id     INT NOT NULL,

    vigente_desde       DATETIME2(0) NOT NULL,
    vigente_hasta       DATETIME2(0) NULL,

    CONSTRAINT PK_tb_ContratoSLA
        PRIMARY KEY (id),

    CONSTRAINT FK_tb_ContratoSLA_tb_Organizacion
        FOREIGN KEY (organizacion_id)
        REFERENCES tb_Organizacion(id),

    CONSTRAINT CK_tb_ContratoSLA_Fechas
        CHECK
        (
            vigente_hasta IS NULL
            OR vigente_hasta > vigente_desde
        )
);
GO

CREATE UNIQUE INDEX UQ_tb_ContratoSLA_Vigente
    ON tb_ContratoSLA(organizacion_id)
    WHERE vigente_hasta IS NULL;
GO


/* ============================================================
   8.1 TABLA: tb_ContratoSLA_Detalle  (NUEVA)
   Horas SLA negociadas por contrato y severidad.
   ============================================================ */

CREATE TABLE tb_ContratoSLA_Detalle
(
    id              INT IDENTITY(1,1) NOT NULL,
    contrato_id     INT NOT NULL,
    severidad_id    INT NOT NULL,
    horas_sla       DECIMAL(10,2) NOT NULL,

    CONSTRAINT PK_tb_ContratoSLA_Detalle
        PRIMARY KEY (id),

    CONSTRAINT UQ_tb_ContratoSLA_Detalle
        UNIQUE (contrato_id, severidad_id),

    CONSTRAINT FK_tb_ContratoSLA_Detalle_tb_ContratoSLA
        FOREIGN KEY (contrato_id)
        REFERENCES tb_ContratoSLA(id),

    CONSTRAINT FK_tb_ContratoSLA_Detalle_tb_Severidad
        FOREIGN KEY (severidad_id)
        REFERENCES tb_Severidad(id),

    CONSTRAINT CK_tb_ContratoSLA_Detalle_Horas
        CHECK (horas_sla > 0)
);
GO


/* ============================================================
   9. TABLA: tb_Usuario
   Cuentas de usuarios del sistema.
   ============================================================ */

CREATE TABLE tb_Usuario
(
    id                  INT IDENTITY(1,1) NOT NULL,
    nombre              NVARCHAR(150) NOT NULL,
    email               NVARCHAR(320) NOT NULL,
    password_hash       NVARCHAR(500) NOT NULL,

    rol_id              INT NOT NULL,
    organizacion_id     INT NULL,

    activo              BIT NOT NULL
                        CONSTRAINT DF_tb_Usuario_Activo
                        DEFAULT 1,

    intentos_fallidos   INT NOT NULL
                        CONSTRAINT DF_tb_Usuario_IntentosFallidos
                        DEFAULT 0,

    bloqueado_hasta     DATETIME2(0) NULL,

    CONSTRAINT PK_tb_Usuario
        PRIMARY KEY (id),

    CONSTRAINT UQ_tb_Usuario_Email
        UNIQUE (email),

    CONSTRAINT FK_tb_Usuario_tb_Rol
        FOREIGN KEY (rol_id)
        REFERENCES tb_Rol(id),

    CONSTRAINT FK_tb_Usuario_tb_Organizacion
        FOREIGN KEY (organizacion_id)
        REFERENCES tb_Organizacion(id),

    CONSTRAINT CK_tb_Usuario_IntentosFallidos
        CHECK (intentos_fallidos >= 0)
);
GO

/* ============================================================
   9.1 TABLA: tb_RefreshToken  (NUEVA)
   Persiste los refresh tokens emitidos para poder revocarlos
   (logout, cambio de contraseña, compromiso de cuenta).
   ============================================================ */

CREATE TABLE tb_RefreshToken
(
    id              BIGINT IDENTITY(1,1) NOT NULL,
    usuario_id      INT NOT NULL,
    token_hash      NVARCHAR(500) NOT NULL,

    fecha_emision   DATETIME2(0) NOT NULL
                    CONSTRAINT DF_tb_RefreshToken_FechaEmision
                    DEFAULT SYSUTCDATETIME(),

    fecha_expira    DATETIME2(0) NOT NULL,

    revocado        BIT NOT NULL
                    CONSTRAINT DF_tb_RefreshToken_Revocado
                    DEFAULT 0,

    CONSTRAINT PK_tb_RefreshToken
        PRIMARY KEY (id),

    CONSTRAINT FK_tb_RefreshToken_tb_Usuario
        FOREIGN KEY (usuario_id)
        REFERENCES tb_Usuario(id),

    CONSTRAINT CK_tb_RefreshToken_Fechas
        CHECK (fecha_expira > fecha_emision)
);
GO


/* ============================================================
   10. TABLA: tb_Activo
   Activos tecnológicos monitoreados
   ============================================================ */

CREATE TABLE tb_Activo
(
    id                  INT IDENTITY(1,1) NOT NULL,
    organizacion_id     INT NOT NULL,
    nombre              NVARCHAR(150) NOT NULL,
    hostname_ip         NVARCHAR(255) NULL,
    categoria_id        INT NOT NULL,

    CONSTRAINT PK_tb_Activo
        PRIMARY KEY (id),

    CONSTRAINT FK_tb_Activo_tb_Organizacion
        FOREIGN KEY (organizacion_id)
        REFERENCES tb_Organizacion(id),

    CONSTRAINT FK_tb_Activo_tb_CategoriaActivo
        FOREIGN KEY (categoria_id)
        REFERENCES tb_CategoriaActivo(id)
);
GO


/* ============================================================
   11. TABLA: tb_Incidente
   Recurso principal de la aplicación.
   ============================================================ */

CREATE TABLE tb_Incidente
(
    id                      INT IDENTITY(1,1) NOT NULL,

    organizacion_id         INT NOT NULL,
    activo_id               INT NOT NULL,

    titulo                  NVARCHAR(250) NOT NULL,
    descripcion             NVARCHAR(MAX) NOT NULL,

    severidad_id            INT NOT NULL,
    estado_id               INT NOT NULL,

    analista_asignado_id    INT NULL,

    fecha_creacion          DATETIME2(0) NOT NULL
                            CONSTRAINT DF_tb_Incidente_FechaCreacion
                            DEFAULT SYSUTCDATETIME(),

    fecha_cierre            DATETIME2(0) NULL,

    row_version             ROWVERSION,

    CONSTRAINT PK_tb_Incidente
        PRIMARY KEY (id),

    CONSTRAINT FK_tb_Incidente_tb_Organizacion
        FOREIGN KEY (organizacion_id)
        REFERENCES tb_Organizacion(id),

    CONSTRAINT FK_tb_Incidente_tb_Activo
        FOREIGN KEY (activo_id)
        REFERENCES tb_Activo(id),

    CONSTRAINT FK_tb_Incidente_tb_Severidad
        FOREIGN KEY (severidad_id)
        REFERENCES tb_Severidad(id),

    CONSTRAINT FK_tb_Incidente_tb_EstadoIncidente
        FOREIGN KEY (estado_id)
        REFERENCES tb_EstadoIncidente(id),

    CONSTRAINT FK_tb_Incidente_tb_Usuario
        FOREIGN KEY (analista_asignado_id)
        REFERENCES tb_Usuario(id),

    CONSTRAINT CK_tb_Incidente_Fechas
        CHECK
        (
            fecha_cierre IS NULL
            OR fecha_cierre >= fecha_creacion
        )
);
GO

/* ============================================================
   12. TABLA: tb_BitacoraIncidente
   Historial de acciones sobre incidentes
   ============================================================ */

CREATE TABLE tb_BitacoraIncidente
(
    id              BIGINT IDENTITY(1,1) NOT NULL,
    incidente_id    INT NOT NULL,
    usuario_id      INT NOT NULL,

    accion          NVARCHAR(100) NOT NULL,
    comentario      NVARCHAR(MAX) NULL,

    [timestamp]     DATETIME2(0) NOT NULL
                    CONSTRAINT DF_tb_BitacoraIncidente_Timestamp
                    DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_tb_BitacoraIncidente
        PRIMARY KEY (id),

    CONSTRAINT FK_tb_BitacoraIncidente_tb_Incidente
        FOREIGN KEY (incidente_id)
        REFERENCES tb_Incidente(id),

    CONSTRAINT FK_tb_BitacoraIncidente_tb_Usuario
        FOREIGN KEY (usuario_id)
        REFERENCES tb_Usuario(id)
);
GO


/* ============================================================
   13. TABLA: tb_EvidenciaIncidente
   Archivos asociados a un incidente
   ============================================================ */

CREATE TABLE tb_EvidenciaIncidente
(
    id                  BIGINT IDENTITY(1,1) NOT NULL,
    incidente_id        INT NOT NULL,

    nombre_archivo      NVARCHAR(255) NOT NULL,
    ruta_almacenamiento NVARCHAR(1000) NOT NULL,

    subido_por_id       INT NOT NULL,

    fecha               DATETIME2(0) NOT NULL
                        CONSTRAINT DF_tb_EvidenciaIncidente_Fecha
                        DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_tb_EvidenciaIncidente
        PRIMARY KEY (id),

    CONSTRAINT FK_tb_EvidenciaIncidente_tb_Incidente
        FOREIGN KEY (incidente_id)
        REFERENCES tb_Incidente(id),

    CONSTRAINT FK_tb_EvidenciaIncidente_tb_Usuario
        FOREIGN KEY (subido_por_id)
        REFERENCES tb_Usuario(id)
);
GO


/* ============================================================
   14. TABLA: tb_LogAuditoria
   Registro de seguridad a nivel de sistema
   ============================================================ */

CREATE TABLE tb_LogAuditoria
(
    id              BIGINT IDENTITY(1,1) NOT NULL,

    usuario_id      INT NULL,
    accion          NVARCHAR(150) NOT NULL,
    ip_origen       VARCHAR(45) NULL,

    [timestamp]     DATETIME2(0) NOT NULL
                    CONSTRAINT DF_tb_LogAuditoria_Timestamp
                    DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_tb_LogAuditoria
        PRIMARY KEY (id),

    CONSTRAINT FK_tb_LogAuditoria_tb_Usuario
        FOREIGN KEY (usuario_id)
        REFERENCES tb_Usuario(id)
);
GO


/* ============================================================
   15. ÍNDICES
   ============================================================ */

/* ---------- Organizacion ---------- */

CREATE INDEX IX_tb_Organizacion_Plan
    ON tb_Organizacion(plan_contratado_id);


/* ---------- ContratoSLA ---------- */

CREATE INDEX IX_tb_ContratoSLA_Organizacion
    ON tb_ContratoSLA(organizacion_id);

CREATE INDEX IX_tb_ContratoSLA_Vigencia
    ON tb_ContratoSLA
    (
        organizacion_id,
        vigente_desde,
        vigente_hasta
    );

/* ---------- ContratoSLA_Detalle ---------- */

CREATE INDEX IX_tb_ContratoSLA_Detalle_Contrato
    ON tb_ContratoSLA_Detalle(contrato_id);

CREATE INDEX IX_tb_ContratoSLA_Detalle_Severidad
    ON tb_ContratoSLA_Detalle(severidad_id);


/* ---------- Usuario ---------- */

CREATE INDEX IX_tb_Usuario_Rol
    ON tb_Usuario(rol_id);

CREATE INDEX IX_tb_Usuario_Organizacion
    ON tb_Usuario(organizacion_id);


/* ---------- RefreshToken ---------- */

CREATE INDEX IX_tb_RefreshToken_Usuario
    ON tb_RefreshToken(usuario_id);

CREATE INDEX IX_tb_RefreshToken_Expira
    ON tb_RefreshToken(fecha_expira);


/* ---------- Activo ---------- */

CREATE INDEX IX_tb_Activo_Organizacion
    ON tb_Activo(organizacion_id);

CREATE INDEX IX_tb_Activo_Categoria
    ON tb_Activo(categoria_id);


/* ---------- Incidente ---------- */

CREATE INDEX IX_tb_Incidente_Organizacion
    ON tb_Incidente(organizacion_id);

CREATE INDEX IX_tb_Incidente_Activo
    ON tb_Incidente(activo_id);

CREATE INDEX IX_tb_Incidente_Severidad
    ON tb_Incidente(severidad_id);

CREATE INDEX IX_tb_Incidente_Estado
    ON tb_Incidente(estado_id);

CREATE INDEX IX_tb_Incidente_Analista
    ON tb_Incidente(analista_asignado_id);

CREATE INDEX IX_tb_Incidente_FechaCreacion
    ON tb_Incidente(fecha_creacion);


/* ---------- BitacoraIncidente ---------- */

CREATE INDEX IX_tb_BitacoraIncidente_Incidente
    ON tb_BitacoraIncidente(incidente_id);

CREATE INDEX IX_tb_BitacoraIncidente_Usuario
    ON tb_BitacoraIncidente(usuario_id);

CREATE INDEX IX_tb_BitacoraIncidente_Timestamp
    ON tb_BitacoraIncidente([timestamp]);


/* ---------- EvidenciaIncidente ---------- */

CREATE INDEX IX_tb_EvidenciaIncidente_Incidente
    ON tb_EvidenciaIncidente(incidente_id);

CREATE INDEX IX_tb_EvidenciaIncidente_Usuario
    ON tb_EvidenciaIncidente(subido_por_id);


/* ---------- LogAuditoria ---------- */

CREATE INDEX IX_tb_LogAuditoria_Usuario
    ON tb_LogAuditoria(usuario_id);

CREATE INDEX IX_tb_LogAuditoria_Timestamp
    ON tb_LogAuditoria([timestamp]);
GO


/* ============================================================
   16. DATOS INICIALES DE CATÁLOGOS
   ============================================================ */

/* ---------- Roles ---------- */

INSERT INTO tb_Rol (nombre)
VALUES
    (N'Analista'),
    (N'Supervisor'),
    (N'Administrador'),
    (N'Cliente');
GO


/* ---------- Planes ---------- */

INSERT INTO tb_PlanServicio
(
    nombre,
    descripcion,
    tiempo_respuesta_base_h
)
VALUES
    (
        N'Básico',
        N'Plan básico de monitoreo y respuesta.',
        24.00
    ),
    (
        N'Estándar',
        N'Plan estándar de monitoreo y respuesta.',
        12.00
    ),
    (
        N'Premium',
        N'Plan premium de monitoreo y respuesta.',
        4.00
    );
GO


/* ---------- Categorías de activos ---------- */

INSERT INTO tb_CategoriaActivo (nombre)
VALUES
    (N'Servidor'),
    (N'Endpoint'),
    (N'Red'),
    (N'Aplicación'),
    (N'Nube');
GO


/* ---------- Severidades ---------- */

INSERT INTO tb_Severidad
(
    nombre,
    horas_sla
)
VALUES
    (N'Baja', 24.00),
    (N'Media', 12.00),
    (N'Alta', 4.00),
    (N'Crítica', 1.00);
GO


/* ---------- Estados de incidentes ---------- */

INSERT INTO tb_EstadoIncidente (nombre)
VALUES
    (N'Abierto'),
    (N'En progreso'),
    (N'Escalado'),
    (N'Resuelto'),
    (N'Cerrado');
GO
