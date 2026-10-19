using Dapper;                                   // Dapper: ejecuta el SQL y mapea filas a objetos.
using Npgsql;                                   // NpgsqlConnection: la conexión a PostgreSQL.
using VCSemilleros.Domain.Modelos;              // El modelo Periodo.
using VCSemilleros.Domain.Repositorios;         // El puerto IRepositorioPeriodo.

namespace VCSemilleros.Infrastructure.Repositorios;

// Adaptador de salida: implementa IRepositorioPeriodo con PostgreSQL (RF5; E7 y E8).
public class RepositorioPeriodoPostgreSql : IRepositorioPeriodo
{
    private readonly string _cadenaConexion;

    public RepositorioPeriodoPostgreSql(string cadenaConexion)
    {
        _cadenaConexion = cadenaConexion;
    }

    // ADVERTENCIA SOBRE LAS FECHAS: el modelo usa DateOnly (fecha sin hora), pero Dapper no sabe
    // convertir por sí solo una columna DATE a DateOnly. Para no registrar conversores globales,
    // este repositorio lee las fechas como DateTime (con "::timestamp" en el SQL) dentro de una
    // clase privada auxiliar, y luego las convierte a DateOnly con DateOnly.FromDateTime.

    // Clase privada auxiliar: una fila de "periodo" con las fechas como DateTime.
    // "private sealed class" = solo se ve dentro de este repositorio y no se puede heredar.
    private sealed class PeriodoFila
    {
        public int Id { get; set; }
        public int SemilleroId { get; set; }
        public string Nombre { get; set; } = "";   // Valor inicial "" para cumplir Nullable (Dapper lo reemplaza).
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
    }

    // Convierte la fila auxiliar en el modelo del dominio.
    // "static" = no necesita una instancia de la clase para usarse.
    private static Periodo Convertir(PeriodoFila fila)
    {
        // "new Periodo { ... }" es un inicializador de objeto: crea el objeto y asigna sus propiedades.
        return new Periodo
        {
            Id = fila.Id,
            SemilleroId = fila.SemilleroId,
            Nombre = fila.Nombre,
            FechaInicio = DateOnly.FromDateTime(fila.FechaInicio),   // DateTime -> DateOnly (descarta la hora).
            FechaFin = DateOnly.FromDateTime(fila.FechaFin)
        };
    }

    // Busca un periodo por id (null si no existe).
    public async Task<Periodo?> ObtenerPorIdAsync(int id)
    {
        // "fecha_inicio::timestamp AS fechainicio": convierte el DATE a timestamp (DateTime en C#)
        // y lo asigna a la propiedad FechaInicio.
        const string sql = @"SELECT id, semillero_id AS semilleroid, nombre,
                                    fecha_inicio::timestamp AS fechainicio,
                                    fecha_fin::timestamp AS fechafin
                             FROM periodo
                             WHERE id = @id";

        await using var conexion = new NpgsqlConnection(_cadenaConexion);

        var fila = await conexion.QuerySingleOrDefaultAsync<PeriodoFila>(sql, new { id });

        // Si no hay fila devuelve null; si la hay, la convierte al modelo.
        return fila is null ? null : Convertir(fila);
    }

    // E7: lista los periodos de un semillero, hasta "limite" filas.
    public async Task<List<Periodo>> ObtenerPorSemilleroAsync(int semilleroId, int limite)
    {
        const string sql = @"SELECT id, semillero_id AS semilleroid, nombre,
                                    fecha_inicio::timestamp AS fechainicio,
                                    fecha_fin::timestamp AS fechafin
                             FROM periodo
                             WHERE semillero_id = @semilleroId
                             ORDER BY id
                             LIMIT @limite";

        await using var conexion = new NpgsqlConnection(_cadenaConexion);

        var filas = await conexion.QueryAsync<PeriodoFila>(sql, new { semilleroId, limite });

        // Select(Convertir) aplica la conversión a cada fila; ToList() arma la lista final.
        return filas.Select(Convertir).ToList();
    }

    // E8: inserta un periodo y devuelve el id generado.
    public async Task<int> CrearAsync(Periodo periodo)
    {
        // ESCRITURA: las fechas viajan como DateTime (medianoche) y "::date" las convierte
        // en DATE dentro de PostgreSQL.
        const string sql = @"INSERT INTO periodo (semillero_id, nombre, fecha_inicio, fecha_fin)
                             VALUES (@semilleroId, @nombre, @fechaInicio::date, @fechaFin::date)
                             RETURNING id";

        await using var conexion = new NpgsqlConnection(_cadenaConexion);

        return await conexion.ExecuteScalarAsync<int>(sql, new
        {
            semilleroId = periodo.SemilleroId,
            nombre = periodo.Nombre,
            // ToDateTime(TimeOnly.MinValue): DateOnly -> DateTime a las 00:00:00.
            fechaInicio = periodo.FechaInicio.ToDateTime(TimeOnly.MinValue),
            fechaFin = periodo.FechaFin.ToDateTime(TimeOnly.MinValue)
        });
    }
}