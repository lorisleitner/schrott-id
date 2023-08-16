using System.Security.Cryptography;

namespace SchrottId;

public static class SchrottIdUtil
{
    /// <summary>
    /// Generates a secure random permutation for the supplied alphabet.
    /// </summary>
    /// <param name="alphabet">The alphabet</param>
    /// <returns>A randomly generated permutation to use the <see cref="SchrottIdEncoder"/> class.</returns>
    /// <exception cref="ArgumentException">Alphabet is not between 2 and 256 chars long or chars are not unique.</exception>
    public static string GeneratePermutation(string alphabet)
    {
        if (alphabet.Length is <= 1 or > 256)
        {
            throw new ArgumentException(
                "Alphabet must have 2 to 256 characters",
                nameof(alphabet));
        }

        if (alphabet.Distinct().Count() != alphabet.Length)
        {
            throw new ArgumentException(
                "Alphabet must have unique characters",
                nameof(alphabet));
        }

        var buf = new byte[alphabet.Length];

        for (var i = 0; i < buf.Length; ++i)
        {
            buf[i] = (byte)i;
        }

        buf.Scramble();

        return Convert.ToBase64String(buf);
    }

    /// <summary>
    /// Randomizes an array
    /// </summary>
    /// <param name="array">The array</param>
    /// <typeparam name="T">Array type</typeparam>
    public static void Scramble<T>(this T[] array)
    {
        for (var i = 0; i < array.Length; ++i)
        {
            var p = RandomInt(0, array.Length);

            (array[i], array[p]) = (array[p], array[i]);
        }
    }

    /// <summary>
    /// Generate a secure random integer
    /// </summary>
    /// <param name="min">Minimum value, inclusive</param>
    /// <param name="max">Maximum value, exclusive</param>
    /// <returns>A random integer in the specified range</returns>
    public static int RandomInt(int min, int max)
    {
        using var rng = RandomNumberGenerator.Create();

        var array = new byte[4];
        rng.GetBytes(array);

        var value = BitConverter.ToInt32(array, 0);
        value = Math.Abs(value % (max - min)) + min;

        return value;
    }
}