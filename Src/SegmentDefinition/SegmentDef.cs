using System;
using System.Collections.Generic;
using System.Linq;
using AdaptiveTanks.Extensions;

namespace AdaptiveTanks;

[Flags]
public enum SegmentKind
{
    body = 1,
    nose = 2,
    mount = 4,
    cap = nose | mount,
    interstage = 8
}

public class SegmentDef : IRepeatedConfigNode
{
    public string ConfigNodeName() => "AT_SEGMENT_DEF";

    [Persistent] public string name;
    [Persistent] public SegmentKind kind;
    public readonly List<SegmentModel> models = [];

    public List<float> AspectRatios { get; private set; }

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

        if (kind != SegmentKind.body && models.Count > 1)
        {
            Debug.LogError($"{kind} segment {name} supports one asset group only; deleting rest");
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
        Debug.Log($"SEGMENT {name}, kind {kind.ToString()}");

        PreProcess();
        Validate();
    }

    public void Save(ConfigNode node)
    {
        ConfigNode.CreateConfigFromObject(this, node);
        node.WriteAllToNodes(models);
    }
}
