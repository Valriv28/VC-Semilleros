// Importa las anotaciones de validación: [Required], [StringLength]...
using System.ComponentModel.DataAnnotations;

// Espacio de nombres de las peticiones: los DTOs de ENTRADA y de SALIDA de la API.
namespace VCSemilleros.Api.Peticiones;

// Petición de E5: POST /api/semilleros (registrar un semillero, body completo).
// Valida la FORMA del body (422). Que el responsable exista (404) y que sea DOCENTE (400)
// lo decide ServicioSemillero: son reglas de negocio, no de forma.
public class SemilleroCrear
{
    // Id del docente responsable. Es "int?" (entero que puede ser null) a propósito:
    //  - si el body no trae el campo, llega null y [Required] responde 422;
    //  - si el body trae 7.5 o "texto", el framework no puede convertirlo a entero y también responde 422.
    // Con un "int" normal, un campo ausente llegaría como 0 y nadie notaría que faltaba.
    [Required(ErrorMessage = "El campo responsableId es obligatorio.")]
    public int? ResponsableId { get; set; }

    // Nombre del semillero: obligatorio, de 1 a 120 caracteres (así lo dice la columna VARCHAR(120)).
    // MinimumLength = 1 evita el texto vacío; [Required] además rechaza el compuesto solo de espacios.
    [Required(ErrorMessage = "El campo nombre es obligatorio.")]
    [StringLength(120, MinimumLength = 1, ErrorMessage = "El campo nombre debe tener entre 1 y 120 caracteres.")]
    public string? Nombre { get; set; }
}