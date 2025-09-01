namespace CarInsurance.Api.Services
{
    public interface IPolicyValidationService
    {
        Task<bool> ValidateInsuranceCoverageAsync(long carId, DateOnly date);
        Task<bool> CarExistsAsync(long carId);
    }
}
