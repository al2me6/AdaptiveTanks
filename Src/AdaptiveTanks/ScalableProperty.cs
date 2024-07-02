using ROUtils.DataTypes;
using UnityEngine;

namespace AdaptiveTanks;

public class ScalableProperty : ConfigNodePersistenceBase
{
    [Persistent] public float baseValue = 0f;
    [Persistent] public float scalePower = 1f;

    public float Evaluate(float diameter) => baseValue * Mathf.Pow(diameter, scalePower);
}
