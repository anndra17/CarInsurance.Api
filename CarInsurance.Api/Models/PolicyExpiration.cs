using System.ComponentModel.DataAnnotations;

namespace CarInsurance.Api.Models
{
    public class PolicyExpiration
    {
        public long Id { get; set; }

        [Required]
        public long PolicyId { get; set; }
        public InsurancePolicy Policy { get; set; } = default!;

        [Required]
        public DateOnly ExpirationDate { get; set; }

        [Required]
        public DateTime ProcessedAt { get; set; }

        public string? LogMessage { get; set; }
    }

    
}
