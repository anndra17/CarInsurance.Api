using CarInsurance.Api.Dtos;
using CarInsurance.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CarInsurance.Api.Controllers
{
    [ApiController]
    [Route("api")]
    public class ClaimsController(IClaimService claimService) : ControllerBase
    {
        private readonly IClaimService _claimService = claimService;

        [HttpGet("cars/{carId:long}/history")]
        public async Task<ActionResult<CarHistoryDto>> GetCarHistory(long carId)
        {
            try
            {
                var history = await _claimService.GetCarHistoryAsync(carId);
                return Ok(history);
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Car {carId} not found");
            }
        }

        [HttpPost("cars/{carId:long}/claims")]
        public async Task<ActionResult<ClaimDto>> CreateClaim(
            long carId,
            [FromBody] CreateClaimRequest request)
        {
            try
            {
                var claim = await _claimService.CreateClaimAsync(carId, request);
                return CreatedAtAction(nameof(GetClaim), new { claimId = claim.Id }, claim);
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Car {carId} not found");
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

        [HttpGet("claims/{claimId:long}")]
        public async Task<ActionResult<ClaimDto>> GetClaim(long claimId)
        {
            var claim = await _claimService.GetClaimAsync(claimId);

            if (claim == null)
                return NotFound($"Claim {claimId} not found");

            return Ok(claim);
        }
    }
}
