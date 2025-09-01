using CarInsurance.Api.Dtos;

namespace CarInsurance.Api.Services
{
    public interface ICarService
    {
        Task<List<CarDto>> ListCarsAsync();
        Task<bool> IsInsuranceValidAsync(long carId, DateOnly date);
        Task<List<InsurancePolicyDto>> GetCarPoliciesAsync(long carId);
        Task<InsurancePolicyDto> CreatePolicyAsync(long carId, string? provider, DateOnly startDate, DateOnly endDate);
    }
}
