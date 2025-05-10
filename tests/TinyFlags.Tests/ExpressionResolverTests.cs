namespace TinyFlags.Tests;

public class ExpressionResolverTests : BaseTest
{

    [Theory]
    [InlineData("local", false)]
    [InlineData("prod", true)]
    public void CanResolveEquality(string environment, bool expectedResult)
    {
        var ruleset = GetParsedFlagset("0001.json");

        var context = Context.Create();
        context.Set("environment", environment);

        var expression = ruleset.Expressions["is_on_prod"]!;

        var resolver = context.CreateResolver();
        var value = resolver.Evaluate(expression);
        Assert.IsType<BooleanExpressionResult>(value);
        Assert.Equal(expectedResult, ((BooleanExpressionResult)value).Value);
    }
}
