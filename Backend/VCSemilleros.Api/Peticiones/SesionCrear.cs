// Importa las anotaciones de validación: [Required], [StringLength].
using System.ComponentModel.DataAnnotations;

// Espacio de nombres de las peticiones: los DTOs de ENTRADA y de SALIDA de la API.
namespace VCSemilleros.Api.Peticiones;

// Petición de E12: POST /api/periodos/{periodoId}/sesiones (registrar una sesión).
// El periodo viaja en la RUTA. Valida la FORMA (422); que el periodo exista (404) lo decide ServicioAsistencia.
public class SesionCrear
{
    // Fecha de la sesión, escrita "2026-10-03". "DateOnly" es una fecha sin hora (como la columna DATE).
    // Es "DateOnly?" a propósito: si el body no la trae llega null y [Required] responde 422;
    // si la trae mal escrita ("03/10/2026"), el framework no puede convertirla y también responde 422.
    [Required(ErrorMessage = "El campo fecha es obligatorio.")]
    public DateOnly? Fecha { get; set; }

    // Descripción de la sesión. Es OPCIONAL (la columna admite NULL): sin [Required], un body sin este
    // campo es válido y la descripción llega como null. Si viene, no puede superar los 200 caracteres.
    // [StringLength] no se queja de un valor null, así que solo comprueba la longitud cuando hay texto.
    [StringLength(200, ErrorMessage = "El campo descripcion no puede superar los 200 caracteres.")]
    public string? Descripcion { get; set; }
}