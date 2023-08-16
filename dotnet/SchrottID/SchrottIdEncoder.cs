using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace SchrottId;

/// <summary>
/// Allows encoding and decoding of SchrottIDs
/// </summary>
public class SchrottIdEncoder
{
    private readonly string _alphabet;
    private readonly Dictionary<char, byte> _inverseAlphabet;

    private readonly byte[] _permutation;
    private readonly byte[] _inversePermutation;

    private readonly int _minLength;

    /// <summary>
    /// Creates a new instance of the SchrottID encoder class.
    /// SchrottIDs can only be decoded if the parameters to this constructor are equal
    /// to the ones that were supplied to create the SchrottID.
    ///
    /// This constructor verifies parameters and creates internal structures.
    /// Instances should be reused as often as possible.
    /// </summary>
    /// <param name="alphabet">The alphabet that the encoder and decoder will use.</param>
    /// <param name="permutation">The randomly generated permutation to use.
    /// Generate permutations using <see cref="GeneratePermutation"/>.
    /// Permutations are dependent on the supplied alphabet.</param>
    /// <param name="minLength">The minimum length of the encoded ID that the <see cref="Encode"/> method will produce.</param>
    /// <exception cref="ArgumentException">A supplied parameter cannot be used to create an encoder.</exception>
    /// <exception cref="FormatException">The permutation is not a valid Base64 string.</exception>
    public SchrottIdEncoder(
        string alphabet,
        string permutation,
        int minLength)
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

        if (minLength <= 0)
        {
            throw new ArgumentException(
                "minLength must be greater than 0",
                nameof(minLength));
        }

        _alphabet = alphabet;
        _inverseAlphabet = _alphabet
            .Select((c, i) => (c, i))
            .ToDictionary(x => x.c, x => (byte)x.i);

        _permutation = Convert.FromBase64String(permutation);

        if (_permutation.Length != _alphabet.Length)
        {
            throw new ArgumentException(
                "Permutation length must be equal to alphabet length. " +
                "Please make sure to use a valid permutation for this alphabet",
                nameof(permutation));
        }

        if (_permutation.Distinct().Count() != _permutation.Length)
        {
            throw new ArgumentException(
                "Invalid permutation. All positions must be unique.",
                nameof(permutation));
        }

        if (_permutation.Min() != 0
            || _permutation.Max() != _alphabet.Length - 1)
        {
            throw new ArgumentException(
                "Invalid permutation. Invalid indices for used alphabet",
                nameof(permutation));
        }

        _inversePermutation = new byte[_permutation.Length];

        for (var i = 0; i < _permutation.Length; ++i)
        {
            _inversePermutation[_permutation[i]] = (byte)i;
        }

        _minLength = minLength;
    }

    /// <summary>
    /// Encodes an integer value to a SchrottID.
    /// </summary>
    /// <param name="value">The values to encode.</param>
    /// <returns>Encoded SchrottID</returns>
    public string Encode(UInt64 value)
    {
        return Encode(new[] { value })[0];
    }

    /// <summary>
    /// Encodes an integer value to a SchrottID.
    /// </summary>
    /// <param name="value">The values to encode.</param>
    /// <returns>Encoded SchrottID</returns>
    /// <exception cref="OverflowException">Cannot convert Int32 to UInt64</exception>
    public string Encode(Int32 value)
    {
        return Encode(checked((UInt64)value));
    }

    /// <summary>
    /// Encodes a collection of integer values to SchrottIDs.
    /// </summary>
    /// <param name="values">Collection of values to encode</param>
    /// <returns>Encoded SchrottIDs, in the same order they were supplied</returns>
    public IEnumerable<string> Encode(IEnumerable<UInt64> values)
    {
        return Encode(values.ToArray());
    }

    public IEnumerable<string> Encode(IEnumerable<Int32> values)
    {
        return Encode(values.Select(x => checked((UInt64)x)).ToArray());
    }

    private string[] Encode(UInt64[] values)
    {
        var result = new string[values.Length];

        for (var i = 0; i < values.Length; i++)
        {
            var value = values[i];

            var length = GetLength(value);
            var buf = new byte[length];

            ConvertToBase(value, buf);

            for (var round = 0;
                 round < buf.Length * 3;
                 ++round)
            {
                RotateLeft(buf);
                PermuteForward(buf);
                RotateLeft(buf);
                CascadeForward(buf);
                RotateLeft(buf);
            }

            result[i] = ConvertToString(buf);
        }

        return result;
    }

    /// <summary>
    /// Decodes a SchrottID back to an integer value.
    /// </summary>
    /// <param name="value">The value to decode</param>
    /// <returns>The decoded SchrottID</returns>
    /// <exception cref="FormatException">The supplied value contains a character that is not present in the alphabet.</exception>
    public UInt64 Decode(string value)
    {
        return Decode(new[] { value })[0];
    }

    /// <summary>
    /// Decodes a collection of SchrottIDs back to integer values.
    /// </summary>
    /// <param name="values">The values to decode</param>
    /// <returns>The decoded SchrottIDs, in the same order they were supplied</returns>
    /// <exception cref="FormatException">A supplied value contains a character that is not present in the alphabet.</exception>
    public IEnumerable<UInt64> Decode(IEnumerable<string> values)
    {
        return Decode(values.ToArray());
    }

    private UInt64[] Decode(string[] values)
    {
        var result = new UInt64[values.Length];

        for (var i = 0; i < values.Length; ++i)
        {
            var value = values[i];

            var buf = new byte[value.Length];
            ConvertFromBase(value, buf);

            for (var round = 0;
                 round < buf.Length * 3;
                 ++round)
            {
                RotateRight(buf);
                CascadeBackward(buf);
                RotateRight(buf);
                PermuteBackward(buf);
                RotateRight(buf);
            }

            result[i] = ConvertToValue(buf);
        }

        return result;
    }

    private int GetLength(UInt64 value)
    {
        return Math.Max(
            _minLength,
            (int)Math.Ceiling(Math.Log(value + 1, _alphabet.Length)));
    }

    private void ConvertToBase(UInt64 value, byte[] buf)
    {
        var i = buf.Length;
        do
        {
            var quotient = value / (UInt64)_alphabet.Length;
            var remainder = value % (UInt64)_alphabet.Length;

            buf[--i] = (byte)remainder;
            value = quotient;
        } while (value > 0);
    }

    private string ConvertToString(byte[] buf)
    {
        var charBuf = new char[buf.Length];

        for (var i = 0; i < buf.Length; ++i)
        {
            charBuf[i] = _alphabet[buf[i]];
        }

        return new string(charBuf);
    }

    private void ConvertFromBase(string value, byte[] buf)
    {
        for (var i = 0; i < value.Length; ++i)
        {
            if (!_inverseAlphabet.TryGetValue(value[i], out buf[i]))
            {
                throw new FormatException("Character not in alphabet");
            }
        }
    }

    private UInt64 ConvertToValue(byte[] buf)
    {
        UInt64 value = 0;

        for (var i = 0; i < buf.Length; ++i)
        {
            if (i > 0)
            {
                value *= (UInt64)_alphabet.Length;
            }

            value += buf[i];
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RotateLeft(byte[] buf)
    {
        var first = buf[0];
        Array.Copy(buf, 1, buf, 0, buf.Length - 1);
        buf[buf.Length - 1] = first;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RotateRight(byte[] buf)
    {
        var last = buf[buf.Length - 1];
        Array.Copy(buf, 0, buf, 1, buf.Length - 1);
        buf[0] = last;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PermuteForward(byte[] buf)
    {
        for (var i = 0; i < buf.Length; ++i)
        {
            buf[i] = _permutation[buf[i]];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PermuteBackward(byte[] buf)
    {
        for (var i = 0; i < buf.Length; ++i)
        {
            buf[i] = _inversePermutation[buf[i]];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CascadeForward(byte[] buf)
    {
        byte last = 0;
        for (var i = 0; i < buf.Length; ++i)
        {
            buf[i] = (byte)((buf[i] + last) % _alphabet.Length);
            last = buf[i];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CascadeBackward(byte[] buf)
    {
        byte last = 0;

        for (var i = 0; i < buf.Length; ++i)
        {
            var t = buf[i];
            buf[i] = (byte)((buf[i] + _alphabet.Length - last) % _alphabet.Length);
            last = t;
        }
    }
}