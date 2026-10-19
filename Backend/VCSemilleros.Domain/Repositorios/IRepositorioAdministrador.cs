// Importa el espacio de nombres de los modelos (Administrador).
using VCSemilleros.Domain.Modelos;

// Espacio de nombres de los puertos del dominio.
namespace VCSemilleros.Domain.Repositorios;

// Puerto de salida para la tabla "administrador". NUEVO: el administrador vive en una tabla aparte
// de "usuario" y no tiene rol en el enum rol_usuario, así que necesita su propio puerto
// para poder ingresar por E2 (A1/A2, enmienda acordada).
public interface IRepositorioAdministrador
{
	Task<Administrador?> ObtenerPorCorreoAsync(string correo);
}