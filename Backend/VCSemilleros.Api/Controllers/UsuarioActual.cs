using System.Security.Claims;                    // ClaimsPrincipal: la persona identificada por el token JWT.

namespace VCSemilleros.Api.Controllers;

// Ayuda para leer QUIÉN está llamando, a partir del token JWT que validó el framework.
// "static class" = clase que no se instancia: solo agrupa métodos de ayuda.
// Se escribe en Controllers porque solo la usan los controladores; Application y Domain nunca ven el token (Art. 4).
public static class UsuarioActual
{
    // "this ClaimsPrincipal usuario" convierte el método en una EXTENSIÓN: se llama como User.ObtenerId().
    // Devuelve el id de la persona que ingresó (el claim "sub" que escribe AutenticacionController),
    // o null si el token no trae un id numérico válido.
    // OJO: si la persona es ADMINISTRADOR, este id es el de la tabla "administrador", no el de "usuario".
    public static int? ObtenerId(this ClaimsPrincipal usuario)
    {
        // MapInboundClaims = false en Program.cs: el claim conserva el nombre "sub" tal como se firmó.
        var valor = usuario.FindFirst("sub")?.Value;

        // TryParse intenta convertir el texto en número sin lanzar excepción si falla.
        return int.TryParse(valor, out var id) ? id : null;
    }
}