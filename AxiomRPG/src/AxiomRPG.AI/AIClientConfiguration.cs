namespace AxiomRPG.AI;

public class AIClientConfiguration
{
    public string ApiKey { get; set; } = "";
    public string ApiBaseUrl { get; set; } = "https://api.openai.com/v1";
    public string Model { get; set; } = "gpt-4o";
    public int MaxTokens { get; set; } = 4096;
    public float Temperature { get; set; } = 0.7f;
    public int ContextWindowTokens { get; set; } = 128000;
    public string? OrganizationId { get; set; }
}
