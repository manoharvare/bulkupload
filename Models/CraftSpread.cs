namespace OilAndGasImport.Models;

public class CraftSpread
{
    public int Id { get; set; }

    public string ProjectId { get; set; } = string.Empty;
    public string Revision { get; set; } = string.Empty;

    public string Week { get; set; } = string.Empty;
    public double Value { get; set; }

    public string ActivityId { get; set; } = string.Empty;
    public string ActivityName { get; set; } = string.Empty;
    public string ResourceId { get; set; } = string.Empty;
    public string ResourceIdName { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
}
