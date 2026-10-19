using System.ComponentModel.DataAnnotations;

namespace VCSemilleros.Api.Peticiones;

public sealed class CriterioPeriodoGuardar
{
    [Range(typeof(decimal), "0", "100")]
    public decimal MinAsistenciaPct { get; set; }

    [Range(typeof(decimal), "0", "100")]
    public decimal EntregablesRequeridosPct { get; set; }

    [Range(0, int.MaxValue)]
    public int PermanenciaMeses { get; set; }
}
