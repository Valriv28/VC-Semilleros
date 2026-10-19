// Importa las anotaciones de validación: [Required].
// Vienen con .NET, no son un paquete externo (Art. 3).
using System.ComponentModel.DataAnnotations;

// Espacio de nombres de las peticiones: los DTOs de ENTRADA y de SALIDA de la API.
namespace VCSemilleros.Api.Peticiones;

// Petición de E26: POST /api/verificaciones (presentar una credencial y verificarla).
// El body trae UN solo campo: "vc", la credencial firmada tal como se le entregó al titular.
// Valida la FORMA (422): que el campo venga y no esté vacío. Si el texto es una credencial válida,
// si la firma corresponde o si el emisor está autorizado lo decide ServicioVerificacion, y un
// veredicto negativo NO es un error HTTP: la verificación se ejecutó y responde 200 (contrato E26).
// Esta operación no escribe nada en la BD ni en la red.
public class VerificacionSolicitar
{
    // Credencial firmada y serializada (el mismo texto que devolvió la emisión, E20 o E23).
    // Es "string?" (puede ser null) a propósito: si el body no trae el campo, llega null
    // y [Required] lo convierte en un 422 claro, con nuestro mensaje en español.
    // [Required] rechaza tres casos: campo ausente, texto vacío ("") y texto de solo espacios.
    // No se limita la longitud: una credencial firmada es un texto largo.
    [Required(ErrorMessage = "El campo vc es obligatorio.")]
    public string? Vc { get; set; }
}