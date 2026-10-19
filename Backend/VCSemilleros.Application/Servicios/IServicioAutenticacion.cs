namespace VCSemilleros.Application.Servicios;

// Puerto de ENTRADA del caso de uso "ingresar" (RF2; endpoint E2: POST /api/autenticacion/ingresar).
// Solo IDENTIFICA a la persona. Convertir esa identidad en un token JWT no es asunto de Application:
// lo hace el controlador en la Api, que es la única capa que conoce el paquete JWT (Art. 4).
public interface IServicioAutenticacion
{
    // E2: comprueba correo y contraseña. Busca primero en "usuario" y, si no está, en "administrador".
    // Devuelve la identidad de la persona (sin la contraseña ni su hash, nunca).
    // Lanza NoAutorizadoExcepcion (401) si el correo no existe, la contraseña no coincide
    // o el usuario está inactivo. El mensaje es el MISMO en los tres casos para no revelar
    // qué correos están registrados.
    Task<IdentidadAutenticada> IngresarAsync(string correo, string contrasena);
}

// "record": tipo de C# para transportar datos, con propiedades de solo lectura.
// IdentidadAutenticada es lo que devuelve el ingreso: los campos de la tabla usuario que fija el contrato E2.
//   Rol: texto con el nombre del rol: ESTUDIANTE, DOCENTE, COORDINADOR, o ADMINISTRADOR cuando la persona
//        salió de la tabla "administrador". Es texto y no el enum RolUsuario porque el enum de la BD
//        no tiene ADMINISTRADOR (el administrador vive en otra tabla y no se toca el enum).
public record IdentidadAutenticada(int Id, string Nombre, string Correo, string Rol);