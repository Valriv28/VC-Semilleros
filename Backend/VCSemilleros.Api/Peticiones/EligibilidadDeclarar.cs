// Importa las anotaciones de validación: [Required].
using System.ComponentModel.DataAnnotations;

// Espacio de nombres de las peticiones: los DTOs de ENTRADA y de SALIDA de la API.
namespace VCSemilleros.Api.Peticiones;

// Petición de E19: PATCH /api/evaluaciones/{id}/elegibilidad (declarar elegible para emisión).
// El nombre "EligibilidadDeclarar" es el que fijan el plan y el contrato (se conserva tal cual).
// La evaluación viaja en la RUTA; el body solo dice QUIÉN declara (PATCH = actualización parcial).
// Valida la FORMA (422). Que la evaluación y el usuario existan (404), que la evaluación cumpla el criterio
// (400), que quien declara sea COORDINADOR (400) y que no estuviera ya declarada (400)
// lo decide ServicioEvaluacion. Declarar la elegibilidad es una decisión distinta del registro
// de asistencia y de la emisión (Art. 2).
public class EligibilidadDeclarar
{
    // Id del coordinador que declara la elegibilidad. Es "int?" para detectar el campo ausente:
    // con un "int" normal llegaría como 0 y pasaría sin avisar. Un tipo equivocado también responde 422.
    [Required(ErrorMessage = "El campo declaradoPor es obligatorio.")]
    public int? DeclaradoPor { get; set; }
}