using Dapper;                                   // Dapper: ejecuta el SQL y mapea filas a objetos.
using Npgsql;                                   // NpgsqlConnection: la conexión a PostgreSQL.
using VCSemilleros.Domain.Modelos;              // Los modelos AnclajeStellar y EstadoAnclaje.
using VCSemilleros.Domain.Repositorios;         // El puerto IRepositorioAnclaje.

namespace VCSemilleros.Infrastructure.Repositorios;

// Guarda el REGISTRO del anclaje en nuestra BD; hablar con la red es trabajo de IClienteStellar.
public class RepositorioAnclajePostgreSql : IRepositorioAnclaje
{
    private readonly string _cadenaConexion;

    public RepositorioAnclajePostgreSql(string cadenaConexion)
    {
        _cadenaConexion = cadenaConexion;
    }

    // Inserta el anclaje en estado PENDIENTE y devuelve el id generado.
    public async Task<int> CrearAsync(AnclajeStellar anclaje)
    {
        // El estado viaja como texto y "@estado::estado_anclaje" lo convierte al enum de PostgreSQL.
        // "tx_hash", "attestation_uid" y "fecha_expiracion" no se insertan (quedan en NULL);
        // "fecha_solicitud" la pone la BD con now().
        const string sql = @"INSERT INTO anclaje_stellar
                                 (credencial_codigo, evidencia_hash, estado)
                             VALUES
                                 (@credencialCodigo, @evidenciaHash, @estado::estado_anclaje)
                             RETURNING id";

        await using var conexion = new NpgsqlConnection(_cadenaConexion);

        return await conexion.ExecuteScalarAsync<int>(sql, new
        {
            credencialCodigo = anclaje.CredencialCodigo,
            evidenciaHash = anclaje.EvidenciaHash,
            estado = anclaje.Estado.ToString()   // Enum -> texto: "PENDIENTE".
        });
    }

    // Busca el anclaje de una credencial (E27, E28, E29). Devuelve null si no tiene.
    public async Task<AnclajeStellar?> ObtenerPorCredencialAsync(string credencialCodigo)
    {
        // "estado::text AS estado": el enum se LEE como texto. Si hubiera varios anclajes de la misma
        // credencial, ORDER BY id DESC LIMIT 1 se queda con el más reciente.
        // CAMBIO: date_trunc('second', ...) recorta las fechas a segundos, porque fecha_solicitud guarda
        // microsegundos (DEFAULT now()) y el contrato E27 muestra "2026-10-17T21:35:01" sin fracción.
        // date_trunc de un valor NULL devuelve NULL, así que fecha_expiracion sigue pudiendo ser nula.
        const string sql = @"SELECT id, credencial_codigo AS credencialcodigo,
                                    evidencia_hash AS evidenciahash, tx_hash AS txhash,
                                    estado::text AS estado, attestation_uid AS attestationuid,
                                    date_trunc('second', fecha_solicitud) AS fechasolicitud,
                                    date_trunc('second', fecha_expiracion) AS fechaexpiracion
                             FROM anclaje_stellar
                             WHERE credencial_codigo = @credencialCodigo
                             ORDER BY id DESC
                             LIMIT 1";

        await using var conexion = new NpgsqlConnection(_cadenaConexion);

        return await conexion.QuerySingleOrDefaultAsync<AnclajeStellar>(sql, new { credencialCodigo });
    }

    // Guarda el resultado de anclar en la red: ANCLADO con su tx_hash y attestation_uid,
    // o FALLIDO con ambos en null.
    // CAMBIO: ahora también guarda attestation_uid.
    public async Task<int> ActualizarResultadoAsync(int id, EstadoAnclaje estado, string? txHash, string? attestationUid)
    {
        // Si txHash o attestationUid son null, Dapper los envía como NULL y la columna queda vacía
        // (la BD lo admite).
        const string sql = @"UPDATE anclaje_stellar
                             SET estado = @estado::estado_anclaje,
                                 tx_hash = @txHash,
                                 attestation_uid = @attestationUid
                             WHERE id = @id";

        await using var conexion = new NpgsqlConnection(_cadenaConexion);

        return await conexion.ExecuteAsync(sql, new
        {
            id,
            estado = estado.ToString(),   // Enum -> texto: "ANCLADO" o "FALLIDO".
            txHash,
            attestationUid
        });
    }
}