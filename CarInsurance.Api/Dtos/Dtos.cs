namespace CarInsurance.Api.Dtos;

public record CarDto(long Id, string Vin, string? Make, string? Model, int Year, long OwnerId, string OwnerName, string? OwnerEmail);
public record InsuranceValidityResponse(long CarId, string Date, bool Valid);
public record InsurancePolicyDto(long Id, long CarId, string? Provider, string StartDate, string EndDate);
public record CreatePolicyRequest(string? Provider, string StartDate, string EndDate);

public record ClaimDto(long Id, long CarId, string? Description, decimal Amount, string ClaimDate);
public record CreateClaimRequest( string? Description, decimal Amount, string ClaimDate);
public record CarHistoryItemDto(string Type,string Date, string? EndDate, string? Description, decimal? Amount, string? Provider, long ItemId);
public record CarHistoryDto(long CarId,List<CarHistoryItemDto> Timeline);