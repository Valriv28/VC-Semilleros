-- Ejecutar una sola vez sobre la base VC-semilleros ya existente.
CREATE TABLE IF NOT EXISTS parametro_acceso (
    clave VARCHAR(80) PRIMARY KEY,
    valor VARCHAR(200) NOT NULL,
    descripcion VARCHAR(200) NOT NULL,
    actualizado_en TIMESTAMP NOT NULL DEFAULT now()
);

INSERT INTO parametro_acceso (clave, valor, descripcion) VALUES
  ('minutos_expiracion_token', '120', 'Duración prevista de las sesiones autenticadas en minutos.'),
  ('registro_publico_habilitado', 'true', 'Indica si el registro público de cuentas está habilitado.')
ON CONFLICT (clave) DO NOTHING;


-- Catálogo inicial de permisos para la pantalla de administración.
INSERT INTO permiso (nombre, descripcion) VALUES
  ('GESTIONAR_PARTICIPANTES', 'Inscribir y consultar participantes de períodos.'),
  ('REGISTRAR_ASISTENCIA', 'Crear sesiones y registrar asistencias.'),
  ('CONSULTAR_CRITERIOS', 'Consultar criterios vigentes de los períodos.'),
  ('EVALUAR_PARTICIPANTES', 'Calcular y consultar resultados de evaluación.'),
  ('GESTIONAR_CRITERIOS', 'Crear y actualizar criterios de evaluación.'),
  ('EMITIR_CREDENCIAL', 'Emitir credenciales verificables.'),
  ('REVOCAR_CREDENCIAL', 'Revocar credenciales emitidas.'),
  ('CONSULTAR_TRANSACCIONES_BLOCKCHAIN', 'Consultar los registros de anclaje blockchain.'),
  ('COMPARTIR_CREDENCIAL', 'Compartir una credencial verificable.')
ON CONFLICT (nombre) DO NOTHING;

INSERT INTO rol_permiso (rol, permiso_id)
SELECT r.rol::rol_usuario, p.id
FROM (VALUES
    ('DOCENTE', 'GESTIONAR_PARTICIPANTES'),
    ('DOCENTE', 'REGISTRAR_ASISTENCIA'),
    ('DOCENTE', 'CONSULTAR_CRITERIOS'),
    ('DOCENTE', 'EVALUAR_PARTICIPANTES'),
    ('COORDINADOR', 'CONSULTAR_CRITERIOS'),
    ('COORDINADOR', 'GESTIONAR_CRITERIOS'),
    ('COORDINADOR', 'EMITIR_CREDENCIAL'),
    ('COORDINADOR', 'REVOCAR_CREDENCIAL'),
    ('COORDINADOR', 'CONSULTAR_TRANSACCIONES_BLOCKCHAIN'),
    ('ESTUDIANTE', 'COMPARTIR_CREDENCIAL')
) AS r(rol, permiso)
JOIN permiso p ON p.nombre = r.permiso
ON CONFLICT (rol, permiso_id) DO NOTHING;
