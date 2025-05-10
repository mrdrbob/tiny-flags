namespace TinyFlags;

/// <summary>
/// Parses a string into a Flagset
/// </summary>
public interface IFlagsetParser
{
    Flagset Parse(string value);
}

/// <summary>
/// A quick, cached, safe serice to get the current set of flags.
/// May return empty or stale data if fresh data is delayed or unavailable.
/// </summary>
public interface IFlagsetResolver
{
    Flagset GetFlagset();
}

/// <summary>
/// A slow, non-cached source of flagset data, generally fetching
/// over a network or other async path. Typically used by a
/// IFlagsetResolver to fetch fresh data in a background thread.
/// </summary>
public interface IFlagsetSource
{
    Task<Flagset> LoadFlagsetAsync();
}
