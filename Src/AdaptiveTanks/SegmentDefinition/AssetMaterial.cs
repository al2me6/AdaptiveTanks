using AdaptiveTanks.Utils;
using ROUtils.DataTypes;
using UnityEngine;

namespace AdaptiveTanks.SegmentDefinition;

public class AssetMaterial : ConfigNodePersistenceBase, IItemName
{
    [Persistent(name = "def")] public string defName;
    [Persistent] private string linkId = null;
    public string LinkId => linkId ?? defName;

    public string ItemName() => LinkId;

    public MaterialDef Def => Library<MaterialDef>.GetOrNull(defName);

    public Material OverrideMaterial { get; private set; } = null;

    internal void Compile(Asset asset)
    {
        if (asset.PrefabMaterial == null) return;

        OverrideMaterial = new Material(asset.PrefabMaterial);

        if (!string.IsNullOrEmpty(Def.shaderName) && Def.shaderName != OverrideMaterial.shader.name)
        {
            var shader = Shader.Find(Def.shaderName);
            if (shader == null)
                Debug.LogError($"asset `{asset.mu}`: shader `{Def.shaderName}` not found");
            else
                OverrideMaterial.shader = shader;
        }

        foreach (var kvp in Def.textures)
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

        foreach (var kvp in Def.floats) OverrideMaterial.SetFloat(kvp.Key, kvp.Value);

        foreach (var kvp in Def.colors) OverrideMaterial.SetColor(kvp.Key, kvp.Value);
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
