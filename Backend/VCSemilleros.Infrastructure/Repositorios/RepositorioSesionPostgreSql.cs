using Dapper;                                   // Dapper: ejecuta el SQL y mapea filas a objetos.
using Npgsql;                                   // NpgsqlConnection: la conexión a PostgreSQL.
using VCSemilleros.Domain.Modelos;              // El modelo Sesion.
using VCSemilleros.Domain.Repositorios;         // El puerto IRepositorioSesion.

namespace VCSemilleros.Infrastructure.Repositorios;

// Adaptador de salida: implementa IRepositorioSesion con PostgreSQL (RF7; E11 y E12, y apoyo a E13 a E15).
public class RepositorioSesionPostgreSql : IRepositorioSesion
{
    private readonly string _cadenaConexion;

    public RepositorioSesionPostgreSql(string cadenaConexion)
    {
        _cadenaConexion = cadenaConexion;
    }

    // Misma técnica de fechas que en RepositorioPeriodoPostgreSql: Dapper no convierte DATE a DateOnly,
    // así que se lee como DateTime en una clase auxiliar privada y luego se convierte.
    private sealed class SesionFila
    {
        public int Id { get; set; }
        public int PeriodoId { get; set; }
        public DateTime Fecha { get; set; }
        public string? Descripcion { get; set; }   // "string?": la descripción es opcional (puede ser NULL).
    }

    // Convierte la fila auxiliar en el modelo del dominio.
    private static Sesion Convertir(SesionFila fila)
    {
        return new Sesion
        {
            Id = fila.Id,
            PeriodoId = fila.PeriodoId,
            Fecha = DateOnly.FromDateTime(fila.Fecha),   // DateTime -> DateOnly (descarta la hora).
            Descripcion = fila.Descripcion
        };
    }

    // Busca una sesión por id (null si no existe).
    public async Task<Sesion?> ObtenerPorIdAsync(int id)
    {
        // "fecha::timestamp AS fecha": convierte el DATE a timestamp (DateTime en C#).
        const string sql = @"SELECT id, periodo_id AS periodoid, fecha::timestamp AS fecha, descripcion
                             FROM sesion
                             WHERE id = @id";

        await using var conexion = new NpgsqlConnection(_cadenaConexion);

        var fila = await conexion.QuerySingleOrDefaultAsync<SesionFila>(sql, new { id });

        return fila is null ? null : Convertir(fila);
    }

    // E11: lista las sesiones de un periodo, hasta "limite" filas.
    public async Task<List<Sesion>> ObtenerPorPeriodoAsync(int periodoId, int limite)
    {
        // ORDER BY fecha, id: primero por fecha de la sesión y, si empatan, por id.
        const string sql = @"SELECT id, periodo_id AS periodoid, fecha::timestamp AS fecha, descripcion
                             FROM sesion
                             WHERE periodo_id = @periodoId
                             ORDER BY fecha, id
                             LIMIT @limite";

        await using var conexion = new NpgsqlConnection(_cadenaConexion);

        var filas = await conexion.QueryAsync<SesionFila>(sql, new { periodoId, limite });

        return filas.Select(Convertir).ToList();
    }

    // Cuenta las sesiones de un periodo: el denominador del porcentaje de asistencia (E15 y E17).
    public async Task<int> ContarPorPeriodoAsync(int periodoId)
    {
        // count(*) devuelve cuántas filas cumplen la condición.
        const string sql = @"SELECT count(*)
                             FROM sesion
                             WHERE periodo_id = @periodoId";

        await using var conexion = new NpgsqlConnection(_cadenaConexion);

        // ExecuteScalarAsync<int>: devuelve ese único número.
        return await conexion.ExecuteScalarAsync<int>(sql, new { periodoId });
    }

    // E12: inserta una sesión y devuelve el id generado.
    public async Task<int> CrearAsync(Sesion sesion)
    {
        // "@fecha::date" convierte el DateTime recibido en un DATE de PostgreSQL.
        // Si "descripcion" es null, Dapper lo envía como NULL (la columna lo admite).
        const string sql = @"INSERT INTO sesion (periodo_id, fecha, descripcion)
                             VALUES (@periodoId, @fecha::date, @descripcion)
                             RETURNING id";

        await using var conexion = new NpgsqlConnection(_cadenaConexion);

        return await conexion.ExecuteScalarAsync<int>(sql, new
        {
            periodoId = sesion.PeriodoId,
            fecha = sesion.Fecha.ToDateTime(TimeOnly.MinValue),   // DateOnly -> DateTime a las 00:00:00.
            descripcion = sesion.Descripcion
        });
    }
}