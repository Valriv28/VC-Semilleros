using VCSemilleros.Application.Servicios;       // ServicioEmision (el que se prueba) y sus resultados.
using VCSemilleros.Domain.Excepciones;          // NoEncontradoExcepcion.
using VCSemilleros.Domain.Modelos;              // EstadoCredencial, EstadoAnclaje.
using Xunit;                                    // xUnit: [Fact] y Assert.

namespace VCSemilleros.Tests.Pruebas.Application;

// Pruebas de ServicioEmision (E20, RF10) con repositorios y cliente de Stellar FALSOS (sin BD ni red).
// Cubre: emisión feliz, estructura incompleta (no llega a firmar), Stellar caído (P4),
// emisión sin elegibilidad, doble emisión y evaluación inexistente.
public class ServicioEmisionPruebas
{
    // xUnit crea una instancia nueva de esta clase por cada prueba: cada una tiene su propio escenario limpio.
    private readonly EscenarioFase5 _e = new();

    [Fact]
    public async Task Emitir_ConEvaluacionElegible_DevuelveCredencialVigenteYAnclada()
    {
        var resultado = await _e.Emision.EmitirAsync(1);

        // El resultado trae los estados esperados por el contrato (criterio 7 del spec).
        Assert.Equal(EstadoCredencial.VIGENTE, resultado.EstadoCredencial);
        Assert.Equal(EstadoAnclaje.ANCLADO, resultado.EstadoAnclaje);
        Assert.NotNull(resultado.TxHash);
        Assert.False(string.IsNullOrWhiteSpace(resultado.Vc));

        // Quedó UNA credencial registrada y UN anclaje, con el resultado de la red.
        var credencial = Assert.Single(_e.Credenciales.Credenciales);
        Assert.Equal(resultado.Codigo, credencial.Codigo);
        Assert.Equal(EstadoCredencial.VIGENTE, credencial.Estado);
        Assert.Equal(3, credencial.TitularId);                       // El titular es el usuario 3 (Ana).
        Assert.Equal(ConstantesCredencial.Tipo, credencial.Tipo);    // "Asistencia a semillero".

        var anclaje = Assert.Single(_e.Anclajes.Anclajes);
        Assert.Equal(EstadoAnclaje.ANCLADO, anclaje.Estado);
        Assert.Equal(resultado.TxHash, anclaje.TxHash);
        Assert.False(string.IsNullOrWhiteSpace(anclaje.AttestationUid));
    }

    [Fact]
    public async Task Emitir_GeneraUnCodigoConElFormatoDeP6()
    {
        var resultado = await _e.Emision.EmitirAsync(1);

        // CRED-AAAA-XXXXXXXX: año de 4 dígitos y 8 caracteres hexadecimales en mayúscula; máximo 40 caracteres.
        Assert.Matches(@"^CRED-\d{4}-[0-9A-F]{8}$", resultado.Codigo);
        Assert.True(resultado.Codigo.Length <= 40);
    }

    [Fact]
    public async Task Emitir_GuardaElHashDelContenidoSinPrueba_YLaFirmaEsValida()
    {
        var resultado = await _e.Emision.EmitirAsync(1);
        var credencial = _e.Credenciales.Credenciales[0];

        // El "vc" entregado se puede leer de vuelta como JSON.
        var leido = _e.Verificable.Leer(resultado.Vc);
        Assert.NotNull(leido);

        // La firma es válida y el hash guardado coincide con el del contenido sin la prueba.
        Assert.True(_e.Verificable.VerificarPrueba(leido!));
        Assert.Equal(_e.Verificable.CalcularHash(leido!), credencial.VcHash);
        Assert.Equal(64, credencial.VcHash.Length);

        // La BD guarda SOLO el hash, no la credencial completa.
        Assert.DoesNotContain("Ana María", credencial.VcHash);
    }

    [Fact]
    public async Task Emitir_LaEvidenciaQueVaALaRedEsUnHashSinDatosPersonales()
    {
        var resultado = await _e.Emision.EmitirAsync(1);

        // Se le pidió UN anclaje a la red, con el código de la credencial.
        var llamada = Assert.Single(_e.Stellar.Anclados);
        Assert.Equal(resultado.Codigo, llamada.Codigo);

        // Art. 5: lo que viaja es un SHA-256 en hexadecimal (64 caracteres en minúscula), nada más.
        Assert.Matches("^[0-9a-f]{64}$", llamada.EvidenciaHash);

        // Y es exactamente el que quedó registrado en la BD.
        Assert.Equal(llamada.EvidenciaHash, _e.Anclajes.Anclajes[0].EvidenciaHash);

        // No es el hash de la credencial: es una huella de (código + hash + fecha).
        Assert.NotEqual(_e.Credenciales.Credenciales[0].VcHash, llamada.EvidenciaHash);
    }

    [Fact]
    public async Task Emitir_GuardaLaFechaTruncadaASegundos()
    {
        await _e.Emision.EmitirAsync(1);

        // La fecha firmada no lleva fracciones de segundo; la guardada debe ser idéntica.
        // Si no, al reconstruir la credencial (E23) el hash no coincidiría.
        var fecha = _e.Credenciales.Credenciales[0].FechaEmision;
        Assert.Equal(0, fecha.Ticks % TimeSpan.TicksPerSecond);
    }

    [Fact]
    public async Task Emitir_ConStellarCaido_LaCredencialQuedaEmitidaYElAnclajeFallido()
    {
        // Simula que la red (o el puente) no responde.
        _e.Stellar.Disponible = false;

        var resultado = await _e.Emision.EmitirAsync(1);

        // P4: NO se revierte la credencial; la respuesta informa el fallo con txHash null.
        Assert.Equal(EstadoCredencial.VIGENTE, resultado.EstadoCredencial);
        Assert.Equal(EstadoAnclaje.FALLIDO, resultado.EstadoAnclaje);
        Assert.Null(resultado.TxHash);
        Assert.False(string.IsNullOrWhiteSpace(resultado.Vc));

        // La credencial sigue registrada y el anclaje quedó FALLIDO, sin tx ni UID.
        Assert.Single(_e.Credenciales.Credenciales);
        var anclaje = Assert.Single(_e.Anclajes.Anclajes);
        Assert.Equal(EstadoAnclaje.FALLIDO, anclaje.Estado);
        Assert.Null(anclaje.TxHash);
        Assert.Null(anclaje.AttestationUid);
    }

    [Fact]
    public async Task Emitir_SinElegibilidadDeclarada_Lanza400YNoCreaNada()
    {
        // La evaluación 2 no cumple y no tiene elegibilidad declarada.
        await Assert.ThrowsAsync<ArgumentException>(() => _e.Emision.EmitirAsync(2));

        // No se registró nada ni se llamó a la red.
        Assert.Empty(_e.Credenciales.Credenciales);
        Assert.Empty(_e.Anclajes.Anclajes);
        Assert.Empty(_e.Stellar.Anclados);
    }

    [Fact]
    public async Task Emitir_UnaEvaluacionYaEmitida_Lanza400()
    {
        await _e.Emision.EmitirAsync(1);

        // Emitir la misma evaluación otra vez es una regla de negocio rota (400).
        await Assert.ThrowsAsync<ArgumentException>(() => _e.Emision.EmitirAsync(1));

        // Sigue habiendo una sola credencial y un solo anclaje.
        Assert.Single(_e.Credenciales.Credenciales);
        Assert.Single(_e.Anclajes.Anclajes);
    }

    [Fact]
    public async Task Emitir_ConEvaluacionInexistente_Lanza404()
    {
        await Assert.ThrowsAsync<NoEncontradoExcepcion>(() => _e.Emision.EmitirAsync(999));
    }

    [Fact]
    public async Task Emitir_ConEstructuraIncompleta_Lanza400YNoLlegaAFirmar()
    {
        // Se deja al titular (usuario 3) sin correo: la credencial construida queda incompleta,
        // porque el correo es un campo obligatorio de la estructura.
        _e.Usuarios.Usuarios.First(u => u.Id == 3).Correo = "";

        var error = await Assert.ThrowsAsync<ArgumentException>(() => _e.Emision.EmitirAsync(1));
        Assert.Contains("incompleta", error.Message);

        // "NO avanza a firmar": no se registró credencial ni anclaje, y no se llamó a la red.
        Assert.Empty(_e.Credenciales.Credenciales);
        Assert.Empty(_e.Anclajes.Anclajes);
        Assert.Empty(_e.Stellar.Anclados);
    }

    [Fact]
    public async Task Emitir_ConElEmisorInexistente_Lanza404()
    {
        // Se quita al coordinador que declaró la elegibilidad: faltan datos relacionados (404).
        _e.Usuarios.Usuarios.RemoveAll(u => u.Id == 2);

        await Assert.ThrowsAsync<NoEncontradoExcepcion>(() => _e.Emision.EmitirAsync(1));
        Assert.Empty(_e.Credenciales.Credenciales);
    }
}