using AxiomRPG.ECS;

namespace AxiomRPG.Components;

public record WeatherComponent : IComponent
{
    public string ComponentType => "weather";
    public string WeatherType { get; set; } = "clear"; // clear, rain, snow, storm, fog, etc.
    public float Intensity { get; set; } = 0f;
    public float Temperature { get; set; } = 20f;
    public float WindSpeed { get; set; } = 0f;
    public string? SpecialEffect { get; set; }
}
