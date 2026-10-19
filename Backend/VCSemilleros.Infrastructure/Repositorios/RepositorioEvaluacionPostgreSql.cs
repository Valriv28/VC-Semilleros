using Dapper;                                   // Dapper: ejecuta el SQL y mapea filas a objetos.
using Npgsql;                                   // NpgsqlConnection: la conexión a PostgreSQL.
using VCSemilleros.Domain.Modelos;              // El modelo Evaluacion.
using VCSemilleros.Domain.Repositorios;         // El puerto IRepositorioEvaluacion.

namespace VCSemilleros.Infrastructure.Repositorios;

// Adaptador de salida: implementa IRepositorioEvaluacion con PostgreSQL (RF8, RF9; E16 a E19, y apoyo a E20).
public class RepositorioEvaluacionPostgreSql : IRepositorioEvaluacion
{
    private readonly string _cadenaConexion;

    public RepositorioEvaluacionPostgreSql(string cadenaConexion)
    {
        _cadenaConexion = cadenaConexion;
    }

    // Lista de columnas de evaluacion con su alias a PascalCase, para no repetirla en cada consulta.
    // Cada "columna AS alias" asigna la columna snake_case a la propiedad del modelo
    // (Dapper compara los nombres sin distinguir mayúsculas).
    // OJO: sigue siendo SQL visible; esta constante solo evita copiar el mismo texto varias veces.
    // El prefijo "e." es el alias de la tabla evaluacion en las consultas que la unen con participante.
    // CAMBIO: date_trunc('second', e.fecha) recorta la fecha a segundos. La columna guarda microsegundos
    // (por el DEFAULT now()), y el JSON saldría como "2026-10-17T21:30:00.123456"; el contrato muestra
    // "2026-10-17T21:30:00". Solo cambia lo que se LEE: la BD no se toca.
    private const string Columnas = @"e.id, e.participante_id AS participanteid,
                                      e.criterio_version AS criterioversion,
                                      e.min_asistencia_pct AS minasistenciapct,
                                      e.definido_por AS definidopor,
                                      e.pct_asistencia AS pctasistencia,
                                      e.cumple,
                                      e.elegible_declarado_por AS elegibledeclaradopor,
                                      date_trunc('second', e.fecha) AS fecha";

    // Busca una evaluación por id (null si no existe).
    public async Task<Evaluacion?> ObtenerPorIdAsync(int id)
    {
        // "evaluacion e" le pone el alias "e" a la tabla; $@"..." permite insertar la constante Columnas.
        var sql = $@"SELECT {Columnas}
                     FROM evaluacion e
                     WHERE e.id = @id";

        await using var conexion = new NpgsqlConnection(_cadenaConexion);

        return await conexion.QuerySingleOrDefaultAsync<Evaluacion>(sql, new { id });
    }

    // E18: lista las evaluaciones de un periodo, hasta "limite" filas.
    // La evaluación no guarda el periodo: se llega a él a través del participante (JOIN).
    public async Task<List<Evaluacion>> ObtenerPorPeriodoAsync(int periodoId, int limite)
    {
        // JOIN une cada evaluación con su participante para poder filtrar por periodo_id.
        var sql = $@"SELECT {Columnas}
                     FROM evaluacion e
                     JOIN participante p ON p.id = e.participante_id
                     WHERE p.periodo_id = @periodoId
                     ORDER BY e.id
                     LIMIT @limite";

        await using var conexion = new NpgsqlConnection(_cadenaConexion);

        var filas = await conexion.QueryAsync<Evaluacion>(sql, new { periodoId, limite });

        return filas.ToList();
    }

    // E16 (propuesta P1): el criterio vigente es el de la evaluación con MAYOR criterio_version del periodo.
    // Si varias comparten la versión, se toma la más reciente (mayor id). Devuelve null si no hay ninguna.
    public async Task<Evaluacion?> ObtenerCriterioVigenteAsync(int periodoId)
    {
        // ORDER BY ... DESC deja primero la versión más alta; LIMIT 1 se queda solo con esa fila.
        var sql = $@"SELECT {Columnas}
                     FROM evaluacion e
                     JOIN participante p ON p.id = e.participante_id
                     WHERE p.periodo_id = @periodoId
                     ORDER BY e.criterio_version DESC, e.id DESC
                     LIMIT 1";

        await using var conexion = new NpgsqlConnection(_cadenaConexion);

        return await conexion.QuerySingleOrDefaultAsync<Evaluacion>(sql, new { periodoId });
    }

    // E17: inserta una evaluación y devuelve el id generado.
    // Si ya existe una del mismo participante con la misma criterio_version, la BD lanza una violación
    // de UNIQUE que sube hasta el controlador (500 con el error del motor).
    public async Task<int> CrearAsync(Evaluacion evaluacion)
    {
        // "fecha" y "elegible_declarado_por" no se insertan: la BD pone now() y NULL respectivamente.
        const string sql = @"INSERT INTO evaluacion
                                 (participante_id, criterio_version, min_asistencia_pct,
                                  definido_por, pct_asistencia, cumple)
                             VALUES
                                 (@participanteId, @criterioVersion, @minAsistenciaPct,
                                  @definidoPor, @pctAsistencia, @cumple)
                             RETURNING id";

        await using var conexion = new NpgsqlConnection(_cadenaConexion);

        return await conexion.ExecuteScalarAsync<int>(sql, new
        {
            participanteId = evaluacion.ParticipanteId,
            criterioVersion = evaluacion.CriterioVersion,
            minAsistenciaPct = evaluacion.MinAsistenciaPct,
            definidoPor = evaluacion.DefinidoPor,
            pctAsistencia = evaluacion.PctAsistencia,
            cumple = evaluacion.Cumple
        });
    }

    // E19 (PATCH): guarda quién declaró la elegibilidad. Devuelve las filas afectadas.
    public async Task<int> DeclararElegibleAsync(int id, int coordinadorId)
    {
        const string sql = @"UPDATE evaluacion
                             SET elegible_declarado_por = @coordinadorId
                             WHERE id = @id";

        await using var conexion = new NpgsqlConnection(_cadenaConexion);

        return await conexion.ExecuteAsync(sql, new { id, coordinadorId });
    }
}