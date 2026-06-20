using System.Security.Cryptography;
using Umbral.IdentityService.Application.Abstractions.Security;

namespace Umbral.IdentityService.Infrastructure.Security;

/// <summary>
/// Genera una contraseña temporal aleatoria criptográficamente segura. Garantiza al menos un
/// carácter de cada clase (minúscula, mayúscula, dígito, símbolo) para satisfacer políticas de
/// contraseña típicas, y omite caracteres ambiguos para facilitar la lectura en el correo.
/// </summary>
public sealed class CryptoTemporaryPasswordGenerator : ITemporaryPasswordGenerator
{
    private const string Lower = "abcdefghijkmnpqrstuvwxyz";   // sin 'l', 'o'
    private const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";    // sin 'I', 'O'
    private const string Digits = "23456789";                  // sin '0', '1'
    private const string Symbols = "!@#$%*?-_";
    private const int Length = 16;

    public string Generate()
    {
        var all = Lower + Upper + Digits + Symbols;
        var chars = new char[Length];

        // Garantiza una de cada clase.
        chars[0] = Pick(Lower);
        chars[1] = Pick(Upper);
        chars[2] = Pick(Digits);
        chars[3] = Pick(Symbols);

        for (var i = 4; i < Length; i++)
        {
            chars[i] = Pick(all);
        }

        Shuffle(chars);
        return new string(chars);
    }

    private static char Pick(string set) => set[RandomNumberGenerator.GetInt32(set.Length)];

    private static void Shuffle(char[] array)
    {
        for (var i = array.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }
    }
}
