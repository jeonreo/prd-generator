namespace PrdGenerator.Models;

public sealed class PrdResponse
{
    public string ProblemStatement { get; set; } = "";
    public string TargetUser { get; set; } = "";
    public List<string> UseCases { get; set; } = new();
    public List<string> FunctionalRequirements { get; set; } = new();
    public List<string> NonFunctionalRequirements { get; set; } = new();
    public List<string> OutOfScope { get; set; } = new();
    public List<string> SuccessMetrics { get; set; } = new();
    public List<string> Risks { get; set; } = new();
}

public sealed class PrdFormatted
{
    public string? Jira { get; set; }
    public string? Confluence { get; set; }
}

public PrdFormatted? Formatted { get; set; }
