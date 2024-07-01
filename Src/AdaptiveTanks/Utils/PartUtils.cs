using UnityEngine;

namespace AdaptiveTanks.Utils;

public static class PartUtils
{
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
}
