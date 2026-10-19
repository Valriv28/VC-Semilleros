// Espacio de nombres: el mismo que el de los demás modelos del dominio.
namespace VCSemilleros.Domain.Modelos;

// Modelo entidad: cada objeto representa UNA fila de la tabla "asistencia".
// Un registro de asistencia NO es una certificación ni una elegibilidad.
public class Asistencia
{
    public int Id { get; set; }
    public int SesionId { get; set; }
    public int ParticipanteId { get; set; }
    public bool Presente { get; set; }

    // La BD impone UNIQUE (sesion_id, participante_id): un participante solo puede tener
    // UN registro por sesión. Repetirlo lo rechaza el motor y la API responde 500.
}