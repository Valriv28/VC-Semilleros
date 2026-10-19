using Front.Anclaje;
using Front.Components;
using Front.Servicios;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Se conserva la integración existente de anclaje.
builder.Services.AddAnclajeAttest(builder.Configuration);

// Dirección de la API.
var apiBaseUrl = builder.Configuration["Api:BaseUrl"]
    ?? throw new InvalidOperationException(
        "Falta Api:BaseUrl en appsettings.json.");

// Servicio de autenticación.
builder.Services.AddHttpClient<AutenticacionService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Sesión independiente por circuito interactivo de Blazor.
builder.Services.AddScoped<SesionUsuario>();

// Cliente HTTP central para los demás servicios.
builder.Services.AddHttpClient<ApiHttpService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Servicios de acceso a la API.
builder.Services.AddScoped<SemillerosService>();
builder.Services.AddScoped<PeriodosService>();
builder.Services.AddScoped<ParticipantesService>();
builder.Services.AddScoped<SesionesService>();
builder.Services.AddScoped<EvaluacionesService>();
builder.Services.AddScoped<CredencialesService>();
builder.Services.AddScoped<VerificacionesService>();
builder.Services.AddScoped<AuditoriaService>();
builder.Services.AddScoped<AdministracionService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(
        "/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute(
    "/not-found", createScopeForStatusCodePages: true);

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
