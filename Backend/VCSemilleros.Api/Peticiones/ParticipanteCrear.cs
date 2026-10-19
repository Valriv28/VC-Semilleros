// Importa las anotaciones de validación: [Required].
using System.ComponentModel.DataAnnotations;

// Espacio de nombres de las peticiones: los DTOs de ENTRADA y de SALIDA de la API.
namespace VCSemilleros.Api.Peticiones;

// Petición de E10: POST /api/periodos/{periodoId}/participantes (registrar un participante).
// El periodo viaja en la RUTA; en el body solo va el estudiante. Valida la FORMA (422).
// Que el periodo y el usuario existan (404) y que el usuario tenga rol ESTUDIANTE (400)
// lo decide ServicioPeriodo. Que el estudiante ya esté en el periodo lo rechaza la BD (UNIQUE) y sale como 500.
public class ParticipanteCrear
{
    // Id del usuario que se inscribe. Es "int?" a propósito: si el body no trae el campo llega null
    // y [Required] responde 422; con un "int" normal llegaría como 0 y pasaría sin avisar.
    // Si trae otro tipo (por ejemplo 3.5 o "tres"), el framework tampoco puede convertirlo y responde 422.
    [Required(ErrorMessage = "El campo estudianteId es obligatorio.")]
    public int? EstudianteId { get; set; }
}