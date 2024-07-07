using System.Collections.Generic;
using AdaptiveTanks.SegmentDefinition;
using AdaptiveTanks.Utils;
using ROUtils.DataTypes;
using UnityEngine;

namespace AdaptiveTanks;

public class Asset : ConfigNodePersistenceBase
{
    [Persistent] public string mu = BuiltinItems.EmptyMuPath;
    [Persistent] public float nativeDiameter = 1f;
    [Persistent] public Vector2 nativeBaseline = new(0f, 1f);
    [Persistent] public bool nativeOrientationIsDown = false;

    [Persistent] public Vector2 diameterRange = new(0f, float.PositiveInfinity);

    public Dictionary<string, AssetMaterial> Materials = new();

    public GameObject Prefab { get; private set; }
    public Material PrefabMaterial { get; private set; }

    public override void Load(ConfigNode node)
    {
        base.Load(node);

        foreach (var material in node.LoadAllFromNodes<AssetMaterial>(nodeName: "Material"))
        {
            if (material.LinkId == null || material.Def == null)
            {
                Debug.LogError($"asset `{mu}`: material must link to valid `MaterialDef`");
                continue;
            }

            if (Materials.ContainsKey(material.LinkId))
            {
                Debug.LogError($"asset `{mu}`: material must have unique `linkId`");
                continue;
            }

            Materials[material.LinkId] = material;
        }

        Prefab = GameDatabase.Instance.GetModelPrefab(mu);
        if (Prefab == null)
        {
            Debug.LogError($"asset `{mu}` not found");
            return;
        }

        PrefabMaterial = Prefab.GetComponentInChildren<Renderer>()?.sharedMaterial;

        foreach (var material in Materials.Values) material.Compile(this);
    }

    public SegmentDef Segment { get; internal set; }

    public float NativeHeight => Mathf.Abs(nativeBaseline.y - nativeBaseline.x);
    public float AspectRatio => NativeHeight / nativeDiameter;

    public float MinDiameter => diameterRange.x;
    public float MaxDiameter => diameterRange.y;

    public bool SupportsDiameter(float diameter) =>
        MinDiameter <= diameter && diameter < MaxDiameter;

    public static (float stretchA, float stretchB) NegotiateAspectRatio(
        Asset a, Asset b, float biasA)
    {
        var targetAspect = a.AspectRatio * biasA + b.AspectRatio * (1f - biasA);
        return (targetAspect / a.AspectRatio, targetAspect / b.AspectRatio);
    }
}
