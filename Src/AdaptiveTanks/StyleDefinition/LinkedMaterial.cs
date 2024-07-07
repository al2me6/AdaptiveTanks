using ROUtils.DataTypes;

namespace AdaptiveTanks;

public class LinkedMaterial : ConfigNodePersistenceBase
{
    [Persistent] public string id;
    [Persistent] private string displayName = null;

    public string DisplayName => displayName ?? id;
}
