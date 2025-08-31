using CarInsurance.Api.Data;
using CarInsurance.Api.Dtos;
using CarInsurance.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CarInsurance.Api.Services;

public class CarService(AppDbContext db): ICarService
{
    private readonly AppDbContext _db = db;

    public async Task<List<CarDto>> ListCarsAsync()
    {
        return await _db.Cars.Include(c => c.Owner)
            .Select(c => new CarDto(c.Id, c.Vin, c.Make, c.Model, c.YearOfManufacture,
                                    c.OwnerId, c.Owner.Name, c.Owner.Email))
            .ToListAsync();
    }

    public async Task<bool> IsInsuranceValidAsync(long carId, DateOnly date)
    {
        var carExists = await _db.Cars.AnyAsync(c => c.Id == carId);
        if (!carExists) throw new KeyNotFoundException($"Car {carId} not found");

        return await _db.Policies.AnyAsync(p =>
            p.CarId == carId &&
            p.StartDate <= date &&
            p.EndDate >= date
        );
    }

    public async Task<List<InsurancePolicyDto>> GetCarPoliciesAsync(long carId)
    {
        var carExists = await _db.Cars.AnyAsync(c => c.Id == carId);
        if (!carExists) throw new KeyNotFoundException($"Car {carId} not found");

        return await _db.Policies
            .Where(p => p.CarId == carId)
            .Select(p => new InsurancePolicyDto(
                p.Id,
                p.CarId,
                p.Provider,
                p.StartDate.ToString("yyyy-MM-dd"),
                p.EndDate.ToString("yyyy-MM-dd") 
            ))
            .ToListAsync();
    }

    public async Task<InsurancePolicyDto> CreatePolicyAsync(long carId, string? provider, DateOnly startDate, DateOnly endDate)
    {
        if (startDate >= endDate)
            throw new ArgumentException("Start date must be before end date");

        var carExists = await _db.Cars.AnyAsync(C => C.Id == carId);
        if (!carExists) throw new KeyNotFoundException($"Car {carId} not found");

        var hasOverlap = await _db.Policies.AnyAsync(p =>
            p.CarId == carId &&
            ((startDate >= p.StartDate && startDate <= p.EndDate) ||
             (endDate >= p.StartDate && endDate <= p.EndDate) ||
             (startDate <= p.StartDate && endDate >= p.EndDate))
        );

        if (hasOverlap)
            throw new InvalidOperationException("Policy dates overlap with existing policy");

        var policy = new InsurancePolicy
        {
            CarId = carId,
            Provider = provider,
            StartDate = startDate,
            EndDate = endDate
        };

        _db.Policies.Add(policy);
        await _db.SaveChangesAsync();

        return new InsurancePolicyDto(
            policy.Id,
            policy.CarId,
            policy.Provider,
            policy.StartDate.ToString("yyyy-MM-dd"),
            policy.EndDate.ToString("yyyy-MM-dd")
        );
    }
}
