// Importa Claim y ClaimsIdentity: un "claim" es un dato que el token afirma sobre la persona (su id, su rol...).
using System.Security.Claims;
// Importa Encoding: convierte la clave de texto en bytes para firmar el token.
using System.Text;
// Importa [AllowAnonymous]: permite llamar a un endpoint SIN token (aquí es obligatorio: es el que lo entrega).
using Microsoft.AspNetCore.Authorization;
// Importa ControllerBase, [ApiController], [HttpPost], [Route], [FromBody], IActionResult...
using Microsoft.AspNetCore.Mvc;
// Importa JsonWebTokenHandler: la clase que crea el token JWT. Viene con el paquete JwtBearer (enmienda acordada).
using Microsoft.IdentityModel.JsonWebTokens;
// Importa SymmetricSecurityKey, SigningCredentials y SecurityTokenDescriptor: las piezas para firmar el token.
using Microsoft.IdentityModel.Tokens;
// Importa nuestros DTOs: la petición de entrada y los sobres de respuesta.
using VCSemilleros.Api.Peticiones;
// Importa los servicios (casos de uso) de Application. El controlador solo conoce INTERFACES (Art. 4).
using VCSemilleros.Application.Servicios;
// Importa NoAutorizadoExcepcion, que el controlador traduce a 401.
using VCSemilleros.Domain.Excepciones;

// Espacio de nombres de los controladores de la capa Api.
namespace VCSemilleros.Api.Controllers;

// Controlador de la autenticación (E2): identifica a la persona y le entrega un token JWT.
//
// REPARTO DE TRABAJO (Art. 4):
//   - ServicioAutenticacion (Application) IDENTIFICA: comprueba correo y contraseña. No sabe qué es un JWT.
//   - Este controlador (Api) convierte esa identidad en un TOKEN. Solo la Api conoce el paquete JwtBearer.
//
// El token viaja después en la cabecera "Authorization: Bearer <token>" de cada llamada, y Program.cs lo
// valida con los MISMOS valores (clave, emisor y audiencia) con que se firma aquí.
//
// [ApiController]: valida el body contra la petición ANTES de entrar al método (422 con la lista de errores).
// [Route("api/autenticacion")]: toda ruta de este controlador empieza con /api/autenticacion.
// [AllowAnonymous]: el rol del contrato es "público": nadie tiene token todavía cuando ingresa.
[ApiController]
[Route("api/autenticacion")]
[AllowAnonymous]
public class AutenticacionController : ControllerBase
{
    // El servicio que identifica a la persona y la configuración de donde salen los valores del token.
    // "readonly": se asignan una sola vez, en el constructor.
    private readonly IServicioAutenticacion _servicioAutenticacion;
    private readonly IConfiguration _configuracion;

    // Constructor: ASP.NET Core inyecta el servicio (registrado en Program.cs) y la configuración
    // (appsettings.json + variables de entorno), que el framework ya trae registrada.
    public AutenticacionController(IServicioAutenticacion servicioAutenticacion, IConfiguration configuracion)
    {
        _servicioAutenticacion = servicioAutenticacion;
        _configuracion = configuracion;
    }

    // ------------------------------------------------------------------
    // E2 · POST /api/autenticacion/ingresar — Identificar al usuario · Rol: público
    // ------------------------------------------------------------------
    [HttpPost("ingresar")]
    public async Task<IActionResult> Ingresar([FromBody] AutenticacionIngresar peticion)
    {
        try
        {
            // Comprueba correo y contraseña. Busca primero en "usuario" y luego en "administrador".
            // Lanza NoAutorizadoExcepcion (401) si el correo no existe, la contraseña no coincide o el
            // usuario está inactivo. El mensaje es el MISMO en los tres casos para no revelar qué correos existen.
            var identidad = await _servicioAutenticacion.IngresarAsync(peticion.Correo!, peticion.Contrasena!);

            // Convierte la identidad en un token JWT firmado.
            var token = CrearToken(identidad);

            // Responde con los campos del contrato (id, nombre, correo, rol) MÁS el token.
            return Ok(new RespuestaIngreso(identidad.Id, identidad.Nombre, identidad.Correo, identidad.Rol, token));
        }
        catch (NoAutorizadoExcepcion ex)
        {
            // 401: no se pudo identificar a la persona. El contrato E2 dice "401 / 404 según A1"; se usa 401.
            return StatusCode(401, new RespuestaError(401, "No autorizado.", ex.Message));
        }
        catch (ArgumentException ex)
        {
            return StatusCode(400, new RespuestaError(400, "Parámetros inválidos.", ex.Message));
        }
        catch (Exception ex)
        {
            // Aquí también cae una configuración del token incompleta (por ejemplo, sin la clave): ver CrearToken.
            return StatusCode(500, new RespuestaError(500, "Error interno.", ex.Message));
        }
    }

    // Construye y firma el token JWT de una persona identificada.
    // "private": solo se usa dentro de este controlador (no es un endpoint).
    private string CrearToken(IdentidadAutenticada identidad)
    {
        // Lee los valores de configuración. Los nombres son "Jwt:Clave", "Jwt:Emisor", etc.
        // En una variable de entorno se escriben con doble guion bajo: Jwt__Clave.
        // La CLAVE no se versiona (misma regla que la cadena de conexión): llega por variable de entorno.
        var clave = _configuracion["Jwt:Clave"];
        var emisor = _configuracion["Jwt:Emisor"];
        var audiencia = _configuracion["Jwt:Audiencia"];

        // Si falta algo, no se puede firmar: se lanza un error claro que sale como 500.
        if (string.IsNullOrWhiteSpace(clave) || string.IsNullOrWhiteSpace(emisor) || string.IsNullOrWhiteSpace(audiencia))
            throw new InvalidOperationException("Falta la configuración Jwt:Clave, Jwt:Emisor o Jwt:Audiencia.");

        // Minutos de vida del token. TryParse intenta convertir el texto a número;
        // si no hay valor o no es un número, se usan 120 minutos (2 horas).
        var minutos = int.TryParse(_configuracion["Jwt:MinutosExpiracion"], out var m) && m > 0 ? m : 120;

        // La clave de firma. HMAC-SHA256 exige una clave de al menos 32 bytes (32 caracteres ASCII);
        // si es más corta, la librería lanza una excepción que sale como 500.
        var claveFirma = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave));

        // Los datos que el token afirma sobre la persona. Se usan nombres CORTOS y propios ("rol", "correo")
        // para que ningún mapeo automático los renombre; Program.cs le dice al framework que el rol está en "rol".
        //   sub    = id de la persona (estándar JWT para el "sujeto").
        //   rol    = ESTUDIANTE, DOCENTE, COORDINADOR o ADMINISTRADOR (lo que leen los [Authorize(Roles = ...)]).
        var descriptor = new SecurityTokenDescriptor
        {
            // Subject: la identidad con sus claims.
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("sub", identidad.Id.ToString()),
                new Claim("nombre", identidad.Nombre),
                new Claim("correo", identidad.Correo),
                new Claim("rol", identidad.Rol)
            }),
            Issuer = emisor,                                     // Quién emite el token (esta API).
            Audience = audiencia,                                // Para quién es el token (esta API también).
            Expires = DateTime.UtcNow.AddMinutes(minutos),       // Cuándo deja de servir.
            // Con qué se firma: así la API detecta después si alguien alteró el token.
            SigningCredentials = new SigningCredentials(claveFirma, SecurityAlgorithms.HmacSha256)
        };

        // Crea el token y lo devuelve como texto ("xxxxx.yyyyy.zzzzz").
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}