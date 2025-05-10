using System;

namespace TinyFlags.Tests;

public class CachedFlagsetResolverTests : BaseTest
{
    [Fact]
    public async Task CanResolveOnSecondTry()
    {
        var flagServce = GetCachedDummyFlagService("0001.json");

        var context = Context.Create()
            .Set("environment", "prod")
            .Set("is_authenticated", true);

        {
            var evaluator = flagServce.WithContext(context);
            var result = evaluator.Get("log_level", "log_level", -3);
            Assert.Equal(-3, result);
        }

        // Now we "wait" for the fake delay to complete
        await Task.Delay(1000);

        // Now that the flags have had a chance to load, we should get the expected result.
        {
            var evaluator = flagServce.WithContext(context);
            var result = evaluator.Get("log_level", "log_level", -3);
            Assert.Equal(1, result);
        }


    }
}
