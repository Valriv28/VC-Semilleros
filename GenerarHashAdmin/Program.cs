using System.Security.Cryptography;
using System.Text;

Console.Write("Nueva contraseña del administrador: ");
var entrada = new StringBuilder();

while (true)
{
    var tecla = Console.ReadKey(intercept: true);

    if (tecla.Key == ConsoleKey.Enter)
        break;

    if (tecla.Key == ConsoleKey.Backspace)
    {
        if (entrada.Length > 0)
            entrada.Length--;

        continue;
    }

    if (!char.IsControl(tecla.KeyChar))
        entrada.Append(tecla.KeyChar);
}

Console.WriteLine();

string contrasena = entrada.ToString();

if (contrasena.Length < 8 || contrasena.Length > 120)
{
    Console.WriteLine("La contraseña debe tener entre 8 y 120 caracteres.");
    return;
}

const int iteraciones = 100_000;
byte[] sal = RandomNumberGenerator.GetBytes(16);
byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
    contrasena, sal, iteraciones, HashAlgorithmName.SHA256, 32);

Console.WriteLine("HASH PARA POSTGRESQL:");
Console.WriteLine(
    $"pbkdf2-sha256${iteraciones}${Convert.ToBase64String(sal)}${Convert.ToBase64String(hash)}");
