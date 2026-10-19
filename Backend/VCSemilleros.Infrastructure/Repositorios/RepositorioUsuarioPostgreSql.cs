// "using" importa espacios de nombres para poder usar sus clases sin escribir la ruta completa.
using Dapper;                                   // Dapper: ejecuta el SQL y convierte filas en objetos (métodos como QuerySingleOrDefaultAsync).
using Npgsql;                                   // Npgsql: el cliente oficial de PostgreSQL (NpgsqlConnection).
using VCSemilleros.Domain.Modelos;              // Los modelos: Usuario.
using VCSemilleros.Domain.Repositorios;         // El puerto que esta clase implementa: IRepositorioUsuario.

// Espacio de nombres de los repositorios PostgreSQL.
namespace VCSemilleros.Infrastructure.Repositorios;

// Adaptador de salida: implementa el puerto IRepositorioUsuario con PostgreSQL.
// ": IRepositorioUsuario" significa "esta clase cumple el contrato de esa interfaz".
public class RepositorioUsuarioPostgreSql : IRepositorioUsuario
{
    // Campo privado de solo lectura: guarda la cadena de conexión (host, base, usuario, clave).
    // "readonly" = solo se puede asignar en el constructor.
    private readonly string _cadenaConexion;

    // Constructor: se ejecuta al crear la clase. Program.cs le pasa la cadena de conexión,
    // que sale de ConnectionStrings__PostgreSql (la clave NO se versiona, Art. 9).
    public RepositorioUsuarioPostgreSql(string cadenaConexion)
    {
        _cadenaConexion = cadenaConexion;   // Guarda la cadena para usarla en cada operación.
    }

    // Lista de columnas de usuario con su alias a PascalCase, para no repetirla en cada consulta.
    // "rol::text AS rol": el enum rol_usuario de PostgreSQL se lee como texto; Dapper lo convierte
    // al enum RolUsuario de C#. "creado_por AS creadopor" y "contrasena_hash AS contrasenahash" asignan
    // la columna snake_case a la propiedad (Dapper compara los nombres sin distinguir mayúsculas).
    private const string Columnas = @"id, nombre, correo, rol::text AS rol,
                                      creado_por AS creadopor, activo,
                                      contrasena_hash AS contrasenahash";

    // Busca un usuario por id. "async" permite usar "await" dentro; devuelve Task<Usuario?>.
    public async Task<Usuario?> ObtenerPorIdAsync(int id)
    {
        // SQL escrito a mano, SIEMPRE parametrizado: @id es un parámetro, nunca se concatena el valor.
        // "$@" es una cadena interpolada: inserta la constante Columnas dentro del SQL.
        var sql = $@"SELECT {Columnas}
                     FROM usuario
                     WHERE id = @id";

        // Crea la conexión. "await using" la cierra y libera sola al terminar el método.
        await using var conexion = new NpgsqlConnection(_cadenaConexion);

        // QuerySingleOrDefaultAsync: ejecuta el SQL y devuelve UN objeto Usuario, o null si no hay fila.
        // "new { id }" es un objeto anónimo: su propiedad "id" llena el parámetro @id.
        // Dapper abre la conexión por nosotros si está cerrada.
        return await conexion.QuerySingleOrDefaultAsync<Usuario>(sql, new { id });
    }

    // NUEVO: busca un usuario por su correo (lo usa el ingreso, E2).
    public async Task<Usuario?> ObtenerPorCorreoAsync(string correo)
    {
        // lower(...) en ambos lados: el correo se compara sin distinguir mayúsculas de minúsculas,
        // porque quien escribe "Ana.Torres@..." es la misma persona que "ana.torres@...".
        // El valor sigue viajando como PARÁMETRO (@correo), nunca concatenado.
        var sql = $@"SELECT {Columnas}
                     FROM usuario
                     WHERE lower(correo) = lower(@correo)";

        await using var conexion = new NpgsqlConnection(_cadenaConexion);

        return await conexion.QuerySingleOrDefaultAsync<Usuario>(sql, new { correo });
    }


    public async Task<int> CrearAsync(Usuario usuario)
    {
        const string sql = @"
            INSERT INTO usuario
                (nombre, correo, rol, creado_por, activo, contrasena_hash)
            VALUES
                (@Nombre, @Correo, CAST(@Rol AS rol_usuario),
                 NULL, TRUE, @ContrasenaHash)
            RETURNING id;";

        await using var conexion = new NpgsqlConnection(_cadenaConexion);

        return await conexion.QuerySingleAsync<int>(sql, new
        {
            usuario.Nombre,
            usuario.Correo,
            Rol = usuario.Rol.ToString(),
            usuario.ContrasenaHash
        });
    }

}