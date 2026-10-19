// Espacio de nombres: el mismo que el de los demás modelos del dominio.
namespace VCSemilleros.Domain.Modelos;

// Refleja el tipo enumerado estado_anclaje de PostgreSQL.
// El "anclaje" es registrar la evidencia mínima (hash + transacción) en Stellar Testnet.
// Los valores van en MAYÚSCULAS para coincidir letra por letra con la BD.
public enum EstadoAnclaje
{
    // PENDIENTE: el anclaje se solicitó, pero todavía no se confirma en la red.
    // Es el estado por defecto al crear la fila.
    PENDIENTE,

    // ANCLADO: la red confirmó el registro y ya existe un tx_hash.
    ANCLADO,

    // FALLIDO: no se pudo anclar (por ejemplo, sin acceso a la red).
    // La credencial NO se revierte: queda emitida con el anclaje en FALLIDO (P4).
    FALLIDO
}