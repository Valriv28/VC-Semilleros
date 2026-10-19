using System.Security.Cryptography;              // ECDsa: para generar una clave de prueba.
using VCSemilleros.Application.Servicios;        // Los servicios REALES que se arman con repositorios falsos.
using VCSemilleros.Domain.Modelos;               // Usuario, Semillero, Periodo, Participante, Evaluacion.

namespace VCSemilleros.Tests.Pruebas.Application;

// Escenario compartido por las pruebas de la Fase 5: datos sembrados + los cinco servicios ya armados
// con repositorios y cliente de Stellar FALSOS (sin PostgreSQL ni red).
// xUnit crea una instancia nueva por prueba, y cada prueba crea su propio escenario: no se afectan entre sí.
//
// Datos sembrados (siguen el smoke test):
//   Usuarios:     1 docente · 2 coordinador (activo) · 3 estudiante "Ana María Torres Vega" · 4 estudiante "Juan Pablo Gómez"
//   Semillero 1, Periodo 1 "2026-2", Participante 1 (usuario 3) y Participante 2 (usuario 4).
//   Evaluación 1: cumple, con elegibilidad declarada por el coordinador 2   -> SE PUEDE emitir.
//   Evaluación 2: no cumple y sin elegibilidad declarada                      -> NO se puede emitir.
public class EscenarioFase5
{
    // Los repositorios falsos (públicos para que cada prueba pueda consultarlos o alterarlos).
    public RepositorioUsuarioFalso Usuarios { get; } = new();
    public RepositorioSemilleroFalso Semilleros { get; } = new();
    public RepositorioPeriodoFalso Periodos { get; } = new();
    public RepositorioParticipanteFalso Participantes { get; } = new();
    public RepositorioEvaluacionFalso Evaluaciones { get; }
    public RepositorioCredencialFalso Credenciales { get; } = new();
    public RepositorioAnclajeFalso Anclajes { get; } = new();
    public ClienteStellarFalso Stellar { get; } = new();

    // La clave privada de prueba (formato PEM) con la que se firma.
    public string ClavePem { get; }

    // Los servicios reales.
    public ServicioCredencialVerificable Verificable { get; }
    public ServicioEmision Emision { get; }
    public ServicioCredencial Consulta { get; }
    public ServicioVerificacion Verificacion { get; }
    public ServicioAuditoria Auditoria { get; }

    // Genera una clave ECDSA P-256 nueva y la exporta como texto PEM, igual que la variable de entorno real.
    public static string NuevaClavePem()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        return ecdsa.ExportPkcs8PrivateKeyPem();
    }

    public EscenarioFase5()
    {
        // Usuarios.
        Usuarios.Usuarios.Add(Nuevo(1, "Carlos Ramírez", "carlos@x.edu", RolUsuario.DOCENTE));
        Usuarios.Usuarios.Add(Nuevo(2, "Coordinador Semilleros", "coordinador@x.edu", RolUsuario.COORDINADOR));
        Usuarios.Usuarios.Add(Nuevo(3, "Ana María Torres Vega", "ana@x.edu", RolUsuario.ESTUDIANTE));
        Usuarios.Usuarios.Add(Nuevo(4, "Juan Pablo Gómez", "juan@x.edu", RolUsuario.ESTUDIANTE));

        // Semillero, periodo y participantes.
        Semilleros.Semilleros.Add(new Semillero { Id = 1, ResponsableId = 1, Nombre = "Semillero de Investigación VC", Activo = true });
        Periodos.Periodos.Add(new Periodo { Id = 1, SemilleroId = 1, Nombre = "2026-2", FechaInicio = new DateOnly(2026, 8, 1), FechaFin = new DateOnly(2026, 12, 15) });
        Participantes.Participantes.Add(new Participante { Id = 1, PeriodoId = 1, EstudianteId = 3 });
        Participantes.Participantes.Add(new Participante { Id = 2, PeriodoId = 1, EstudianteId = 4 });

        // El falso de evaluaciones necesita el de participantes para saber el periodo.
        Evaluaciones = new RepositorioEvaluacionFalso(Participantes);

        // Evaluación 1: cumple y su elegibilidad ya la declaró el coordinador 2.
        Evaluaciones.Evaluaciones.Add(new Evaluacion
        {
            Id = 1,
            ParticipanteId = 1,
            CriterioVersion = 1,
            MinAsistenciaPct = 80m,
            DefinidoPor = 2,
            PctAsistencia = 100m,
            Cumple = true,
            ElegibleDeclaradoPor = 2,
            Fecha = new DateTime(2026, 10, 17, 21, 30, 0)
        });

        // Evaluación 2: no cumple y sin elegibilidad declarada.
        Evaluaciones.Evaluaciones.Add(new Evaluacion
        {
            Id = 2,
            ParticipanteId = 2,
            CriterioVersion = 1,
            MinAsistenciaPct = 80m,
            DefinidoPor = 2,
            PctAsistencia = 0m,
            Cumple = false,
            ElegibleDeclaradoPor = null,
            Fecha = new DateTime(2026, 10, 17, 21, 30, 0)
        });

        // Los servicios, armados con los falsos.
        ClavePem = NuevaClavePem();
        Verificable = new ServicioCredencialVerificable(Participantes, Periodos, Semilleros, Usuarios, ClavePem);
        Emision = new ServicioEmision(Evaluaciones, Credenciales, Anclajes, Stellar, Verificable);
        Consulta = new ServicioCredencial(Credenciales, Evaluaciones, Usuarios, Anclajes, Stellar, Verificable);
        Verificacion = new ServicioVerificacion(Verificable, Credenciales, Evaluaciones, Usuarios);
        Auditoria = new ServicioAuditoria(Credenciales, Evaluaciones, Anclajes, Stellar);
    }

    // Crea un usuario activo con el hash de contraseña de prueba.
    private static Usuario Nuevo(int id, string nombre, string correo, RolUsuario rol)
        => new() { Id = id, Nombre = nombre, Correo = correo, Rol = rol, Activo = true, ContrasenaHash = DatosDePrueba.HashContrasena };
}