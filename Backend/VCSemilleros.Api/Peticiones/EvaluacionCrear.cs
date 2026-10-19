// Importa las anotaciones de validación: [Required], [Range].
using System.ComponentModel.DataAnnotations;

// Espacio de nombres de las peticiones: los DTOs de ENTRADA y de SALIDA de la API.
namespace VCSemilleros.Api.Peticiones;

// Petición de E17: POST /api/participantes/{participanteId}/evaluaciones (evaluar contra un criterio).
// El participante viaja en la RUTA. Valida la FORMA (422). Que el participante y el usuario existan (404),
// que "definidoPor" sea COORDINADOR (400), que el periodo tenga sesiones (400) y que la versión de criterio
// no esté repetida (500, UNIQUE de la BD) lo deciden ServicioEvaluacion y la BD.
// El porcentaje de asistencia y "cumple" NO vienen en el body: los calcula el sistema.
public class EvaluacionCrear
{
    // Porcentaje mínimo de asistencia que exige el criterio: de 0 a 100.
    // "decimal?" acepta números con decimales (80 o 80.5) y detecta el campo ausente (null).
    // Un valor fuera de rango (120) o de otro tipo ("alto") responde 422, como el criterio 5 del smoke test.
    // Range recibe los límites como números con decimales (0.0 y 100.0) y también comprueba un "decimal?".
    [Required(ErrorMessage = "El campo minAsistenciaPct es obligatorio.")]
    [Range(0.0, 100.0, ErrorMessage = "El campo minAsistenciaPct debe estar entre 0 y 100.")]
    public decimal? MinAsistenciaPct { get; set; }

    // Id del usuario que define el criterio (debe ser un coordinador; eso lo decide el servicio).
    // "int?" para detectar el campo ausente: con un "int" normal llegaría como 0 y pasaría sin avisar.
    [Required(ErrorMessage = "El campo definidoPor es obligatorio.")]
    public int? DefinidoPor { get; set; }

    // Versión del criterio. Es OPCIONAL: si el body no la trae queda null y el controlador usa 1
    // (el mismo valor por defecto de la columna criterio_version).
    // Sin [Required], un campo ausente es válido; si viene, debe ser un entero de 1 en adelante.
    // [Range] no se queja de un valor null, así que solo comprueba el rango cuando hay un número.
    [Range(1, int.MaxValue, ErrorMessage = "El campo criterioVersion debe ser un entero mayor o igual a 1.")]
    public int? CriterioVersion { get; set; }
}