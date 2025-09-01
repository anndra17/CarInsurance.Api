﻿using System.ComponentModel.DataAnnotations;

namespace CarInsurance.Api.Models
{
    public class Claim
    {
        public long Id { get; set; }

        public long CarId { get; set; }
        public Car Car { get; set; } = default!;


        [Required]
        public string? Description { get; set; }
        [Required]
        public decimal Amount { get; set; }
        [Required]
        public DateOnly ClaimDate { get; set; }

    }
}
