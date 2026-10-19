// Espacio de nombres de los puertos del dominio.
namespace VCSemilleros.Domain.Repositorios;

// Puerto de salida hacia la red Stellar (Art. 4 y 5; plan §4.6).
// Vive en esta carpeta porque Estructura.txt define aquí los puertos, aunque no sea un
// repositorio de datos. El dominio NUNCA invoca SDK, RPC ni contratos de Stellar: pide el
// anclaje a través de esta interfaz, y el adaptador (Infrastructure.Stellar) lo traduce a la red.
// Por detrás, el adaptador habla con un "puente" que usa el contrato Attest ya existente de Soroban;
// este puerto no sabe nada de eso.
public interface IClienteStellar
{
    // Registra en la red la EVIDENCIA MÍNIMA de una credencial (Art. 5).
    // Recibe el código de la credencial y el hash de la evidencia (SHA-256 en hexadecimal):
    // NUNCA datos personales ni la credencial completa. Devuelve el resultado en un objeto y NO lanza
    // excepción si la red no responde, para que el servicio guarde el anclaje como FALLIDO (P4).
    Task<ResultadoAnclaje> AnclarAsync(string credencialCodigo, string evidenciaHash);

    // CAMBIO: revoca en la red la atestación de una credencial ya anclada (E25).
    // Recibe el código de la credencial y el attestationUid con el que quedó anclada.
    // Como AnclarAsync, no lanza excepción si la red falla: devuelve Exitoso = false.
    Task<ResultadoRevocacion> RevocarAsync(string credencialCodigo, string attestationUid);

    // CAMBIO: consulta en la red lo que quedó registrado bajo una atestación (E27 y E29).
    // Ahora se consulta por attestationUid, que es lo que el puente entiende.
    // Devuelve null si no se encuentra (o si la red no responde): eso significa existeEnRed = false.
    Task<EvidenciaEnRed?> ConsultarAsync(string attestationUid);
}

// "record" es un tipo de C# pensado para transportar datos: se crea con "new" pasando los valores
// en el orden de los parámetros y sus propiedades son de solo lectura.

// ResultadoAnclaje: lo que devuelve AnclarAsync.
//   Exitoso:        true si la red confirmó el registro; false si falló (sin red, puente apagado...).
//   TxHash:         la referencia de la transacción; null si falló.
//   AttestationUid: el identificador de la atestación creada (columna attestation_uid); null si falló.
//   Mensaje:        explicación del fallo en español; null si todo salió bien.
public record ResultadoAnclaje(bool Exitoso, string? TxHash, string? AttestationUid, string? Mensaje);

public record ResultadoRevocacion(bool Exitoso, string? TxHash, string? Mensaje);

public record EvidenciaEnRed(string AttestationUid, string EvidenciaHash, bool Revocada);