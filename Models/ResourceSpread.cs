namespace OilAndGasImport.Models;

public class ResourceSpread
{
    public int Id { get; set; }
    public string ProjectId { get; set; } = string.Empty;
    public string Revision { get; set; } = string.Empty;

    public string ActivityId { get; set; } = string.Empty;
    public string ActivityName { get; set; } = string.Empty;
    public string WBS { get; set; } = string.Empty;
    public string WBSName { get; set; } = string.Empty;
    public string Curve { get; set; } = string.Empty;
    public string Calendar { get; set; } = string.Empty;
    public string ResourceId { get; set; } = string.Empty;
    public string ResourceIdName { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;

    public double BudgetedUnits { get; set; }
    public double ActualUnits { get; set; }
    public double RemainingUnits { get; set; }
    public double RemainingLateFinish { get; set; }

    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
}
