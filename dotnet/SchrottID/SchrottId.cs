using System.Buffers.Text;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace SchrottId;

public class SchrottId
{
    private readonly string _alphabet;
    private readonly Dictionary<char, byte> _inverseAlphabet;

    private readonly byte[] _permutation;
    private readonly byte[] _inversePermutation;

    private readonly int _minLength;

    public SchrottId(
        string alphabet,
        string permutation,
        int minLength)
    {
        if (alphabet.Length is <= 1 or > 256)
        {
            throw new ArgumentException(
                "Alphabet length must have 2 to 256 characters",
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

    public static string GeneratePermutation(string alphabet)
    {
        if (alphabet.Length is <= 1 or > 256)
        {
            throw new ArgumentException(
                "Alphabet length must have 2 to 256 characters",
                nameof(alphabet));
        }

        if (alphabet.Distinct().Count() != alphabet.Length)
        {
            throw new ArgumentException(
                "Alphabet must have unique characters",
                nameof(alphabet));
        }

        Span<byte> buf = stackalloc byte[alphabet.Length];

        for (var i = 0; i < buf.Length; ++i)
        {
            buf[i] = (byte)i;
        }

        for (var i = 0; i < buf.Length; ++i)
        {
            var p = RandomNumberGenerator.GetInt32(buf.Length);

            (buf[i], buf[p]) = (buf[p], buf[i]);
        }

        return Convert.ToBase64String(buf);
    }

    public string Encode(UInt64 value)
    {
        var length = GetLength(value);
        Span<byte> buf = stackalloc byte[length];

        ConvertToBase(value, buf);

        for (var i = 0;
             i < buf.Length + _alphabet.Length;
             ++i)
        {
            RotateLeft(buf);
            PermuteForward(buf);
            RotateLeft(buf);
            CascadeForward(buf);
            RotateLeft(buf);
        }

        return ConvertToString(buf);
    }

    public UInt64 Decode(string value)
    {
        Span<byte> buf = stackalloc byte[value.Length];
        ConvertFromBase(value, buf);

        for (var i = 0;
             i < buf.Length + _alphabet.Length;
             ++i)
        {
            RotateRight(buf);
            CascadeBackward(buf);
            RotateRight(buf);
            PermuteBackward(buf);
            RotateRight(buf);
        }

        return ConvertToValue(buf);
    }

    private int GetLength(UInt64 value)
    {
        return Math.Max(
            _minLength,
            (int)Math.Ceiling(Math.Log(value + 1, _alphabet.Length)));
    }

    private void ConvertToBase(UInt64 value, Span<byte> buf)
    {
        var i = buf.Length;
        do
        {
            var (quotient, remainder) = UInt64.DivRem(
                value,
                (UInt64)_alphabet.Length);

            buf[--i] = (byte)remainder;
            value = quotient;
        } while (value > 0);
    }

    private string ConvertToString(Span<byte> buf)
    {
        Span<char> charBuf = stackalloc char[buf.Length];

        for (var i = 0; i < buf.Length; ++i)
        {
            charBuf[i] = _alphabet[buf[i]];
        }

        return new string(charBuf);
    }

    private void ConvertFromBase(string value, Span<byte> buf)
    {
        for (var i = 0; i < value.Length; ++i)
        {
            if (!_inverseAlphabet.TryGetValue(value[i], out buf[i]))
            {
                throw new FormatException("Character not in alphabet");
            }
        }
    }

    private UInt64 ConvertToValue(Span<byte> buf)
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
    private void RotateLeft(Span<byte> buf)
    {
        var first = buf[0];
        buf[1..].CopyTo(buf);
        buf[^1] = first;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RotateRight(Span<byte> buf)
    {
        var last = buf[^1];
        buf[..^1].CopyTo(buf[1..]);
        buf[0] = last;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PermuteForward(Span<byte> buf)
    {
        for (var i = 0; i < buf.Length; ++i)
        {
            buf[i] = _permutation[buf[i]];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PermuteBackward(Span<byte> buf)
    {
        for (var i = 0; i < buf.Length; ++i)
        {
            buf[i] = _inversePermutation[buf[i]];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CascadeForward(Span<byte> buf)
    {
        byte last = 0;
        for (var i = 0; i < buf.Length; ++i)
        {
            buf[i] = (byte)((buf[i] + last) % _alphabet.Length);
            last = buf[i];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CascadeBackward(Span<byte> buf)
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