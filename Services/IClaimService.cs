using CarInsurance.Api.Dtos;

namespace CarInsurance.Api.Services
{
    public interface IClaimService
    {
        Task<ClaimDto> CreateClaimAsync(long carId, CreateClaimRequest request, DateOnly claimDate);
        Task<List<ClaimDto>> GetCarClaims(long carId);
        Task<ClaimDto?> GetClaimAsync(long claimId);
        Task<CarHistoryDto> GetCarHistoryAsync(long carId);

    }
}
