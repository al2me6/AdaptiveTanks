using System;
using System.Collections.Generic;
using AdaptiveTanks.Utils;
using ProceduralTools;
using UnityEngine;

namespace AdaptiveTanks;

public partial class ModuleAdaptiveTankBase
{
    public void InitializeModel()
    {
        FindStackAttachNodes();
        ReStack(true);
    }

    #region stacking

    public const string SkinStackAnchorName = "__ATSkinStack";
    public const string CoreStackAnchorName = "__ATCoreStack";

    protected SegmentStacks? segmentStacks;

    protected readonly Dictionary<Transform, HashSet<Renderer>> stackRenderersCache = new();

    private readonly Dictionary<string, List<GameObject>> segmentMeshCache = new();

    protected bool RealizeStackIncremental(
        SegmentStack stack, string anchorName, string materialId, bool fullRebuild)
    {
        var anchor = part.GetOrCreateAnchor(anchorName);
        var rendererCache = stackRenderersCache.GetOrCreateValue(anchor);

        foreach (Transform segmentMesh in anchor)
            segmentMeshCache.GetOrCreateValue(segmentMesh.name).Add(segmentMesh.gameObject);

        var didInstantiateGO = false;

        foreach (var (asset, transformation) in stack.IterSegments(diameter))
        {
            if (fullRebuild ||
                !segmentMeshCache.GetOrCreateValue(asset.mu).TryPop(out var segmentMesh))
            {
                if (asset.Prefab == null) continue;
                segmentMesh = Instantiate(asset.Prefab);
                segmentMesh.name = asset.mu;
                segmentMesh.transform.NestToParent(anchor);
                segmentMesh.transform.SetLayerRecursive(part.gameObject.layer);
                rendererCache.UnionWith(segmentMesh.GetComponentsInChildren<Renderer>());
                didInstantiateGO = true;
            }

            segmentMesh.SetActive(true);
            transformation.ApplyTo(segmentMesh);

            if (asset.materials.Contains(materialId))
                asset.materials[materialId].ApplyTo(segmentMesh);
        }

        foreach (var entry in segmentMeshCache)
        {
            while (entry.Value.TryPop(out var segmentMesh))
            {
                foreach (var renderer in segmentMesh.GetComponentsInChildren<Renderer>())
                    rendererCache.Remove(renderer);
                Destroy(segmentMesh);
            }
        }

        return didInstantiateGO;
    }

    protected void RealizeGeometry(bool isInitialize)
    {
        var didInstantiateGO = RealizeStackIncremental(
            segmentStacks!.Skin,
            SkinStackAnchorName,
            skinLinkedMaterial,
            isInitialize);
        didInstantiateGO |= RealizeStackIncremental(
            segmentStacks.Core,
            CoreStackAnchorName,
            coreLinkedMaterial,
            isInitialize);

        if (didInstantiateGO) part.ResetAllRendererCaches();

        var skinDistortion = segmentStacks.Skin.WorstDistortion();
        var coreDistortion = segmentStacks.Core.WorstDistortion();
        sWorstDistortion = $"skin {skinDistortion:P1}; core {coreDistortion:P1}";
    }

    protected void RecenterStack()
    {
        part.GetOrCreateAnchor(SkinStackAnchorName).localPosition =
            part.GetOrCreateAnchor(CoreStackAnchorName).localPosition =
                Vector3.down * segmentStacks!.HalfHeight;
    }

    public void ReStack(bool isInitialize)
    {
        var oldDiameter = segmentStacks?.Diameter;
        segmentStacks = SegmentStacker.SolveStack(
            diameter,
            height,
            SkinSegments(),
            CoreSegments(),
            VolumetricMixtureRatio(),
            maxIntertankVolumetricDeviation);

        var solutionHeight = segmentStacks.Height;
        if (!Mathf.Approximately(height, solutionHeight))
        {
            Debug.LogError($"solution height ({solutionHeight}) differs from target ({height})");
            height = solutionHeight;
            MonoUtilities.RefreshPartContextWindow(part);
        }

        RealizeGeometry(isInitialize);
        RecenterStack();
        UpdateAttachNodes();
        MoveSurfaceAttachedChildren(oldDiameter);

        if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
            DragCubeTool.UpdateDragCubes(part);

        UpdateVolume(isInitialize);
    }

    #endregion

    #region attach node management

    protected AttachNode nodeTop = null!;
    protected AttachNode nodeBottom = null!;
    protected AttachNode nodeSurface => part.srfAttachNode;

    protected void FindStackAttachNodes()
    {
        nodeTop = part.attachNodes.Find(node => node.id == nodeStackTopId);
        nodeBottom = part.attachNodes.Find(node => node.id == nodeStackBottomId);
    }

    protected int CalculateAttachNodeSize() =>
        Math.Min((int)(diameter / attachNodeSizeIncrementFactor), maxAttachNodeSize);

    protected void UpdateAttachNodes()
    {
        var halfHeight = segmentStacks!.HalfHeight;
        nodeTop.MoveTo(Vector3.up * halfHeight);
        nodeBottom.MoveTo(Vector3.down * halfHeight);
        nodeSurface.MoveTo(Vector3.right * diameter * 0.5f);
        nodeTop.size = nodeBottom.size = nodeSurface.size = CalculateAttachNodeSize();
    }

    protected void MoveSurfaceAttachedChildren(float? oldDiameter)
    {
        if (oldDiameter == null) return;

        var deltaRadius = (diameter - oldDiameter.Value) / 2f;

        foreach (var child in part.IterSurfaceAttachedChildren())
        {
            var worldPos = child.transform.position;
            var localPos = transform.InverseTransformPoint(worldPos);
            if (deltaRadius != 0)
            {
                var localPushNrm =
                    Vector3.ProjectOnPlane(localPos, Vector3.up).normalized * deltaRadius;
                var worldPushNrm = transform.TransformVector(localPushNrm);
                child.PushBy(worldPushNrm);
            }
            // TODO: take local geometry at position of attachment into account.
            // Current logic only works for cylindrical objects.
            // TODO: shift vertically on height change. This will depend on cap vs body.
        }
    }

    #endregion

    #region shader property management

    private MaterialPropertyBlock? mpb;
    private PartUtils.PartMPBProperties partMPBProps = new();

    protected void RefreshMPB()
    {
        if (mpb == null) mpb = new MaterialPropertyBlock();

        mpb.Clear();
        part.ExtractMPBProperties(ref partMPBProps);
        partMPBProps.WriteTo(ref mpb);

        foreach (var kvp in stackRenderersCache)
        {
            var anchor = kvp.Key;
            if (anchor.name == SkinStackAnchorName && transparentSkin)
                mpb.SetFloat(PropertyIDs._Opacity, transparentSkinOpacity);
            foreach (var renderer in kvp.Value) renderer.SetPropertyBlock(mpb);
            mpb.SetFloat(PropertyIDs._Opacity, partMPBProps.Opacity);
        }
    }

    #endregion
}
