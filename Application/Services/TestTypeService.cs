using Application.DTOs;
using Application.Interfaces;
using Application.Validators;
using Domain.Entities;
using Infrastructure.Repositories;


namespace Application.Services
{
    public class TestTypeService : ITestTypeService
    {
        private readonly TestTypeRepository _testTypeRespository;

        public TestTypeService(TestTypeRepository testTypeRespository)
        {
            _testTypeRespository= testTypeRespository;
        }

        // ================= GET ALL =================
        public async Task<List<TestTypeDto>> GetAllTestTypesAsync()
        {
            var testTypes = await _testTypeRespository.GetAllTestTypeAsync();
            return [.. testTypes.Select(MapToDto)];
        }

        // ================= GET BY ID =================
        public async Task<TestTypeDto?> GetTestTypeByIdAsync(int id)
        {
            var testType = await _testTypeRespository.GetTestTypeByIdAsync(id);
            return testType != null ? MapToDto(testType) : null;
        }

        // ================= UPDATE =================
        public async Task<bool> UpdateTestTypeAsync(int id, TestTypeDto dto)
        {
            var testType = await _testTypeRespository.GetTestTypeByIdAsync(id);
            if (testType == null) return false;

            testType.TestTypeTitle = dto.TestTypeTitle;
            testType.TestTypeDescription = dto.TestTypeDescription;
            testType.TestTypeFees = dto.TestTypeFees;

            return await _testTypeRespository.UpdateTestTypeAsync(testType);
        }


        // ================= MAPPING =================
        private TestTypeDto MapToDto(TestType testtype)
        {
            return new TestTypeDto
            {
                TestTypeId = testtype.TestTypeId,
                TestTypeTitle = testtype.TestTypeTitle,
                TestTypeDescription = testtype.TestTypeDescription,
                TestTypeFees = testtype.TestTypeFees
            };
        }

    }
}
