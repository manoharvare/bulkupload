using CsvHelper;
using CsvHelper.Configuration;
using OilAndGasImport.Data;
using OilAndGasImport.Models;
using System.Globalization;

namespace OilAndGasImport.Services;

public class CsvImportService
{
    private readonly AppDbContext _context;

    public CsvImportService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<string> ImportCsvAsync(string filePath)
    {
        // Generate a unique batch revision for this import
        var revision = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true
        });

        var records = csv.GetRecords<dynamic>().ToList();
        if (!records.Any())
            return revision;

        // Identify week columns dynamically (those that look like dates)
        var headerRow = csv.Context.Reader.HeaderRecord!;
        var weekHeaders = headerRow
            .Where(h => DateTime.TryParse(h.Replace("-", " "), out _))
            .ToList();

        foreach (var record in records)
        {
            var dict = (IDictionary<string, object>)record;

            // Create ResourceSpread entry
            var resource = new ResourceSpread
            {
                ProjectId = dict.GetValueOrDefault("ProjectId")?.ToString() ?? string.Empty,
                Revision = revision,

                ActivityId = dict.GetValueOrDefault("ActivityId")?.ToString() ?? string.Empty,
                ActivityName = dict.GetValueOrDefault("ActivityName")?.ToString() ?? string.Empty,
                ResourceId = dict.GetValueOrDefault("ResourceId")?.ToString() ?? string.Empty,
                ResourceIdName = dict.GetValueOrDefault("Resource Id Name")?.ToString() ?? string.Empty,
                ResourceType = dict.GetValueOrDefault("Resource Type")?.ToString() ?? string.Empty,

                WBS = dict.GetValueOrDefault("WBS")?.ToString() ?? string.Empty,
                WBSName = dict.GetValueOrDefault("WBS Name")?.ToString() ?? string.Empty,
                Curve = dict.GetValueOrDefault("Curve")?.ToString() ?? string.Empty,
                Calendar = dict.GetValueOrDefault("Calendar")?.ToString() ?? string.Empty,

                BudgetedUnits = double.TryParse(dict.GetValueOrDefault("Budgeted Units")?.ToString(), out var b) ? b : 0,
                ActualUnits = double.TryParse(dict.GetValueOrDefault("Actual Units")?.ToString(), out var a) ? a : 0,
                RemainingUnits = double.TryParse(dict.GetValueOrDefault("Remaining Units")?.ToString(), out var r) ? r : 0,
                RemainingLateFinish = double.TryParse(dict.GetValueOrDefault("Remaining Late Finish")?.ToString(), out var rl) ? rl : 0,
            };

            _context.ResourceSpreads.Add(resource);
            await _context.SaveChangesAsync();

            // Now handle week-by-week CraftSpread
            foreach (var week in weekHeaders)
            {
                var weekValue = dict.GetValueOrDefault(week)?.ToString();
                if (!string.IsNullOrWhiteSpace(weekValue) &&
                    double.TryParse(weekValue, out double value))
                {
                    var craft = new CraftSpread
                    {
                        ProjectId = resource.ProjectId,
                        Revision = revision,
                        Week = week,
                        Value = value,

                        ActivityId = resource.ActivityId,
                        ActivityName = resource.ActivityName,
                        ResourceId = resource.ResourceId,
                        ResourceIdName = resource.ResourceIdName,
                        ResourceType = resource.ResourceType
                    };

                    _context.CraftSpreads.Add(craft);
                }
            }
        }

        await _context.SaveChangesAsync();
        return revision;
    }
}

public static class DictionaryExtensions
{
    public static object? GetValueOrDefault(this IDictionary<string, object> dict, string key)
    {
        return dict.TryGetValue(key, out var value) ? value : null;
    }
}
