using Application.Common.Results;
using Application.DTOs.UserDTO;
using Application.Interfaces;
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

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result =
            await service.GetUserByIdAsync(id);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    [HttpGet("person/{personId:int}")]
    public async Task<IActionResult> GetByPersonId(
        int personId)
    {
        var result =
            await service.GetUserByPersonIdAsync(
                personId);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    [HttpGet("username/{username}")]
    public async Task<IActionResult> GetByUsername(
        string username)
    {
        var result =
            await service.GetUserByUsernameAsync(
                username);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserDto dto)
    {
        var result =
            await service.AddUserAsync(dto);

        if (result.IsFailure)
            return HandleFailure(result);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value },
            new { userId = result.Value });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateUserDto dto)
    {
        var result =
            await service.UpdateUserAsync(
                id,
                dto);

        return result.IsSuccess
            ? NoContent()
            : HandleFailure(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result =
            await service.DeleteUserAsync(id);

        return result.IsSuccess
            ? NoContent()
            : HandleFailure(result);
    }

    private static IActionResult HandleFailure(
        Result result)
    {
        return result.ErrorType switch
        {
            ErrorType.Validation =>
                new BadRequestObjectResult(
                    new { error = result.Error }),

            ErrorType.NotFound =>
                new NotFoundObjectResult(
                    new { error = result.Error }),

            ErrorType.Conflict =>
                new ConflictObjectResult(
                    new { error = result.Error }),

            ErrorType.Forbidden =>
                new ObjectResult(
                    new { error = result.Error })
                {
                    StatusCode =
                        StatusCodes.Status403Forbidden
                },

            _ =>
                new ObjectResult(
                    new { error = result.Error })
                {
                    StatusCode =
                        StatusCodes.Status500InternalServerError
                }
        };
    }
}