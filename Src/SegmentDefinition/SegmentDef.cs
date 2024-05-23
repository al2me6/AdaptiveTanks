using System;
using System.Collections.Generic;
using System.Linq;
using AdaptiveTanks.Extensions;

namespace AdaptiveTanks;

[Flags]
public enum SegmentRoleSerialize
{
    body = 1,
    nose = 2,
    mount = 4,
    cap = nose | mount,
    intertank = 8
}

public static class SegmentRoleSerializeExtensions
{
    public static bool Is(this SegmentRoleSerialize serialized, SegmentRole role)
    {
        return serialized.HasFlag(role switch
        {
            SegmentRole.Nose => SegmentRoleSerialize.nose,
            SegmentRole.Body => SegmentRoleSerialize.body,
            SegmentRole.Mount => SegmentRoleSerialize.mount,
            _ => throw new ArgumentOutOfRangeException(nameof(role))
        });
    }
}

[LibraryLoad]
public class SegmentDef : IRepeatedConfigNode, INamedConfigNode
{
    public string ConfigNodeName() => "AT_SEGMENT";

    [Persistent] public string name;
    [Persistent] protected string displayName;
    [Persistent] public SegmentRoleSerialize role;
    public readonly List<SegmentModel> models = [];

    public string Name() => name;
    public string DisplayName => displayName ?? name;
    public List<float> AspectRatios { get; private set; }
    public float AspectRatio => AspectRatios[0];

    private void PreProcess()
    {
        models.Sort((a, b) => a.nativeAspectRatio.CompareTo(b.nativeAspectRatio));
        AspectRatios = models.Select(model => model.nativeAspectRatio).ToList();
    }

    private void Validate()
    {
        if (models.Count == 0)
        {
            Debug.LogError($"segment {name} must have at least one valid model");
        }

        if (role != SegmentRoleSerialize.body && models.Count > 1)
        {
            Debug.LogError($"{role} segment {name} supports one asset group only; deleting rest");
            models.RemoveRange(1, models.Count - 1);
        }

        for (var i = 0; i < AspectRatios.Count; ++i)
        {
            if (AspectRatios[i] > 0f) break;

            Debug.LogError($"segment {name} contains model with invalid (<=0) aspect ratio");
            AspectRatios[i] = 1f;
        }
    }

    public void Load(ConfigNode node)
    {
        ConfigNode.LoadObjectFromConfig(this, node);
        models.AddRange(node.LoadAllFromNodes<SegmentModel>());
        Debug.Log($"SEGMENT {name}, kind {role.ToString()}");

        PreProcess();
        Validate();
    }

    public void Save(ConfigNode node)
    {
        ConfigNode.CreateConfigFromObject(this, node);
        node.WriteAllToNodes(models);
    }
}
