using VCSemilleros.Domain.Modelos;               // EstadoCredencial, EstadoAnclaje.

namespace VCSemilleros.Application.Servicios;

// Puerto de ENTRADA del caso de uso "emitir la credencial" (RF10; endpoint E20).
// Art. 2: emitir es una decisión SEPARADA de la asistencia y de la elegibilidad. Este servicio no
// declara elegibilidad: exige que ya esté declarada.
public interface IServicioEmision
{
    // E20: ejecuta el flujo de emisión de 9 pasos (plan §4.7) para una evaluación.
    //   404 si la evaluación no existe.
    //   400 si no tiene la elegibilidad declarada, si ya tiene una credencial, o si la credencial
    //       construida está incompleta (en ese caso NO se firma).
    // Si Stellar falla, la credencial queda emitida igual y el anclaje se guarda como FALLIDO (P4).
    Task<ResultadoEmision> EmitirAsync(int evaluacionId);
}

// "record": tipo de C# para transportar datos, con propiedades de solo lectura.
// ResultadoEmision es lo que E20 devuelve:
//   Codigo:           el código de la credencial emitida.
//   EstadoCredencial: VIGENTE al emitir.
//   EstadoAnclaje:    ANCLADO o FALLIDO.
//   TxHash:           la transacción en Stellar; null si el anclaje falló.
//   Vc:               la credencial firmada y serializada, lista para entregarse.
public record ResultadoEmision(
    string Codigo,
    EstadoCredencial EstadoCredencial,
    EstadoAnclaje EstadoAnclaje,
    string? TxHash,
    string Vc);