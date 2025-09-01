using CarInsurance.Api.Dtos;
using CarInsurance.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CarInsurance.Api.Controllers;

[ApiController]
[Route("api")]
public class CarsController(ICarService service) : ControllerBase
{
    private readonly ICarService _service = service;

    [HttpGet("cars")]
    public async Task<ActionResult<List<CarDto>>> GetCars()
        => Ok(await _service.ListCarsAsync());

    [HttpGet("cars/{carId:long}/insurance-valid")]
    public async Task<ActionResult<InsuranceValidityResponse>> IsInsuranceValid(long carId, [FromQuery] string date)
    {
        if (string.IsNullOrWhiteSpace(date))
            return BadRequest("Date parameter is required. Use YYYY-MM-DD format.");

        if (!DateOnly.TryParse(date, out var parsed))
        {
            if (DateTime.TryParse(date, out var dateTime))
            {
                if (dateTime.Year < 1900 || dateTime.Year > 2100)
                    return BadRequest("Invalid date: Year must be between 1900 and 2100.");
            }
            return BadRequest("Invalid date format. Use YYYY-MM-DD format (e.g., 2024-03-15).");
        }

        try
        {
            var valid = await _service.IsInsuranceValidAsync(carId, parsed);
            return Ok(new InsuranceValidityResponse(carId, parsed.ToString("yyyy-MM-dd"), valid));
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Car with ID {carId} not found.");
        }
    }

    [HttpGet("cars/{carId:long}/policies")]
    public async Task<ActionResult<List<InsurancePolicyDto>>> GetCarPolicies(long carId)
    {
        try
        {
            var policies = await _service.GetCarPoliciesAsync(carId);
            return Ok(policies);
        }
        catch
        {
            return NotFound($"Car with ID {carId} not found.");
        }
    }

    [HttpPost("cars/{carId:long}/policies")]
    public async Task<ActionResult<InsurancePolicyDto>> CreatePolicy(
       long carId,
       [FromBody] CreatePolicyRequest request)
    {
        if (!DateOnly.TryParse(request.StartDate, out var startDate))
            return BadRequest("Invalid start date format. Use YYYY-MM-DD.");

        if (!DateOnly.TryParse(request.EndDate, out var endDate))
            return BadRequest("Invalid end date format. Use YYYY-MM-DD.");

        try
        {
            var policy = await _service.CreatePolicyAsync(carId, request.Provider, startDate, endDate);
            return CreatedAtAction(nameof(GetCarPolicies), new { carId }, policy);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Car with ID {carId} not found.");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }
}
