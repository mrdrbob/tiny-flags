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
    private readonly HttpClient _client = new HttpClient();

    public async Task<Flagset> LoadFlagsetAsync()
    {
        if (string.IsNullOrEmpty(settings.Value.Url))
            throw new InvalidOperationException("HttpFlagsetSourceSettings URL is not set.");

        HttpResponseMessage response = await _client.GetAsync(settings.Value.Url);
        response.EnsureSuccessStatusCode();
        var rawJson = await response.Content.ReadAsStringAsync();
        var parsedFlagset = parser.Parse(rawJson);
        return parsedFlagset;
    }
}

