// Espacio de nombres de las excepciones de negocio del dominio.
namespace VCSemilleros.Domain.Excepciones;

// Una excepción es un objeto que representa un error y que interrumpe el flujo normal.
// Esta se lanza cuando un recurso pedido no existe (un semillero, una evaluación, una credencial...).
// El controlador la captura y la traduce a HTTP 404 (Art. 4 y plan §4.5).
// ": Exception" significa que hereda de Exception, la clase base de todos los errores de C#.
public class NoEncontradoExcepcion : Exception
{
    public NoEncontradoExcepcion(string mensaje) : base(mensaje)
    {
    }
}