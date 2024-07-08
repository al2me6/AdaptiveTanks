using UnityEngine;

namespace AdaptiveTanks.Utils;

public static class PartUtils
{
    public static bool IsLoadingPrefab => !PartLoader.Instance.IsReady();

    /// Reset all renderer caches, including the highlighter cache.
    /// Re-apply transparency state if necessary.
    public static void ResetAllRendererCaches(this Part part)
    {
        part.ResetModelRenderersCache();
        part.ResetModelMeshRenderersCache();
        part.ResetModelSkinnedMeshRenderersCache();
        part.rendererlistscreated = false;
        if (!part.IsAttachedToVessel()) part.SetOpacity(0.4f);
    }

    public static void PushBy(this Part part, Vector3 worldDelta)
    {
        // Gotmachine (private communication): `orgPos` is the part position relative to the root
        // part and is what is relied on generally. `attPos` and `attPos0` are legacy things that
        // aren't used and shouldn't be relied upon.
        part.orgPos = part.transform.position += worldDelta;
    }

    public static bool IsAttachedToVessel(this Part part)
    {
        if (!HighLogic.LoadedSceneIsEditor || !part.editorStarted) return true;
        return part.localRoot == EditorLogic.RootPart;
    }

    public record PartMPBProperties
    {
        public float Opacity { get; set; }
        public float? RimFalloff { get; set; }
        public Color RimColor { get; set; }
        public Color? TemperatureColor { get; set; }

        public void WriteTo(ref MaterialPropertyBlock mpb)
        {
            mpb.SetFloat(PropertyIDs._Opacity, Opacity);
            if (RimFalloff != null) mpb.SetFloat(PropertyIDs._RimFalloff, RimFalloff.Value);
            mpb.SetColor(PropertyIDs._RimColor, RimColor);
            if (TemperatureColor != null)
                mpb.SetColor(PhysicsGlobals.TemperaturePropertyID, TemperatureColor.Value);
        }
    }

    public static void ExtractMPBProperties(this Part part, ref PartMPBProperties props)
    {
        var opacity = part.mpb.GetFloat(PropertyIDs._Opacity);
        props.Opacity = opacity > 0f ? opacity : 1f;

        var rimFalloff = part.mpb.GetFloat(PropertyIDs._RimFalloff);
        if (rimFalloff != 0f) props.RimFalloff = rimFalloff;

        props.RimColor = part.mpb.GetColor(PropertyIDs._RimColor);

        props.TemperatureColor = HighLogic.LoadedSceneIsFlight
            ? part.mpb.GetColor(PhysicsGlobals.TemperaturePropertyID)
            : null;
    }
}
