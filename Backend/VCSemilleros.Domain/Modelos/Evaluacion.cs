// Espacio de nombres: el mismo que el de los demás modelos del dominio.
namespace VCSemilleros.Domain.Modelos;

// Modelo entidad: cada objeto representa UNA fila de la tabla "evaluacion".
// Une dos cosas: el CRITERIO aplicado (tal como estaba al evaluar) y el RESULTADO
// (porcentaje de asistencia y si cumple). La elegibilidad la declara después un coordinador.
public class Evaluacion
{
	public int Id { get; set; }
	public int ParticipanteId { get; set; }
	public int CriterioVersion { get; set; }
	public decimal MinAsistenciaPct { get; set; }
	public int DefinidoPor { get; set; }

	// --- Resultado ---

	// PctAsistencia: columna "pct_asistencia" (NUMERIC(5,2) NOT NULL).
	// El porcentaje real calculado a partir de las asistencias del participante.
	public decimal PctAsistencia { get; set; }
	public bool Cumple { get; set; }
	public int? ElegibleDeclaradoPor { get; set; }
	public DateTime Fecha { get; set; }
}