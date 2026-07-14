using GCTL.Core.Data;
using GCTL.Data.Models;

namespace GCTL.UI.Core.Helpers.BackgroundServices
{
    public class StaleSessionSweepService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly TimeSpan _sessionTimeout = TimeSpan.FromMinutes(20); // match cookie ExpireTimeSpan

        public StaleSessionSweepService(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                using var scope = _scopeFactory.CreateScope();
                var logRepo = scope.ServiceProvider.GetRequiredService<IRepository<CoreUserLog>>();

                var cutoff = DateTime.Now - _sessionTimeout;
                var stale = await logRepo.FindByAsync(l => l.LogoutDate == null && l.LoginDate < cutoff);

                foreach (var log in stale.ToList())
                {
                    log.LogoutDate = log.LoginDate!.Value.Add(_sessionTimeout);
                    await logRepo.UpdateAsync(log);
                }
            }
        }
    }
}
