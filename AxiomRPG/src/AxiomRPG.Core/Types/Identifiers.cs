namespace AxiomRPG.Core.Types;

public readonly record struct EntityId(string Value)
{
    public static EntityId New() => new($"entity_{Guid.NewGuid():N}");
    public static EntityId From(string value) => new(value);
    public override string ToString() => Value;
}

public readonly record struct TemplateId(string Value)
{
    public static TemplateId New() => new($"tpl_{Guid.NewGuid():N}");
    public static TemplateId From(string value) => new(value);
    public override string ToString() => Value;
}

public readonly record struct ToolId(string Value)
{
    public static ToolId New() => new($"tool_{Guid.NewGuid():N}");
    public static ToolId From(string value) => new(value);
    public override string ToString() => Value;
}

public readonly record struct QuestId(string Value)
{
    public static QuestId New() => new($"quest_{Guid.NewGuid():N}");
    public static QuestId From(string value) => new(value);
    public override string ToString() => Value;
}

public readonly record struct FactionId(string Value)
{
    public static FactionId New() => new($"faction_{Guid.NewGuid():N}");
    public static FactionId From(string value) => new(value);
    public override string ToString() => Value;
}

public readonly record struct RegionId(string Value)
{
    public static RegionId New() => new($"region_{Guid.NewGuid():N}");
    public static RegionId From(string value) => new(value);
    public override string ToString() => Value;
}
