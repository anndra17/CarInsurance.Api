using CarInsurance.Api.Controllers;
using CarInsurance.Api.Data;
using CarInsurance.Api.Dtos;
using CarInsurance.Api.Models;
using CarInsurance.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CarInsurance.Api.Tests
{
    public class InsuranceValidationTests: IDisposable
    {
        private readonly AppDbContext _context;
        private readonly IPolicyValidationService _policyValidationService;
        private readonly ICarService _carService;
        private readonly CarsController _controller;


        public InsuranceValidationTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);

            _policyValidationService = new PolicyValidationService(_context);
            _carService = new CarService(_context, _policyValidationService);
            _controller = new CarsController(_carService);

            SeedTestData();
        }

        private void SeedTestData()
        {
            var owner = new Owner { Id = 1, Name = "Test Owner", Email = "test@example.com" };
            var car = new Car
            {
                Id = 1,
                Vin = "TEST123",
                Make = "Toyota",
                Model = "Camry",
                YearOfManufacture = 2020,
                OwnerId = 1,
                Owner = owner
            };

            var policy = new InsurancePolicy
            {
                Id = 1,
                CarId = 1,
                Provider = "TestInsurance",
                StartDate = new DateOnly(2024, 1, 1),  // Coverage: Jan 1, 2024 to Dec 31, 2024
                EndDate = new DateOnly(2024, 12, 31)
            };

            _context.Owners.Add(owner);
            _context.Cars.Add(car);
            _context.Policies.Add(policy);
            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        // ============= CONTROLLER VALIDATION TESTS =============

        [Fact]
        public async Task IsInsuranceValid_CarNotFound_Returns404()
        {
            // Arrange
            long nonExistentCarId = 999;
            string validDate = "2024-06-15";

            // Act
            var result = await _controller.IsInsuranceValid(nonExistentCarId, validDate);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal($"Car with ID {nonExistentCarId} not found.", notFoundResult.Value);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task IsInsuranceValid_EmptyDate_Returns400(string emptyDate)
        {
            // Act
            var result = await _controller.IsInsuranceValid(1, emptyDate);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Date parameter is required. Use YYYY-MM-DD format.", badRequestResult.Value);
        }

        [Theory]
        [InlineData("invalid-date")]
        [InlineData("2024-13-01")]  // Invalid month
        [InlineData("2024-02-30")]  // Invalid day for February
        [InlineData("2024/01/01")]  // Wrong format
        [InlineData("01-01-2024")]  // Wrong format
        public async Task IsInsuranceValid_InvalidDateFormat_Returns400(string invalidDate)
        {
            // Act
            var result = await _controller.IsInsuranceValid(1, invalidDate);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("Invalid date format", badRequestResult.Value?.ToString());
        }


        [Theory]
        [InlineData("1800-01-01")]  // Too far in past
        [InlineData("2300-01-01")]  // Too far in future
        public async Task IsInsuranceValid_UnreasonableDateRange_Returns400(string unreasonableDate)
        {
            // Act
            var result = await _controller.IsInsuranceValid(1, unreasonableDate);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("Year must be between 1900 and 2200", badRequestResult.Value?.ToString());
        }


        // ============= BOUNDARY CASE TESTS =============
        [Fact]
        public async Task IsInsuranceValid_DateExactlyEqualToStartDate_ReturnsTrue()
        {
            // Arrange - Policy starts on 2024-01-01
            string startDate = "2024-01-01";

            // Act
            var result = await _controller.IsInsuranceValid(1, startDate);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<InsuranceValidityResponse>(okResult.Value);
            Assert.True(response.Valid, "Insurance should be valid on the exact start date");
            Assert.Equal(1, response.CarId);
            Assert.Equal(startDate, response.Date);
        }

        [Fact]
        public async Task IsInsuranceValid_DateExactlyEqualToEndDate_ReturnsTrue()
        {
            // Arrange - Policy ends on 2024-12-31
            string endDate = "2024-12-31";

            // Act
            var result = await _controller.IsInsuranceValid(1, endDate);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<InsuranceValidityResponse>(okResult.Value);
            Assert.True(response.Valid, "Insurance should be valid on the exact end date");
            Assert.Equal(1, response.CarId);
            Assert.Equal(endDate, response.Date);
        }

        [Fact]
        public async Task IsInsuranceValid_DateJustBeforeStartDate_ReturnsFalse()
        {
            // Arrange - One day before policy starts (2023-12-31)
            string beforeStartDate = "2023-12-31";

            // Act
            var result = await _controller.IsInsuranceValid(1, beforeStartDate);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<InsuranceValidityResponse>(okResult.Value);
            Assert.False(response.Valid, "Insurance should NOT be valid one day before start date");
            Assert.Equal(1, response.CarId);
            Assert.Equal(beforeStartDate, response.Date);
        }

        [Fact]
        public async Task IsInsuranceValid_DateJustAfterEndDate_ReturnsFalse()
        {
            // Arrange - One day after policy ends (2025-01-01)
            string afterEndDate = "2025-01-01";

            // Act
            var result = await _controller.IsInsuranceValid(1, afterEndDate);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<InsuranceValidityResponse>(okResult.Value);
            Assert.False(response.Valid, "Insurance should NOT be valid one day after end date");
            Assert.Equal(1, response.CarId);
            Assert.Equal(afterEndDate, response.Date);
        }

        [Fact]
        public async Task IsInsuranceValid_DateWellWithinCoverage_ReturnsTrue()
        {
            // Arrange - Date well within coverage period
            string withinCoverageDate = "2024-06-15";

            // Act
            var result = await _controller.IsInsuranceValid(1, withinCoverageDate);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<InsuranceValidityResponse>(okResult.Value);
            Assert.True(response.Valid, "Insurance should be valid within coverage period");
        }

        // ============= SERVICE LAYER TESTS (SOLID PRINCIPLES) =============

        [Fact]
        public async Task PolicyValidationService_CarExistsAsync_ExistingCar_ReturnsTrue()
        {
            // Arrange & Act
            var exists = await _policyValidationService.CarExistsAsync(1);

            // Assert
            Assert.True(exists);
        }

        [Fact]
        public async Task PolicyValidationService_CarExistsAsync_NonExistentCar_ReturnsFalse()
        {
            // Arrange & Act
            var exists = await _policyValidationService.CarExistsAsync(999);

            // Assert
            Assert.False(exists);
        }

        [Fact]
        public async Task PolicyValidationService_ValidateInsuranceCoverageAsync_BoundaryStartDate_ReturnsTrue()
        {
            // Arrange
            var startDate = new DateOnly(2024, 1, 1);

            // Act
            var isValid = await _policyValidationService.ValidateInsuranceCoverageAsync(1, startDate);

            // Assert
            Assert.True(isValid, "Coverage should include start date boundary");
        }

        [Fact]
        public async Task PolicyValidationService_ValidateInsuranceCoverageAsync_BoundaryEndDate_ReturnsTrue()
        {
            // Arrange
            var endDate = new DateOnly(2024, 12, 31);

            // Act
            var isValid = await _policyValidationService.ValidateInsuranceCoverageAsync(1, endDate);

            // Assert
            Assert.True(isValid, "Coverage should include end date boundary");
        }

        // ============= INTEGRATION TESTS =============

        [Fact]
        public async Task IsInsuranceValid_ConcurrentRequests_ReturnConsistentResults()
        {
            // Arrange
            var tasks = new List<Task<ActionResult<InsuranceValidityResponse>>>();
            var testDate = "2024-06-15";

            // Act
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(_controller.IsInsuranceValid(1, testDate));
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            foreach (var result in results)
            {
                var okResult = Assert.IsType<OkObjectResult>(result.Result);
                var response = Assert.IsType<InsuranceValidityResponse>(okResult.Value);
                Assert.True(response.Valid);
                Assert.Equal(1, response.CarId);
                Assert.Equal(testDate, response.Date);
            }
        }

        // ============= MULTIPLE POLICIES EDGE CASES =============

        [Fact]
        public async Task IsInsuranceValid_MultipleOverlappingPolicies_ReturnsTrue()
        {
            // Arrange - Add overlapping policy for edge case testing
            var overlappingPolicy = new InsurancePolicy
            {
                Id = 2,
                CarId = 1,
                Provider = "AnotherInsurance",
                StartDate = new DateOnly(2024, 6, 1),
                EndDate = new DateOnly(2024, 12, 31)
            };
            _context.Policies.Add(overlappingPolicy);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.IsInsuranceValid(1, "2024-07-15");

            // Assert - Should still return true as ANY matching policy makes it valid
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<InsuranceValidityResponse>(okResult.Value);
            Assert.True(response.Valid);
        }

        [Fact]
        public async Task IsInsuranceValid_NoPoliciesForCar_ReturnsFalse()
        {
            // Arrange - Add car without policies
            var carWithoutPolicies = new Car
            {
                Id = 2,
                Vin = "TEST456",
                Make = "Honda",
                Model = "Civic",
                YearOfManufacture = 2021,
                OwnerId = 1
            };
            _context.Cars.Add(carWithoutPolicies);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.IsInsuranceValid(2, "2024-06-15");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<InsuranceValidityResponse>(okResult.Value);
            Assert.False(response.Valid, "Car with no policies should not be valid");
        }
    }
}
