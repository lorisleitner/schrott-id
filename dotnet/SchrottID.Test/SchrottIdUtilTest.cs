namespace SchrottId.Test;

public class SchrottIdUtilTest
{
    [Fact]
    public void TestScramble()
    {
        const string value = "SchrottID is a great library!";
        var array = value.ToCharArray();

        array.Scramble();

        Assert.NotEqual(value, new string(array));
    }

    [Fact]
    public void TestRandomInt()
    {
        var numbers = Enumerable
            .Range(0, 1000)
            .Select(_ => SchrottIdUtil.RandomInt(0, 10))
            .ToList();

        Assert.Equal(0, numbers.Min());
        Assert.Equal(9, numbers.Max());
    }
}