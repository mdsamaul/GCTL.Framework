using GCTL.Core.Data;
using GCTL.Data.Models;
using Microsoft.Extensions.Caching.Memory;

namespace GCTL.UI.Core.Helpers.BackgroundServices
{
    public class StaleSessionSweepService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _sessionTimeout = TimeSpan.FromMinutes(30);

        public StaleSessionSweepService(IServiceScopeFactory scopeFactory, IMemoryCache cache)
        {
            _scopeFactory = scopeFactory;
            _cache = cache;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                using var scope = _scopeFactory.CreateScope();
                var logRepo = scope.ServiceProvider.GetRequiredService<IRepository<CoreUserLog>>();

                var cutoff = DateTime.Now - _sessionTimeout;
                var openLogs = await logRepo.FindByAsync(l => l.LogoutDate == null);

                foreach (var log in openLogs.ToList())
                {
                    if (!string.IsNullOrEmpty(log.SessionId))
                    {
                        var cacheKey = $"UserSession_LastActivity_{log.SessionId}";
                        if (_cache.TryGetValue(cacheKey, out DateTime lastActivity))
                        {
                            if (lastActivity < cutoff)
                            {
                                log.LogoutDate = lastActivity.Add(_sessionTimeout);
                                await logRepo.UpdateAsync(log);
                            }
                        }
                        else
                        {
                            // Cache key lost due to App Domain / Dev server restart.
                            // Re-initialize cache timestamp to current time so active user session is preserved.
                            _cache.Set(cacheKey, DateTime.Now, TimeSpan.FromHours(1));
                        }
                    }
                    else if (log.LoginDate.HasValue && log.LoginDate.Value < cutoff)
                    {
                        log.LogoutDate = log.LoginDate.Value.Add(_sessionTimeout);
                        await logRepo.UpdateAsync(log);
                    }
                }
            }
        }
    }
}
