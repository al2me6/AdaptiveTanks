using System;
using System.Collections.Generic;
using System.Linq;
using AdaptiveTanks.Utils;
using ROUtils.DataTypes;

namespace AdaptiveTanks;

public abstract class GeometryModel : ConfigNodePersistenceBase
{
    private static readonly IReadOnlyDictionary<string, Type> subclasses = AssemblyLoader
        .loadedAssemblies
        .SelectMany(asm => asm.assembly.GetTypes())
        .Where(type => type.IsSubclassOf(typeof(GeometryModel)))
        .ToDictionary(type => type.Name);

    public static GeometryModel? TryLoadFirstSubclassFromNode(ConfigNode node)
    {
        foreach (ConfigNode child in node.nodes)
        {
            if (subclasses.TryGetValue(child.name, out var subclass))
            {
                var instance = (GeometryModel)Activator.CreateInstance(subclass);
                instance.Load(child);
                return instance;
            }
        }

        return null;
    }

    public abstract float EvaluateVolume(float diameter, float height);
}

public class GeometryModelCylinder : GeometryModel
{
    public override float EvaluateVolume(float diameter, float height) =>
        MathUtils.CylinderVolume(diameter, height);
}

/// <summary>
/// Half a spheroid. The height is precisely the polar radius.
/// </summary>
public class GeometryModelDome : GeometryModel
{
    public override float EvaluateVolume(float diameter, float height) =>
        MathUtils.SpheroidVolume(diameter * 0.5f, height) * 0.5f;
}

/// <summary>
/// Two half-spheroids joined nose-to-nose, volumetrically equivalent to one whole spheroid.
/// The height is precisely the polar diameter.
/// </summary>
public class GeometryModelSeparateDomes : GeometryModel
{
    public override float EvaluateVolume(float diameter, float height) =>
        MathUtils.SpheroidVolume(diameter * 0.5f, height * 0.5f);
}
