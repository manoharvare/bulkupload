using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OilAndGasImport.Data;

namespace OilAndGasImport.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ResourceSpreadController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ResourceSpreadController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get Resource Spread data with pagination and optional filters.
        /// </summary>
        /// <param name="projectId">Optional Project ID filter</param>
        /// <param name="revision">Optional Revision filter (use 'latest' for latest batch)</param>
        /// <param name="page">Page number (default 1)</param>
        /// <param name="pageSize">Page size (default 20)</param>
        [HttpGet]
        public async Task<IActionResult> Get(
            string? projectId = null,
            string? revision = null,
            int page = 1,
            int pageSize = 20)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 20;

            var query = _context.ResourceSpreads.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(projectId))
                query = query.Where(r => r.ProjectId == projectId);

            // Handle revision filter (e.g., 'latest')
            if (!string.IsNullOrEmpty(revision))
            {
                if (revision.ToLower() == "latest")
                {
                    var latestRevision = await query
                        .Where(r => !string.IsNullOrEmpty(r.Revision))
                        .OrderByDescending(r => r.Revision)
                        .Select(r => r.Revision)
                        .FirstOrDefaultAsync();

                    if (latestRevision != null)
                        query = query.Where(r => r.Revision == latestRevision);
                }
                else
                {
                    query = query.Where(r => r.Revision == revision);
                }
            }

            var totalCount = await query.CountAsync();

            var data = await query
                .OrderBy(r => r.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new
                {
                    r.Id,
                    r.ProjectId,
                    r.Revision,
                    r.ActivityId,
                    r.ActivityName,
                    r.WBS,
                    r.WBSName,
                    r.Curve,
                    r.Calendar,
                    r.ResourceId,
                    r.ResourceIdName,
                    r.ResourceType,
                    r.BudgetedUnits,
                    r.ActualUnits,
                    r.RemainingUnits,
                    r.RemainingLateFinish
                })
                .ToListAsync();

            return Ok(new
            {
                totalCount,
                page,
                pageSize,
                data
            });
        }
    }
}
