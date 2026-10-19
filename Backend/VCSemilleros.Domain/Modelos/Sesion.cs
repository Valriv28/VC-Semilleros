// Espacio de nombres: el mismo que el de los demás modelos del dominio.
namespace VCSemilleros.Domain.Modelos;

// Modelo entidad: cada objeto representa UNA fila de la tabla "sesion".
// Una sesión es una reunión del semillero dentro de un periodo; en ella se toma asistencia.
public class Sesion
{
    public int Id { get; set; }
    public int PeriodoId { get; set; }
    public DateOnly Fecha { get; set; }
    public string? Descripcion { get; set; }
}