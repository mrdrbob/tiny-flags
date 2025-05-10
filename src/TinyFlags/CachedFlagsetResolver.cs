using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TinyFlags;

public class CachedFlagsetResolverSettings
{
    public TimeSpan TimeToLive { get; set; } = TimeSpan.FromMinutes(2);
    public int RetyDelayMilliseconds { get; set; } = 500;
}

public class CachedFlagsetResolver(
    IOptions<CachedFlagsetResolverSettings> resolverSettings,
    IFlagsetSource flagsetSource,
    ILogger<CachedFlagsetResolver> logger
) : IFlagsetResolver
{
    private record CachedFlagset(Flagset Flagset, DateTime Expiration);

    private CachedFlagset? _flagset = null;
    private bool _isLoading = false;
    private ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();


    public Flagset GetFlagset()
    {
        if (_flagset == null)
        {
            _lock.EnterWriteLock();
            try
            {
                if (!_isLoading)
                {
                    _isLoading = true;
                    Task.Run(LoadFreshFlagset);
                }

                logger.LogInformation("Initial request, loading flagset");
                return Flagset.Empty;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }


        _lock.EnterReadLock();
        try
        {
            if (_flagset is not null && _flagset.Expiration > DateTime.Now)
            {
                logger.LogInformation("Returning cached data. Cache time left: {0}.", DateTime.Now.Subtract(_flagset.Expiration).TotalSeconds);
                return _flagset.Flagset;
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }

        _lock.EnterWriteLock();
        try
        {
            if (!_isLoading)
            {
                _isLoading = true;
                Task.Run(LoadFreshFlagset);
            }

            if (_flagset is not null)
                return _flagset.Flagset;
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        logger.LogInformation("Returning empty flagset");
        return Flagset.Empty;
    }

    private async Task LoadFreshFlagset()
    {
        using var scope = logger.BeginScope("Loading Fresh Flagset");
        CachedFlagset? newFlagset = null;
        try
        {
            using var requestScope = logger.BeginScope("Requesting from Web");
            var parsedFlagset = await flagsetSource.LoadFlagsetAsync();
            newFlagset = new CachedFlagset(parsedFlagset, DateTime.Now.Add(resolverSettings.Value.TimeToLive));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception while loading updated flagset");
            // Poor man's throttling
            Thread.Sleep(resolverSettings.Value.RetyDelayMilliseconds);
        }
        finally
        {
            _lock.EnterWriteLock();
            try
            {
                if (newFlagset != null)
                    _flagset = newFlagset;
                _isLoading = false;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
    }

}
