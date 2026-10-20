-- Database: VC-semilleros
-- DROP DATABASE IF EXISTS "VC-semilleros";
-- CREATE DATABASE "VC-semilleros"
--     WITH
--     OWNER = postgres
--     ENCODING = 'UTF8'
--     LC_COLLATE = 'Spanish_Mexico.1252'
--     LC_CTYPE = 'Spanish_Mexico.1252'
--     LOCALE_PROVIDER = 'libc'
--     TABLESPACE = pg_default
--     CONNECTION LIMIT = -1
--     IS_TEMPLATE = False;

-- =========================================================
-- TIPOS ENUMERADOS
-- =========================================================
CREATE TYPE rol_usuario AS ENUM ('ESTUDIANTE', 'COORDINADOR', 'DOCENTE');

CREATE TYPE estado_credencial AS ENUM (
    'VIGENTE', 'SUSPENDIDA', 'REVOCADA', 'INDETERMINADA'
);

CREATE TYPE estado_emision AS ENUM (
    'SOLICITADA', 'EMITIDA', 'FALLIDA'
);

CREATE TYPE estado_anclaje AS ENUM (
    'PENDIENTE', 'ANCLADO', 'FALLIDO'
);

-- =========================================================
-- ADMINISTRADORES Y USUARIOS
-- =========================================================
CREATE TABLE Administrador (
    id SERIAL PRIMARY KEY,
    --institucion_id INT NOT NULL REFERENCES institucion(id),
    nombre VARCHAR(120) NOT NULL,
    correo VARCHAR(120) UNIQUE NOT NULL,
    contrasena_hash VARCHAR(200) NOT NULL
);

CREATE TABLE Usuario (
    id SERIAL PRIMARY KEY,
    --institucion_id INT NOT NULL REFERENCES institucion(id),
    nombre VARCHAR(120) NOT NULL,
    correo VARCHAR(120) UNIQUE NOT NULL,
    rol rol_usuario NOT NULL,
    creado_por INT REFERENCES Administrador(id),
    activo BOOLEAN NOT NULL DEFAULT TRUE,
    contrasena_hash VARCHAR(200) NOT NULL
);

-- =========================================================
-- SEMILLEROS, PERIODOS Y CRITERIOS
-- =========================================================
CREATE TABLE Semillero (
    id SERIAL PRIMARY KEY,
    --institucion_id INT NOT NULL REFERENCES institucion(id),
    responsable_id INT NOT NULL REFERENCES Usuario(id),
    nombre VARCHAR(120) NOT NULL,
    activo BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE Periodo (
    id SERIAL PRIMARY KEY,
    semillero_id INT NOT NULL REFERENCES Semillero(id),
    nombre VARCHAR(20) NOT NULL,
    fecha_inicio DATE NOT NULL,
    fecha_fin DATE NOT NULL
);

-- Criterios definidos por el coordinador para cada periodo.
-- Se mantiene separado de Evaluacion para permitir configurar
-- los criterios antes de evaluar a cualquier participante.
CREATE TABLE CriterioPeriodo (
    id SERIAL PRIMARY KEY,
    periodo_id INT NOT NULL UNIQUE REFERENCES Periodo(id),
    criterio_version INT NOT NULL DEFAULT 1,
    min_asistencia_pct NUMERIC(5,2) NOT NULL
        CHECK (min_asistencia_pct BETWEEN 0 AND 100),
    entregables_requeridos_pct NUMERIC(5,2) NOT NULL
        CHECK (entregables_requeridos_pct BETWEEN 0 AND 100),
    permanencia_meses INT NOT NULL
        CHECK (permanencia_meses >= 0),
    definido_por INT NOT NULL REFERENCES Usuario(id),
    fecha_actualizacion TIMESTAMP NOT NULL DEFAULT now()
);

-- =========================================================
-- PARTICIPANTES, SESIONES Y ASISTENCIAS
-- =========================================================
CREATE TABLE Participante (
    id SERIAL PRIMARY KEY,
    periodo_id INT NOT NULL REFERENCES Periodo(id),
    estudiante_id INT NOT NULL REFERENCES Usuario(id),
    UNIQUE (periodo_id, estudiante_id)
);

CREATE TABLE Sesion (
    id SERIAL PRIMARY KEY,
    periodo_id INT NOT NULL REFERENCES Periodo(id),
    fecha DATE NOT NULL,
    descripcion VARCHAR(200)
);

CREATE TABLE Asistencia (
    id SERIAL PRIMARY KEY,
    sesion_id INT NOT NULL REFERENCES Sesion(id),
    participante_id INT NOT NULL REFERENCES Participante(id),
    presente BOOLEAN NOT NULL DEFAULT TRUE,
    UNIQUE (sesion_id, participante_id)
);

-- =========================================================
-- EVALUACIONES, CREDENCIALES Y ANCLAJES STELLAR
-- =========================================================
CREATE TABLE Evaluacion (
    id SERIAL PRIMARY KEY,
    participante_id INT NOT NULL REFERENCES Participante(id),
    -- criterio aplicado (se guarda tal como estaba al evaluar)
    criterio_version INT NOT NULL DEFAULT 1,
    min_asistencia_pct NUMERIC(5,2) NOT NULL,
    definido_por INT NOT NULL REFERENCES Usuario(id),
    -- resultado
    pct_asistencia NUMERIC(5,2) NOT NULL,
    cumple BOOLEAN NOT NULL,
    elegible_declarado_por INT REFERENCES Usuario(id),
    fecha TIMESTAMP NOT NULL DEFAULT now(),
    UNIQUE (participante_id, criterio_version)
);

CREATE TABLE Credencial (
    codigo VARCHAR(40) PRIMARY KEY,
    evaluacion_id INT UNIQUE NOT NULL REFERENCES Evaluacion(id),
    titular_id INT NOT NULL REFERENCES Usuario(id),
    --autorizada_por INT NOT NULL REFERENCES usuario(id),
    tipo VARCHAR(60) NOT NULL,
    --version_perfil VARCHAR(20),
    estado estado_credencial NOT NULL DEFAULT 'VIGENTE',
    vc_hash VARCHAR(128) NOT NULL,
    fecha_emision TIMESTAMP NOT NULL DEFAULT now(),
    revocada_por INT REFERENCES Usuario(id),
    motivo_revocacion TEXT,
    fecha_revocacion TIMESTAMP
);

CREATE TABLE Anclaje_stellar (
    id SERIAL PRIMARY KEY,
    credencial_codigo VARCHAR(40) NOT NULL REFERENCES Credencial(codigo),
    evidencia_hash VARCHAR(128) NOT NULL,
    tx_hash VARCHAR(64),
    estado estado_anclaje NOT NULL DEFAULT 'PENDIENTE',
    attestation_uid VARCHAR(64),
    fecha_solicitud TIMESTAMP NOT NULL DEFAULT now(),
    fecha_expiracion TIMESTAMP
);

-- =========================================================
-- PERMISOS Y RELACIÓN ENTRE ROLES Y PERMISOS
-- =========================================================
CREATE TABLE Permiso (
    id SERIAL PRIMARY KEY,
    nombre VARCHAR(60) UNIQUE NOT NULL,
    descripcion VARCHAR(200)
);

CREATE TABLE Rol_permiso (
    rol rol_usuario NOT NULL,
    permiso_id INT NOT NULL REFERENCES Permiso(id),
    PRIMARY KEY (rol, permiso_id)
);

-- =========================================================
-- DEFINICIONES OPCIONALES/ANTERIORES CONSERVADAS DE SCRIPT.sql
-- =========================================================
/* -- Se puede eliminar y poner autorizada_por directo en credencial.
CREATE TABLE solicitud_emision (
  id SERIAL PRIMARY KEY,
  evaluacion_id INT UNIQUE NOT NULL REFERENCES evaluacion(id),
  autorizada_por INT NOT NULL REFERENCES usuario(id),
  estado estado_emision NOT NULL DEFAULT 'SOLICITADA',
  fecha TIMESTAMP NOT NULL DEFAULT now()
);
*/

/*
CREATE TABLE credencial_evento (
  id SERIAL PRIMARY KEY,
  credencial_codigo VARCHAR(40) NOT NULL REFERENCES credencial(codigo),
  tipo_evento VARCHAR(30) NOT NULL, -- EMITIDA, SUSPENDIDA, REVOCADA...
  actor_id INT REFERENCES usuario(id), -- NULL = sistema
  motivo TEXT,
  hash_previo VARCHAR(128),
  hash_evento VARCHAR(128) NOT NULL,
  fecha TIMESTAMP NOT NULL DEFAULT now()
);
*/

/*
CREATE TABLE Institucion (
  id SERIAL PRIMARY KEY,
  nombre VARCHAR(150) NOT NULL
);
*/

/*
CREATE TABLE credencial (
  codigo VARCHAR(40) PRIMARY KEY,
  --solicitud_id INT UNIQUE NOT NULL REFERENCES solicitud_emision(id),
  titular_id INT NOT NULL REFERENCES usuario(id),
  tipo VARCHAR(60) NOT NULL,
  version_perfil VARCHAR(20),
  estado estado_credencial NOT NULL DEFAULT 'VIGENTE', -- el unico que cambia estado es el coordi
  vc_hash VARCHAR(128) NOT NULL,
  fecha_emision TIMESTAMP NOT NULL DEFAULT now(),
)*/

-- AUDITORIA

-- =========================================================
-- DATOS DE PRUEBA Y CONSULTAS (contenido conservado de CRIPT 2.sql)
-- =========================================================

INSERT INTO public.administrador (nombre, correo, contrasena_hash)
VALUES (
    'Administrador',
    'admin.prueba@example.com',
    'pbkdf2-sha256$100000$WTgxmI1/kwHJ8hXzVeV57g==$kF/LzWoQ3fvvlMdyeOvu1RJWFwF7409k3wOnCPLvMgE='
);

SELECT id, nombre, correo, rol, activo
FROM usuario
ORDER BY id;


INSERT INTO usuario (
    nombre,
    correo,
    rol,
    creado_por,
    activo,
    contrasena_hash
)
SELECT
    'Docente Prueba',
    'docente.prueba@example.com',
    'DOCENTE'::rol_usuario,
    NULL,
    TRUE,
    'pbkdf2-sha256$100000$rpVdIVYOqHg4f5YZsLHDsg==$sZvaIjKoESDD7niGQz4aR/4Xlzdi7dhAP0ylG36ezM8='
WHERE NOT EXISTS (
    SELECT 1
    FROM usuario
    WHERE correo = 'docente.prueba@example.com'
)
AND NOT EXISTS (
    SELECT 1
    FROM administrador
    WHERE correo = 'docente.prueba@example.com'
)
RETURNING id, nombre, correo, rol, activo;


SELECT column_name, data_type
FROM information_schema.columns
WHERE table_schema = 'public'
  AND lower(table_name) = 'criterioperiodo'
ORDER BY ordinal_position;

BEGIN;

-- Coordinador de prueba
INSERT INTO public.usuario
    (nombre, correo, rol, creado_por, activo, contrasena_hash)
SELECT
    'Coordinador Prueba',
    'coordinador.prueba@example.com',
    'COORDINADOR'::public.rol_usuario,
    NULL,
    TRUE,
    d.contrasena_hash
FROM public.usuario d
WHERE d.correo = 'docente.prueba@example.com'
  AND NOT EXISTS (
      SELECT 1 FROM public.usuario
      WHERE correo = 'coordinador.prueba@example.com'
  );

-- Estudiantes de prueba
INSERT INTO public.usuario
    (nombre, correo, rol, creado_por, activo, contrasena_hash)
SELECT
    datos.nombre,
    datos.correo,
    'ESTUDIANTE'::public.rol_usuario,
    NULL,
    TRUE,
    d.contrasena_hash
FROM (
    VALUES
        ('Ana Prueba',  'ana.prueba@example.com'),
        ('Luis Prueba', 'luis.prueba@example.com'),
        ('Sara Prueba', 'sara.prueba@example.com')
) AS datos(nombre, correo)
CROSS JOIN public.usuario d
WHERE d.correo = 'docente.prueba@example.com'
  AND NOT EXISTS (
      SELECT 1
      FROM public.usuario existente
      WHERE existente.correo = datos.correo
  )
  AND NOT EXISTS (
      SELECT 1
      FROM public.administrador a
      WHERE a.correo = datos.correo
  );

COMMIT;

-- Comprobar las cuentas
SELECT id, nombre, correo, rol, activo
FROM public.usuario
ORDER BY id;

BEGIN;

-- Semilleros
INSERT INTO public.semillero
    (responsable_id, nombre, activo)
SELECT
    u.id,
    datos.nombre,
    TRUE
FROM public.usuario u
CROSS JOIN (
    VALUES
        ('Semillero de Robótica'),
        ('Semillero de Desarrollo Web')
) AS datos(nombre)
WHERE u.correo = 'docente.prueba@example.com'
  AND NOT EXISTS (
      SELECT 1
      FROM public.semillero s
      WHERE s.nombre = datos.nombre
  );

-- Períodos de prueba
INSERT INTO public.periodo
    (semillero_id, nombre, fecha_inicio, fecha_fin)
SELECT
    s.id,
    datos.nombre,
    datos.fecha_inicio,
    datos.fecha_fin
FROM (
    VALUES
        ('Semillero de Robótica', '2026-2',
         DATE '2026-07-01', DATE '2026-12-31'),
        ('Semillero de Robótica', '2027-1',
         DATE '2027-01-15', DATE '2027-06-30'),
        ('Semillero de Desarrollo Web', '2026-2',
         DATE '2026-07-01', DATE '2026-12-31')
) AS datos(semillero, nombre, fecha_inicio, fecha_fin)
JOIN public.semillero s ON s.nombre = datos.semillero
WHERE NOT EXISTS (
    SELECT 1
    FROM public.periodo p
    WHERE p.semillero_id = s.id
      AND p.nombre = datos.nombre
);

COMMIT;

-- Verificar los períodos
SELECT
    s.nombre AS semillero,
    p.id AS periodo_id,
    p.nombre AS periodo,
    p.fecha_inicio,
    p.fecha_fin
FROM public.periodo p
JOIN public.semillero s ON s.id = p.semillero_id
ORDER BY s.nombre, p.fecha_inicio;

INSERT INTO public.participante
    (periodo_id, estudiante_id)
SELECT
    p.id,
    u.id
FROM public.periodo p
JOIN public.semillero s
    ON s.id = p.semillero_id
JOIN public.usuario u
    ON u.correo IN (
        'ana.prueba@example.com',
        'luis.prueba@example.com',
        'sara.prueba@example.com'
    )
WHERE s.nombre = 'Semillero de Robótica'
  AND p.nombre = '2026-2'
ON CONFLICT (periodo_id, estudiante_id) DO NOTHING;

SELECT
    pa.id AS participante_id,
    u.nombre AS estudiante,
    s.nombre AS semillero,
    p.nombre AS periodo
FROM public.participante pa
JOIN public.usuario u ON u.id = pa.estudiante_id
JOIN public.periodo p ON p.id = pa.periodo_id
JOIN public.semillero s ON s.id = p.semillero_id
ORDER BY pa.id;

BEGIN;

-- Crear las cuatro sesiones si todavía no existen
INSERT INTO public.sesion (periodo_id, fecha, descripcion)
SELECT
    p.id,
    datos.fecha,
    datos.descripcion
FROM public.periodo p
JOIN public.semillero s ON s.id = p.semillero_id
CROSS JOIN (
    VALUES
        (DATE '2026-09-05', 'Introducción al proyecto'),
        (DATE '2026-09-12', 'Diseño del prototipo'),
        (DATE '2026-09-19', 'Construcción del prototipo'),
        (DATE '2026-09-26', 'Pruebas del prototipo')
) AS datos(fecha, descripcion)
WHERE s.nombre = 'Semillero de Robótica'
  AND p.nombre = '2026-2'
  AND NOT EXISTS (
      SELECT 1
      FROM public.sesion se
      WHERE se.periodo_id = p.id
        AND se.fecha = datos.fecha
  );

-- Registrar asistencias
INSERT INTO public.asistencia
    (sesion_id, participante_id, presente)
SELECT
    se.id,
    pa.id,
    CASE
        WHEN u.correo = 'ana.prueba@example.com'
             AND se.fecha = DATE '2026-09-26' THEN FALSE
        WHEN u.correo = 'luis.prueba@example.com'
             AND se.fecha IN (
                 DATE '2026-09-19',
                 DATE '2026-09-26'
             ) THEN FALSE
        ELSE TRUE
    END
FROM public.participante pa
JOIN public.usuario u ON u.id = pa.estudiante_id
JOIN public.periodo p ON p.id = pa.periodo_id
JOIN public.semillero s ON s.id = p.semillero_id
JOIN public.sesion se ON se.periodo_id = p.id
WHERE s.nombre = 'Semillero de Robótica'
  AND p.nombre = '2026-2'
  AND se.fecha IN (
      DATE '2026-09-05',
      DATE '2026-09-12',
      DATE '2026-09-19',
      DATE '2026-09-26'
  )
ON CONFLICT (sesion_id, participante_id) DO NOTHING;

COMMIT;

-- Resumen de asistencia
SELECT
    u.nombre AS estudiante,
    COUNT(a.id) AS sesiones_registradas,
    COUNT(a.id) FILTER (WHERE a.presente) AS asistencias,
    ROUND(
        100.0 * COUNT(a.id) FILTER (WHERE a.presente)
        / NULLIF(COUNT(a.id), 0),
        2
    ) AS porcentaje_asistencia
FROM public.participante pa
JOIN public.usuario u ON u.id = pa.estudiante_id
JOIN public.periodo p ON p.id = pa.periodo_id
JOIN public.semillero s ON s.id = p.semillero_id
LEFT JOIN public.asistencia a ON a.participante_id = pa.id
WHERE s.nombre = 'Semillero de Robótica'
  AND p.nombre = '2026-2'
GROUP BY u.id, u.nombre
ORDER BY u.nombre;

--Otra


INSERT INTO public.criterioperiodo (
    periodo_id,
    criterio_version,
    min_asistencia_pct,
    entregables_requeridos_pct,
    permanencia_meses,
    definido_por
)
SELECT
    p.id,
    1,
    75.00,
    80.00,
    3,
    u.id
FROM public.periodo p
JOIN public.semillero s ON s.id = p.semillero_id
JOIN public.usuario u
    ON u.correo = 'coordinador.prueba@example.com'
WHERE s.nombre = 'Semillero de Robótica'
  AND p.nombre = '2026-2'
ON CONFLICT (periodo_id) DO NOTHING;

SELECT
    cp.id,
    cp.periodo_id,
    cp.criterio_version,
    cp.min_asistencia_pct,
    cp.entregables_requeridos_pct,
    cp.permanencia_meses,
    u.nombre AS coordinador
FROM public.criterioperiodo cp
JOIN public.usuario u ON u.id = cp.definido_por;

SELECT
    id,
    nombre,
    correo,
    rol,
    activo
FROM public.usuario
ORDER BY id;


SELECT
    nombre,
    correo,
    contrasena_hash =
        (SELECT contrasena_hash
         FROM public.usuario
         WHERE correo = 'docente.prueba@example.com')
        AS misma_clave_que_docente
FROM public.usuario
WHERE correo IN (
    'docente.prueba@example.com',
    'coordinador.prueba@example.com'
);
