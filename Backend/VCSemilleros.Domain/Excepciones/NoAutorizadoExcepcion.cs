// Espacio de nombres de las excepciones de negocio del dominio.
namespace VCSemilleros.Domain.Excepciones;

// Se lanza cuando alguien no logra identificarse: correo inexistente, contraseña incorrecta o usuario inactivo.
// El controlador la captura y la traduce a HTTP 401 (E2). Es hermana de NoEncontradoExcepcion (404)
// y de ArgumentException (400): cada tipo de excepción se convierte en un código HTTP distinto.
// ": Exception" significa que hereda de Exception, la clase base de todos los errores de C#.
public class NoAutorizadoExcepcion : Exception
{
	// Constructor: recibe el mensaje en español que verá quien consuma la API.
	// ": base(mensaje)" pasa ese mensaje a la clase Exception, que lo guarda en la propiedad Message.
	public NoAutorizadoExcepcion(string mensaje) : base(mensaje)
	{
	}
}