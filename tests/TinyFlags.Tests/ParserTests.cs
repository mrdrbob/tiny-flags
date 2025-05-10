namespace TinyFlags.Tests;

public class ParserTests : BaseTest
{
    [Theory]
    [InlineData("0001.json")]
    public void CanParseExampleFiles(string name)
    {
        var data = GetParsedFlagset(name);

        Assert.True(data.Variants.Any());
        Assert.True(data.Expressions.Any());
        Assert.True(data.Rules.Any());
    }
}
