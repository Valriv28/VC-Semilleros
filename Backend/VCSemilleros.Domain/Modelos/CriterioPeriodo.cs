namespace VCSemilleros.Domain.Modelos;

public class CriterioPeriodo
{
    public int Id { get; set; }
    public int PeriodoId { get; set; }
    public int CriterioVersion { get; set; }
    public decimal MinAsistenciaPct { get; set; }
    public decimal EntregablesRequeridosPct { get; set; }
    public int PermanenciaMeses { get; set; }
    public int DefinidoPor { get; set; }
    public DateTime FechaActualizacion { get; set; }
}
