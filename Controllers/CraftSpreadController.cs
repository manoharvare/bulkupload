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

        var query = _context.CraftSpreads.AsQueryable();

        if (!string.IsNullOrEmpty(projectId))
            query = query.Where(c => c.ProjectId == projectId);

        if (!string.IsNullOrEmpty(revision))
            query = query.Where(c => c.Revision == revision);

        // Fetch all filtered rows (we need full activity data for grouping)
        var rawData = await query
            .Select(c => new
            {
                c.Id,
                c.ProjectId,
                c.Revision,
                c.ActivityId,
                c.ActivityName,
                c.ResourceId,
                c.ResourceIdName,
                c.ResourceType,
                c.Week,
                c.Value
            })
            .ToListAsync();

        if (!rawData.Any())
            return Ok(new { message = "No data found.", totalCount = 0, page, pageSize, columns = Array.Empty<string>(), data = Array.Empty<object>() });

        // Determine all unique week columns
        var weeks = rawData.Select(d => d.Week).Distinct().OrderBy(w => w).ToList();

        // Group by Project + Activity
        var grouped = rawData
            .GroupBy(d => new { d.ProjectId, d.ActivityId, d.ActivityName })
            .Select(g => new
            {
                g.Key.ProjectId,
                g.Key.ActivityId,
                g.Key.ActivityName,
                Weeks = weeks.ToDictionary(
                    w => w,
                    w => g.Where(x => x.Week == w).Sum(x => x.Value) // Sum values if multiple resources per week
                )
            })
            .ToList();

        var totalCount = grouped.Count;

        // Apply pagination at activity level
        var pagedData = grouped
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Ok(new
        {
            totalCount,
            page,
            pageSize,
            columns = weeks,
            data = pagedData
        });
    }
}
