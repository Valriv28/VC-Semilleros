// Importa las anotaciones de validación: [Required], [StringLength].
using System.ComponentModel.DataAnnotations;

// Espacio de nombres de las peticiones: los DTOs de ENTRADA y de SALIDA de la API.
namespace VCSemilleros.Api.Peticiones;

// Petición de E8: POST /api/semilleros/{semilleroId}/periodos (crear un periodo).
// El semillero viaja en la RUTA, no en el body. Valida la FORMA (422). Que el semillero exista (404),
// que esté activo (400) y que la fecha de fin no sea anterior a la de inicio (400)
// lo decide ServicioPeriodo: son reglas de negocio.
public class PeriodoCrear
{
    // Nombre del periodo, por ejemplo "2026-2": de 1 a 20 caracteres (la columna es VARCHAR(20)).
    // Un nombre de más de 20 caracteres es el 422 del criterio 3 del smoke test.
    [Required(ErrorMessage = "El campo nombre es obligatorio.")]
    [StringLength(20, MinimumLength = 1, ErrorMessage = "El campo nombre debe tener entre 1 y 20 caracteres.")]
    public string? Nombre { get; set; }

    // Fecha de inicio, escrita "2026-08-01". "DateOnly" es una fecha sin hora (como la columna DATE).
    // Es "DateOnly?" (puede ser null) a propósito: si el body no la trae llega null y [Required] responde 422.
    // Si la trae mal escrita ("01/08/2026" o "ayer"), el framework no puede convertirla y también responde 422.
    [Required(ErrorMessage = "El campo fechaInicio es obligatorio.")]
    public DateOnly? FechaInicio { get; set; }

    // Fecha de fin, con el mismo formato. Que no sea anterior a la de inicio NO se comprueba aquí:
    // comparar dos campos entre sí es una regla de negocio y sale como 400 desde el servicio.
    [Required(ErrorMessage = "El campo fechaFin es obligatorio.")]
    public DateOnly? FechaFin { get; set; }
}