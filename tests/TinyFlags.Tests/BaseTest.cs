
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace TinyFlags.Tests;

public abstract class BaseTest
{
    public class DummyFlagResolver(Flagset flags) : IFlagsetResolver
    {
        public Flagset GetFlagset() => flags;
    }

    public class DummyFlagSource(Flagset flags, int delay) : IFlagsetSource
    {
        public async Task<Flagset> LoadFlagsetAsync()
        {
            await Task.Delay(delay);
            return flags;
        }
    }

    public Flagset GetParsedFlagset(string fileName = "0001.json")
    {
        using var stream = GetType().Assembly.GetManifestResourceStream($"TinyFlags.Tests.{fileName}");
        using var reader = new StreamReader(stream!);

        var rawJson = reader.ReadToEnd();
        var parser = new JsonFlagsetParser();

        return parser.Parse(rawJson);
    }

    public FlagsService GetDummyFlagService(string fileName = "0001.json")
    { 
        var resolver = new DummyFlagResolver(GetParsedFlagset(fileName));
        return new FlagsService(resolver);
    }

    public FlagsService GetCachedDummyFlagService(string fileName = "0001.json")
    {
        var source = new DummyFlagSource(GetParsedFlagset(fileName), 100);
        var nullLogger = new NullLogger<CachedFlagsetResolver>();

        var resolverSettings = new OptionsWrapper<CachedFlagsetResolverSettings>(new CachedFlagsetResolverSettings
        {
            RetyDelayMilliseconds = 100,
            TimeToLive = TimeSpan.FromSeconds(10)
        });

        var resolver = new CachedFlagsetResolver(resolverSettings, source, nullLogger);
        return new FlagsService(resolver);
    }


}
