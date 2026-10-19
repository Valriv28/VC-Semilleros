namespace VCSemilleros.Application.Servicios;

// Puerto de ENTRADA del caso de uso "verificar una credencial presentada" (RF14; endpoint E26).
// Art. 2: verificar es un análisis técnico y de confianza SEPARADO de emitir. La verificación
// NO escribe nada: ni en la BD ni en la red.
public interface IServicioVerificacion
{
    // E26: recibe la credencial firmada tal como se entregó (el texto "vc") y ejecuta cuatro controles
    // independientes: ESTRUCTURA, PRUEBA_CRIPTOGRAFICA, EMISOR y ESTADO.
    // Un veredicto negativo NO es una excepción: la verificación se ejecutó y la respuesta es 200.
    Task<ResultadoVerificacion> VerificarAsync(string vc);
}

// Resultado de un control. Los nombres van en MAYÚSCULAS porque así salen en el JSON del contrato.
public enum ResultadoControl
{
    PASO,           // El control se cumple.
    FALLO,          // El control no se cumple.
    NO_EVALUADO     // No se pudo evaluar (por ejemplo, la firma si la estructura falló).
}

// Veredicto final de la verificación (regla de RF14).
public enum Veredicto
{
    VERIFICADA,     // Los cuatro controles pasan.
    RECHAZADA,      // Algún control falla.
    INDETERMINADA   // Ninguno falla, pero alguno quedó sin evaluar.
}

// "record": tipo de C# para transportar datos, con propiedades de solo lectura.
// ControlVerificacion: un control con su resultado y una explicación en español.
//   Control: ESTRUCTURA, PRUEBA_CRIPTOGRAFICA, EMISOR o ESTADO.
public record ControlVerificacion(string Control, ResultadoControl Resultado, string Detalle);

// ResultadoVerificacion: lo que devuelve E26.
//   Codigo: el código leído de la credencial; null si no se pudo leer.
public record ResultadoVerificacion(Veredicto Veredicto, string? Codigo, List<ControlVerificacion> Controles);