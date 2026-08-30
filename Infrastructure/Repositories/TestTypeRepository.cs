using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class TestTypeRepository
    : ITestTypeRepository
{
    private readonly DVLDDbContext _context;


    public TestTypeRepository(
        DVLDDbContext context)
    {
        _context =
            context
            ?? throw new ArgumentNullException(
                nameof(context));
    }


    // =========================================================
    // GET ALL
    // =========================================================

    public async Task<List<TestType>>
        GetAllTestTypeAsync()
    {
        return await _context.TestTypes
            .AsNoTracking()
            .ToListAsync();
    }


    // =========================================================
    // GET BY ID
    // =========================================================

    public async Task<TestType?>
        GetTestTypeByIdAsync(
            int id)
    {
        if (id <= 0)
            return null;

        return await _context.TestTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(
                t =>
                    t.TestTypeId == id);
    }


    // =========================================================
    // UPDATE
    // =========================================================

    public async Task<bool>
        UpdateTestTypeAsync(
            TestType testtype)
    {
        ArgumentNullException.ThrowIfNull(
            testtype);

        if (testtype.TestTypeId <= 0)
            return false;


        var existing =
            await _context.TestTypes
                .FirstOrDefaultAsync(
                    t =>
                        t.TestTypeId ==
                        testtype.TestTypeId);

        if (existing is null)
            return false;


        // =====================================================
        // UPDATE VALUES
        // =====================================================

        _context.Entry(existing)
            .CurrentValues
            .SetValues(testtype);


        // =====================================================
        // IMPORTANT
        // =====================================================
        // لا يوجد SaveChangesAsync هنا.
        //
        // الـ Repository مسؤول عن تعديل الـ Entity فقط.
        //
        // الـ UnitOfWork مسؤول عن:
        //
        //     SaveChangesAsync()
        //
        // =====================================================

        return true;
    }
}