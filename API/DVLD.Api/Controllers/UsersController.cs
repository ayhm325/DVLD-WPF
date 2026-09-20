using Application.DTOs.UserDTO;
using Application.Interfaces;
using DVLD.Api.Results;
using DVLD.Contracts.User;
using DVLD.Contracts.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVLD.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/[controller]")]
public sealed class UsersController(
    IUserService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result =
            await service.GetAllUsersAsync();

        if (result.IsFailure)
            return result.ToActionResult(this);

        return Ok(
            result.Value!
                .Select(MapToResponse)
                .ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result =
            await service.GetUserByIdAsync(id);

        if (result.IsFailure)
            return result.ToActionResult(this);

        return Ok(
            MapToResponse(result.Value!));
    }

    [HttpGet("person/{personId:int}")]
    public async Task<IActionResult> GetByPersonId(
        int personId)
    {
        var result =
            await service.GetUserByPersonIdAsync(personId);

        if (result.IsFailure)
            return result.ToActionResult(this);

        return Ok(
            MapToResponse(result.Value!));
    }

    [HttpGet("{id:int}/details")]
    public async Task<IActionResult> GetDetails(int id)
    {
        var result =
            await service.GetUserDetailsAsync(id);

        if (result.IsFailure)
            return result.ToActionResult(this);

        var dto = result.Value!;

        var response = new UserDetailsResponse(
            dto.UserId,
            dto.PersonId,
            dto.UserName,
            dto.IsActive,
            dto.Role,
            dto.NationalNo,
            dto.FirstName,
            dto.SecondName,
            dto.ThirdName,
            dto.LastName,
            dto.DateOfBirth,
            dto.Gender,
            dto.Address,
            dto.Phone,
            dto.Email,
            dto.NationalityCountryID,
            dto.CountryName,
            dto.ImagePath);

        return Ok(response);
    }

    [HttpGet("username/{username}")]
    public async Task<IActionResult> GetByUsername(
        string username)
    {
        var result =
            await service.GetUserByUsernameAsync(username);

        if (result.IsFailure)
            return result.ToActionResult(this);

        return Ok(
            MapToResponse(result.Value!));
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserRequest request)
    {
        var result =
            await service.AddUserAsync(
                new CreateUserDto
                {
                    PersonId = request.PersonId,
                    UserName = request.UserName,
                    Password = request.Password,
                    IsActive = request.IsActive
                });

        if (result.IsFailure)
            return result.ToActionResult(this);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value },
            result.Value);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateUserRequest request)
    {
        var result =
            await service.UpdateUserAsync(
                id,
                new UpdateUserDto
                {
                    PersonId = request.PersonId,
                    UserName = request.UserName,
                    IsActive = request.IsActive
                });

        return result.IsSuccess
            ? NoContent()
            : result.ToActionResult(this);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result =
            await service.DeleteUserAsync(id);

        return result.IsSuccess
            ? NoContent()
            : result.ToActionResult(this);
    }

    private static UserResponse MapToResponse(
        UserDto dto)
        => new(
            UserId: dto.UserId,
            PersonId: dto.PersonId,
            UserName: dto.UserName,
            IsActive: dto.IsActive,
            PersonFullName: dto.FullName);
}