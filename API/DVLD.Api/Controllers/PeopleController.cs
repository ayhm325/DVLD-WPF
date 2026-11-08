using Application.DTOs.PersonDTO;
using Application.Interfaces;
using DVLD.Api.Results;
using ContractPerson = DVLD.Contracts.Person;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVLD.Api.Controllers;

[ApiController]
[Authorize(Policy = "StaffOrAdmin")]
[Route("api/[controller]")]
public sealed class PeopleController(
    IPersonService personService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] PersonQueryRequest request)
    {
        if (Request.Query.ContainsKey("pageNumber") ||
            Request.Query.ContainsKey("pageSize") ||
            Request.Query.ContainsKey("searchQuery") ||
            Request.Query.ContainsKey("searchBy") ||
            Request.Query.ContainsKey("genderFilter") ||
            Request.Query.ContainsKey("paged"))
        {
            var result = await personService.GetAllPeopleAsync(request);

            if (result.IsFailure)
                return result.ToActionResult(this);

            var page = result.Value!;

            return Ok(new
            {
                items = page.Items
                    .Select(ToListResponse)
                    .ToList(),
                pageNumber = page.PageNumber,
                pageSize = page.PageSize,
                totalCount = page.TotalCount,
                totalPages = page.TotalPages,
                hasPreviousPage = page.HasPreviousPage,
                hasNextPage = page.HasNextPage
            });
        }

        var unpagedResult = await personService.GetAllPeopleAsync();

        if (unpagedResult.IsFailure)
            return unpagedResult.ToActionResult(this);

        return Ok(
            unpagedResult.Value!
                .Select(ToListResponse)
                .ToList());
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        var result = await personService.GetStatisticsAsync();

        if (result.IsFailure)
            return result.ToActionResult(this);

        var stats = result.Value!;
        return Ok(new
        {
            totalCount = stats.TotalCount,
            maleCount = stats.MaleCount,
            femaleCount = stats.FemaleCount
        });
    }

    [HttpGet("available-for-user")]
    public async Task<IActionResult> GetAvailableForUser(
        [FromQuery] PersonQueryRequest request)
    {
        var result = await personService.GetPeopleAvailableForUserAsync(request);

        if (result.IsFailure)
            return result.ToActionResult(this);

        var page = result.Value!;

        return Ok(new
        {
            items = page.Items
                .Select(ToListResponse)
                .ToList(),
            pageNumber = page.PageNumber,
            pageSize = page.PageSize,
            totalCount = page.TotalCount,
            totalPages = page.TotalPages,
            hasPreviousPage = page.HasPreviousPage,
            hasNextPage = page.HasNextPage
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result =
            await personService.GetPersonByIdAsync(id);

        if (result.IsFailure)
            return result.ToActionResult(this);

        return Ok(
            ToResponse(result.Value!));
    }

    [HttpGet("national/{nationalNo}")]
    public async Task<IActionResult> GetByNationalNo(
        string nationalNo)
    {
        var result =
            await personService.GetPersonByNationalNoAsync(
                nationalNo);

        if (result.IsFailure)
            return result.ToActionResult(this);

        return Ok(
            ToResponse(result.Value!));
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody]
        ContractPerson.CreatePersonRequest request)
    {
        var result =
            await personService.AddPersonAsync(
                new PersonCreateDto
                {
                    NationalNo = request.NationalNo,
                    FirstName = request.FirstName,
                    SecondName = request.SecondName,
                    ThirdName = request.ThirdName,
                    LastName = request.LastName,
                    DateOfBirth = request.DateOfBirth,
                    Gender = (int)request.Gender,
                    Address = request.Address,
                    Phone = request.Phone,
                    Email = request.Email,
                    NationalityCountryID =
                        request.NationalityCountryID,
                    ImagePath = request.ImagePath
                });

        if (result.IsFailure)
            return result.ToActionResult(this);

        return CreatedAtAction(
            nameof(GetById),
            new
            {
                id = result.Value
            },
            new ContractPerson.CreatePersonResponse
            {
                PersonId = result.Value
            });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody]
        ContractPerson.UpdatePersonRequest request)
    {
        var result =
            await personService.UpdatePersonAsync(
                id,
                new PersonUpdateDto
                {
                    NationalNo = request.NationalNo,
                    FirstName = request.FirstName,
                    SecondName = request.SecondName,
                    ThirdName = request.ThirdName,
                    LastName = request.LastName,
                    DateOfBirth = request.DateOfBirth,
                    Gender = (int)request.Gender,
                    Address = request.Address,
                    Phone = request.Phone,
                    Email = request.Email,
                    NationalityCountryID =
                        request.NationalityCountryID,
                    ImagePath = request.ImagePath
                });

        return result.IsSuccess
            ? NoContent()
            : result.ToActionResult(this);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result =
            await personService.DeletePersonAsync(id);

        return result.IsSuccess
            ? NoContent()
            : result.ToActionResult(this);
    }

    [HttpPost("{id:int}/photo")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadPhoto(
        int id,
        IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { error = "An image file is required." });
        }

        await using var stream = file.OpenReadStream();
        var result = await personService.UploadPersonPhotoAsync(
            id,
            stream,
            file.Length,
            file.ContentType,
            HttpContext.RequestAborted);

        if (result.IsFailure)
        {
            return result.ToActionResult(this);
        }

        return Ok(new
        {
            personId = id,
            imagePath = result.Value
        });
    }

    [HttpGet("{id:int}/photo")]
    public async Task<IActionResult> GetPhoto(int id)
    {
        var result = await personService.GetPersonPhotoAsync(
            id,
            HttpContext.RequestAborted);

        if (result.IsFailure)
        {
            return result.ToActionResult(this);
        }

        var etag = $"\"{result.Value.StorageKey}\"";
        if (Request.Headers.IfNoneMatch.ToString() == etag)
        {
            return StatusCode(StatusCodes.Status304NotModified);
        }

        Response.Headers.ETag = etag;
        Response.Headers.CacheControl = "private, max-age=86400, must-revalidate";
        return File(result.Value.Stream, result.Value.ContentType);
    }

    [HttpDelete("{id:int}/photo")]
    public async Task<IActionResult> DeletePhoto(int id)
    {
        var result = await personService.DeletePersonPhotoAsync(
            id,
            HttpContext.RequestAborted);

        return result.IsSuccess
            ? NoContent()
            : result.ToActionResult(this);
    }

    private static ContractPerson.PersonResponse ToResponse(
        PersonDto dto)
        => new()
        {
            PersonId = dto.PersonId,
            NationalNo = dto.NationalNo,
            FirstName = dto.FirstName,
            SecondName = dto.SecondName,
            ThirdName = dto.ThirdName,
            LastName = dto.LastName,
            FullName = dto.FullName,
            DateOfBirth = dto.DateOfBirth,
            Gender = (ContractPerson.Gender)dto.Gender,
            Address = dto.Address,
            Phone = dto.Phone,
            Email = dto.Email,
            NationalityCountryID =
                dto.NationalityCountryID,
            CountryName = dto.CountryName,
            ImagePath = dto.ImagePath
        };

    private static ContractPerson.PersonListResponse
        ToListResponse(PersonDto dto)
        => new()
        {
            PersonId = dto.PersonId,
            NationalNo = dto.NationalNo,
            FullName = dto.FullName,
            DateOfBirth = dto.DateOfBirth,
            Gender = (ContractPerson.Gender)dto.Gender,
            Address = dto.Address,
            Phone = dto.Phone,
            Email = dto.Email,
            CountryName = dto.CountryName,
            HasPhoto = !string.IsNullOrWhiteSpace(dto.ImagePath),
            ImagePath = dto.ImagePath
        };
}
