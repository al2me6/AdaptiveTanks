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

public class Material : ConfigNodePersistenceBase
{
    [Persistent] public string id;
    [Persistent] private string displayName = null;

    [Persistent(name = "shader")] public string shaderName = null;

    [Persistent(name = "Texture")]
    public PersistentDictionaryValueTypes<string, string> textures = new();

    [Persistent(name = "Float")]
    public PersistentDictionaryValueTypes<string, float> floats = new();

    [Persistent(name = "Color")]
    public PersistentDictionaryValueTypes<string, Color> colors = new();

    public UnityEngine.Material OverrideMaterial { get; private set; } = null;

    public string DisplayName => displayName ?? id;

    internal void Compile(Asset asset)
    {
        if (asset.SharedMaterial == null) return;

        OverrideMaterial = new UnityEngine.Material(asset.SharedMaterial);

        if (!string.IsNullOrEmpty(shaderName) && shaderName != asset.SharedMaterial.shader.name)
        {
            var shader = Shader.Find(shaderName);
            if (shader == null)
                Debug.LogError($"asset `{asset.mu}`: shader `{shaderName}` not found");
            else
                OverrideMaterial.shader = shader;
        }

        foreach (var kvp in textures)
        {
            var propertyName = kvp.Key;
            var texInfo = GameDatabase.Instance.GetTextureInfo(kvp.Value);
            if (texInfo == null)
            {
                Debug.LogError($"asset `{asset.mu}`: texture `{kvp.Value}` not found");
                continue;
            }

            var nrmPropertyName = Library<NormalMapShaderProperty>.GetOrNull(propertyName);
            var isNormalMap = propertyName == nrmPropertyName?.property;
            OverrideMaterial.SetTexture(
                propertyName, isNormalMap ? texInfo.normalMap : texInfo.texture);
        }

        foreach (var kvp in floats) OverrideMaterial.SetFloat(kvp.Key, kvp.Value);

        foreach (var kvp in colors) OverrideMaterial.SetColor(kvp.Key, kvp.Value);
    }

    public void ApplyTo(GameObject go)
    {
        if (OverrideMaterial == null) return;
        foreach (var renderer in go.transform.GetComponentsInChildren<Renderer>())
        {
            renderer.material = null;
            renderer.sharedMaterial = OverrideMaterial;
        }
    }
}
