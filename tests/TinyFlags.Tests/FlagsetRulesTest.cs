namespace TinyFlags.Tests;

public class FlagsetRulesTest : BaseTest
{
    [Theory]
    [InlineData("prod", true, 1)]
    [InlineData("local", true, 0)]
    [InlineData("stage", false, -1)]
    public void CanCalculateLogLevel(string environment, bool authenticated, int expectedResult)
    {
        var serverContext = Context.Create()
            .Set("environment", environment);

        var context = serverContext
            .ChildContext()
            .Set("is_authenticated", authenticated);

        var flags = GetDummyFlagService("0001.json");

        var solver = flags.WithContext(context);
        var value = solver.Get("log_level", "log_level", 2);
        Assert.Equal(expectedResult, value);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(2, false)]
    public void CanCalculateShowMessage(int userId, bool expectedResult)
    {
        var context = Context.Create()
            .Set("user_id", userId);

        var flags = GetDummyFlagService("0001.json");

        var solver = flags.WithContext(context);
        var value = solver.Get("show_odd_messages", "show_message", false);
        Assert.Equal(expectedResult, value);
    }

    [Theory]
    [InlineData(101, "You are very popular!")]
    [InlineData(49, "Try harder!")]
    [InlineData(100, "Keep Going!")]
    [InlineData(50, "Keep Going!")]
    public void EvaluatePopularity(int totalLikes, string expectedMessage)
    {
        var context = Context.Create()
            .Set("total_likes", totalLikes);

        var flags = GetDummyFlagService("0001.json");

        var solver = flags.WithContext(context);
        var value = solver.Get("praise_popularity", "message", "Keep Going!");
        Assert.Equal(expectedMessage, value);
    }
}
