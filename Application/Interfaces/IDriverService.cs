using Application.Common.Results;
using Application.DTOs;
using Application.DTOs.DriverDTO;

namespace Application.Interfaces;

public interface IDriverService
{
    // =========================================================
    // GET
    // =========================================================

    Task<Result<DriverDto>>
        GetByIdAsync(int id);

    Task<Result<List<DriverDto>>>
        GetAllAsync();

    Task<Result<DriverDto>>
        GetByPersonIdAsync(int personId);

    Task<Result<List<DriverDto>>>
        GetByCreatedUserIdAsync(int userId);


    // =========================================================
    // CHECKS
    // =========================================================

    Task<bool>
        ExistsByIdAsync(int driverId);

    Task<bool>
        ExistsByPersonIdAsync(int personId);


    // =========================================================
    // COMMANDS
    // =========================================================

    Task<Result<int>>
        AddAsync(CreateDriverDto dto);

    Task<Result>
        UpdateAsync(UpdateDriverDto dto);

    Task<Result>
        DeleteAsync(int id);
}