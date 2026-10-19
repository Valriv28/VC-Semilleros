using Dapper;                                   // Dapper: ejecuta el SQL y mapea filas a objetos.
using Npgsql;                                   // NpgsqlConnection: la conexión a PostgreSQL.
using VCSemilleros.Domain.Modelos;              // El modelo Asistencia.
using VCSemilleros.Domain.Repositorios;         // El puerto IRepositorioAsistencia.

namespace VCSemilleros.Infrastructure.Repositorios;

// Adaptador de salida: implementa IRepositorioAsistencia con PostgreSQL (RF7; E13 y E14, y apoyo a E15 y E17).
public class RepositorioAsistenciaPostgreSql : IRepositorioAsistencia
{
    private readonly string _cadenaConexion;

    public RepositorioAsistenciaPostgreSql(string cadenaConexion)
    {
        _cadenaConexion = cadenaConexion;
    }

    // E14: lista las asistencias de una sesión, hasta "limite" filas.
    public async Task<List<Asistencia>> ObtenerPorSesionAsync(int sesionId, int limite)
    {
        const string sql = @"SELECT id, sesion_id AS sesionid, participante_id AS participanteid, presente
                             FROM asistencia
                             WHERE sesion_id = @sesionId
                             ORDER BY id
                             LIMIT @limite";

        await using var conexion = new NpgsqlConnection(_cadenaConexion);

        var filas = await conexion.QueryAsync<Asistencia>(sql, new { sesionId, limite });

        return filas.ToList();
    }

    // E13: inserta TODAS las asistencias de la sesión o NINGUNA.
    // Para lograrlo se usa una TRANSACCIÓN: un grupo de operaciones que se confirman juntas (Commit)
    // o se deshacen juntas (Rollback). Si una fila viola UNIQUE (sesion_id, participante_id),
    // se lanza la excepción, la transacción se deshace y no queda guardada ninguna fila.
    public async Task<int> CrearVariasAsync(List<Asistencia> asistencias)
    {
        const string sql = @"INSERT INTO asistencia (sesion_id, participante_id, presente)
                             VALUES (@sesionId, @participanteId, @presente)";

        await using var conexion = new NpgsqlConnection(_cadenaConexion);

        // Con transacción hay que abrir la conexión explícitamente antes de iniciarla.
        await conexion.OpenAsync();

        // Inicia la transacción. "await using" la libera al salir del método; si no se llamó
        // a CommitAsync (porque hubo una excepción), PostgreSQL deshace todo automáticamente.
        await using var transaccion = await conexion.BeginTransactionAsync();

        // Un objeto anónimo por asistencia. Si Dapper recibe una LISTA de parámetros,
        // ejecuta el mismo INSERT una vez por cada elemento y suma las filas afectadas.
        var parametros = asistencias.Select(a => new
        {
            sesionId = a.SesionId,
            participanteId = a.ParticipanteId,
            presente = a.Presente
        });

        // El tercer argumento es la transacción: el INSERT forma parte de ella.
        var filas = await conexion.ExecuteAsync(sql, parametros, transaccion);

        // Todo salió bien: confirma la transacción y las filas quedan guardadas.
        await transaccion.CommitAsync();

        return filas;
    }

    // Cuenta en cuántas sesiones estuvo presente un participante: el numerador del porcentaje (E15 y E17).
    public async Task<int> ContarPresentesAsync(int participanteId)
    {
        const string sql = @"SELECT count(*)
                             FROM asistencia
                             WHERE participante_id = @participanteId
                               AND presente = TRUE";

        await using var conexion = new NpgsqlConnection(_cadenaConexion);

        return await conexion.ExecuteScalarAsync<int>(sql, new { participanteId });
    }
}