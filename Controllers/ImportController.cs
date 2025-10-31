using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Text;
using OilAndGasImport.Data;
using OilAndGasImport.Hubs;
using OilAndGasImport.Models;
using EFCore.BulkExtensions;

namespace OilAndGasImport.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImportController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<ImportHub> _hubContext;
        private const int BatchSize = 2000;

        public ImportController(AppDbContext context, IHubContext<ImportHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;

            _context.ChangeTracker.QueryTrackingBehavior = Microsoft.EntityFrameworkCore.QueryTrackingBehavior.NoTracking;
            _context.ChangeTracker.AutoDetectChangesEnabled = false;
        }

        [DisableRequestSizeLimit]
        [RequestFormLimits(MultipartBodyLengthLimit = 209715200)] // 200 MB
        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            string fileKey = "resource-import";
            int totalRowCount = 0, batchRowCount = 0, totalWeekCount = 0;
            var resourceBatch = new List<ResourceSpread>();
            var craftBatch = new List<CraftSpread>();

            // --- STEP 1: Count total records for progress
            int totalEstimatedRecords = 0;
            using (var countReader = new StreamReader(file.OpenReadStream(), Encoding.UTF8, leaveOpen: true))
            using (var countCsv = new CsvReader(countReader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                BadDataFound = null,
                MissingFieldFound = null
            }))
            {
                await countCsv.ReadAsync();
                countCsv.ReadHeader();
                while (await countCsv.ReadAsync())
                    totalEstimatedRecords++;
            }

            file.OpenReadStream().Position = 0;
            int totalBatches = (int)Math.Ceiling((double)totalEstimatedRecords / BatchSize);
            int currentBatch = 0;

            // --- STEP 2: Start actual import
            using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                BadDataFound = null,
                MissingFieldFound = null,
                TrimOptions = TrimOptions.Trim
            });

            await csv.ReadAsync();
            csv.ReadHeader();

            var headers = csv.HeaderRecord!;
            var weekHeaders = headers
                .Where(h => DateTime.TryParse(h.Replace("-", " "), out _))
                .ToList();
            totalWeekCount = weekHeaders.Count;

            while (await csv.ReadAsync())
            {
                try
                {
                    var resource = new ResourceSpread
                    {
                        ProjectId = csv.GetField("ProjectId"),
                        Revision = fileKey,
                        ActivityId = csv.GetField("ActivityId"),
                        ActivityName = csv.GetField("ActivityName"),
                        WBS = csv.GetField("WBS"),
                        WBSName = csv.GetField("WBS Name"),
                        Curve = csv.GetField("Curve"),
                        Calendar = csv.GetField("Calendar"),
                        ResourceId = csv.GetField("ResourceId"),
                        ResourceIdName = csv.GetField("Resource Id Name"),
                        ResourceType = csv.GetField("Resource Type"),
                        BudgetedUnits = ParseDouble(csv, "Budgeted Units"),
                        ActualUnits = ParseDouble(csv, "Actual Units"),
                        RemainingUnits = ParseDouble(csv, "Remaining Units"),
                        RemainingLateFinish = ParseDouble(csv, "Remaining Late Finish")
                    };

                    resourceBatch.Add(resource);

                    foreach (var week in weekHeaders)
                    {
                        var valueText = csv.GetField(week);
                        if (double.TryParse(valueText, out double value))
                        {
                            craftBatch.Add(new CraftSpread
                            {
                                ProjectId = resource.ProjectId,
                                Revision = fileKey,
                                Week = week,
                                Value = value,
                                ActivityId = resource.ActivityId,
                                ActivityName = resource.ActivityName,
                                ResourceId = resource.ResourceId,
                                ResourceIdName = resource.ResourceIdName,
                                ResourceType = resource.ResourceType
                            });
                        }
                    }

                    totalRowCount++;
                    batchRowCount++;

                    if (batchRowCount >= BatchSize)
                    {
                        currentBatch++;
                        await FlushBatchAsync(resourceBatch, craftBatch);
                        await SendProgressAsync(fileKey, totalRowCount, currentBatch, totalBatches, totalEstimatedRecords);
                        batchRowCount = 0;
                    }
                }
                catch (Exception ex)
                {
                    await _hubContext.Clients.Group(fileKey).SendAsync("ImportError", new
                    {
                        fileKey,
                        row = totalRowCount + 1,
                        error = ex.Message
                    });
                }
            }

            // ✅ Final flush
            if (batchRowCount > 0)
            {
                currentBatch++;
                await FlushBatchAsync(resourceBatch, craftBatch);
                await SendProgressAsync(fileKey, totalRowCount, currentBatch, totalBatches, totalEstimatedRecords);
            }

            await _hubContext.Clients.Group(fileKey).SendAsync("ImportCompleted", new
            {
                fileKey,
                totalRecords = totalRowCount,
                totalWeekColumns = totalWeekCount,
                message = "✅ Import completed successfully!"
            });

            return Ok(new
            {
                message = "Import completed successfully.",
                totalRecords = totalRowCount,
                fileKey
            });
        }

        // ⚡ Optimized batch insert using EFCore.BulkExtensions
        private async Task FlushBatchAsync(List<ResourceSpread> resources, List<CraftSpread> crafts)
        {
            var bulkConfig = new BulkConfig
            {
                PreserveInsertOrder = true,
                SetOutputIdentity = false,
                BatchSize = BatchSize
            };

            if (resources.Count > 0)
                await _context.BulkInsertAsync(resources, bulkConfig);

            if (crafts.Count > 0)
                await _context.BulkInsertAsync(crafts, bulkConfig);

            resources.Clear();
            crafts.Clear();
        }

        private async Task SendProgressAsync(
            string fileKey,
            int rowsProcessed,
            int currentBatch,
            int totalBatches,
            int totalEstimatedRecords)
        {
            double recordPercent = totalEstimatedRecords > 0
                ? Math.Round((double)rowsProcessed / totalEstimatedRecords * 100, 2)
                : 0;

            await _hubContext.Clients.Group(fileKey).SendAsync("ImportProgress", new
            {
                fileKey,
                rowsProcessed,
                totalEstimatedRecords,
                currentBatch,
                totalBatches,
                progressPercent = recordPercent,
                message = $"Processed {rowsProcessed} of {totalEstimatedRecords} records (Batch {currentBatch}/{totalBatches})"
            });
        }

        private static double ParseDouble(CsvReader csv, string fieldName)
        {
            var value = csv.GetField(fieldName);
            return double.TryParse(value, out var result) ? result : 0;
        }
    }
}
