using System.Text.Json.Nodes;                    // JsonObject y JsonArray: para alterar la credencial en las pruebas.
using VCSemilleros.Application.Servicios;        // El servicio que se prueba.
using VCSemilleros.Domain.Modelos;               // Evaluacion.
using Xunit;                                     // xUnit: [Fact] y Assert.

namespace VCSemilleros.Tests.Pruebas.Application;

// Pruebas de ServicioCredencialVerificable (construir, validar, firmar, serializar y verificar la prueba).
// Usa el EscenarioFase5: repositorios falsos y una clave ECDSA P-256 generada para cada prueba.
public class ServicioCredencialVerificablePruebas
{
    private const string Codigo = "CRED-2026-AAAA0001";

    // Fecha fija de emisión: el contenido firmado la incluye, así que debe ser la misma en todas las pruebas.
    private static readonly DateTime Fecha = new(2026, 10, 17, 21, 35, 0);

    private readonly EscenarioFase5 _escenario = new();

    // Método auxiliar: arma el contenido (sin prueba) de la evaluación 1, que cumple y tiene elegibilidad declarada.
    private async Task<JsonObject> ContenidoDeLaEvaluacion1Async()
    {
        var evaluacion = _escenario.Evaluaciones.Evaluaciones.First(e => e.Id == 1);
        var datos = await _escenario.Verificable.CargarDatosAsync(evaluacion, Codigo, Fecha);
        return _escenario.Verificable.Construir(datos);
    }

    // ---------- Reunir los datos ----------

    [Fact]
    public async Task CargarDatos_ConEvaluacionElegible_TraeLosDatosAutorizados()
    {
        var evaluacion = _escenario.Evaluaciones.Evaluaciones.First(e => e.Id == 1);

        var datos = await _escenario.Verificable.CargarDatosAsync(evaluacion, Codigo, Fecha);

        Assert.Equal(2, datos.EmisorId);                          // El coordinador que declaró la elegibilidad.
        Assert.Equal("Ana María Torres Vega", datos.TitularNombre);
        Assert.Equal("2026-2", datos.PeriodoNombre);
        Assert.Equal(100m, datos.PorcentajeAsistencia);
    }

    [Fact]
    public async Task CargarDatos_SinElegibilidadDeclarada_Lanza400()
    {
        // La evaluación 2 no tiene elegibilidad declarada.
        var evaluacion = _escenario.Evaluaciones.Evaluaciones.First(e => e.Id == 2);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _escenario.Verificable.CargarDatosAsync(evaluacion, Codigo, Fecha));
    }

    // ---------- Estructura ----------

    [Fact]
    public async Task Construir_ConDatosCompletos_CumpleElPerfil()
    {
        var contenido = await ContenidoDeLaEvaluacion1Async();

        // Una lista vacía de campos faltantes significa estructura completa.
        Assert.Empty(_escenario.Verificable.ValidarEstructura(contenido));
    }

    [Fact]
    public async Task ValidarEstructura_SinElNombreDelTitular_ReportaElCampoFaltante()
    {
        var contenido = await ContenidoDeLaEvaluacion1Async();
        ((JsonObject)contenido["credentialSubject"]!).Remove("name");

        var faltantes = _escenario.Verificable.ValidarEstructura(contenido);

        Assert.Contains("credentialSubject.name", faltantes);
    }

    [Fact]
    public async Task ValidarEstructura_ConPerfilIncorrecto_ReportaElTipo()
    {
        var contenido = await ContenidoDeLaEvaluacion1Async();
        contenido["type"] = new JsonArray("VerifiableCredential", "OtroTipo");

        Assert.Contains("type", _escenario.Verificable.ValidarEstructura(contenido));
    }

    // ---------- Firma y verificación de la prueba ----------

    [Fact]
    public async Task Firmar_AgregaLaPruebaYSuFirmaEsValida()
    {
        var contenido = await ContenidoDeLaEvaluacion1Async();

        var firmada = _escenario.Verificable.Firmar(contenido);

        Assert.NotNull(firmada["proof"]);
        Assert.True(_escenario.Verificable.VerificarPrueba(firmada));
        // Firmar no modifica el objeto original (trabaja sobre una copia).
        Assert.Null(contenido["proof"]);
    }

    [Fact]
    public async Task VerificarPrueba_ConContenidoAlterado_Falla()
    {
        var firmada = _escenario.Verificable.Firmar(await ContenidoDeLaEvaluacion1Async());
        var vc = _escenario.Verificable.Serializar(firmada);

        // Se cambia una letra del nombre del titular, como en el smoke test (paso 9).
        var alterado = vc.Replace("Ana", "Ona");
        Assert.NotEqual(vc, alterado);

        var leido = _escenario.Verificable.Leer(alterado);

        Assert.NotNull(leido);
        Assert.False(_escenario.Verificable.VerificarPrueba(leido!));
    }

    [Fact]
    public async Task VerificarPrueba_ConLaClaveDeOtroEmisor_Falla()
    {
        var firmada = _escenario.Verificable.Firmar(await ContenidoDeLaEvaluacion1Async());

        // Otro servicio con OTRA clave privada: no puede validar una firma que no es suya.
        var otro = new ServicioCredencialVerificable(
            _escenario.Participantes, _escenario.Periodos, _escenario.Semilleros, _escenario.Usuarios,
            EscenarioFase5.NuevaClavePem());

        Assert.False(otro.VerificarPrueba(firmada));
    }

    [Fact]
    public async Task VerificarPrueba_SinPrueba_Falla()
    {
        var contenido = await ContenidoDeLaEvaluacion1Async();

        Assert.False(_escenario.Verificable.VerificarPrueba(contenido));
    }

    // ---------- Hash ----------

    [Fact]
    public async Task CalcularHash_EsIgualConYSinLaPrueba()
    {
        var contenido = await ContenidoDeLaEvaluacion1Async();
        var firmada = _escenario.Verificable.Firmar(contenido);

        Assert.Equal(_escenario.Verificable.CalcularHash(contenido), _escenario.Verificable.CalcularHash(firmada));
    }

    [Fact]
    public async Task CalcularHash_EsDeterminista_YTiene64CaracteresHexadecimales()
    {
        var primero = _escenario.Verificable.CalcularHash(await ContenidoDeLaEvaluacion1Async());
        var segundo = _escenario.Verificable.CalcularHash(await ContenidoDeLaEvaluacion1Async());

        Assert.Equal(primero, segundo);
        Assert.Equal(64, primero.Length);
        Assert.Matches("^[0-9a-f]{64}$", primero);
    }

    [Fact]
    public async Task Firmar_DosVeces_DaFirmasDistintasPeroElMismoHash()
    {
        var contenido = await ContenidoDeLaEvaluacion1Async();

        var primera = _escenario.Verificable.Firmar(contenido);
        var segunda = _escenario.Verificable.Firmar(contenido);

        // ECDSA no es determinista: las firmas difieren, pero ambas son válidas y el contenido hasheado es el mismo.
        Assert.NotEqual((string?)primera["proof"]!["proofValue"], (string?)segunda["proof"]!["proofValue"]);
        Assert.True(_escenario.Verificable.VerificarPrueba(primera));
        Assert.True(_escenario.Verificable.VerificarPrueba(segunda));
        Assert.Equal(_escenario.Verificable.CalcularHash(primera), _escenario.Verificable.CalcularHash(segunda));
    }

    // ---------- Serializar y leer ----------

    [Fact]
    public async Task SerializarYLeer_ConservanLaFirmaYElHash()
    {
        var firmada = _escenario.Verificable.Firmar(await ContenidoDeLaEvaluacion1Async());

        var vc = _escenario.Verificable.Serializar(firmada);
        var leido = _escenario.Verificable.Leer(vc);

        Assert.NotNull(leido);
        Assert.True(_escenario.Verificable.VerificarPrueba(leido!));
        Assert.Equal(_escenario.Verificable.CalcularHash(firmada), _escenario.Verificable.CalcularHash(leido!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("esto no es json")]
    [InlineData("[1, 2, 3]")]
    public void Leer_ConTextoQueNoEsUnObjetoJson_DevuelveNull(string texto)
    {
        Assert.Null(_escenario.Verificable.Leer(texto));
    }

    // ---------- Auxiliares de lectura ----------

    [Fact]
    public async Task ObtenerCodigoYEmisor_LeenLosIdentificadores()
    {
        var contenido = await ContenidoDeLaEvaluacion1Async();

        Assert.Equal(Codigo, _escenario.Verificable.ObtenerCodigo(contenido));
        Assert.Equal(_escenario.Verificable.IdEmisor(2), _escenario.Verificable.ObtenerIdEmisor(contenido));
    }

    // ---------- Configuración ----------

    [Fact]
    public void Constructor_SinClavePrivada_FallaConUnMensajeClaro()
    {
        Assert.Throws<InvalidOperationException>(() => new ServicioCredencialVerificable(
            _escenario.Participantes, _escenario.Periodos, _escenario.Semilleros, _escenario.Usuarios, ""));
    }
}