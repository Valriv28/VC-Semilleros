// "namespace" agrupa clases relacionadas bajo un nombre; evita choques de nombres entre proyectos.
// La sintaxis con punto y coma (file-scoped) aplica el namespace a todo el archivo.
namespace VCSemilleros.Domain.Modelos;

// "enum" define un conjunto fijo de valores con nombre.
// Refleja el tipo enumerado rol_usuario de PostgreSQL (la BD viene dada, Art. 6).
// Los nombres van en MAYÚSCULAS porque así están escritos en la BD; los repositorios
// leen el enum como texto (rol::text), por eso deben coincidir letra por letra.
public enum RolUsuario
{
    // Estudiante: persona que participa en un semillero y puede ser titular de una credencial.
    ESTUDIANTE,

    // Coordinador: define el criterio, declara la elegibilidad, autoriza la emisión y revoca.
    COORDINADOR,

    // Docente: responsable de un semillero; registra sesiones y asistencia.
    DOCENTE
}