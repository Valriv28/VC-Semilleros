// Importa Encoding: convierte la clave de texto en bytes para validar la firma del token JWT.
using System.Text;
// Importa JsonStringEnumConverter: hace que los enums salgan como texto ("VIGENTE") y no como número.
using System.Text.Json.Serialization;
// Importa Dapper: aquí solo se usa DefaultTypeMap, para mapear columnas snake_case a propiedades PascalCase.
using Dapper;
// Importa JwtBearerDefaults y JwtBearerEvents: validan el token que viaja en "Authorization: Bearer ...".
using Microsoft.AspNetCore.Authentication.JwtBearer;
// Importa ObjectResult y ModelStateDictionary: se usan para personalizar la respuesta 422.
using Microsoft.AspNetCore.Mvc;
// Importa SymmetricSecurityKey y TokenValidationParameters: las reglas con que se valida el token.
using Microsoft.IdentityModel.Tokens;
// Importa los tipos de Swagger para describir el botón "Authorize" (el token en la página /swagger).
using Microsoft.OpenApi.Models;
// Importa los sobres de respuesta (RespuestaError) para el 422, el 401 y el 403.
using VCSemilleros.Api.Peticiones;
// Importa los servicios (casos de uso) y sus interfaces.
using VCSemilleros.Application.Servicios;
// Importa los puertos del dominio: las interfaces IRepositorio… e IClienteStellar.
using VCSemilleros.Domain.Repositorios;
// Importa las clases CONCRETAS de PostgreSQL. Solo este archivo (el ensamblador) las conoce (Art. 4).
using VCSemilleros.Infrastructure.Repositorios;
// Importa el adaptador de Stellar. Hoy es el PROVISIONAL; cuando exista ClienteStellar, se cambia aquí.
using VCSemilleros.Infrastructure.Stellar.Adaptadores;

// ============================================================================
// PROGRAM.CS — EL ENSAMBLADOR
// Aquí se "enchufa" todo: qué clase concreta responde a cada interfaz (inyección de dependencias),
// cómo se valida el token, cómo se responde un 422, y qué rutas existen. Es el ÚNICO lugar que
// conoce clases concretas (Art. 4). Los demás archivos solo ven interfaces.
// ============================================================================

// Crea el "constructor" de la aplicación web; "args" son los argumentos de la línea de comandos.
// Al crearlo, .NET ya lee appsettings.json y las variables de entorno (estas últimas ganan).
var builder = WebApplication.CreateBuilder(args);

var rutaClavePrivada = Path.GetFullPath(
    Path.Combine(
        builder.Environment.ContentRootPath,
        "..",
        "GeneradorClaves",
        "credencial-privada.pem"
    )
);

if (!File.Exists(rutaClavePrivada))
{
    throw new FileNotFoundException(
        "No se encontró el archivo de la clave privada.",
        rutaClavePrivada
    );
}

builder.Configuration["Credenciales:ClavePrivada"] =
    File.ReadAllText(rutaClavePrivada);
// ----------------------------------------------------------------------------
// 1) CONFIGURACIÓN OBLIGATORIA — si falta algo, la app NO arranca y dice qué falta
// ----------------------------------------------------------------------------

// Mapeo de columnas: Dapper convierte "responsable_id" (BD) en "ResponsableId" (C#) automáticamente.
// Sin esta línea, las propiedades con guion bajo en la BD llegarían vacías (plan §4.4).
DefaultTypeMap.MatchNamesWithUnderscores = true;

// Cadena de conexión a PostgreSQL. Viene de appsettings.json (sin clave) y la variable de entorno
// ConnectionStrings__PostgreSql la reemplaza completa, con la clave. La clave NO se versiona (Art. 9).
// "??" significa: si lo de la izquierda es null, ejecuta lo de la derecha (aquí, lanzar un error claro).
var cadenaConexion = builder.Configuration.GetConnectionString("PostgreSql")
    ?? throw new InvalidOperationException("Falta la cadena de conexión: configure ConnectionStrings__PostgreSql.");

// Clave con que se FIRMAN y VALIDAN los tokens JWT. Llega por variable de entorno (Jwt__Clave).
var claveJwt = builder.Configuration["Jwt:Clave"];
// HMAC-SHA256 exige una clave de al menos 32 bytes; se comprueba aquí para fallar con un mensaje claro.
if (string.IsNullOrWhiteSpace(claveJwt) || Encoding.UTF8.GetByteCount(claveJwt) < 32)
    throw new InvalidOperationException("Falta la clave de los tokens: configure Jwt__Clave con al menos 32 caracteres.");

// Valores del token que NO son secretos y viven en appsettings.json. Son los mismos que usa
// AutenticacionController al firmar: si difieren, todos los tokens se rechazarían.
var emisorJwt = builder.Configuration["Jwt:Emisor"]
    ?? throw new InvalidOperationException("Falta la configuración Jwt:Emisor.");
var audienciaJwt = builder.Configuration["Jwt:Audiencia"]
    ?? throw new InvalidOperationException("Falta la configuración Jwt:Audiencia.");

// Clave privada ECDSA P-256 del emisor de credenciales (formato PEM). Llega por variable de entorno
// (Credenciales__ClavePrivada) y NUNCA va en el repositorio (A3).
var clavePrivadaPem = builder.Configuration["Credenciales:ClavePrivada"];
if (string.IsNullOrWhiteSpace(clavePrivadaPem))
    throw new InvalidOperationException("Falta la clave de firma: configure Credenciales__ClavePrivada (PEM).");
// Comodidad: si el PEM se pegó en una sola línea con "\n" escritos como texto, se convierten en saltos reales.
clavePrivadaPem = clavePrivadaPem.Replace("\\n", "\n");

// ----------------------------------------------------------------------------
// 2) CONTROLADORES, JSON Y EL 422 PERSONALIZADO
// ----------------------------------------------------------------------------

builder.Services
    // Registra los controladores (las clases con [ApiController] de la carpeta Controllers).
    .AddControllers()
    // JSON: los nombres salen en camelCase por defecto (Art. 9); aquí se agrega que los enums
    // (EstadoCredencial, Veredicto...) salgan como TEXTO: "VIGENTE" y no 0.
    .AddJsonOptions(opciones => opciones.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    // Personaliza qué responde el framework cuando el body NO cumple la petición (plan §4.2).
    .ConfigureApiBehaviorOptions(opciones =>
    {
        // Esta función se ejecuta ANTES del método del controlador cuando la validación falla.
        opciones.InvalidModelStateResponseFactory = contexto =>
        {
            // Lista de mensajes en español que irá en "errores".
            var errores = new List<string>();

            // Recorre cada campo con problemas (la "clave" es el nombre del campo).
            foreach (var entrada in contexto.ModelState)
            {
                // Un campo sin entrada de valor no tiene errores que contar.
                if (entrada.Value is null) continue;

                // Recorre cada error de ese campo.
                foreach (var error in entrada.Value.Errors)
                {
                    string mensaje;

                    // Caso 1: el body no se pudo ni leer (no es JSON, o viene vacío).
                    if (error.ErrorMessage.Contains("non-empty request body", StringComparison.OrdinalIgnoreCase)
                        || entrada.Key == "$" || string.IsNullOrWhiteSpace(entrada.Key))
                    {
                        mensaje = "El cuerpo de la petición es obligatorio y debe ser un JSON válido.";
                    }
                    // Caso 2: el valor tiene el tipo equivocado (por ejemplo "texto" donde va un entero,
                    // o 7.5 donde va un entero). El framework avisa con una excepción y un mensaje en inglés.
                    else if (error.Exception is not null || string.IsNullOrWhiteSpace(error.ErrorMessage)
                             || error.ErrorMessage.StartsWith("The JSON value", StringComparison.Ordinal))
                    {
                        // "$.responsableId" → "responsableId".
                        var campo = entrada.Key.Replace("$.", "");
                        mensaje = $"El campo {campo} tiene un tipo o un formato inválido.";
                    }
                    // Caso 3: falló una anotación de la petición ([Required], [Range]...): su mensaje ya está en español.
                    else
                    {
                        mensaje = error.ErrorMessage;
                    }

                    // Evita mensajes repetidos.
                    if (!errores.Contains(mensaje)) errores.Add(mensaje);
                }
            }

            // Responde 422 con el sobre { estado, mensaje, errores:[…] } (contratos §0).
            // "Errores:" nombra el parámetro para saltar "Detalle", que el 422 no lleva.
            return new ObjectResult(new RespuestaError(422, "Datos inválidos.", Errores: errores))
            {
                StatusCode = 422
            };
        };
    });

// ----------------------------------------------------------------------------
// 3) AUTENTICACIÓN (JWT) Y PERMISOS POR ROL
// ----------------------------------------------------------------------------

builder.Services
    // Declara que la forma de identificar a quien llama es el token JWT en la cabecera Authorization.
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opciones =>
    {
        // No renombrar los claims del token: se conservan "sub", "rol" y "correo" tal como se firmaron.
        opciones.MapInboundClaims = false;

        // Reglas con que se acepta un token.
        opciones.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,                 // El emisor debe ser esta API.
            ValidIssuer = emisorJwt,
            ValidateAudience = true,               // La audiencia debe ser la configurada.
            ValidAudience = audienciaJwt,
            ValidateLifetime = true,               // Un token vencido se rechaza.
            ValidateIssuerSigningKey = true,       // La firma debe ser válida: así se detecta un token alterado.
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(claveJwt)),
            // Le dice al framework dónde está el rol: en el claim "rol" que escribe AutenticacionController.
            // Sin esto, TODOS los [Authorize(Roles = ...)] responderían 403.
            RoleClaimType = "rol",
            NameClaimType = "nombre",
            // Tolerancia de reloj: 1 minuto (el valor por defecto sería 5).
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        // Por defecto, un 401 y un 403 salen SIN cuerpo. Aquí se responden con el sobre de errores del Art. 9.
        opciones.Events = new JwtBearerEvents
        {
            // 401: no hay token, está vencido o es inválido.
            OnChallenge = async contexto =>
            {
                // HandleResponse() le dice al framework: "la respuesta la escribo yo".
                contexto.HandleResponse();
                contexto.Response.StatusCode = 401;
                await contexto.Response.WriteAsJsonAsync(new RespuestaError(
                    401, "No autorizado.", "Falta el token o no es válido. Ingrese por POST /api/autenticacion/ingresar."));
            },
            // 403: hay token válido, pero el rol no puede usar este endpoint.
            OnForbidden = async contexto =>
            {
                contexto.Response.StatusCode = 403;
                await contexto.Response.WriteAsJsonAsync(new RespuestaError(
                    403, "Acceso denegado.", "Su rol no tiene permiso para esta operación."));
            }
        };
    });

// Activa el sistema de permisos que lee los [Authorize] de los controladores.
builder.Services.AddAuthorization();

// ----------------------------------------------------------------------------
// 4) SWAGGER — documentación interactiva en /swagger (con botón para pegar el token)
// ----------------------------------------------------------------------------

// Descubre los endpoints de los controladores para que Swagger los documente.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opciones =>
{
    // Título y versión que muestra la página.
    opciones.SwaggerDoc("v1", new OpenApiInfo { Title = "API VC-Semilleros", Version = "mvp" });

    // Describe el esquema de seguridad: un token "Bearer" de tipo JWT.
    opciones.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Pegue el token que devuelve POST /api/autenticacion/ingresar (sin la palabra Bearer)."
    });

    // Aplica ese esquema a todos los endpoints: Swagger enviará el token en cada prueba.
    opciones.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ----------------------------------------------------------------------------
// 5) INYECCIÓN DE DEPENDENCIAS — qué clase concreta responde a cada interfaz
// ----------------------------------------------------------------------------
// AddScoped = se crea UNA instancia por cada petición HTTP y se descarta al terminar.
// Los repositorios reciben la cadena de conexión; cada operación abre y cierra su propia conexión.

// --- Repositorios (puertos de salida hacia PostgreSQL) ---
builder.Services.AddScoped<IRepositorioAdministrador>(_ => new RepositorioAdministradorPostgreSql(cadenaConexion));
builder.Services.AddScoped<IRepositorioUsuario>(_ => new RepositorioUsuarioPostgreSql(cadenaConexion));
builder.Services.AddScoped<IRepositorioSemillero>(_ => new RepositorioSemilleroPostgreSql(cadenaConexion));
builder.Services.AddScoped<IRepositorioPeriodo>(_ => new RepositorioPeriodoPostgreSql(cadenaConexion));
builder.Services.AddScoped<IRepositorioCriterioPeriodo>(_ => new RepositorioCriterioPeriodoPostgreSql(cadenaConexion));
builder.Services.AddScoped<IRepositorioParticipante>(_ => new RepositorioParticipantePostgreSql(cadenaConexion));
builder.Services.AddScoped<IRepositorioSesion>(_ => new RepositorioSesionPostgreSql(cadenaConexion));
builder.Services.AddScoped<IRepositorioAsistencia>(_ => new RepositorioAsistenciaPostgreSql(cadenaConexion));
builder.Services.AddScoped<IRepositorioEvaluacion>(_ => new RepositorioEvaluacionPostgreSql(cadenaConexion));
builder.Services.AddScoped<IRepositorioCredencial>(_ => new RepositorioCredencialPostgreSql(cadenaConexion));
builder.Services.AddScoped<IRepositorioAnclaje>(_ => new RepositorioAnclajePostgreSql(cadenaConexion));

// --- Puerto de salida hacia la red Stellar ---
// Adaptador real: habla con el puente de Attest. La sección "Stellar" de appsettings.json trae la
// dirección del puente; la clave y las direcciones llegan por variables de entorno (Stellar__ApiKey,
// Stellar__DireccionEmisora, Stellar__SchemaUid). Si faltan, el anclaje queda FALLIDO (P4) y el Back arranca igual.
var opcionesStellar = new OpcionesStellar();
builder.Configuration.GetSection("Stellar").Bind(opcionesStellar);
builder.Services.AddSingleton(opcionesStellar);

builder.Services.AddHttpClient<IClienteStellar, ClienteStellar>((sp, http) =>
{
    var o = sp.GetRequiredService<OpcionesStellar>();
    http.BaseAddress = new Uri(o.BaseUrl);
    http.Timeout = TimeSpan.FromSeconds(o.TimeoutSegundos);
    if (!string.IsNullOrWhiteSpace(o.ApiKey))
        http.DefaultRequestHeaders.Add("X-Bridge-Key", o.ApiKey);
});

// --- Servicios (casos de uso de la capa Application) ---
// Los que solo necesitan interfaces se registran con la forma corta: <interfaz, clase>.
builder.Services.AddScoped<IServicioSemillero, ServicioSemillero>();
builder.Services.AddScoped<IServicioPeriodo, ServicioPeriodo>();
builder.Services.AddScoped<IServicioCriterioPeriodo, ServicioCriterioPeriodo>();
builder.Services.AddScoped<IServicioAsistencia, ServicioAsistencia>();
builder.Services.AddScoped<IServicioEvaluacion, ServicioEvaluacion>();
builder.Services.AddScoped<IServicioAutenticacion, ServicioAutenticacion>();

// ServicioCredencialVerificable además de interfaces recibe un TEXTO (la clave PEM), y el framework no
// sabe inventar textos: por eso se arma a mano. "sp" es el proveedor de servicios; GetRequiredService
// le pide cada interfaz ya construida.
builder.Services.AddScoped<IServicioCredencialVerificable>(sp => new ServicioCredencialVerificable(
    sp.GetRequiredService<IRepositorioParticipante>(),
    sp.GetRequiredService<IRepositorioPeriodo>(),
    sp.GetRequiredService<IRepositorioSemillero>(),
    sp.GetRequiredService<IRepositorioUsuario>(),
    clavePrivadaPem));

// Los casos de uso de credenciales dependen de las interfaces anteriores (incluida IClienteStellar).
builder.Services.AddScoped<IServicioEmision, ServicioEmision>();
builder.Services.AddScoped<IServicioCredencial, ServicioCredencial>();
builder.Services.AddScoped<IServicioVerificacion, ServicioVerificacion>();
builder.Services.AddScoped<IServicioAuditoria, ServicioAuditoria>();

// ----------------------------------------------------------------------------
// 6) CONSTRUCCIÓN Y TUBERÍA DE PETICIONES (el ORDEN importa)
// ----------------------------------------------------------------------------

// Construye la aplicación con todo lo registrado arriba.
var app = builder.Build();

// Swagger: genera el documento JSON y la página interactiva en /swagger.
app.UseSwagger();
app.UseSwaggerUI();

// 1.º identifica a quien llama (lee el token)...
app.UseAuthentication();
// 2.º ...y 3.º decide si su rol puede entrar al endpoint ([Authorize]).
app.UseAuthorization();

// Conecta las rutas de los controladores ([HttpGet], [HttpPost]...).
app.MapControllers();

// E1 · GET / — Diagnóstico (público): confirma que la API está viva (contrato E1).
app.MapGet("/", () => Results.Ok(new
{
    mensaje = "API VC-Semilleros funcionando",
    version = "mvp",
    contratos = "spec_kit/6_contracts.md"
}));

// Pone la aplicación a escuchar peticiones (escucha en el puerto de "Urls": 5158).
app.Run();