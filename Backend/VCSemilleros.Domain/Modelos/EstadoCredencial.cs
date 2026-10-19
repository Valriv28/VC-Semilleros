// Espacio de nombres: el mismo que el de los demás modelos del dominio.
namespace VCSemilleros.Domain.Modelos;

// Refleja el tipo enumerado estado_credencial de PostgreSQL.
// Los valores van en MAYÚSCULAS para coincidir letra por letra con la BD.
public enum EstadoCredencial
{
    // VIGENTE: la credencial es válida. Es el estado por defecto al emitirla.
    VIGENTE,

    // SUSPENDIDA: está en pausa; en la verificación cuenta como FALLO.
    // (Suspender es una historia Should Have: no hay endpoint, pero el valor existe en la BD.)
    SUSPENDIDA,

    // REVOCADA: se dejó sin efecto, con motivo, fecha y quien la revocó.
    // En la verificación cuenta como FALLO.
    REVOCADA,

    // INDETERMINADA: no se puede saber el estado; en la verificación es NO_EVALUADO.
    INDETERMINADA
}