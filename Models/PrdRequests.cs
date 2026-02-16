namespace PrdGenerator.Models;

public sealed class PrdRequest
{
    public string ProductIdea { get; set; } = "";
    public string TargetUser { get; set; } = "";
    public string Problem { get; set; } = "";
    public List<string>? Constraints { get; set; }
    public int TimelineWeeks { get; set; }
    public string? OutputFormat { get; set; } // "json" | "jira" | "confluence" | "both"

}
