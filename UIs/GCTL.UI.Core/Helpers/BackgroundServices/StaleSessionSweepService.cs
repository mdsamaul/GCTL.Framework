using GCTL.Core.Data;
using GCTL.Data.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace GCTL.UI.Core.Helpers.BackgroundServices
{
    public class StaleSessionSweepService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _sessionTimeout;

        public StaleSessionSweepService(IServiceScopeFactory scopeFactory, IMemoryCache cache, IConfiguration configuration)
        {
            _scopeFactory = scopeFactory;
            _cache = cache;
            int timeoutMinutes = configuration.GetValue<int>("SessionSettings:IdleTimeoutMinutes", 5);
            _sessionTimeout = TimeSpan.FromMinutes(timeoutMinutes);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(2));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
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
                                    log.LogoutDate = lastActivity;
                                    await logRepo.UpdateAsync(log);
                                    _cache.Remove(cacheKey);
                                }
                            }
                            else
                            {
                                // Cache key expired or missing. If login was before cutoff, close the stale session.
                                if (log.LoginDate.HasValue && log.LoginDate.Value < cutoff)
                                {
                                    log.LogoutDate = log.LoginDate.Value.Add(_sessionTimeout);
                                    await logRepo.UpdateAsync(log);
                                }
                            }
                        }
                        else if (log.LoginDate.HasValue && log.LoginDate.Value < cutoff)
                        {
                            log.LogoutDate = log.LoginDate.Value.Add(_sessionTimeout);
                            await logRepo.UpdateAsync(log);
                        }
                    }
                }
                catch
                {
                    // Best-effort background service; avoid unhandled background crash
                }
            }
        }
    }
}
