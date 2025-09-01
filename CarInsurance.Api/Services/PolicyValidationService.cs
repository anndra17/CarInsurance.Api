using CarInsurance.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CarInsurance.Api.Services
{
    public class PolicyValidationService(AppDbContext db): IPolicyValidationService
    {
        private readonly AppDbContext _db = db;

        public async Task<bool> CarExistsAsync(long carId)
        {
            // Using NoTracking for read-only operations to improve performance and avoid side effects
            return await _db.Cars.AsNoTracking().AnyAsync(c => c.Id == carId);
        }

        public async Task<bool> ValidateInsuranceCoverageAsync(long carId, DateOnly date)
        {
            return await _db.Policies
                .AsNoTracking()
                .AnyAsync(p => 
                p.CarId == carId && 
                p.StartDate <= date && 
                p.EndDate >= date);
        }
    }
}
