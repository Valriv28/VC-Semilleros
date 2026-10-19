using VCSemilleros.Application.Servicios;       // ServicioCredencial (el que se prueba).
using VCSemilleros.Domain.Excepciones;          // NoEncontradoExcepcion.
using VCSemilleros.Domain.Modelos;              // EstadoCredencial, EstadoAnclaje.
using Xunit;                                    // xUnit: [Fact] y Assert.

namespace VCSemilleros.Tests.Pruebas.Application;

// Pruebas de ServicioCredencial (E21 a E25; RF11, RF12, RF13) con repositorios y cliente de Stellar FALSOS.
// Cubre: consulta por titular, reconstrucción de la credencial firmada (E23) con su control de hash,
// estado, y la revocación (motivo, rol, doble revocación y red caída).
public class ServicioCredencialPruebas
{
    // xUnit crea una instancia nueva por prueba: cada una arranca con su propio escenario limpio.
    private readonly EscenarioFase5 _e = new();

    // Método auxiliar: emite la credencial de la evaluación 1 (la elegible) y devuelve su código.
    private async Task<string> EmitirAsync()
    {
        var resultado = await _e.Emision.EmitirAsync(1);
        return resultado.Codigo;
    }

    // ---------- E21: listar por titular ----------

    [Fact]
    public async Task Listar_ConTitularConCredencial_DevuelveSuCredencial()
    {
        var codigo = await EmitirAsync();

        // El titular de la evaluación 1 es el usuario 3 (Ana).
        var lista = await _e.Consulta.ListarPorTitularAsync(3, 10);

        var unica = Assert.Single(lista);
        Assert.Equal(codigo, unica.Codigo);
    }

    [Fact]
    public async Task Listar_ConTitularSinCredenciales_DevuelveListaVacia()
    {
        await EmitirAsync();

        // El usuario 4 no tiene credenciales: la lista vacía es válida (el controlador la vuelve 204).
        var lista = await _e.Consulta.ListarPorTitularAsync(4, 10);

        Assert.Empty(lista);
    }

    [Fact]
    public async Task Listar_ConLimiteCero_Lanza400()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _e.Consulta.ListarPorTitularAsync(3, 0));
    }

    // ---------- E22: obtener una ----------

    [Fact]
    public async Task Obtener_ConCodigoExistente_DevuelveLaCredencial()
    {
        var codigo = await EmitirAsync();

        var credencial = await _e.Consulta.ObtenerAsync(codigo);

        Assert.Equal(codigo, credencial.Codigo);
        Assert.Equal(1, credencial.EvaluacionId);
        Assert.Equal(EstadoCredencial.VIGENTE, credencial.Estado);
    }

    [Fact]
    public async Task Obtener_ConCodigoInexistente_Lanza404()
    {
        await Assert.ThrowsAsync<NoEncontradoExcepcion>(() => _e.Consulta.ObtenerAsync("CRED-0000"));
    }

    // ---------- E23: obtener la credencial firmada ----------

    [Fact]
    public async Task ObtenerVc_ReconstruyeUnaCredencialValidaConElMismoHash()
    {
        var codigo = await EmitirAsync();
        var credencial = await _e.Consulta.ObtenerAsync(codigo);

        var vc = await _e.Consulta.ObtenerVcAsync(codigo);

        // El texto devuelto es una credencial legible, con firma válida y el MISMO hash registrado al emitir.
        // (La firma en sí puede ser distinta de la original: ECDSA no es determinista; por eso el hash
        // se calcula sin la prueba.)
        var leida = _e.Verificable.Leer(vc);
        Assert.NotNull(leida);
        Assert.True(_e.Verificable.VerificarPrueba(leida!));
        Assert.Equal(credencial.VcHash, _e.Verificable.CalcularHash(leida!));
        Assert.Equal(codigo, _e.Verificable.ObtenerCodigo(leida!));
    }

    [Fact]
    public async Task ObtenerVc_ConDatosCambiadosDespuesDeEmitir_LanzaErrorDelServidor()
    {
        var codigo = await EmitirAsync();

        // Cambia el nombre del titular DESPUÉS de emitir: el contenido reconstruido ya no da el hash registrado.
        _e.Usuarios.Usuarios.First(u => u.Id == 3).Nombre = "Otro Nombre";

        // No es un error del usuario sino de los datos: InvalidOperationException, que la API responde como 500.
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => _e.Consulta.ObtenerVcAsync(codigo));
        Assert.Contains("no coincide", error.Message);
    }

    [Fact]
    public async Task ObtenerVc_ConCodigoInexistente_Lanza404()
    {
        await Assert.ThrowsAsync<NoEncontradoExcepcion>(() => _e.Consulta.ObtenerVcAsync("CRED-0000"));
    }

    // ---------- E24: estado ----------

    [Fact]
    public async Task ObtenerEstado_DeUnaCredencialRecienEmitida_EsVigente()
    {
        var codigo = await EmitirAsync();

        var estado = await _e.Consulta.ObtenerEstadoAsync(codigo);

        Assert.Equal(EstadoCredencial.VIGENTE, estado);
    }

    [Fact]
    public async Task ObtenerEstado_ConCodigoInexistente_Lanza404()
    {
        await Assert.ThrowsAsync<NoEncontradoExcepcion>(() => _e.Consulta.ObtenerEstadoAsync("CRED-0000"));
    }

    // ---------- E25: revocar ----------

    [Fact]
    public async Task Revocar_ConCoordinadorYMotivo_RevocaYGuardaQuienCuandoYPorQue()
    {
        var codigo = await EmitirAsync();

        // El motivo llega con espacios sobrantes: el servicio los recorta antes de guardar.
        var filas = await _e.Consulta.RevocarAsync(codigo, 2, "  Error en el registro de asistencia  ");

        Assert.Equal(1, filas);
        var credencial = await _e.Consulta.ObtenerAsync(codigo);
        Assert.Equal(EstadoCredencial.REVOCADA, credencial.Estado);
        Assert.Equal(2, credencial.RevocadaPor);
        Assert.Equal("Error en el registro de asistencia", credencial.MotivoRevocacion);
        Assert.NotNull(credencial.FechaRevocacion);
    }

    [Fact]
    public async Task Revocar_AvisaALaRedConElUidDelAnclaje()
    {
        var codigo = await EmitirAsync();
        var uid = _e.Anclajes.Anclajes[0].AttestationUid!;

        await _e.Consulta.RevocarAsync(codigo, 2, "Motivo");

        // Se le pidió UNA revocación a la red, con el código y el attestationUid del anclaje.
        var llamada = Assert.Single(_e.Stellar.Revocados);
        Assert.Equal(codigo, llamada.Codigo);
        Assert.Equal(uid, llamada.AttestationUid);

        // El falso de la red la marcó como revocada.
        Assert.True(_e.Stellar.Red[uid].Revocada);
    }

    [Fact]
    public async Task Revocar_DosVeces_Lanza400()
    {
        var codigo = await EmitirAsync();
        await _e.Consulta.RevocarAsync(codigo, 2, "Primera vez");

        // Una credencial ya revocada no se revoca de nuevo (criterio 10).
        await Assert.ThrowsAsync<ArgumentException>(() => _e.Consulta.RevocarAsync(codigo, 2, "Otra vez"));

        // Y solo se avisó una vez a la red.
        Assert.Single(_e.Stellar.Revocados);
    }

    [Fact]
    public async Task Revocar_SinMotivo_Lanza400()
    {
        var codigo = await EmitirAsync();

        // Un motivo vacío o de solo espacios no vale (la petición lo rechaza con 422; el servicio lo respalda con 400).
        await Assert.ThrowsAsync<ArgumentException>(() => _e.Consulta.RevocarAsync(codigo, 2, "   "));

        // La credencial sigue vigente.
        Assert.Equal(EstadoCredencial.VIGENTE, await _e.Consulta.ObtenerEstadoAsync(codigo));
    }

    [Fact]
    public async Task Revocar_ConUnDocente_Lanza400()
    {
        var codigo = await EmitirAsync();

        // El usuario 1 es DOCENTE: solo un COORDINADOR revoca.
        await Assert.ThrowsAsync<ArgumentException>(() => _e.Consulta.RevocarAsync(codigo, 1, "Motivo"));

        Assert.Equal(EstadoCredencial.VIGENTE, await _e.Consulta.ObtenerEstadoAsync(codigo));
        Assert.Empty(_e.Stellar.Revocados);
    }

    [Fact]
    public async Task Revocar_ConUsuarioInexistente_Lanza404()
    {
        var codigo = await EmitirAsync();

        await Assert.ThrowsAsync<NoEncontradoExcepcion>(() => _e.Consulta.RevocarAsync(codigo, 999, "Motivo"));
    }

    [Fact]
    public async Task Revocar_ConCredencialInexistente_Lanza404()
    {
        await Assert.ThrowsAsync<NoEncontradoExcepcion>(() => _e.Consulta.RevocarAsync("CRED-0000", 2, "Motivo"));
    }

    [Fact]
    public async Task Revocar_ConLaRedCaida_LaRevocacionEnLaBDSeMantiene()
    {
        // Se emite con la red disponible (el anclaje queda ANCLADO) y LUEGO se "cae" la red.
        var codigo = await EmitirAsync();
        _e.Stellar.Disponible = false;

        var filas = await _e.Consulta.RevocarAsync(codigo, 2, "Motivo");

        // La BD es la fuente de verdad: se revocó allí aunque la red no respondió (mejor esfuerzo).
        Assert.Equal(1, filas);
        Assert.Equal(EstadoCredencial.REVOCADA, await _e.Consulta.ObtenerEstadoAsync(codigo));

        // Sí se intentó avisar a la red; solo que ella falló.
        Assert.Single(_e.Stellar.Revocados);
    }

    [Fact]
    public async Task Revocar_ConAnclajeFallido_NoLlamaALaRed()
    {
        // Se emite con la red caída: el anclaje queda FALLIDO y no tiene attestationUid.
        _e.Stellar.Disponible = false;
        var codigo = await EmitirAsync();
        Assert.Equal(EstadoAnclaje.FALLIDO, _e.Anclajes.Anclajes[0].Estado);

        _e.Stellar.Disponible = true;
        await _e.Consulta.RevocarAsync(codigo, 2, "Motivo");

        // No hay nada que revocar en la red, así que no se le pide; la revocación en la BD sí se hace.
        Assert.Empty(_e.Stellar.Revocados);
        Assert.Equal(EstadoCredencial.REVOCADA, await _e.Consulta.ObtenerEstadoAsync(codigo));
    }
}