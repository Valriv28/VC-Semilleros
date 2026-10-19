// Espacio de nombres: el mismo que el de los demás modelos del dominio.
namespace VCSemilleros.Domain.Modelos;

// Modelo entidad: cada objeto representa UNA fila de la tabla "credencial".
// La credencial es el artefacto verificable firmado (Art. 2). OJO: aquí NO se guarda la
// credencial completa, solo su código, su estado y su hash (vc_hash).
// La revocación vive en la propia fila (ya no hay tabla de eventos).
public class Credencial
{
    public required string Codigo { get; set; }
    public int EvaluacionId { get; set; }
    public int TitularId { get; set; }
    public required string Tipo { get; set; }
    public EstadoCredencial Estado { get; set; }
    public required string VcHash { get; set; }
    public DateTime FechaEmision { get; set; }
    public int? RevocadaPor { get; set; }
    public string? MotivoRevocacion { get; set; }
    public DateTime? FechaRevocacion { get; set; }
}