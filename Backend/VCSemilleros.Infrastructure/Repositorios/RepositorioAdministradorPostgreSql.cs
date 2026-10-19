using Dapper;                                   // Dapper: ejecuta el SQL y convierte filas en objetos.
using Npgsql;                                   // Npgsql: el cliente oficial de PostgreSQL (NpgsqlConnection).
using VCSemilleros.Domain.Modelos;              // El modelo Administrador.
using VCSemilleros.Domain.Repositorios;         // El puerto que esta clase implementa: IRepositorioAdministrador.

namespace VCSemilleros.Infrastructure.Repositorios;

// Adaptador de salida: implementa IRepositorioAdministrador con PostgreSQL.
// NUEVO: el administrador vive en su propia tabla y necesita ingresar por E2.
public class RepositorioAdministradorPostgreSql : IRepositorioAdministrador
{
	// Cadena de conexión recibida por constructor (la registra Program.cs).
	private readonly string _cadenaConexion;

	public RepositorioAdministradorPostgreSql(string cadenaConexion)
	{
		_cadenaConexion = cadenaConexion;
	}

	// Busca un administrador por su correo. Devuelve null si no existe.
	public async Task<Administrador?> ObtenerPorCorreoAsync(string correo)
	{
		// "contrasena_hash AS contrasenahash" asigna la columna a la propiedad ContrasenaHash.
		// lower(...) compara el correo sin distinguir mayúsculas; el valor viaja como parámetro (@correo).
		const string sql = @"SELECT id, nombre, correo, contrasena_hash AS contrasenahash
                             FROM administrador
                             WHERE lower(correo) = lower(@correo)";

		await using var conexion = new NpgsqlConnection(_cadenaConexion);

		return await conexion.QuerySingleOrDefaultAsync<Administrador>(sql, new { correo });
	}
}