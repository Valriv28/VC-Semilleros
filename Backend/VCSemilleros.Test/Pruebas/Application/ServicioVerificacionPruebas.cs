using System.Text.Json.Nodes;                   // JsonObject: para quitarle un campo a una credencial y simular una estructura incompleta.
using VCSemilleros.Application.Servicios;       // ServicioVerificacion (el que se prueba), Veredicto, ResultadoControl.
using VCSemilleros.Domain.Modelos;              // EstadoCredencial.
using Xunit;                                    // xUnit: [Fact] y Assert.

namespace VCSemilleros.Tests.Pruebas.Application;

// Pruebas de ServicioVerificacion (E26; RF14) con repositorios y cliente de Stellar FALSOS.
// Cubre: VERIFICADA, RECHAZADA (firma alterada, revocada, suspendida, otra clave, emisor inactivo...),
// INDETERMINADA (credencial desconocida o de estado INDETERMINADA) y que la verificación NO escribe nada.
// Un veredicto negativo NUNCA lanza excepción: la verificación se ejecutó y el resultado es normal.
public class ServicioVerificacionPruebas
{
    // xUnit crea una instancia nueva por prueba: cada una arranca con su propio escenario limpio.
    private readonly EscenarioFase5 _e = new();

    // Método auxiliar: emite la credencial de la evaluación 1 y devuelve (código, texto "vc").
    private async Task<(string Codigo, string Vc)> EmitirAsync()
    {
        var resultado = await _e.Emision.EmitirAsync(1);
        return (resultado.Codigo, resultado.Vc);
    }

    // Método auxiliar: busca un control por su nombre y devuelve su resultado.
    // Single falla si no hay exactamente uno: así también se comprueba que cada control aparece una vez.
    private static ResultadoControl Control(ResultadoVerificacion r, string nombre)
        => r.Controles.Single(c => c.Control == nombre).Resultado;

    // ---------- VERIFICADA ----------

    [Fact]
    public async Task Verificar_UnaCredencialRecienEmitida_EsVerificadaConLosCuatroControlesEnPaso()
    {
        var (codigo, vc) = await EmitirAsync();

        var r = await _e.Verificacion.VerificarAsync(vc);

        Assert.Equal(Veredicto.VERIFICADA, r.Veredicto);
        Assert.Equal(codigo, r.Codigo);

        // Cuatro controles, en este orden, todos en PASO (criterio 9).
        Assert.Equal(new[] { "ESTRUCTURA", "PRUEBA_CRIPTOGRAFICA", "EMISOR", "ESTADO" },
                     r.Controles.Select(c => c.Control).ToArray());
        Assert.All(r.Controles, c => Assert.Equal(ResultadoControl.PASO, c.Resultado));
    }

    // ---------- RECHAZADA ----------

    [Fact]
    public async Task Verificar_ConUnCaracterAlterado_EsRechazadaPorLaPruebaCriptografica()
    {
        var (_, vc) = await EmitirAsync();

        // Igual que el smoke test: se cambia "Ana" por "Ona" en el contenido firmado.
        var alterado = vc.Replace("Ana", "Ona");
        Assert.NotEqual(vc, alterado);   // Garantiza que la alteración sí ocurrió.

        var r = await _e.Verificacion.VerificarAsync(alterado);

        Assert.Equal(Veredicto.RECHAZADA, r.Veredicto);
        Assert.Equal(ResultadoControl.PASO, Control(r, "ESTRUCTURA"));            // Sigue siendo una credencial completa...
        Assert.Equal(ResultadoControl.FALLO, Control(r, "PRUEBA_CRIPTOGRAFICA")); // ...pero la firma ya no coincide.
    }

    [Fact]
    public async Task Verificar_UnaCredencialRevocada_EsRechazadaPorElEstado()
    {
        var (codigo, vc) = await EmitirAsync();
        await _e.Consulta.RevocarAsync(codigo, 2, "Error en el registro de asistencia");

        // Se presenta la credencial ORIGINAL, que técnicamente sigue íntegra.
        var r = await _e.Verificacion.VerificarAsync(vc);

        Assert.Equal(Veredicto.RECHAZADA, r.Veredicto);
        Assert.Equal(ResultadoControl.PASO, Control(r, "PRUEBA_CRIPTOGRAFICA"));  // La firma sigue siendo válida.
        Assert.Equal(ResultadoControl.FALLO, Control(r, "ESTADO"));               // Pero está REVOCADA (criterio 10).
    }

    [Fact]
    public async Task Verificar_UnaCredencialSuspendida_EsRechazadaPorElEstado()
    {
        var (codigo, vc) = await EmitirAsync();

        // No hay endpoint para suspender (es Should Have), pero el control sí debe tratarla como FALLO.
        _e.Credenciales.Credenciales.First(c => c.Codigo == codigo).Estado = EstadoCredencial.SUSPENDIDA;

        var r = await _e.Verificacion.VerificarAsync(vc);

        Assert.Equal(Veredicto.RECHAZADA, r.Veredicto);
        Assert.Equal(ResultadoControl.FALLO, Control(r, "ESTADO"));
    }

    [Fact]
    public async Task Verificar_ConEstructuraIncompleta_EsRechazadaYLaFirmaQuedaSinEvaluar()
    {
        var (_, vc) = await EmitirAsync();

        // Se le quita el emisor a una credencial válida: queda estructuralmente incompleta.
        var credencial = _e.Verificable.Leer(vc)!;
        credencial.Remove("issuer");
        var incompleta = _e.Verificable.Serializar(credencial);

        var r = await _e.Verificacion.VerificarAsync(incompleta);

        Assert.Equal(Veredicto.RECHAZADA, r.Veredicto);
        Assert.Equal(ResultadoControl.FALLO, Control(r, "ESTRUCTURA"));
        // Si la estructura falla, la prueba criptográfica queda NO_EVALUADO (así lo fija el contrato).
        Assert.Equal(ResultadoControl.NO_EVALUADO, Control(r, "PRUEBA_CRIPTOGRAFICA"));
    }

    [Fact]
    public async Task Verificar_UnaCredencialFirmadaConOtraClave_EsRechazadaPorLaPrueba()
    {
        // Otro "sistema" con SU PROPIA clave arma y firma una credencial perfectamente formada.
        var otro = new ServicioCredencialVerificable(
            _e.Participantes, _e.Periodos, _e.Semilleros, _e.Usuarios, EscenarioFase5.NuevaClavePem());

        var evaluacion = _e.Evaluaciones.Evaluaciones.First(x => x.Id == 1);
        var datos = await otro.CargarDatosAsync(evaluacion, "CRED-2026-AAAAAAAA", new DateTime(2026, 10, 18, 10, 0, 0));
        var vcAjeno = otro.Serializar(otro.Firmar(otro.Construir(datos)));

        var r = await _e.Verificacion.VerificarAsync(vcAjeno);

        // La estructura es correcta, pero la firma no la emitió ESTE sistema.
        Assert.Equal(Veredicto.RECHAZADA, r.Veredicto);
        Assert.Equal(ResultadoControl.PASO, Control(r, "ESTRUCTURA"));
        Assert.Equal(ResultadoControl.FALLO, Control(r, "PRUEBA_CRIPTOGRAFICA"));
    }

    [Fact]
    public async Task Verificar_ConElCoordinadorYaInactivo_EsRechazadaPorElEmisor()
    {
        var (_, vc) = await EmitirAsync();

        // P9: solo se conoce el estado ACTUAL del coordinador que declaró la elegibilidad.
        _e.Usuarios.Usuarios.First(u => u.Id == 2).Activo = false;

        var r = await _e.Verificacion.VerificarAsync(vc);

        Assert.Equal(Veredicto.RECHAZADA, r.Veredicto);
        Assert.Equal(ResultadoControl.PASO, Control(r, "PRUEBA_CRIPTOGRAFICA"));
        Assert.Equal(ResultadoControl.FALLO, Control(r, "EMISOR"));
    }

    [Fact]
    public async Task Verificar_ConUnEmisorDistintoAlRegistrado_EsRechazadaPorElEmisor()
    {
        var (codigo, _) = await EmitirAsync();
        var evaluacion = _e.Evaluaciones.Evaluaciones.First(x => x.Id == 1);
        var credencial = _e.Credenciales.Credenciales.First(c => c.Codigo == codigo);

        // Se reconstruye la credencial real pero declarando como emisor al usuario 1 (un docente),
        // y se firma con la clave VÁLIDA: así la firma es correcta y lo único que falla es el emisor.
        var datos = await _e.Verificable.CargarDatosAsync(evaluacion, codigo, credencial.FechaEmision);
        var conOtroEmisor = datos with { EmisorId = 1, EmisorNombre = "Carlos Ramírez" };
        var vc = _e.Verificable.Serializar(_e.Verificable.Firmar(_e.Verificable.Construir(conOtroEmisor)));

        var r = await _e.Verificacion.VerificarAsync(vc);

        Assert.Equal(Veredicto.RECHAZADA, r.Veredicto);
        Assert.Equal(ResultadoControl.PASO, Control(r, "PRUEBA_CRIPTOGRAFICA"));
        Assert.Equal(ResultadoControl.FALLO, Control(r, "EMISOR"));
    }

    // ---------- INDETERMINADA ----------

    [Fact]
    public async Task Verificar_UnaCredencialNoRegistrada_EsIndeterminada()
    {
        var (_, vc) = await EmitirAsync();

        // Se borra el registro: la credencial es auténtica (firma válida) pero este sistema ya no la conoce.
        _e.Credenciales.Credenciales.Clear();

        var r = await _e.Verificacion.VerificarAsync(vc);

        // Ningún control falla, pero dos no se pudieron evaluar: INDETERMINADA, no RECHAZADA.
        Assert.Equal(Veredicto.INDETERMINADA, r.Veredicto);
        Assert.Equal(ResultadoControl.PASO, Control(r, "ESTRUCTURA"));
        Assert.Equal(ResultadoControl.PASO, Control(r, "PRUEBA_CRIPTOGRAFICA"));
        Assert.Equal(ResultadoControl.NO_EVALUADO, Control(r, "EMISOR"));
        Assert.Equal(ResultadoControl.NO_EVALUADO, Control(r, "ESTADO"));
    }

    [Fact]
    public async Task Verificar_UnaCredencialEnEstadoIndeterminado_EsIndeterminada()
    {
        var (codigo, vc) = await EmitirAsync();
        _e.Credenciales.Credenciales.First(c => c.Codigo == codigo).Estado = EstadoCredencial.INDETERMINADA;

        var r = await _e.Verificacion.VerificarAsync(vc);

        Assert.Equal(Veredicto.INDETERMINADA, r.Veredicto);
        Assert.Equal(ResultadoControl.NO_EVALUADO, Control(r, "ESTADO"));
    }

    // ---------- Textos que ni siquiera son una credencial ----------

    [Fact]
    public async Task Verificar_ConTextoQueNoEsJson_EsRechazadaSinLanzarExcepcion()
    {
        var r = await _e.Verificacion.VerificarAsync("esto no es una credencial");

        Assert.Equal(Veredicto.RECHAZADA, r.Veredicto);
        Assert.Null(r.Codigo);                                                   // No hay código que leer.
        Assert.Equal(ResultadoControl.FALLO, Control(r, "ESTRUCTURA"));
        Assert.Equal(ResultadoControl.NO_EVALUADO, Control(r, "PRUEBA_CRIPTOGRAFICA"));
        Assert.Equal(ResultadoControl.NO_EVALUADO, Control(r, "EMISOR"));
        Assert.Equal(ResultadoControl.NO_EVALUADO, Control(r, "ESTADO"));
    }

    [Fact]
    public async Task Verificar_ConTextoVacioOJsonQueNoEsObjeto_EsRechazada()
    {
        // La petición ya rechaza un vc vacío con 422, pero el servicio no debe depender de eso.
        var vacio = await _e.Verificacion.VerificarAsync("");
        var arreglo = await _e.Verificacion.VerificarAsync("[]");   // JSON válido, pero no es un objeto.

        Assert.Equal(Veredicto.RECHAZADA, vacio.Veredicto);
        Assert.Equal(Veredicto.RECHAZADA, arreglo.Veredicto);
    }

    // ---------- La verificación NO escribe ----------

    [Fact]
    public async Task Verificar_NoEscribeEnLaBDNiEnLaRed()
    {
        var (codigo, vc) = await EmitirAsync();

        // Foto del estado ANTES de verificar.
        var credenciales = _e.Credenciales.Credenciales.Count;
        var anclajes = _e.Anclajes.Anclajes.Count;
        var anclados = _e.Stellar.Anclados.Count;
        var revocados = _e.Stellar.Revocados.Count;

        // Se verifica una credencial buena y otra alterada.
        await _e.Verificacion.VerificarAsync(vc);
        await _e.Verificacion.VerificarAsync(vc.Replace("Ana", "Ona"));

        // Nada cambió: ni filas nuevas, ni llamadas a la red, ni el estado de la credencial.
        Assert.Equal(credenciales, _e.Credenciales.Credenciales.Count);
        Assert.Equal(anclajes, _e.Anclajes.Anclajes.Count);
        Assert.Equal(anclados, _e.Stellar.Anclados.Count);
        Assert.Equal(revocados, _e.Stellar.Revocados.Count);
        Assert.Equal(EstadoCredencial.VIGENTE, _e.Credenciales.Credenciales.First(c => c.Codigo == codigo).Estado);
    }
}
