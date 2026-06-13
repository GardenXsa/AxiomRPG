using System.Text.Json.Nodes;

namespace AxiomRPG.ECS;

public interface IComponentResolver
{
    string ComponentTypeName { get; }
    IComponent Deserialize(JsonObject data);
    JsonObject Serialize(IComponent component);
}
