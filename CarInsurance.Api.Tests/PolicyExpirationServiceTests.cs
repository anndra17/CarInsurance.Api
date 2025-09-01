using CarInsurance.Api.Data;
using CarInsurance.Api.Models;
using CarInsurance.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CarInsurance.Api.Tests.Services;

public class PolicyExpirationServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly ILogger<PolicyExpirationService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    private Owner _owner1;
    private Owner _owner2;
    private Car _car1;
    private Car _car2;
    private InsurancePolicy _policy1;
    private InsurancePolicy _policy2;

    public PolicyExpirationServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
           .UseInMemoryDatabase(Guid.NewGuid().ToString())
           .Options;
        _db = new AppDbContext(options);

        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
        });
        _logger = loggerFactory.CreateLogger<PolicyExpirationService>();

        // Create a real service provider for testing
        var services = new ServiceCollection();
        services.AddSingleton(_db);
        var serviceProvider = services.BuildServiceProvider();
        _serviceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        SeedTestData().Wait();
    }

    private async Task SeedTestData()
    {
        _owner1 = new Owner { Name = "Test Owner 1", Email = "owner1@example.com" };
        _owner2 = new Owner { Name = "Test Owner 2", Email = "owner2@example.com" };
        _db.Owners.AddRange(_owner1, _owner2);
        await _db.SaveChangesAsync();

        _car1 = new Car { Vin = "VIN12345", Make = "Dacia", Model = "Logan", YearOfManufacture = 2018, OwnerId = _owner1.Id };
        _car2 = new Car { Vin = "VIN67890", Make = "VW", Model = "Golf", YearOfManufacture = 2021, OwnerId = _owner2.Id };
        _db.Cars.AddRange(_car1, _car2);
        await _db.SaveChangesAsync();

        _policy1 = new InsurancePolicy { CarId = _car1.Id, Provider = "Allianz", StartDate = new DateOnly(2024, 1, 1), EndDate = new DateOnly(2024, 12, 31) };
        _policy2 = new InsurancePolicy { CarId = _car2.Id, Provider = "Groupama", StartDate = new DateOnly(2025, 1, 1), EndDate = new DateOnly(2025, 12, 31) };
        _db.Policies.AddRange(_policy1, _policy2);
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task Service_CanBeInstantiated()
    {
        var service = new PolicyExpirationService(_serviceScopeFactory, _logger);
        await service.ProcessRecentlyExpiredPoliciesAsync();
        Assert.True(true);
    }

    [Fact]
    public async Task LogsPolicyExpiredWithinLastHour()
    {
        _policy1.EndDate = DateOnly.FromDateTime(DateTime.UtcNow);
        _db.Policies.Update(_policy1);
        await _db.SaveChangesAsync();

        var service = new PolicyExpirationService(_serviceScopeFactory, _logger);
        await service.ProcessRecentlyExpiredPoliciesAsync();

        var logCount = await _db.PolicyExpirations.CountAsync();
        Assert.Equal(1, logCount);

        var expirationLog = await _db.PolicyExpirations.FirstAsync();
        Assert.Equal(_policy1.Id, expirationLog.PolicyId);
        Assert.Equal(_policy1.EndDate, expirationLog.ExpirationDate);
    }

    [Fact]
    public async Task DoesNotLogPoliciesExpiredMoreThanAnHourAgo()
    {
        _policy1.EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2));
        _db.Policies.Update(_policy1);
        await _db.SaveChangesAsync();

        var service = new PolicyExpirationService(_serviceScopeFactory, _logger);

        await service.ProcessRecentlyExpiredPoliciesAsync();

        var logCount = await _db.PolicyExpirations.CountAsync();
        Assert.Equal(0, logCount);
    }

    [Fact]
    public async Task DoesNotDuplicateProcessedPolicies()
    {
        _policy1.EndDate = DateOnly.FromDateTime(DateTime.UtcNow);
        _db.Policies.Update(_policy1);
        await _db.SaveChangesAsync();

        _db.PolicyExpirations.Add(new PolicyExpiration
        {
            PolicyId = _policy1.Id,
            ExpirationDate = _policy1.EndDate,
            ProcessedAt = DateTime.UtcNow.AddMinutes(-10)
        });
        await _db.SaveChangesAsync();

        var service = new PolicyExpirationService(_serviceScopeFactory, _logger);
        await service.ProcessRecentlyExpiredPoliciesAsync();

        var logCount = await _db.PolicyExpirations.CountAsync();
        Assert.Equal(1, logCount);
    }

    [Fact]
    public async Task IgnoresFuturePolicies()
    {
        _policy2.EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)); // expiră în viitor
        _db.Policies.Update(_policy2);
        await _db.SaveChangesAsync();

        var service = new PolicyExpirationService(_serviceScopeFactory, _logger);

        await service.ProcessRecentlyExpiredPoliciesAsync();

        var logCount = await _db.PolicyExpirations.CountAsync();
        Assert.Equal(0, logCount);
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}