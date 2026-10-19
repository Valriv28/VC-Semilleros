// Importa las anotaciones de validación: [Required], [StringLength].
using System.ComponentModel.DataAnnotations;

// Espacio de nombres de las peticiones: los DTOs de ENTRADA y de SALIDA de la API.
namespace VCSemilleros.Api.Peticiones;

// Petición de E25: POST /api/credenciales/{codigo}/revocacion (revocar con motivo).
// El código de la credencial viaja en la RUTA. Valida la FORMA (422). Que la credencial y el usuario
// existan (404), que quien revoca sea COORDINADOR (400) y que la credencial no estuviera ya REVOCADA (400)
// lo decide ServicioCredencial.
public class CredencialRevocar
{
    // Id del coordinador que revoca. Es "int?" para detectar el campo ausente:
    // con un "int" normal llegaría como 0 y pasaría sin avisar.
    [Required(ErrorMessage = "El campo revocadaPor es obligatorio.")]
    public int? RevocadaPor { get; set; }

    // Motivo de la revocación: obligatorio, de 1 a 500 caracteres. Queda guardado en la BD junto con
    // quién revocó y cuándo (RF13). Un motivo ausente, vacío o de solo espacios es el 422 del criterio 10:
    // [Required] rechaza esos tres casos, y [StringLength] rechaza el que supera los 500 caracteres.
    [Required(ErrorMessage = "El campo motivo es obligatorio.")]
    [StringLength(500, MinimumLength = 1, ErrorMessage = "El campo motivo debe tener entre 1 y 500 caracteres.")]
    public string? Motivo { get; set; }
}