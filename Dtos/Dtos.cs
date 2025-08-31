namespace CarInsurance.Api.Dtos;

public record CarDto(long Id, string Vin, string? Make, string? Model, int Year, long OwnerId, string OwnerName, string? OwnerEmail);
public record InsuranceValidityResponse(long CarId, string Date, bool Valid);
public record InsurancePolicyDto(long Id, long CarId, string? Provider, string StartDate, string EndDate);
public record CreatePolicyRequest(string? Provider, string StartDate, string EndDate);
