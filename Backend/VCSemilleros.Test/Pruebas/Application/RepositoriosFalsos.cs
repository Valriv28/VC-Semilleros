using VCSemilleros.Domain.Modelos;              // Los modelos del dominio.
using VCSemilleros.Domain.Repositorios;         // Los puertos que estas clases falsas implementan.

namespace VCSemilleros.Tests.Pruebas.Application;

// REPOSITORIOS FALSOS (tambiÃ©n llamados "dobles de prueba"): implementan los mismos puertos que
// los repositorios PostgreSQL, pero guardan los datos en LISTAS EN MEMORIA.
// Gracias a la arquitectura hexagonal, los servicios no notan la diferencia: reciben una
// IRepositorioâ€¦ y funciona igual. AsÃ­ las pruebas corren sin PostgreSQL ni red (criterio 12).
// Las listas son pÃºblicas para que cada prueba pueda sembrar los datos que necesita.
// Los falsos de credencial, anclaje y el cliente de Stellar llegan en la Fase 5.

// Datos que las pruebas comparten. "internal static": solo se ve dentro del proyecto de pruebas.
internal static class DatosDePrueba
{
    // Valor cualquiera para la columna contrasena_hash de los usuarios sembrados.
    // NUNCA se verifica como contraseÃ±a real (salvo en ServicioAutenticacionPruebas, que genera el suyo),
    // asÃ­ que basta con un texto fijo. "const" = valor fijo que nunca cambia.
    public const string HashContrasena = "hash-de-prueba";
}

// Clase estÃ¡tica auxiliar: calcula el siguiente id, imitando al SERIAL de PostgreSQL.
internal static class Ids
{
    // "IEnumerable<int>" es cualquier colecciÃ³n de enteros; si estÃ¡ vacÃ­a, el primer id es 1.
    public static int Siguiente(IEnumerable<int> idsExistentes)
    {
        // Any() dice si la colecciÃ³n tiene al menos un elemento; Max() devuelve el mayor.
        return idsExistentes.Any() ? idsExistentes.Max() + 1 : 1;
    }
}

// ---------- Usuario ----------
public class RepositorioUsuarioFalso : IRepositorioUsuario
{
    // Lista pÃºblica: la prueba agrega aquÃ­ los usuarios que necesite.
    public List<Usuario> Usuarios { get; } = new();

    // Task.FromResult(x) devuelve un Task ya terminado con el valor x (no hay nada que esperar).
    // FirstOrDefault devuelve el primer usuario que cumple la condiciÃ³n, o null si no hay ninguno.
    public Task<Usuario?> ObtenerPorIdAsync(int id)
        => Task.FromResult(Usuarios.FirstOrDefault(u => u.Id == id));

    // NUEVO: busca por correo sin distinguir mayÃºsculas, como el lower(...) del SQL real.
    public Task<Usuario?> ObtenerPorCorreoAsync(string correo)
        => Task.FromResult(Usuarios.FirstOrDefault(u =>
               string.Equals(u.Correo, correo, StringComparison.OrdinalIgnoreCase)));

    public Task<int> CrearAsync(Usuario usuario)
    {
        if (Usuarios.Any(u =>
            string.Equals(u.Correo, usuario.Correo, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("El correo ya estÃ¡ registrado.");
        }

        usuario.Id = Ids.Siguiente(Usuarios.Select(u => u.Id));
        Usuarios.Add(usuario);

        return Task.FromResult(usuario.Id);
    }


// ---------- Administrador ----------
// NUEVO: imita IRepositorioAdministrador para probar el ingreso del administrador.
}
public class RepositorioAdministradorFalso : IRepositorioAdministrador
{
    public List<Administrador> Administradores { get; } = new();

    public Task<Administrador?> ObtenerPorCorreoAsync(string correo)
        => Task.FromResult(Administradores.FirstOrDefault(a =>
               string.Equals(a.Correo, correo, StringComparison.OrdinalIgnoreCase)));
}


// ---------- Semillero ----------
public class RepositorioSemilleroFalso : IRepositorioSemillero
{
    public List<Semillero> Semilleros { get; } = new();

    // Devuelve los semilleros ordenados por id.
    public Task<List<Semillero>> ObtenerTodosAsync(int limite)
        => Task.FromResult(
            Semilleros
                .OrderBy(s => s.Id)
                .Take(limite)
                .ToList());

    // Devuelve únicamente los semilleros del responsable indicado.
    public Task<List<Semillero>> ObtenerPorResponsableAsync(
        int responsableId,
        int limite)
        => Task.FromResult(
            Semilleros
                .Where(s => s.ResponsableId == responsableId)
                .OrderBy(s => s.Id)
                .Take(limite)
                .ToList());

    public Task<Semillero?> ObtenerPorIdAsync(int id)
        => Task.FromResult(
            Semilleros.FirstOrDefault(s => s.Id == id));

    public Task<int> CrearAsync(Semillero semillero)
    {
        semillero.Id = Ids.Siguiente(Semilleros.Select(s => s.Id));
        semillero.Activo = true;
        Semilleros.Add(semillero);

        return Task.FromResult(semillero.Id);
    }

    // Devuelve 1 si encontró el semillero y 0 si no existe.
    public Task<int> ActualizarEstadoAsync(int id, bool activo)
    {
        var semillero = Semilleros.FirstOrDefault(s => s.Id == id);

        if (semillero is null)
            return Task.FromResult(0);

        semillero.Activo = activo;

        return Task.FromResult(1);
    }
}

// ---------- Periodo ----------
public class RepositorioPeriodoFalso : IRepositorioPeriodo
{
    public List<Periodo> Periodos { get; } = new();

    public Task<Periodo?> ObtenerPorIdAsync(int id)
        => Task.FromResult(Periodos.FirstOrDefault(p => p.Id == id));

    public Task<List<Periodo>> ObtenerPorSemilleroAsync(int semilleroId, int limite)
        => Task.FromResult(Periodos.Where(p => p.SemilleroId == semilleroId)
                                   .OrderBy(p => p.Id).Take(limite).ToList());

    public Task<int> CrearAsync(Periodo periodo)
    {
        periodo.Id = Ids.Siguiente(Periodos.Select(p => p.Id));
        Periodos.Add(periodo);
        return Task.FromResult(periodo.Id);
    }
}

// ---------- Participante ----------
public class RepositorioParticipanteFalso : IRepositorioParticipante
{
    public List<Participante> Participantes { get; } = new();

    public Task<Participante?> ObtenerPorIdAsync(int id)
        => Task.FromResult(Participantes.FirstOrDefault(p => p.Id == id));

    public Task<List<Participante>> ObtenerPorPeriodoAsync(int periodoId, int limite)
        => Task.FromResult(Participantes.Where(p => p.PeriodoId == periodoId)
                                        .OrderBy(p => p.Id).Take(limite).ToList());

    public Task<int> CrearAsync(Participante participante)
    {
        // Imita el UNIQUE (periodo_id, estudiante_id) de la BD: si ya existe, lanza una excepciÃ³n
        // (en la BD real serÃ­a un error del motor que la API responde como 500).
        if (Participantes.Any(p => p.PeriodoId == participante.PeriodoId
                                && p.EstudianteId == participante.EstudianteId))
            throw new InvalidOperationException("duplicate key value violates unique constraint (participante)");

        participante.Id = Ids.Siguiente(Participantes.Select(p => p.Id));
        Participantes.Add(participante);
        return Task.FromResult(participante.Id);
    }
}

// ---------- Sesion ----------
public class RepositorioSesionFalso : IRepositorioSesion
{
    public List<Sesion> Sesiones { get; } = new();

    public Task<Sesion?> ObtenerPorIdAsync(int id)
        => Task.FromResult(Sesiones.FirstOrDefault(s => s.Id == id));

    public Task<List<Sesion>> ObtenerPorPeriodoAsync(int periodoId, int limite)
        => Task.FromResult(Sesiones.Where(s => s.PeriodoId == periodoId)
                                   .OrderBy(s => s.Fecha).ThenBy(s => s.Id).Take(limite).ToList());

    // Count(condiciÃ³n) cuenta los elementos que cumplen la condiciÃ³n (como count(*) con WHERE).
    public Task<int> ContarPorPeriodoAsync(int periodoId)
        => Task.FromResult(Sesiones.Count(s => s.PeriodoId == periodoId));

    public Task<int> CrearAsync(Sesion sesion)
    {
        sesion.Id = Ids.Siguiente(Sesiones.Select(s => s.Id));
        Sesiones.Add(sesion);
        return Task.FromResult(sesion.Id);
    }
}

// ---------- Asistencia ----------
public class RepositorioAsistenciaFalso : IRepositorioAsistencia
{
    public List<Asistencia> Asistencias { get; } = new();

    public Task<List<Asistencia>> ObtenerPorSesionAsync(int sesionId, int limite)
        => Task.FromResult(Asistencias.Where(a => a.SesionId == sesionId)
                                      .OrderBy(a => a.Id).Take(limite).ToList());

    // Imita la TRANSACCIÃ“N del repositorio real: primero comprueba TODAS las filas contra el
    // UNIQUE (sesion_id, participante_id); si alguna repite, lanza la excepciÃ³n SIN guardar ninguna.
    public Task<int> CrearVariasAsync(List<Asistencia> asistencias)
    {
        foreach (var nueva in asistencias)
        {
            if (Asistencias.Any(a => a.SesionId == nueva.SesionId && a.ParticipanteId == nueva.ParticipanteId))
                throw new InvalidOperationException("duplicate key value violates unique constraint (asistencia)");
        }

        foreach (var nueva in asistencias)
        {
            nueva.Id = Ids.Siguiente(Asistencias.Select(a => a.Id));
            Asistencias.Add(nueva);
        }

        return Task.FromResult(asistencias.Count);
    }

    public Task<int> ContarPresentesAsync(int participanteId)
        => Task.FromResult(Asistencias.Count(a => a.ParticipanteId == participanteId && a.Presente));
}

// ---------- Evaluacion ----------
public class RepositorioEvaluacionFalso : IRepositorioEvaluacion
{
    // La evaluaciÃ³n no guarda el periodo: se llega a Ã©l por el participante (como el JOIN del SQL real).
    // Por eso este falso recibe el falso de participantes por constructor.
    private readonly RepositorioParticipanteFalso _participantes;

    public RepositorioEvaluacionFalso(RepositorioParticipanteFalso participantes)
    {
        _participantes = participantes;
    }

    public List<Evaluacion> Evaluaciones { get; } = new();

    // Ids de los participantes de un periodo (auxiliar privado).
    private HashSet<int> IdsParticipantesDelPeriodo(int periodoId)
        => _participantes.Participantes.Where(p => p.PeriodoId == periodoId).Select(p => p.Id).ToHashSet();

    public Task<Evaluacion?> ObtenerPorIdAsync(int id)
        => Task.FromResult(Evaluaciones.FirstOrDefault(e => e.Id == id));

    public Task<List<Evaluacion>> ObtenerPorPeriodoAsync(int periodoId, int limite)
    {
        var ids = IdsParticipantesDelPeriodo(periodoId);
        return Task.FromResult(Evaluaciones.Where(e => ids.Contains(e.ParticipanteId))
                                           .OrderBy(e => e.Id).Take(limite).ToList());
    }

    // La evaluaciÃ³n de MAYOR criterio_version; si empatan, la de mayor id (igual que el SQL real).
    public Task<Evaluacion?> ObtenerCriterioVigenteAsync(int periodoId)
    {
        var ids = IdsParticipantesDelPeriodo(periodoId);
        return Task.FromResult(Evaluaciones.Where(e => ids.Contains(e.ParticipanteId))
                                           .OrderByDescending(e => e.CriterioVersion)
                                           .ThenByDescending(e => e.Id)
                                           .FirstOrDefault());
    }

    public Task<int> CrearAsync(Evaluacion evaluacion)
    {
        // Imita el UNIQUE (participante_id, criterio_version) de la BD.
        if (Evaluaciones.Any(e => e.ParticipanteId == evaluacion.ParticipanteId
                               && e.CriterioVersion == evaluacion.CriterioVersion))
            throw new InvalidOperationException("duplicate key value violates unique constraint (evaluacion)");

        evaluacion.Id = Ids.Siguiente(Evaluaciones.Select(e => e.Id));
        evaluacion.Fecha = DateTime.Now;          // La BD aplica DEFAULT now().
        Evaluaciones.Add(evaluacion);
        return Task.FromResult(evaluacion.Id);
    }

    public Task<int> DeclararElegibleAsync(int id, int coordinadorId)
    {
        var evaluacion = Evaluaciones.FirstOrDefault(e => e.Id == id);
        if (evaluacion is null) return Task.FromResult(0);
        evaluacion.ElegibleDeclaradoPor = coordinadorId;
        return Task.FromResult(1);
    }
}


