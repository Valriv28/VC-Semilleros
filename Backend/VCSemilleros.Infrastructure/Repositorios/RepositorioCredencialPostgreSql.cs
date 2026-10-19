using Dapper;                                   // Dapper: ejecuta el SQL y mapea filas a objetos.
using Npgsql;                                   // NpgsqlConnection: la conexión a PostgreSQL.
using VCSemilleros.Domain.Modelos;              // El modelo Credencial.
using VCSemilleros.Domain.Repositorios;         // El puerto IRepositorioCredencial.

namespace VCSemilleros.Infrastructure.Repositorios;

// Adaptador de salida: implementa IRepositorioCredencial con PostgreSQL (RF10 a RF14; E20 a E26, y auditoría).
public class RepositorioCredencialPostgreSql : IRepositorioCredencial
{
    private readonly string _cadenaConexion;

    public RepositorioCredencialPostgreSql(string cadenaConexion)
    {
        _cadenaConexion = cadenaConexion;
    }

    // Lista de columnas de credencial con su alias a PascalCase, para no repetirla en cada consulta.
    // "estado::text AS estado": el enum estado_credencial de PostgreSQL se LEE como texto;
    // Dapper convierte ese texto al enum EstadoCredencial de C# (los nombres coinciden letra por letra).
    // CAMBIO: date_trunc('second', fecha_revocacion) recorta la fecha de revocación a segundos, porque se
    // guarda con now() (microsegundos) y el contrato muestra fechas sin fracción.
    // OJO: fecha_emision NO se recorta aquí a propósito. Ya se guarda en segundos exactos, y E23 la usa para
    // reconstruir el contenido que se firmó: no debe cambiar ni un dígito.
    private const string Columnas = @"codigo, evaluacion_id AS evaluacionid, titular_id AS titularid,
                                      tipo, estado::text AS estado, vc_hash AS vchash,
                                      fecha_emision AS fechaemision, revocada_por AS revocadapor,
                                      motivo_revocacion AS motivorevocacion,
                                      date_trunc('second', fecha_revocacion) AS fecharevocacion";

    // Busca una credencial por su código (clave primaria de texto). Devuelve null si no existe.
    public async Task<Credencial?> ObtenerPorCodigoAsync(string codigo)
    {
        // $@"..." es una cadena interpolada: inserta la constante Columnas dentro del SQL.
        // El valor del usuario (@codigo) sigue siendo un PARÁMETRO, nunca se concatena.
        var sql = $@"SELECT {Columnas}
                     FROM credencial
                     WHERE codigo = @codigo";

        await using var conexion = new NpgsqlConnection(_cadenaConexion);

        return await conexion.QuerySingleOrDefaultAsync<Credencial>(sql, new { codigo });
    }

    // Busca la credencial que salió de una evaluación (null si aún no se emitió).
    public async Task<Credencial?> ObtenerPorEvaluacionAsync(int evaluacionId)
    {
        var sql = $@"SELECT {Columnas}
                     FROM credencial
                     WHERE evaluacion_id = @evaluacionId";

        await using var conexion = new NpgsqlConnection(_cadenaConexion);

        return await conexion.QuerySingleOrDefaultAsync<Credencial>(sql, new { evaluacionId });
    }

    // E21: lista las credenciales de un titular, hasta "limite" filas.
    public async Task<List<Credencial>> ObtenerPorTitularAsync(int titularId, int limite)
    {
        var sql = $@"SELECT {Columnas}
                     FROM credencial
                     WHERE titular_id = @titularId
                     ORDER BY fecha_emision, codigo
                     LIMIT @limite";

        await using var conexion = new NpgsqlConnection(_cadenaConexion);

        var filas = await conexion.QueryAsync<Credencial>(sql, new { titularId, limite });

        return filas.ToList();
    }

    // E20: inserta la credencial emitida. Devuelve las filas insertadas (1 si salió bien).
    public async Task<int> CrearAsync(Credencial credencial)
    {
        // ESCRITURA de un enum: el estado viaja como TEXTO y "@estado::estado_credencial" lo convierte
        // al tipo enumerado dentro de PostgreSQL (conversión explícita, plan §4.4).
        // CAMBIO: "fecha_emision" ahora SÍ se inserta. La fecha forma parte del contenido FIRMADO de la
        // credencial, así que la decide el servicio ANTES de firmar y no la BD con now(); si no, la fecha
        // guardada no coincidiría con la fecha que quedó firmada. Las columnas de revocación quedan en NULL.
        const string sql = @"INSERT INTO credencial
                                 (codigo, evaluacion_id, titular_id, tipo, estado, vc_hash, fecha_emision)
                             VALUES
                                 (@codigo, @evaluacionId, @titularId, @tipo,
                                  @estado::estado_credencial, @vcHash, @fechaEmision)";

        await using var conexion = new NpgsqlConnection(_cadenaConexion);

        return await conexion.ExecuteAsync(sql, new
        {
            codigo = credencial.Codigo,
            evaluacionId = credencial.EvaluacionId,
            titularId = credencial.TitularId,
            tipo = credencial.Tipo,
            estado = credencial.Estado.ToString(),   // Enum -> texto: "VIGENTE".
            vcHash = credencial.VcHash,
            fechaEmision = credencial.FechaEmision
        });
    }

    // E25: revoca una credencial. Todo en la misma fila (ya no hay tabla de eventos).
    public async Task<int> RevocarAsync(string codigo, int revocadaPor, string motivo)
    {
        // 'REVOCADA'::estado_credencial es un valor fijo escrito en el SQL (no viene del usuario),
        // convertido al tipo enumerado. "now()" pone la fecha y hora actuales del servidor de BD.
        const string sql = @"UPDATE credencial
                             SET estado = 'REVOCADA'::estado_credencial,
                                 revocada_por = @revocadaPor,
                                 motivo_revocacion = @motivo,
                                 fecha_revocacion = now()
                             WHERE codigo = @codigo";

        await using var conexion = new NpgsqlConnection(_cadenaConexion);

        return await conexion.ExecuteAsync(sql, new { codigo, revocadaPor, motivo });
    }
}