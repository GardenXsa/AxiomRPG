using AxiomRPG.ECS;

namespace AxiomRPG.Components;

public record InventoryComponent : IComponent
{
    public string ComponentType => "inventory";
    public int Capacity { get; init; } = 20;
    public List<InventorySlot> Slots { get; init; } = new();
    public List<string> EquippedItems { get; init; } = new(); // item entity IDs

    public bool IsFull => Slots.Count >= Capacity;
}

public record InventorySlot(string ItemEntityId, int Quantity = 1, string? SlotName = null);
