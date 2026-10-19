// Importa JsonIgnore y JsonIgnoreCondition: permiten OMITIR del JSON los campos que valen null.
using System.Text.Json.Serialization;
// Importa los modelos del dominio: aquí se usan los enums EstadoCredencial y EstadoAnclaje.
using VCSemilleros.Domain.Modelos;
// Importa los servicios de Application: aquí se reutilizan sus "records" ControlVerificacion,
// Veredicto y ResumenAsistencia, que ya tienen exactamente la forma que fijan los contratos.
using VCSemilleros.Application.Servicios;

// Espacio de nombres de las peticiones: los DTOs de ENTRADA y de SALIDA de la API.
// Este archivo guarda los DTOs de SALIDA: los sobres de respuesta (plan §2: "Respuestas.cs").
namespace VCSemilleros.Api.Peticiones;

// NOTA GENERAL SOBRE ESTE ARCHIVO
//  - "record" es un tipo de C# para transportar datos: se declara con sus propiedades entre paréntesis
//    y el compilador crea el constructor y las propiedades de solo lectura.
//  - El ORDEN de las propiedades es el orden en que salen en el JSON (igual que en 6_contracts.md).
//  - ASP.NET Core convierte los nombres a camelCase solo (Estado → "estado"), como exige el Art. 9.
//  - Los enums (EstadoCredencial, Veredicto...) salen como TEXTO ("VIGENTE") porque Program.cs
//    registra el conversor JsonStringEnumConverter; sin él saldrían como números.

// Sobre de las LISTAS: { recurso, limite, total, datos } (Art. 9, contratos §0).
// "<T>" es un tipo genérico: un solo sobre sirve para listas de semilleros, periodos, etc.
//   Recurso: nombre del recurso en singular ("semillero", "periodo"...).
//   Limite:  el límite usado en la consulta (por defecto 1000).
//   Total:   cuántos elementos trae "datos".
//   Datos:   los elementos. (Si la lista está vacía el controlador responde 204 y no usa este sobre.)
public record RespuestaLista<T>(string Recurso, int Limite, int Total, List<T> Datos);

// Respuesta de los POST que CREAN algo y devuelven el id generado por la BD
// (E5, E8, E10, E12): { estado, mensaje, id }.
public record RespuestaCreada(int Estado, string Mensaje, int Id);

// Respuesta de las operaciones que MODIFICAN filas y devuelven cuántas cambiaron
// (E6, E13, E19, E25): { estado, mensaje, filasAfectadas }.
public record RespuestaFilas(int Estado, string Mensaje, int FilasAfectadas);

// Respuesta de E17 (evaluar): además del id trae lo que CALCULÓ el sistema,
// porque el body de la petición no incluye ni el porcentaje ni "cumple".
public record RespuestaEvaluacionCreada(int Estado, string Mensaje, int Id, decimal PctAsistencia, bool Cumple);

// Respuesta de E16 (criterio vigente de un periodo, propuesta P1): el criterio de MAYOR versión.
public record RespuestaCriterioVigente(int PeriodoId, int CriterioVersion, decimal MinAsistenciaPct, int DefinidoPor);

// Respuesta de E20 (emitir la credencial).
//   EstadoCredencial: VIGENTE al emitir.
//   EstadoAnclaje:    ANCLADO o FALLIDO (P4: si Stellar falla, la credencial queda emitida igual).
//   TxHash:           referencia de la transacción; null si el anclaje falló. Este campo SIEMPRE aparece
//                     en el JSON (el contrato muestra "txHash":null), por eso aquí NO se omite si es null.
//   Vc:               la credencial firmada y serializada.
public record RespuestaEmision(
    int Estado,
    string Mensaje,
    string Codigo,
    EstadoCredencial EstadoCredencial,
    EstadoAnclaje EstadoAnclaje,
    string? TxHash,
    string Vc);

// Respuesta de E23 (obtener la credencial firmada): { codigo, vc }.
public record RespuestaCredencialVc(string Codigo, string Vc);

// Respuesta de E24 (estado vigente de una credencial): { codigo, estado }.
public record RespuestaEstadoCredencial(string Codigo, EstadoCredencial Estado);

// Respuesta de E26 (verificación): { estado, veredicto, codigo, controles:[…] }.
// Un veredicto negativo (RECHAZADA, INDETERMINADA) NO es un error HTTP: se responde 200.
//   Codigo:    el código leído de la credencial; null si no se pudo leer.
//   Controles: los cuatro controles con su resultado y su explicación en español.
public record RespuestaVerificacion(int Estado, Veredicto Veredicto, string? Codigo, List<ControlVerificacion> Controles);

// Respuesta de E2 (ingresar): los campos de la tabla usuario que fija el contrato
// (id, nombre, correo, rol) MÁS el token JWT con el que la persona llamará a los demás endpoints.
// Agregar "token" es la consecuencia de la decisión acordada de usar JWT (el contrato E2 estaba
// pendiente de A1/A2).
//   Rol: texto: ESTUDIANTE, DOCENTE, COORDINADOR, o ADMINISTRADOR si salió de la tabla "administrador".
public record RespuestaIngreso(int Id, string Nombre, string Correo, string Rol, string Token);

// Respuesta de TODOS los errores: { estado, mensaje, detalle } (Art. 9, contratos §0).
// El 422 usa el mismo sobre pero lleva "errores" (la lista de mensajes de validación) y NO lleva "detalle".
// Un 400, 404 o 500 lleva "detalle" y NO lleva "errores".
// [property: JsonIgnore(Condition = WhenWritingNull)] le dice al serializador: "si este campo vale null,
// no lo escribas en el JSON". Así cada tipo de error muestra solo los campos que le corresponden.
public record RespuestaError(
    int Estado,
    string Mensaje,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Detalle = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] List<string>? Errores = null);  