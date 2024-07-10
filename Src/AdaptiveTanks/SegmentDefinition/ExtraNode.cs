using ROUtils.DataTypes;
using UnityEngine;

namespace AdaptiveTanks.SegmentDefinition;

public class ExtraNode : ConfigNodePersistenceBase
{
    [Persistent] public Vector3 position = Vector3.up;
    [Persistent] public Vector3 orientation = Vector3.up;
}
