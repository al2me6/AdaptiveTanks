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
        if (PartUtils.IsLoadingPrefab) BuildStackAttachNodes();
        FindStackAttachNodes();
        ReStack(true);
    }

    #region stacking

    public const string SkinStackAnchorName = "__ATSkinStack";
    public const string CoreStackAnchorName = "__ATCoreStack";

    protected SegmentStacks? segmentStacks;

    protected readonly Dictionary<Transform, HashSet<Renderer>> stackRenderersCache = new();

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

        RealizeGeometry(fullRebuild: isInitialize);
        RecenterStack();
        UpdateAttachNodes(pushParts: !isInitialize);
        if (!isInitialize) MoveSurfaceAttachedChildren(oldDiameter);

        if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
            DragCubeTool.UpdateDragCubes(part);

        UpdateVolume(isInitialize);
    }

    protected void RealizeGeometry(bool fullRebuild)
    {
        var didInstantiateGO = RealizeStackIncremental(
            segmentStacks!.Skin,
            SkinStackAnchorName,
            skinLinkedMaterial,
            fullRebuild);
        didInstantiateGO |= RealizeStackIncremental(
            segmentStacks.Core,
            CoreStackAnchorName,
            coreLinkedMaterial,
            fullRebuild);

        if (didInstantiateGO) part.ResetAllRendererCaches();

        var skinDistortion = segmentStacks.Skin.WorstDistortion();
        var coreDistortion = segmentStacks.Core.WorstDistortion();
        sWorstDistortion = $"skin {skinDistortion:P1}; core {coreDistortion:P1}";
    }

    private readonly Dictionary<string, List<GameObject>> _existingMeshes = new();

    protected bool RealizeStackIncremental(
        SegmentStack stack, string anchorName, string materialId, bool fullRebuild)
    {
        var anchor = part.GetOrCreateAnchor(anchorName);
        var rendererCache = stackRenderersCache.GetOrCreateValue(anchor);

        foreach (Transform mesh in anchor)
            _existingMeshes.GetOrCreateValue(mesh.name).Add(mesh.gameObject);

        var didInstantiateGO = false;

        foreach (var (asset, transformation) in stack.IterSegments(diameter))
        {
            if (fullRebuild || !_existingMeshes.GetOrCreateValue(asset.mu).TryPop(out var mesh))
            {
                if (asset.Prefab == null) continue;

                mesh = Instantiate(asset.Prefab);
                mesh.name = asset.mu;

                mesh.SetActive(true);
                mesh.transform.NestToParent(anchor);
                mesh.transform.SetLayerRecursive(part.gameObject.layer);

                rendererCache.UnionWith(mesh.GetComponentsInChildren<Renderer>());

                didInstantiateGO = true;
            }

            transformation.ApplyTo(mesh);

            if (asset.materials.Contains(materialId)) asset.materials[materialId].ApplyTo(mesh);
        }

        foreach (var entry in _existingMeshes)
        {
            while (entry.Value.TryPop(out var mesh))
            {
                foreach (var renderer in mesh.GetComponentsInChildren<Renderer>())
                    rendererCache.Remove(renderer);
                Destroy(mesh);
            }
        }

        return didInstantiateGO;
    }

    protected void RecenterStack()
    {
        part.GetOrCreateAnchor(SkinStackAnchorName).localPosition =
            part.GetOrCreateAnchor(CoreStackAnchorName).localPosition =
                Vector3.down * segmentStacks!.HalfHeight;
    }

    #endregion

    #region attach node management

    // We generate all nodes once when building the prefab. This is indistinguishable from nodes
    // parsed by the stock game.

    public const string nodeSurfaceId = "srfAttach";

    protected AttachNode nodeTop = null!;
    protected AttachNode nodeBottom = null!;
    protected AttachNode nodeSurface => part.srfAttachNode;

    protected void BuildStackAttachNodes()
    {
        if (!PartUtils.IsLoadingPrefab)
        {
            Debug.LogError("cannot generate attach nodes outside of prefab");
            return;
        }

        part.attachNodes.Clear();
        part.attachNodes.Add(
            AttachNodeUtils.New(AttachNode.NodeType.Stack, nodeStackTopId, Vector3.up, part));
        part.attachNodes.Add(
            AttachNodeUtils.New(AttachNode.NodeType.Stack, nodeStackBottomId, Vector3.down, part));

        part.srfAttachNode =
            AttachNodeUtils.New(AttachNode.NodeType.Surface, nodeSurfaceId, Vector3.right, part);
    }

    protected void FindStackAttachNodes()
    {
        nodeTop = part.attachNodes.Find(node => node.id == nodeStackTopId);
        nodeBottom = part.attachNodes.Find(node => node.id == nodeStackBottomId);
    }

    protected int CalculateAttachNodeSize() =>
        Math.Min((int)(diameter / attachNodeSizeIncrementFactor), maxAttachNodeSize);

    protected bool StackNodeIsEnabled(CapPosition position) =>
        !Segment(SegmentLayer.Skin, position.AsRoleTerminator()).terminatorDisableStackNode;

    protected void UpdateAttachNodes(bool pushParts)
    {
        nodeSurface.MoveTo(Vector3.right * diameter * 0.5f, pushParts);

        var halfHeight = segmentStacks!.HalfHeight;
        nodeTop.MoveTo(Vector3.up * halfHeight, pushParts);
        nodeBottom.MoveTo(Vector3.down * halfHeight, pushParts);

        nodeTop.size = nodeBottom.size = nodeSurface.size = CalculateAttachNodeSize();

        nodeTop.SetVisibility(StackNodeIsEnabled(CapPosition.Top));
        nodeBottom.SetVisibility(StackNodeIsEnabled(CapPosition.Bottom));
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
