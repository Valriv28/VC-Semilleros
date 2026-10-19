// Importa las anotaciones de validación: [Required].
using System.ComponentModel.DataAnnotations;

// Espacio de nombres de las peticiones: los DTOs de ENTRADA y de SALIDA de la API.
namespace VCSemilleros.Api.Peticiones;

// Petición de E6: PATCH /api/semilleros/{id}/estado (activar o inactivar un semillero).
// PATCH = actualización PARCIAL: solo viaja el campo que cambia. Por eso el body es únicamente {"activo": true|false}.
// (Contraste con E5, un POST que lleva el body completo.)
public class SemilleroEstado
{
    // Nuevo estado del semillero: true = activo, false = inactivo.
    // Es "bool?" (booleano que puede ser null) a propósito:
    //  - si el body no trae el campo, llega null y [Required] responde 422;
    //  - si el body trae otro tipo (por ejemplo "activo": "si"), el framework tampoco puede convertirlo y responde 422.
    // Con un "bool" normal, un campo ausente llegaría como false e INACTIVARÍA el semillero sin que nadie lo pidiera.
    [Required(ErrorMessage = "El campo activo es obligatorio.")]
    public bool? Activo { get; set; }
}