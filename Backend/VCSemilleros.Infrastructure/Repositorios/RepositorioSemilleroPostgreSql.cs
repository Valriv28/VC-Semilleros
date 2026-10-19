using Dapper;                                   // Dapper: QueryAsync, ExecuteAsync, ExecuteScalarAsync...
using Npgsql;                                   // NpgsqlConnection: la conexión a PostgreSQL.
using VCSemilleros.Domain.Modelos;              // El modelo Semillero.
using VCSemilleros.Domain.Repositorios;         // El puerto IRepositorioSemillero.

namespace VCSemilleros.Infrastructure.Repositorios;

// Adaptador de salida: implementa IRepositorioSemillero con PostgreSQL (RF3, RF4; E3 a E6).
public class RepositorioSemilleroPostgreSql : IRepositorioSemillero
{
    // Cadena de conexión recibida por constructor (la registra Program.cs).
    private readonly string _cadenaConexion;

    public RepositorioSemilleroPostgreSql(string cadenaConexion)
    {
        _cadenaConexion = cadenaConexion;
    }

    // E3: lista los semilleros, hasta "limite" filas.
    public async Task<List<Semillero>> ObtenerTodosAsync(int limite)
    {
        // "LIMIT @limite": PostgreSQL acepta parámetro en el límite. "responsable_id AS responsableid"
        // asigna la columna snake_case a la propiedad ResponsableId.
        const string sql = @"SELECT id, responsable_id AS responsableid, nombre, activo
                             FROM semillero
                             ORDER BY id
                             LIMIT @limite";

        await using var conexion = new NpgsqlConnection(_cadenaConexion);

        // QueryAsync devuelve una colección de objetos Semillero (una por fila).
        var filas = await conexion.QueryAsync<Semillero>(sql, new { limite });

        // ToList() la convierte en List<Semillero>; si no hay filas queda vacía.
        return filas.ToList();
    }

    // E4: busca un semillero por id (null si no existe).
    public async Task<Semillero?> ObtenerPorIdAsync(int id)
    {
        const string sql = @"SELECT id, responsable_id AS responsableid, nombre, activo
                             FROM semillero
                             WHERE id = @id";

        await using var conexion = new NpgsqlConnection(_cadenaConexion);

        return await conexion.QuerySingleOrDefaultAsync<Semillero>(sql, new { id });
    }

    // E5: inserta un semillero y devuelve el id generado.
    public async Task<int> CrearAsync(Semillero semillero)
    {
        // "activo" no se inserta: la BD aplica su valor por defecto (TRUE).
        // "RETURNING id" hace que PostgreSQL devuelva el id generado por el SERIAL.
        const string sql = @"INSERT INTO semillero (responsable_id, nombre)
                             VALUES (@responsableId, @nombre)
                             RETURNING id";

        await using var conexion = new NpgsqlConnection(_cadenaConexion);

        // ExecuteScalarAsync<int> ejecuta el SQL y devuelve el primer valor de la primera fila (el id).
        // El objeto anónimo llena los parámetros @responsableId y @nombre.
        return await conexion.ExecuteScalarAsync<int>(sql, new
        {
            responsableId = semillero.ResponsableId,
            nombre = semillero.Nombre
        });
    }

    // E6 (PATCH): cambia solo la columna "activo". Devuelve las filas afectadas (0 si el id no existe).
    public async Task<int> ActualizarEstadoAsync(int id, bool activo)
    {
        const string sql = @"UPDATE semillero
                             SET activo = @activo
                             WHERE id = @id";

        await using var conexion = new NpgsqlConnection(_cadenaConexion);

        // ExecuteAsync ejecuta una sentencia sin devolver filas y retorna cuántas filas afectó.
        return await conexion.ExecuteAsync(sql, new { id, activo });
    }
}