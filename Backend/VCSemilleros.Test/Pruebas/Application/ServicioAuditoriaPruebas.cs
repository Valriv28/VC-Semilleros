using System.Text.Json;                         // JsonSerializer: para comprobar que las respuestas no llevan datos personales.
using VCSemilleros.Application.Servicios;       // ServicioAuditoria (el que se prueba) y sus records.
using VCSemilleros.Domain.Excepciones;          // NoEncontradoExcepcion.
using VCSemilleros.Domain.Modelos;              // Credencial, EstadoCredencial, EstadoAnclaje.
using VCSemilleros.Domain.Repositorios;         // IClienteStellar y sus records (para el cliente que lanza excepciones).
using Xunit;                                    // xUnit: [Fact] y Assert.

namespace VCSemilleros.Tests.Pruebas.Application;

// Pruebas de ServicioAuditoria (E27, E28, E29; RF15) con repositorios y cliente de Stellar FALSOS.
// Cubre: existeEnRed con la red disponible, caída o que lanza excepciones; la línea de tiempo derivada
// (P3) con sus eventos en orden; la integridad con el hash de la red alterado; y que ninguna respuesta
// lleve datos personales (solo identificadores).
public class ServicioAuditoriaPruebas
{
    // xUnit crea una instancia nueva por prueba: cada una arranca con su propio escenario limpio.
    private readonly EscenarioFase5 _e = new();

    // Cliente de Stellar que LANZA excepciones en todo: sirve para probar que una auditoría no falla
    // aunque el adaptador se comporte mal. Es "private" porque solo esta clase de pruebas lo usa.
    private sealed class ClienteStellarQueLanza : IClienteStellar
    {
        // "=> throw ..." es un método cuyo único contenido es lanzar la excepción.
        public Task<ResultadoAnclaje> AnclarAsync(string credencialCodigo, string evidenciaHash)
            => throw new InvalidOperationException("El adaptador falló.");

        public Task<ResultadoRevocacion> RevocarAsync(string credencialCodigo, string attestationUid)
            => throw new InvalidOperationException("El adaptador falló.");

        public Task<EvidenciaEnRed?> ConsultarAsync(string attestationUid)
            => throw new InvalidOperationException("El adaptador falló.");
    }

    // Método auxiliar: emite la credencial de la evaluación 1 y devuelve su código.
    // Antes de emitir, la evaluación se fecha una hora atrás: así la línea de tiempo queda en orden
    // cronológico aunque el reloj real sea anterior a la fecha fija con la que se sembró el escenario.
    private async Task<string> EmitirAsync()
    {
        _e.Evaluaciones.Evaluaciones.First(x => x.Id == 1).Fecha = DateTime.Now.AddHours(-1);
        var resultado = await _e.Emision.EmitirAsync(1);
        return resultado.Codigo;
    }

    // Método auxiliar: registra una credencial SIN anclaje (para probar el 404 "sin anclaje registrado").
    private string AgregarCredencialSinAnclaje()
    {
        _e.Evaluaciones.Evaluaciones.First(x => x.Id == 1).Fecha = DateTime.Now.AddHours(-1);
        _e.Credenciales.Credenciales.Add(new Credencial
        {
            Codigo = "CRED-2026-SINANCLA",
            EvaluacionId = 1,
            TitularId = 3,
            Tipo = ConstantesCredencial.Tipo,
            Estado = EstadoCredencial.VIGENTE,
            VcHash = new string('a', 64),
            FechaEmision = DateTime.Now
        });
        return "CRED-2026-SINANCLA";
    }

    // ---------- E27: evidencia de anclaje ----------

    [Fact]
    public async Task Anclaje_ConCredencialAnclada_DevuelveLaEvidenciaYExisteEnRed()
    {
        var codigo = await EmitirAsync();
        var guardado = _e.Anclajes.Anclajes[0];

        var r = await _e.Auditoria.ObtenerAnclajeAsync(codigo);

        Assert.Equal(codigo, r.Codigo);
        Assert.Equal(EstadoAnclaje.ANCLADO, r.Estado);
        Assert.Equal(guardado.EvidenciaHash, r.EvidenciaHash);
        Assert.False(string.IsNullOrWhiteSpace(r.TxHash));
        Assert.False(string.IsNullOrWhiteSpace(r.AttestationUid));
        Assert.True(r.ExisteEnRed);                          // La red falsa sí tiene la atestación.
    }

    [Fact]
    public async Task Anclaje_ConLaRedCaida_ExisteEnRedEsFalseYNoFalla()
    {
        var codigo = await EmitirAsync();
        _e.Stellar.Disponible = false;                       // La red deja de responder DESPUÉS de anclar.

        var r = await _e.Auditoria.ObtenerAnclajeAsync(codigo);

        // Una auditoría no falla porque la red esté caída: responde "no existe en la red".
        Assert.False(r.ExisteEnRed);
        Assert.Equal(EstadoAnclaje.ANCLADO, r.Estado);       // Lo que dice NUESTRA BD no cambia.
    }

    [Fact]
    public async Task Anclaje_ConUnClienteQueLanzaExcepciones_ExisteEnRedEsFalseSinFallar()
    {
        var codigo = await EmitirAsync();

        // Se arma un servicio de auditoría con un cliente que explota en cada llamada.
        var auditoria = new ServicioAuditoria(_e.Credenciales, _e.Evaluaciones, _e.Anclajes, new ClienteStellarQueLanza());

        var r = await auditoria.ObtenerAnclajeAsync(codigo);

        Assert.False(r.ExisteEnRed);
    }

    [Fact]
    public async Task Anclaje_ConAnclajeFallido_NoTieneUidNiTxYNoExisteEnRed()
    {
        _e.Stellar.Disponible = false;                       // La emisión ocurre con la red caída (P4)...
        var codigo = await EmitirAsync();
        _e.Stellar.Disponible = true;                        // ...y luego la red vuelve: igual no hay UID que consultar.

        var r = await _e.Auditoria.ObtenerAnclajeAsync(codigo);

        Assert.Equal(EstadoAnclaje.FALLIDO, r.Estado);
        Assert.Null(r.TxHash);
        Assert.Null(r.AttestationUid);
        Assert.False(r.ExisteEnRed);
    }

    [Fact]
    public async Task Anclaje_ConCredencialInexistente_Lanza404()
    {
        await Assert.ThrowsAsync<NoEncontradoExcepcion>(() => _e.Auditoria.ObtenerAnclajeAsync("CRED-0000"));
    }

    [Fact]
    public async Task Anclaje_ConCredencialSinAnclajeRegistrado_Lanza404()
    {
        var codigo = AgregarCredencialSinAnclaje();

        await Assert.ThrowsAsync<NoEncontradoExcepcion>(() => _e.Auditoria.ObtenerAnclajeAsync(codigo));
    }

    // ---------- E28: línea de tiempo ----------

    [Fact]
    public async Task LineaTiempo_DeUnaCredencialEmitida_TieneTresEventosEnOrden()
    {
        var codigo = await EmitirAsync();

        var eventos = await _e.Auditoria.ObtenerLineaTiempoAsync(codigo);

        Assert.Equal(new[] { "EVALUADA", "EMITIDA", "ANCLAJE_SOLICITADO" },
                     eventos.Select(x => x.Evento).ToArray());

        // Quién hizo cada cosa: el coordinador 2 definió el criterio y declaró la elegibilidad;
        // el anclaje lo pide el sistema (sin actor). Ningún evento lleva motivo todavía.
        Assert.Equal(2, eventos[0].ActorId);
        Assert.Equal(2, eventos[1].ActorId);
        Assert.Null(eventos[2].ActorId);
        Assert.All(eventos, x => Assert.Null(x.Motivo));
    }

    [Fact]
    public async Task LineaTiempo_DespuesDeRevocar_TieneCuatroEventosYElMotivo()
    {
        var codigo = await EmitirAsync();
        await _e.Consulta.RevocarAsync(codigo, 2, "Error en el registro de asistencia");

        var eventos = await _e.Auditoria.ObtenerLineaTiempoAsync(codigo);

        // Criterio 11: total 4, en orden cronológico.
        Assert.Equal(new[] { "EVALUADA", "EMITIDA", "ANCLAJE_SOLICITADO", "REVOCADA" },
                     eventos.Select(x => x.Evento).ToArray());

        // La revocación trae quién la hizo y por qué.
        var revocada = eventos[3];
        Assert.Equal(2, revocada.ActorId);
        Assert.Equal("Error en el registro de asistencia", revocada.Motivo);

        // Las fechas nunca retroceden.
        Assert.Equal(eventos.OrderBy(x => x.Fecha).Select(x => x.Fecha), eventos.Select(x => x.Fecha));
    }

    [Fact]
    public async Task LineaTiempo_ConCredencialSinAnclaje_OmiteElEventoDeAnclaje()
    {
        var codigo = AgregarCredencialSinAnclaje();

        var eventos = await _e.Auditoria.ObtenerLineaTiempoAsync(codigo);

        // La línea de tiempo se deriva de lo que existe: sin anclaje no hay ANCLAJE_SOLICITADO.
        Assert.Equal(new[] { "EVALUADA", "EMITIDA" }, eventos.Select(x => x.Evento).ToArray());
    }

    [Fact]
    public async Task LineaTiempo_ConCredencialInexistente_Lanza404()
    {
        await Assert.ThrowsAsync<NoEncontradoExcepcion>(() => _e.Auditoria.ObtenerLineaTiempoAsync("CRED-0000"));
    }

    // ---------- E29: integridad ----------

    [Fact]
    public async Task Integridad_ConLaRedIntacta_Coincide()
    {
        var codigo = await EmitirAsync();

        var r = await _e.Auditoria.ObtenerIntegridadAsync(codigo);

        Assert.Equal(codigo, r.Codigo);
        Assert.Equal(_e.Anclajes.Anclajes[0].EvidenciaHash, r.EvidenciaHashBD);
        Assert.Equal(r.EvidenciaHashBD, r.EvidenciaHashRed);
        Assert.True(r.Coincide);
    }

    [Fact]
    public async Task Integridad_ConHashAlteradoEnLaRed_NoCoincide()
    {
        var codigo = await EmitirAsync();
        var uid = _e.Anclajes.Anclajes[0].AttestationUid!;

        // Se simula que la red guarda OTRO hash distinto del de la BD.
        var alterado = new string('0', 64);
        _e.Stellar.Red[uid] = new EvidenciaEnRed(uid, alterado, false);

        var r = await _e.Auditoria.ObtenerIntegridadAsync(codigo);

        Assert.False(r.Coincide);
        Assert.Equal(alterado, r.EvidenciaHashRed);
        Assert.NotEqual(alterado, r.EvidenciaHashBD);        // El de la BD no se tocó.
    }

    [Fact]
    public async Task Integridad_ConHashDeLaRedEnMayusculas_Coincide()
    {
        var codigo = await EmitirAsync();
        var anclaje = _e.Anclajes.Anclajes[0];
        var uid = anclaje.AttestationUid!;

        // El mismo hash escrito en mayúsculas es el mismo valor: la comparación no distingue mayúsculas.
        _e.Stellar.Red[uid] = new EvidenciaEnRed(uid, anclaje.EvidenciaHash.ToUpperInvariant(), false);

        var r = await _e.Auditoria.ObtenerIntegridadAsync(codigo);

        Assert.True(r.Coincide);
    }

    [Fact]
    public async Task Integridad_ConLaRedCaida_NoCoincideYElHashDeLaRedEsNull()
    {
        var codigo = await EmitirAsync();
        _e.Stellar.Disponible = false;

        var r = await _e.Auditoria.ObtenerIntegridadAsync(codigo);

        // "Coincide" solo es true si la red respondió y el hash es idéntico.
        Assert.False(r.Coincide);
        Assert.Null(r.EvidenciaHashRed);
        Assert.False(string.IsNullOrWhiteSpace(r.EvidenciaHashBD));
    }

    [Fact]
    public async Task Integridad_ConAnclajeFallido_NoCoincide()
    {
        _e.Stellar.Disponible = false;
        var codigo = await EmitirAsync();
        _e.Stellar.Disponible = true;

        var r = await _e.Auditoria.ObtenerIntegridadAsync(codigo);

        Assert.False(r.Coincide);
        Assert.Null(r.EvidenciaHashRed);
    }

    [Fact]
    public async Task Integridad_ConCredencialInexistente_Lanza404()
    {
        await Assert.ThrowsAsync<NoEncontradoExcepcion>(() => _e.Auditoria.ObtenerIntegridadAsync("CRED-0000"));
    }

    [Fact]
    public async Task Integridad_ConCredencialSinAnclajeRegistrado_Lanza404()
    {
        var codigo = AgregarCredencialSinAnclaje();

        await Assert.ThrowsAsync<NoEncontradoExcepcion>(() => _e.Auditoria.ObtenerIntegridadAsync(codigo));
    }

    // ---------- Privacidad ----------

    [Fact]
    public async Task LasRespuestasDeAuditoria_NoIncluyenDatosPersonales()
    {
        var codigo = await EmitirAsync();
        await _e.Consulta.RevocarAsync(codigo, 2, "Error en el registro de asistencia");

        // Se convierten las tres respuestas a JSON y se busca en ellas lo que NUNCA debe aparecer.
        var json = JsonSerializer.Serialize(new
        {
            anclaje = await _e.Auditoria.ObtenerAnclajeAsync(codigo),
            lineaTiempo = await _e.Auditoria.ObtenerLineaTiempoAsync(codigo),
            integridad = await _e.Auditoria.ObtenerIntegridadAsync(codigo)
        });

        // Nombres y correos de los usuarios sembrados en el escenario: solo se permiten identificadores.
        Assert.DoesNotContain("Ana María", json);
        Assert.DoesNotContain("Torres", json);
        Assert.DoesNotContain("Coordinador Semilleros", json);
        Assert.DoesNotContain("@x.edu", json);
    }
}