// Importa las validaciones de los datos recibidos por la API.
using System.ComponentModel.DataAnnotations;

namespace VCSemilleros.Api.Peticiones;

// Datos necesarios para registrar una cuenta de estudiante.
public class AutenticacionRegistrar
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(120, ErrorMessage = "El nombre no puede superar los 120 caracteres.")]
    public string? Nombre { get; set; }

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [StringLength(120, ErrorMessage = "El correo no puede superar los 120 caracteres.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    public string? Correo { get; set; }

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(128, MinimumLength = 8,
        ErrorMessage = "La contraseña debe tener entre 8 y 128 caracteres.")]
    public string? Contrasena { get; set; }
}
