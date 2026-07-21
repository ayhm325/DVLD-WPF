using Application.Common.Results;
using Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface ICountryService
    {
        Task<Result<List<Country>>> GetAllCountriesAsync();
    }
}