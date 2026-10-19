// Espacio de nombres: el mismo que el de los demás modelos del dominio.
namespace VCSemilleros.Domain.Modelos;

// Modelo entidad: cada objeto representa UNA fila de la tabla "anclaje_stellar".
// solo un hash y la referencia de la transacción. NUNCA datos personales ni la credencial completa.
public class AnclajeStellar
{
	public int Id { get; set; }
	public required string CredencialCodigo { get; set; }
	public required string EvidenciaHash { get; set; }
	public string? TxHash { get; set; }
	public EstadoAnclaje Estado { get; set; }
	public string? AttestationUid { get; set; }
	public DateTime FechaSolicitud { get; set; }
	public DateTime? FechaExpiracion { get; set; }
}