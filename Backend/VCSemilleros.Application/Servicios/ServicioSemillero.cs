using VCSemilleros.Domain.Excepciones;          // NoEncontradoExcepcion (el controlador la vuelve 404).
using VCSemilleros.Domain.Modelos;              // Semillero, RolUsuario.
using VCSemilleros.Domain.Repositorios;         // Los puertos: IRepositorioSemillero, IRepositorioUsuario.

namespace VCSemilleros.Application.Servicios;

// Caso de uso: gestionar semilleros (RF3, RF4). Aquí viven las REGLAS DE NEGOCIO.
// Solo conoce interfaces del dominio: no sabe nada de HTTP, Dapper ni Npgsql (Art. 4).
public class ServicioSemillero : IServicioSemillero
{
    // Los puertos que necesita. "readonly": se asignan una sola vez, en el constructor.
    private readonly IRepositorioSemillero _repositorioSemillero;
    private readonly IRepositorioUsuario _repositorioUsuario;

    // Constructor: recibe las INTERFACES por inyección de dependencias.
    // Program.cs decide qué clase concreta entrega (PostgreSQL en producción, falsos en las pruebas).
    public ServicioSemillero(IRepositorioSemillero repositorioSemillero, IRepositorioUsuario repositorioUsuario)
    {
        _repositorioSemillero = repositorioSemillero;
        _repositorioUsuario = repositorioUsuario;
    }

    // E3: lista los semilleros.
    public async Task<List<Semillero>> ListarAsync(int limite)
    {
        // Regla del contrato: limite <= 0 es un parámetro inválido (400).
        if (limite <= 0)
            throw new ArgumentException("El límite debe ser mayor que cero.");

        return await _repositorioSemillero.ObtenerTodosAsync(limite);
    }

    // E4: obtiene un semillero o lanza 404.
    public async Task<Semillero> ObtenerAsync(int id)
    {
        var semillero = await _repositorioSemillero.ObtenerPorIdAsync(id);

        // "is null" comprueba que no se encontró. "throw" lanza la excepción: interrumpe el método.
        if (semillero is null)
            throw new NoEncontradoExcepcion($"No existe un semillero con id = {id}");

        return semillero;
    }

    // E5: registra un semillero, validando que el responsable sea un docente.
    public async Task<int> CrearAsync(int responsableId, string nombre)
    {
        // 1) El responsable debe existir (404).
        var responsable = await _repositorioUsuario.ObtenerPorIdAsync(responsableId);
        if (responsable is null)
            throw new NoEncontradoExcepcion($"No existe un usuario con id = {responsableId}");

        // 2) Regla de negocio: debe tener rol DOCENTE (400). La BD no lo impone; lo hace este servicio.
        if (responsable.Rol != RolUsuario.DOCENTE)
            throw new ArgumentException("El responsable debe ser un usuario con rol DOCENTE.");

        // 3) Construye el modelo y lo guarda. "required" obliga a dar Nombre al crear el objeto.
        var semillero = new Semillero
        {
            ResponsableId = responsableId,
            Nombre = nombre
        };

        return await _repositorioSemillero.CrearAsync(semillero);
    }

    // E6: activa o inactiva un semillero.
    public async Task<int> CambiarEstadoAsync(int id, bool activo)
    {
        // Primero se comprueba que exista (404); el UPDATE por sí solo devolvería 0 filas sin explicar.
        var semillero = await _repositorioSemillero.ObtenerPorIdAsync(id);
        if (semillero is null)
            throw new NoEncontradoExcepcion($"No existe un semillero con id = {id}");

        return await _repositorioSemillero.ActualizarEstadoAsync(id, activo);
    }
}   