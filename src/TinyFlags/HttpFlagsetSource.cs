using Microsoft.Extensions.Options;

namespace TinyFlags;

public class HttpFlagsetSourceSettings
{
    public string? Url { get; set; }
}

public class HttpFlagsetSource(
    IOptions<HttpFlagsetSourceSettings> settings,
    IFlagsetParser parser
) : IFlagsetSource
{
    protected readonly HttpClient httpClient = new HttpClient();

    public virtual async Task<Flagset> LoadFlagsetAsync()
    {
        if (string.IsNullOrEmpty(settings.Value.Url))
            throw new InvalidOperationException("HttpFlagsetSourceSettings URL is not set.");

        HttpResponseMessage response = await httpClient.GetAsync(settings.Value.Url);
        response.EnsureSuccessStatusCode();
        var rawJson = await response.Content.ReadAsStringAsync();
        var parsedFlagset = parser.Parse(rawJson);
        return parsedFlagset;
    }
}

public class SecureFlagsetSourceSettings : HttpFlagsetSourceSettings
{
    public string? ApiKey { get; set; }
}

public class SecureFlagsetSource : HttpFlagsetSource
{
    public SecureFlagsetSource(IOptions<SecureFlagsetSourceSettings> settings, IFlagsetParser parser) 
        : base(settings, parser)
    {
        if (!string.IsNullOrEmpty(settings.Value.ApiKey))
        {
            this.httpClient.DefaultRequestHeaders.Add("X-API-Key", settings.Value.ApiKey);
        }
    }
}

