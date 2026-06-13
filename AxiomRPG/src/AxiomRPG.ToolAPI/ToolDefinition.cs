using System.Text.Json.Nodes;

namespace AxiomRPG.ToolAPI;

public record ToolDefinition(
    string Name,
    string Description,
    JsonObject ParametersSchema
)
{
    public JsonObject ToOpenAIToolFormat()
    {
        return new JsonObject
        {
            ["type"] = "function",
            ["function"] = new JsonObject
            {
                ["name"] = Name,
                ["description"] = Description,
                ["parameters"] = ParametersSchema
            }
        };
    }
}
