using Dapper;                                   // Dapper: ejecuta el SQL y mapea filas a objetos.
using Npgsql;                                   // NpgsqlConnection: la conexión a PostgreSQL.
using VCSemilleros.Domain.Modelos;              // El modelo Participante.
using VCSemilleros.Domain.Repositorios;         // El puerto IRepositorioParticipante.

namespace VCSemilleros.Infrastructure.Repositorios;

// Adaptador de salida: implementa IRepositorioParticipante con PostgreSQL (RF6; E9 y E10).
public class RepositorioParticipantePostgreSql : IRepositorioParticipante
{
    private readonly string _cadenaConexion;

    public RepositorioParticipantePostgreSql(string cadenaConexion)
    {
        _cadenaConexion = cadenaConexion;
    }

    // Busca un participante por id (null si no existe).
    public async Task<Participante?> ObtenerPorIdAsync(int id)
    {
        // Alias snake_case -> PascalCase: periodo_id -> periodoid (PeriodoId), estudiante_id -> estudianteid.
        const string sql = @"SELECT id, periodo_id AS periodoid, estudiante_id AS estudianteid
                             FROM participante
                             WHERE id = @id";

        await using var conexion = new NpgsqlConnection(_cadenaConexion);

        return await conexion.QuerySingleOrDefaultAsync<Participante>(sql, new { id });
    }

    // E9: lista los participantes de un periodo, hasta "limite" filas.
    public async Task<List<Participante>> ObtenerPorPeriodoAsync(int periodoId, int limite)
    {
        const string sql = @"SELECT id, periodo_id AS periodoid, estudiante_id AS estudianteid
                             FROM participante
                             WHERE periodo_id = @periodoId
                             ORDER BY id
                             LIMIT @limite";

        await using var conexion = new NpgsqlConnection(_cadenaConexion);

        var filas = await conexion.QueryAsync<Participante>(sql, new { periodoId, limite });

        return filas.ToList();
    }

    // E10: inscribe un estudiante en un periodo y devuelve el id generado.
    // Si ya estaba inscrito, la BD lanza una violación de UNIQUE (periodo_id, estudiante_id);
    // aquí NO se atrapa: sube hasta el controlador, que responde 500 con el error del motor (D7).
    public async Task<int> CrearAsync(Participante participante)
    {
        const string sql = @"INSERT INTO participante (periodo_id, estudiante_id)
                             VALUES (@periodoId, @estudianteId)
                             RETURNING id";

        await using var conexion = new NpgsqlConnection(_cadenaConexion);

        return await conexion.ExecuteScalarAsync<int>(sql, new
        {
            periodoId = participante.PeriodoId,
            estudianteId = participante.EstudianteId
        });
    }
}