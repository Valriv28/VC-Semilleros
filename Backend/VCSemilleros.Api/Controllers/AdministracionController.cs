using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace VCSemilleros.Api.Controllers;

[ApiController]
[Route("api/administracion")]
[Authorize]
public sealed class AdministracionController : ControllerBase
{
    private readonly string _cadenaConexion;

    public AdministracionController(IConfiguration configuracion)
    {
        _cadenaConexion = configuracion.GetConnectionString("PostgreSql")
            ?? throw new InvalidOperationException("Falta ConnectionStrings:PostgreSql.");
    }

    private static readonly string[] RolesValidos = ["ESTUDIANTE", "DOCENTE", "COORDINADOR"];

    [HttpGet("usuarios")]
    [Authorize(Roles = "DOCENTE,ADMINISTRADOR")]
    public async Task<IActionResult> ListarUsuarios([FromQuery] string? rol = null)
    {
        if (!string.IsNullOrWhiteSpace(rol) && !RolesValidos.Contains(rol.Trim().ToUpperInvariant()))
            return BadRequest(new { mensaje = "El rol debe ser ESTUDIANTE, DOCENTE o COORDINADOR." });

        const string sql = @"SELECT id, nombre, correo, rol::text AS rol, activo
                             FROM usuario
                             WHERE activo = TRUE AND (@rol IS NULL OR rol::text = @rol)
                             ORDER BY nombre, correo";
        await using var cn = new NpgsqlConnection(_cadenaConexion);
        var rows = await cn.QueryAsync(sql, new { rol = string.IsNullOrWhiteSpace(rol) ? null : rol.Trim().ToUpperInvariant() });
        return Ok(new { datos = rows });
    }

    [HttpPut("usuarios/{id:int}/rol")]
    [Authorize(Roles = "ADMINISTRADOR")]
    public async Task<IActionResult> CambiarRol(int id, [FromBody] CambiarRolPeticion peticion)
    {
        var rol = peticion.Rol?.Trim().ToUpperInvariant();
        if (rol is null || !RolesValidos.Contains(rol))
            return BadRequest(new { mensaje = "El rol debe ser ESTUDIANTE, DOCENTE o COORDINADOR." });

        const string sql = @"UPDATE usuario SET rol = @rol::rol_usuario WHERE id = @id";
        await using var cn = new NpgsqlConnection(_cadenaConexion);
        var filas = await cn.ExecuteAsync(sql, new { id, rol });
        return filas == 0 ? NotFound(new { mensaje = "No se encontró el usuario." })
            : Ok(new { mensaje = "Rol actualizado.", id, rol });
    }

    [HttpGet("permisos")]
    [Authorize(Roles = "ADMINISTRADOR")]
    public async Task<IActionResult> ListarPermisos([FromQuery] string rol = "DOCENTE")
    {
        rol = rol.Trim().ToUpperInvariant();
        if (!RolesValidos.Contains(rol))
            return BadRequest(new { mensaje = "Rol no válido." });

        const string sql = @"SELECT p.id, p.nombre, p.descripcion,
                                    (rp.permiso_id IS NOT NULL) AS asignado
                             FROM permiso p
                             LEFT JOIN rol_permiso rp ON rp.permiso_id = p.id AND rp.rol = @rol::rol_usuario
                             ORDER BY p.nombre";
        await using var cn = new NpgsqlConnection(_cadenaConexion);
        var rows = await cn.QueryAsync(sql, new { rol });
        return Ok(new { rol, datos = rows });
    }

    [HttpPost("permisos")]
    [Authorize(Roles = "ADMINISTRADOR")]
    public async Task<IActionResult> CrearPermiso([FromBody] PermisoCrearPeticion peticion)
    {
        var nombre = peticion.Nombre?.Trim().ToUpperInvariant();
        var descripcion = peticion.Descripcion?.Trim();
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Length > 60)
            return BadRequest(new { mensaje = "El nombre del permiso es obligatorio y admite máximo 60 caracteres." });
        if (descripcion?.Length > 200)
            return BadRequest(new { mensaje = "La descripción admite máximo 200 caracteres." });

        const string sql = @"INSERT INTO permiso(nombre, descripcion)
                             VALUES (@nombre, @descripcion)
                             RETURNING id, nombre, descripcion";
        try
        {
            await using var cn = new NpgsqlConnection(_cadenaConexion);
            var row = await cn.QuerySingleAsync(sql, new { nombre, descripcion });
            return Ok(row);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return Conflict(new { mensaje = "Ya existe un permiso con ese nombre." });
        }
    }

    [HttpPut("roles/{rol}/permisos")]
    [Authorize(Roles = "ADMINISTRADOR")]
    public async Task<IActionResult> GuardarPermisosRol(string rol, [FromBody] PermisosRolPeticion peticion)
    {
        rol = rol.Trim().ToUpperInvariant();
        if (!RolesValidos.Contains(rol))
            return BadRequest(new { mensaje = "Rol no válido." });
        var ids = (peticion.PermisoIds ?? []).Where(x => x > 0).Distinct().ToArray();

        await using var cn = new NpgsqlConnection(_cadenaConexion);
        await cn.OpenAsync();
        await using var tx = await cn.BeginTransactionAsync();
        try
        {
            await cn.ExecuteAsync("DELETE FROM rol_permiso WHERE rol = @rol::rol_usuario", new { rol }, tx);
            foreach (var id in ids)
                await cn.ExecuteAsync(@"INSERT INTO rol_permiso(rol, permiso_id)
                                        VALUES (@rol::rol_usuario, @id)", new { rol, id }, tx);
            await tx.CommitAsync();
            return Ok(new { mensaje = "Permisos del rol guardados.", rol, total = ids.Length });
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            await tx.RollbackAsync();
            return BadRequest(new { mensaje = "Uno de los permisos seleccionados no existe." });
        }
    }

    [HttpGet("parametros-acceso")]
    [Authorize(Roles = "ADMINISTRADOR")]
    public async Task<IActionResult> ListarParametrosAcceso()
    {
        const string sql = @"SELECT clave, valor, descripcion, actualizado_en AS actualizadoen
                             FROM parametro_acceso ORDER BY clave";
        await using var cn = new NpgsqlConnection(_cadenaConexion);
        var rows = await cn.QueryAsync(sql);
        return Ok(new { datos = rows });
    }

    [HttpPut("parametros-acceso/{clave}")]
    [Authorize(Roles = "ADMINISTRADOR")]
    public async Task<IActionResult> GuardarParametroAcceso(string clave, [FromBody] ParametroAccesoGuardarPeticion peticion)
    {
        if (string.IsNullOrWhiteSpace(peticion.Valor) || peticion.Valor.Length > 200)
            return BadRequest(new { mensaje = "El valor es obligatorio y admite máximo 200 caracteres." });

        var valor = peticion.Valor.Trim();
        if (clave == "minutos_expiracion_token"
            && (!int.TryParse(valor, out var minutos) || minutos is < 1 or > 10080))
            return BadRequest(new { mensaje = "La duración del token debe ser un entero entre 1 y 10080 minutos." });
        if (clave == "registro_publico_habilitado" && !bool.TryParse(valor, out _))
            return BadRequest(new { mensaje = "El registro público debe ser true o false." });

        const string sql = @"UPDATE parametro_acceso
                             SET valor = @valor, actualizado_en = now()
                             WHERE clave = @clave
                             RETURNING clave, valor, descripcion, actualizado_en AS actualizadoen";
        await using var cn = new NpgsqlConnection(_cadenaConexion);
        var row = await cn.QuerySingleOrDefaultAsync(sql, new { clave, valor });
        return row is null ? NotFound(new { mensaje = "No existe ese parámetro." }) : Ok(row);
    }

    [HttpGet("credenciales-emitidas")]
    [Authorize(Roles = "COORDINADOR,ADMINISTRADOR")]
    public async Task<IActionResult> ListarCredencialesEmitidas([FromQuery] int periodoId)
    {
        if (periodoId <= 0)
            return BadRequest(new { mensaje = "El periodoId debe ser mayor que cero." });

        const string sql = @"SELECT c.evaluacion_id AS evaluacionid, c.codigo, c.estado::text AS estado
                             FROM credencial c
                             JOIN evaluacion e ON e.id = c.evaluacion_id
                             JOIN participante p ON p.id = e.participante_id
                             WHERE p.periodo_id = @periodoId
                             ORDER BY c.fecha_emision DESC";
        await using var cn = new NpgsqlConnection(_cadenaConexion);
        var rows = await cn.QueryAsync(sql, new { periodoId });
        return Ok(new { datos = rows });
    }

    [HttpGet("transacciones-blockchain")]
    [Authorize(Roles = "COORDINADOR,ADMINISTRADOR")]
    public async Task<IActionResult> ListarTransaccionesBlockchain([FromQuery] int limite = 500)
    {
        if (limite < 1 || limite > 2000)
            return BadRequest(new { mensaje = "El límite debe estar entre 1 y 2000." });

        const string sql = @"SELECT a.id, a.credencial_codigo AS credencialcodigo,
                                    a.evidencia_hash AS evidenciahash, a.tx_hash AS txhash,
                                    a.estado::text AS estado, a.attestation_uid AS attestationuid,
                                    date_trunc('second', a.fecha_solicitud) AS fechasolicitud,
                                    date_trunc('second', a.fecha_expiracion) AS fechaexpiracion
                             FROM anclaje_stellar a
                             ORDER BY a.fecha_solicitud DESC, a.id DESC
                             LIMIT @limite";
        await using var cn = new NpgsqlConnection(_cadenaConexion);
        var rows = await cn.QueryAsync(sql, new { limite });
        return Ok(new { datos = rows });
    }

    public sealed class CambiarRolPeticion { public string? Rol { get; set; } }
    public sealed class PermisoCrearPeticion { public string? Nombre { get; set; } public string? Descripcion { get; set; } }
    public sealed class PermisosRolPeticion { public int[]? PermisoIds { get; set; } }
    public sealed class ParametroAccesoGuardarPeticion { public string Valor { get; set; } = ""; }
}
