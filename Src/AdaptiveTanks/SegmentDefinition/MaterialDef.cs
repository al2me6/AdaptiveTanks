using ROUtils.DataTypes;
using UnityEngine;

namespace AdaptiveTanks.SegmentDefinition;

[LibraryLoad("AT_SHADER_NORMAL_MAP_PROPERTY", loadOrder: -1)]
public class NormalMapShaderProperty : ConfigNodePersistenceBase, ILibraryLoad
{
    [Persistent] public string shader;
    [Persistent] public string property;
    public string ItemName() => shader;
}

[LibraryLoad("AT_MATERIAL_DEF", loadOrder: -1)]
public class MaterialDef : ConfigNodePersistenceBase, ILibraryLoad
{
    [Persistent] public string name;
    [Persistent] private string displayName = null;

    [Persistent(name = "shader")] public string shaderName = null;

    [Persistent(name = "Texture")]
    public PersistentDictionaryValueTypes<string, string> textures = new();

    [Persistent(name = "Float")]
    public PersistentDictionaryValueTypes<string, float> floats = new();

    [Persistent(name = "Color")]
    public PersistentDictionaryValueTypes<string, Color> colors = new();

    public string ItemName() => name;

    public string DisplayName => displayName ?? name;
}
