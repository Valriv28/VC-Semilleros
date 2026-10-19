// Importa las anotaciones de validación: [Required], [MinLength].
using System.ComponentModel.DataAnnotations;

// Espacio de nombres de las peticiones: los DTOs de ENTRADA y de SALIDA de la API.
namespace VCSemilleros.Api.Peticiones;

// Petición de E13: POST /api/sesiones/{sesionId}/asistencias (registrar la asistencia de TODA la sesión).
// La sesión viaja en la RUTA; el body trae la lista de participantes con su presencia.
// Valida la FORMA (422). Que la sesión y los participantes existan (404), que cada participante pertenezca
// al periodo de la sesión (400) y que no se repita una asistencia (500, UNIQUE de la BD) lo deciden
// ServicioAsistencia y la BD.
public class AsistenciaRegistrar
{
    // Lista de asistencias de la sesión. Debe venir y NO estar vacía:
    //  - [Required] rechaza el campo ausente (llega null).
    //  - [MinLength(1)] rechaza la lista vacía "[]": una sesión sin ninguna asistencia no tiene sentido.
    // ASP.NET también valida CADA elemento de la lista contra las anotaciones de AsistenciaItem,
    // así que un ítem incompleto produce su propio error 422.
    [Required(ErrorMessage = "El campo asistencias es obligatorio.")]
    [MinLength(1, ErrorMessage = "El campo asistencias debe tener al menos un elemento.")]
    public List<AsistenciaItem>? Asistencias { get; set; }
}

// Un elemento de la lista: la presencia de UN participante en la sesión.
// Va en el mismo archivo porque solo existe para esta petición (plan §2: "con su ítem por participante").
public class AsistenciaItem
{
    // Id del participante (no del usuario). Es "int?" para detectar el campo ausente:
    // con un "int" normal llegaría como 0 y pasaría sin avisar.
    [Required(ErrorMessage = "El campo participanteId es obligatorio en cada asistencia.")]
    public int? ParticipanteId { get; set; }

    // true = estuvo presente, false = estuvo ausente. Es "bool?" por la misma razón:
    // con un "bool" normal, un campo ausente llegaría como false y registraría una ausencia que nadie indicó.
    [Required(ErrorMessage = "El campo presente es obligatorio en cada asistencia.")]
    public bool? Presente { get; set; }
}