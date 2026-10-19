// Importa las anotaciones de validación: [Required], [StringLength]...
// Vienen con .NET, no son un paquete externo (Art. 3).
using System.ComponentModel.DataAnnotations;

// Espacio de nombres de las peticiones: los DTOs de ENTRADA y de SALIDA de la API (Estructura.txt, Art. 9).
namespace VCSemilleros.Api.Peticiones;

// Petición de E2: POST /api/autenticacion/ingresar.
// ASP.NET lee el JSON del body y lo convierte en este objeto; si alguna anotación falla,
// responde 422 con la lista de errores ANTES de ejecutar el método del controlador (plan §4.2).
// La petición valida la FORMA del dato (¿viene? ¿es texto?). Si el correo existe o la contraseña
// coincide lo decide ServicioAutenticacion (401), no esta clase.
public class AutenticacionIngresar
{
    // Correo con el que la persona ingresa. Es "string?" (puede ser null) a propósito:
    // si el body no trae el campo, llega null y [Required] lo convierte en un 422 claro.
    // Sin el "?", C# lo trataría como obligatorio con un mensaje en inglés que no controlamos.
    [Required(ErrorMessage = "El campo correo es obligatorio.")]
    [StringLength(120, ErrorMessage = "El campo correo no puede superar los 120 caracteres.")]
    public string? Correo { get; set; }

    // Contraseña en texto plano, tal como la escribe la persona. Solo viaja en esta petición:
    // nunca se guarda ni se devuelve (en la BD solo existe su hash PBKDF2).
    // [Required] rechaza el texto vacío y el compuesto solo de espacios.
    [Required(ErrorMessage = "El campo contrasena es obligatorio.")]
    public string? Contrasena { get; set; }
}