-- Database: VC-semilleros

-- DROP DATABASE IF EXISTS "VC-semilleros";

CREATE DATABASE "VC-semilleros"
    WITH
    OWNER = postgres
    ENCODING = 'UTF8'
    LC_COLLATE = 'Spanish_Mexico.1252'
    LC_CTYPE = 'Spanish_Mexico.1252'
    LOCALE_PROVIDER = 'libc'
    TABLESPACE = pg_default
    CONNECTION LIMIT = -1
    IS_TEMPLATE = False;

----- DAB

------------------------- ENUMS
CREATE TYPE rol_usuario AS ENUM ('ESTUDIANTE','COORDINADOR','DOCENTE');
CREATE TYPE estado_credencial AS ENUM ('VIGENTE','SUSPENDIDA','REVOCADA', 'INDETERMINADA');
CREATE TYPE estado_emision AS ENUM ('SOLICITADA','EMITIDA','    FALLIDA');

-- CONTRATO
CREATE TYPE estado_anclaje AS ENUM ('PENDIENTE','ANCLADO', 'FALLIDO');  

CREATE TABLE Administrador (
  id SERIAL PRIMARY KEY,
  --institucion_id INT NOT NULL REFERENCES institucion(id),
  nombre VARCHAR(120) NOT NULL,
  correo VARCHAR(120) UNIQUE NOT NULL,
  contrasena_hash VARCHAR(200) NOT NULL  -- NUEVO (enmienda Art. 6)
);

-- PARA LOGIN
CREATE TABLE Usuario (
  id SERIAL PRIMARY KEY,
  --institucion_id INT NOT NULL REFERENCES institucion(id),
  nombre VARCHAR(120) NOT NULL,
  correo VARCHAR(120) UNIQUE NOT NULL,
  rol rol_usuario NOT NULL,
  creado_por INT REFERENCES administrador(id),
  activo BOOLEAN NOT NULL DEFAULT TRUE,
  contrasena_hash VARCHAR(200) NOT NULL  -- NUEVO (enmienda Art. 6)
);

CREATE TABLE Semillero (
  id SERIAL PRIMARY KEY,
  --institucion_id INT NOT NULL REFERENCES institucion(id),
  responsable_id INT NOT NULL REFERENCES usuario(id), -- docente
  nombre VARCHAR(120) NOT NULL,
  activo BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE Periodo (
  id SERIAL PRIMARY KEY,
  semillero_id INT NOT NULL REFERENCES semillero(id),
  nombre VARCHAR(20) NOT NULL,
  fecha_inicio DATE NOT NULL,
  fecha_fin DATE NOT NULL
);

-- GESTION DE DOCENTE
CREATE TABLE Participante (
  id SERIAL PRIMARY KEY,
  periodo_id INT NOT NULL REFERENCES periodo(id),
  estudiante_id INT NOT NULL REFERENCES usuario(id),
  UNIQUE (periodo_id, estudiante_id)
);

-- PARA DOCENTE
CREATE TABLE Sesion (
  id SERIAL PRIMARY KEY,
  periodo_id INT NOT NULL REFERENCES periodo(id),
  fecha DATE NOT NULL,
  descripcion VARCHAR(200)
);

-- PARA DOCENTE
CREATE TABLE Asistencia (
  id SERIAL PRIMARY KEY,
  sesion_id INT NOT NULL REFERENCES sesion(id),
  participante_id INT NOT NULL REFERENCES participante(id),
  presente BOOLEAN NOT NULL DEFAULT TRUE,
  UNIQUE (sesion_id, participante_id)
);

-- PARA VALIDAR SI CUMPLE CON LA CREDENCIAL -- COORDINADOR
-- CRITERIO + EVALUACIÓN (COORDINADOR)
CREATE TABLE Evaluacion (
  id SERIAL PRIMARY KEY,
  participante_id INT NOT NULL REFERENCES participante(id),
  -- criterio aplicado (se guarda tal como estaba al evaluar)
  criterio_version INT NOT NULL DEFAULT 1,
  min_asistencia_pct NUMERIC(5,2) NOT NULL,
  definido_por INT NOT NULL REFERENCES usuario(id),  -- coordinador que define la regla
  -- resultado
  pct_asistencia NUMERIC(5,2) NOT NULL,
  cumple BOOLEAN NOT NULL,
  elegible_declarado_por INT REFERENCES usuario(id), -- coordinador
  fecha TIMESTAMP NOT NULL DEFAULT now(),
  UNIQUE (participante_id, criterio_version)
);


CREATE TABLE Credencial (
  codigo VARCHAR(40) PRIMARY KEY,
  evaluacion_id INT UNIQUE NOT NULL REFERENCES evaluacion(id),
  titular_id INT NOT NULL REFERENCES usuario(id),
  --autorizada_por INT NOT NULL REFERENCES usuario(id),  -- coordinador
  tipo VARCHAR(60) NOT NULL,
  --version_perfil VARCHAR(20),
  estado estado_credencial NOT NULL DEFAULT 'VIGENTE',
  vc_hash VARCHAR(128) NOT NULL,
  fecha_emision TIMESTAMP NOT NULL DEFAULT now(),
  -- revocación (ya no hay tabla de eventos)
  revocada_por INT REFERENCES usuario(id),
  motivo_revocacion TEXT,
  fecha_revocacion TIMESTAMP
);

CREATE TABLE Anclaje_stellar (
  id SERIAL PRIMARY KEY,
  credencial_codigo VARCHAR(40) NOT NULL REFERENCES credencial(codigo),
  evidencia_hash VARCHAR(128) NOT NULL,
  tx_hash VARCHAR(64),
  estado estado_anclaje NOT NULL DEFAULT 'PENDIENTE',
  attestation_uid VARCHAR(64),  --PARA CONSULTAR EN LA TESTNET
  fecha_Solicitud TIMESTAMP NOT NULL DEFAULT now(),
  fecha_expiracion TIMESTAMP   -- nullable: no todo anclaje expira
);

CREATE TABLE Permiso (
  id SERIAL PRIMARY KEY,
  nombre VARCHAR(60) UNIQUE NOT NULL,  -- ej. 'REVOCAR_CREDENCIAL'
  descripcion VARCHAR(200)
);

CREATE TABLE Rol_permiso (
  rol rol_usuario NOT NULL,
  permiso_id INT NOT NULL REFERENCES permiso(id),
  PRIMARY KEY (rol, permiso_id)
);


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