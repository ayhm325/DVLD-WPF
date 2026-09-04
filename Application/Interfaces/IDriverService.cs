using Application.Common.Results;
using Application.DTOs;
using Application.DTOs.DriverDTO;

namespace Application.Interfaces;

public interface IDriverService
{
    Task<Result<DriverDto>> GetByIdAsync(int id);
    Task<Result<List<DriverDto>>> GetAllAsync();
    Task<Result<DriverDto>> GetByPersonIdAsync(int personId);
    Task<Result<List<DriverDto>>> GetByCreatedUserIdAsync(int userId);

    Task<bool> ExistsByIdAsync(int driverId);
    Task<bool> ExistsByPersonIdAsync(int personId);

    Task<Result<int>> AddAsync(CreateDriverDto dto);
    Task<Result> UpdateAsync(UpdateDriverDto dto);
    Task<Result> DeleteAsync(int id);
}