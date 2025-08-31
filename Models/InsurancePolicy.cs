namespace CarInsurance.Api.Models;
using System.ComponentModel.DataAnnotations;

public class InsurancePolicy
{
    public long Id { get; set; }

    public long CarId { get; set; }
    public Car Car { get; set; } = default!;

    public string? Provider { get; set; }

    [Required]
    public DateOnly StartDate { get; set; }
    
    [Required]
    public DateOnly EndDate { get; set; } 

}
