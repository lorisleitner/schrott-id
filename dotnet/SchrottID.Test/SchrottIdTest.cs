namespace SchrottId.Test;

public class SchrottIdTest
{
    private const string Permutation =
        "HwEMFAcAMAYEPxc4Dy4RAxAkEgstJggbGSMiKB0yHgk7OSsNMxoYKRMWNg49LzEFFTQKPDUhHAIsICclOio+Nw==";

    [Fact]
    public void TestEncodeDecode10000()
    {
        var schrottId = new SchrottIdEncoder(Alphabets.Base64, Permutation, minLength: 3);

        for (UInt64 i = 0; i < 10000; ++i)
        {
            var encoded = schrottId.Encode(i);
            var decoded = schrottId.Decode(encoded);

            Assert.Equal(i, decoded);
        }
    }

    [Fact]
    public void TestEncodeDecodeList()
    {
        var schrottId = new SchrottIdEncoder(Alphabets.Base64, Permutation, minLength: 3);

        var values = Enumerable
            .Range(0, 1000)
            .Select(x => (UInt64)x)
            .ToList();

        var encoded = schrottId.Encode(values);
        var decoded = schrottId.Decode(encoded);

        Assert.Equal(values, decoded);
    }

    [Fact]
    public void TestEncodeNegativeIntList()
    {
        var schrottId = new SchrottIdEncoder(Alphabets.Base64, Permutation, minLength: 3);

        Assert.Throws<OverflowException>(() => schrottId.Encode(new[] { -1, -2 }));
    }

    [Fact]
    public void TestEncodeNegativeInt()
    {
        var schrottId = new SchrottIdEncoder(Alphabets.Base64, Permutation, minLength: 3);

        Assert.Throws<OverflowException>(() => schrottId.Encode(-1));
    }

    [Fact]
    public void TestEncodeDecodeControl()
    {
        // control.txt contains the encoded values from 0 to 9999

        var controlLines = File
            .ReadAllLines("../../../../../test/control.txt")
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Where(x => !x.StartsWith("#"));

        var schrottId = new SchrottIdEncoder(Alphabets.Base64, Permutation, minLength: 3);

        var schrottIds = Enumerable.Range(0, 10000)
            .Select(x => schrottId.Encode((UInt64)x));

        Assert.Equal(controlLines, schrottIds);
    }

    [Fact]
    public void TestGeneratePermutation()
    {
        // This test depends on randomness, loop 1000 times to make sure we cover as many cases as possible

        for (var i = 0; i < 1000; ++i)
        {
            var permutation = SchrottIdUtil.GeneratePermutation(Alphabets.Base64);
            var permutationBytes = Convert.FromBase64String(permutation);

            Assert.Equal(Alphabets.Base64.Length, permutationBytes.Length);

            Assert.True(permutationBytes.Distinct().Count() == permutationBytes.Length);
            Assert.True(permutationBytes.Min() == 0);
            Assert.True(permutationBytes.Max() == Alphabets.Base64.Length - 1);
        }
    }

    [Fact]
    public void TestGeneratePermutationAlphabetTooShort()
    {
        var ex = Assert.Throws<ArgumentException>(() => SchrottIdUtil.GeneratePermutation("A"));

        Assert.Contains("Alphabet must have 2 to 256 characters", ex.Message);
    }

    [Fact]
    public void TestGeneratePermutationAlphabetTooLong()
    {
        var ex = Assert.Throws<ArgumentException>(() => SchrottIdUtil.GeneratePermutation(new string('A', 257)));

        Assert.Contains("Alphabet must have 2 to 256 characters", ex.Message);
    }

    [Fact]
    public void TestGeneratePermutationAlphabetNonUnique()
    {
        var ex = Assert.Throws<ArgumentException>(() => SchrottIdUtil.GeneratePermutation(new string('A', 256)));

        Assert.Contains("Alphabet must have unique characters", ex.Message);
    }

    [Fact]
    public void TestAlphabetTooShort()
    {
        var ex = Assert.Throws<ArgumentException>(() => new SchrottIdEncoder("A", "B", 3));

        Assert.Contains("Alphabet must have 2 to 256 characters", ex.Message);
    }

    [Fact]
    public void TestAlphabetTooLong()
    {
        var ex = Assert.Throws<ArgumentException>(() => new SchrottIdEncoder(new string('A', 257), "B", 3));

        Assert.Contains("Alphabet must have 2 to 256 characters", ex.Message);
    }

    [Fact]
    public void TestAlphabetNonUnique()
    {
        var ex = Assert.Throws<ArgumentException>(() => new SchrottIdEncoder(new string('A', 4), "ABCD", 3));

        Assert.Contains("Alphabet must have unique characters", ex.Message);
    }

    [Fact]
    public void TestNegativeMinLength()
    {
        var ex = Assert.Throws<ArgumentException>(() => new SchrottIdEncoder(Alphabets.Base64, Permutation, -1));

        Assert.Contains("minLength must be greater than 0", ex.Message);
    }

    [Fact]
    public void TestInvalidPermutationBase64()
    {
        var ex = Assert.Throws<FormatException>(() => new SchrottIdEncoder(Alphabets.Base64, "√∫¥", 3));

        Assert.Contains("The input is not a valid Base-64 string", ex.Message);
    }

    [Fact]
    public void TestPermutationLengthNotEqualToAlphabet()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new SchrottIdEncoder(Alphabets.Base64, "ChwDGxoUBBMLFRARDhIFDAIXGAcAHg0PAR8WCAYdCRk=", 3));

        Assert.Contains("Permutation length must be equal to alphabet length", ex.Message);
    }

    [Fact]
    public void TestPermutationNotUnique()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new SchrottIdEncoder(Alphabets.Base32, "QUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUE=", 3));

        Assert.Contains("All positions must be unique", ex.Message);
    }

    [Fact]
    public void TestPermutationInvalidIndices()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new SchrottIdEncoder(Alphabets.Base32, "twUkTIghtQiRcOQfJtmNRrYbOa9viXe784YeeHp8gec=", 3));

        Assert.Contains("Invalid indices for used alphabet", ex.Message);
    }

    [Fact]
    public void TestDecodeInvalid()
    {
        var schrottId = new SchrottIdEncoder(Alphabets.Base64, Permutation, 3);

        var ex = Assert.Throws<FormatException>(() => schrottId.Decode("$%&"));

        Assert.Contains("Character not in alphabet", ex.Message);
    }
}