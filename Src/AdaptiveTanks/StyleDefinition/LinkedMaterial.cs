using AdaptiveTanks.Utils;

namespace AdaptiveTanks;

public record LinkedMaterial(string Id, string DisplayName) : IItemName
{
    public string ItemName() => Id;
}
