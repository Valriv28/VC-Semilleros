using Dapper;
using Npgsql;
using VCSemilleros.Domain.Modelos;
using VCSemilleros.Domain.Repositorios;

namespace VCSemilleros.Infrastructure.Repositorios;

public sealed class RepositorioCriterioPeriodoPostgreSql : IRepositorioCriterioPeriodo
{
    private readonly string _cadenaConexion;

    public RepositorioCriterioPeriodoPostgreSql(string cadenaConexion)
        => _cadenaConexion = cadenaConexion;

    public async Task<CriterioPeriodo?> ObtenerPorPeriodoAsync(int periodoId)
    {
        const string sql = @"
            SELECT id, periodo_id, criterio_version, min_asistencia_pct,
                   entregables_requeridos_pct, permanencia_meses,
                   definido_por, fecha_actualizacion
            FROM criterioperiodo
            WHERE periodo_id = @periodoId";

        await using var conexion = new NpgsqlConnection(_cadenaConexion);
        return await conexion.QuerySingleOrDefaultAsync<CriterioPeriodo>(sql, new { periodoId });
    }

    public async Task<CriterioPeriodo> GuardarAsync(CriterioPeriodo criterio)
    {
        await using var conexion = new NpgsqlConnection(_cadenaConexion);
        await conexion.OpenAsync();
        await using var transaccion = await conexion.BeginTransactionAsync();

        // Serializa las escrituras del mismo período sin requerir que se modifique el esquema.
        await conexion.ExecuteAsync(
            "SELECT pg_advisory_xact_lock(@clave)",
            new { clave = (long)criterio.PeriodoId },
            transaccion);

        const string buscar = @"
            SELECT id FROM criterioperiodo
            WHERE periodo_id = @PeriodoId
            ORDER BY criterio_version DESC, id DESC
            LIMIT 1";
        var idExistente = await conexion.QuerySingleOrDefaultAsync<int?>(
            buscar, criterio, transaccion);

        CriterioPeriodo guardado;
        if (idExistente.HasValue)
        {
            const string actualizar = @"
                UPDATE criterioperiodo
                SET criterio_version = criterio_version + 1,
                    min_asistencia_pct = @MinAsistenciaPct,
                    entregables_requeridos_pct = @EntregablesRequeridosPct,
                    permanencia_meses = @PermanenciaMeses,
                    definido_por = @DefinidoPor,
                    fecha_actualizacion = CURRENT_TIMESTAMP
                WHERE id = @Id
                RETURNING id, periodo_id, criterio_version, min_asistencia_pct,
                          entregables_requeridos_pct, permanencia_meses,
                          definido_por, fecha_actualizacion";
            guardado = await conexion.QuerySingleAsync<CriterioPeriodo>(
                actualizar, new {
                    Id = idExistente.Value,
                    criterio.MinAsistenciaPct,
                    criterio.EntregablesRequeridosPct,
                    criterio.PermanenciaMeses,
                    criterio.DefinidoPor
                }, transaccion);
        }
        else
        {
            const string insertar = @"
                INSERT INTO criterioperiodo
                    (periodo_id, criterio_version, min_asistencia_pct,
                     entregables_requeridos_pct, permanencia_meses,
                     definido_por, fecha_actualizacion)
                VALUES
                    (@PeriodoId, 1, @MinAsistenciaPct, @EntregablesRequeridosPct,
                     @PermanenciaMeses, @DefinidoPor, CURRENT_TIMESTAMP)
                RETURNING id, periodo_id, criterio_version, min_asistencia_pct,
                          entregables_requeridos_pct, permanencia_meses,
                          definido_por, fecha_actualizacion";
            guardado = await conexion.QuerySingleAsync<CriterioPeriodo>(
                insertar, criterio, transaccion);
        }

        await transaccion.CommitAsync();
        return guardado;
    }
}
