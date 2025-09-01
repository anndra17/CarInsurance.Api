using CarInsurance.Api.Data;
using CarInsurance.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CarInsurance.Api.Services
{
    public class PolicyExpirationService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<PolicyExpirationService> _logger;
        private readonly TimeSpan _executionInterval = TimeSpan.FromMinutes(30);

        public PolicyExpirationService(IServiceScopeFactory serviceScopeFactory, ILogger<PolicyExpirationService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessRecentlyExpiredPoliciesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An unexpected error occurred while checking for expired insurance policies");
                }

                await Task.Delay(_executionInterval, cancellationToken);
            }
        }

        public async Task ProcessRecentlyExpiredPoliciesAsync()
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var utcNow = DateTime.UtcNow;
            var currentDate = DateOnly.FromDateTime(utcNow);

            var recentlyExpiredPolicies = await db.Policies
                .Include(p => p.Car)
                .ThenInclude(c => c.Owner)
                .Where(p => p.EndDate <= currentDate)
                .Where(p => !db.PolicyExpirations.Any(log => log.PolicyId == p.Id))
                .ToListAsync();

            foreach (var policy in recentlyExpiredPolicies)
            {
                var expirationDateTime = policy.EndDate.ToDateTime(TimeOnly.MinValue);
                var elapsedSinceExpiration = utcNow - expirationDateTime;

                if (elapsedSinceExpiration <= TimeSpan.FromDays(1) && elapsedSinceExpiration >= TimeSpan.Zero)
                {
                    _logger.LogWarning(
                        "Insurance policy {PolicyId} (Provider: {Provider}) for vehicle {CarVin} owned by {OwnerName} expired on {ExpirationDate:yyyy-MM-dd}.",
                        policy.Id,
                        policy.Provider,
                        policy.Car.Vin,
                        policy.Car.Owner.Name,
                        policy.EndDate);

                    var expirationLog = new PolicyExpiration
                    {
                        PolicyId = policy.Id,
                        ExpirationDate = policy.EndDate,
                        ProcessedAt = utcNow
                    };

                    db.PolicyExpirations.Add(expirationLog);
                }
            }

            await db.SaveChangesAsync();
        }
    }
}