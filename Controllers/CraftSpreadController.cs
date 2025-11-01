using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OilAndGasImport.Data;

namespace OilAndGasImport.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CraftSpreadController : ControllerBase
{
    private readonly AppDbContext _context;

    public CraftSpreadController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        string? projectId = null,
        string? revision = null,
        int page = 1,
        int pageSize = 20)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 20;

        var query = _context.CraftSpreads.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(projectId))
            query = query.Where(c => c.ProjectId == projectId);

        if (!string.IsNullOrEmpty(revision))
            query = query.Where(c => c.Revision == revision);

        // ✅ 1. Get all distinct weeks only once
        var weeks = await query
            .Select(c => c.Week)
            .Distinct()
            .OrderBy(w => w)
            .ToListAsync();

        // ✅ 2. Aggregate directly in SQL for paging performance
        var groupedQuery = query
            .GroupBy(c => new { c.ProjectId, c.ActivityId, c.ActivityName })
            .Select(g => new
            {
                g.Key.ProjectId,
                g.Key.ActivityId,
                g.Key.ActivityName,
                Values = g.GroupBy(x => x.Week)
                          .Select(wg => new { Week = wg.Key, Total = wg.Sum(x => x.Value) })
            });

        // ✅ 3. Count total records (before pagination)
        var totalCount = await groupedQuery.CountAsync();

        // ✅ 4. Fetch only the required page
        var pagedData = await groupedQuery
            .OrderBy(x => x.ProjectId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // ✅ 5. Build compact result (fill missing weeks as 0)
        var result = pagedData.Select(row => new
        {
            row.ProjectId,
            row.ActivityId,
            row.ActivityName,
            Weeks = weeks.ToDictionary(
                w => w,
                w => row.Values.FirstOrDefault(v => v.Week == w)?.Total ?? 0)
        }).ToList();

        return Ok(new
        {
            totalCount,
            page,
            pageSize,
            columns = weeks,
            data = result
        });
    }
}