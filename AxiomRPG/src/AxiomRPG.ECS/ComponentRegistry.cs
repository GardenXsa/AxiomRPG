using System.Text.Json.Nodes;

namespace AxiomRPG.ECS;

public class ComponentRegistry
{
    private readonly Dictionary<string, IComponentResolver> _resolvers = new();

    public void Register(IComponentResolver resolver) => _resolvers[resolver.ComponentTypeName] = resolver;

    public IComponentResolver? GetResolver(string componentTypeName) =>
        _resolvers.GetValueOrDefault(componentTypeName);

    public IComponent? DeserializeComponent(string typeName, JsonObject data)
    {
        var resolver = GetResolver(typeName);
        return resolver?.Deserialize(data);
    }

    public IReadOnlyDictionary<string, IComponentResolver> GetAllResolvers() => _resolvers;
}
