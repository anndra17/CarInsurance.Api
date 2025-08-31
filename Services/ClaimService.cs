using CarInsurance.Api.Data;
using CarInsurance.Api.Dtos;
using CarInsurance.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CarInsurance.Api.Services
{
    public class ClaimService(AppDbContext db) : IClaimService
    {
        private readonly AppDbContext _db = db;

        public async Task<ClaimDto> CreateClaimAsync(long carId, CreateClaimRequest request)
        {
            if (request.Amount <= 0)
            {
                throw new ArgumentException("Amount must be greater than zero.");
            }

            if (!DateOnly.TryParse(request.ClaimDate, out var claimDate))
                throw new ArgumentException("Invalid claim date format. Use YYYY-MM-DD.");


            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var carExists = await _db.Cars.AnyAsync(c => c.Id == carId);
                if (!carExists)
                    throw new KeyNotFoundException($"Car with ID {carId} not found.");

                
                var claim = new Claim
                {
                    CarId = carId,
                    Description = request.Description,
                    Amount = request.Amount,
                    ClaimDate = claimDate,
                };

                _db.Claims.Add(claim);
                await _db.SaveChangesAsync();

                await transaction.CommitAsync();
                return new ClaimDto(
                claim.Id,
                claim.CarId,
                claim.Description,
                claim.Amount,
                claim.ClaimDate.ToString("yyyy-MM-dd")
            );
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<ClaimDto>> GetCarClaims(long carId)
        {
            var carExists = await _db.Cars.AnyAsync(c => c.Id == carId);
            if (!carExists)
                throw new KeyNotFoundException($"Car with ID {carId} not found.");

            return await _db.Claims 
                .Where(c => c.CarId == carId)
                .OrderByDescending(c => c.ClaimDate)
                .Select(c => new ClaimDto(
                    c.Id,
                    c.CarId,
                    c.Description,
                    c.Amount,
                    c.ClaimDate.ToString("yyyy-MM-dd")
                    ))
                .ToListAsync();
        }

        public async Task<ClaimDto?> GetClaimAsync(long claimId)
        {
            var claim = await _db.Claims
                .FirstOrDefaultAsync(c => c.Id == claimId);
            if (claim == null)
                return null;
            return new ClaimDto(
                claim.Id,
                claim.CarId,
                claim.Description,
                claim.Amount,
                claim.ClaimDate.ToString("yyyy-MM-dd")            
            );
        }

        public async Task<CarHistoryDto> GetCarHistoryAsync(long carId)
        {
            var carExists = await _db.Cars.AnyAsync(c => c.Id == carId);
            if (!carExists)
                throw new KeyNotFoundException($"Car with ID {carId} not found.");

            var policies = await _db.Policies
                .Where(p => p.CarId == carId)
                .Select(p => new CarHistoryItemDto(
                    "Policy",
                    p.StartDate.ToString("yyyy-MM-dd"),
                    p.EndDate.ToString("yyyy-MM-dd"),
                    $"Insurance policy with {p.Provider ?? "Unknown Provider"}",
                    null,
                    p.Provider,
                    p.Id
                ))
                .ToListAsync();

            var claims = await _db.Claims
                .Where(c => c.CarId == carId)
                .Select(c => new CarHistoryItemDto(
                    "Claim",
                    c.ClaimDate.ToString("yyyy-MM-dd"),
                    null,
                    c.Description,
                    c.Amount,
                    null,
                    c.Id
                ))
                .ToListAsync();

            var timeline = policies.Concat(claims)
                .OrderByDescending(item => DateTime.Parse(item.Date))
                .ToList();

            return new CarHistoryDto(carId, timeline);
        }
    }
}
