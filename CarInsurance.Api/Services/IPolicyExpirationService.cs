using CarInsurance.Api.Models;

namespace CarInsurance.Api.Services
{
    public interface IPolicyExpirationService
    {
        Task ProcessExpiredPoliciesAsync(CancellationToken cancellationToken = default);
        Task<List<InsurancePolicy>> GetRecentlyExpiredPoliciesAsync(DateTime cutoffTime, CancellationToken cancellationToken = default);
        Task MarkPolicyAsProcessedAsync(long policyId, string logMessage, CancellationToken cancellationToken = default);
    }
}
