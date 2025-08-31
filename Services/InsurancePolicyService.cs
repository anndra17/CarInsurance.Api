using CarInsurance.Api.Data;
using CarInsurance.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace CarInsurance.Api.Services
{
    public class InsurancePolicyService(AppDbContext db)
    {
        private readonly AppDbContext _db = db;



    }
}
