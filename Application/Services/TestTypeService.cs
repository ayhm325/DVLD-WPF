using Application.Common.Results;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class TestTypeService : ITestTypeService
    {
        private readonly ITestTypeRepository _testTypeRespository;

        public TestTypeService(ITestTypeRepository testTypeRespository)
        {
            _testTypeRespository = testTypeRespository;
        }

        // ================= GET ALL =================
        public async Task<Result<List<TestTypeDto>>> GetAllTestTypesAsync()
        {
            var testTypes = await _testTypeRespository.GetAllTestTypeAsync();

            return Result<List<TestTypeDto>>.Success(
                [.. testTypes.Select(MapToDto)]);
        }

        // ================= GET BY ID =================
        public async Task<Result<TestTypeDto>> GetTestTypeByIdAsync(int id)
        {
            var testType = await _testTypeRespository.GetTestTypeByIdAsync(id);

            if (testType == null)
                return Result<TestTypeDto>.Fail("نوع الاختبار غير موجود.");

            return Result<TestTypeDto>.Success(MapToDto(testType));
        }

        // ================= UPDATE =================
        public async Task<Result> UpdateTestTypeAsync(int id, TestTypeDto dto)
        {
            if (dto == null)
                return Result.Failure("بيانات نوع الاختبار مطلوبة.");

            var testType = await _testTypeRespository.GetTestTypeByIdAsync(id);

            if (testType == null)
                return Result.Failure("نوع الاختبار غير موجود.");

            testType.TestTypeTitle = dto.TestTypeTitle;
            testType.TestTypeDescription = dto.TestTypeDescription;
            testType.TestTypeFees = dto.TestTypeFees;

            var isSuccess = await _testTypeRespository.UpdateTestTypeAsync(testType);

            return isSuccess
                ? Result.Success()
                : Result.Failure("فشل في تحديث نوع الاختبار.");
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