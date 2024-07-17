using System;
using System.Collections.Generic;
using System.Linq;
using AdaptiveTanks.Utils;
using ProceduralTools;
using UnityEngine;

namespace AdaptiveTanks;

using Cap = CapPosition;

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
        if (!MathUtils.ApproxEqAbs(height, solutionHeight, SegmentStacker.Tolerance))
        {
            Debug.LogError($"solution height ({solutionHeight}) differs from target ({height})");
            height = solutionHeight;
            MonoUtilities.RefreshPartContextWindow(part);
        }

        RealizeGeometry(fullRebuild: isInitialize);

        RecenterStack();

        // Only push parts when not initializing.
        // When dropping a new part, there's nothing to push.
        // When loading a vessel, the parts are already in the right places and pushing
        // will result in catastrophe.
        UpdateAttachNodes(pushParts: !isInitialize);
        if (!isInitialize) MoveSurfaceAttachedChildren(oldDiameter!.Value);
        UpdateStackSymmetry();

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

        foreach (var placement in stack.Placements)
        {
            var asset = placement.Asset;
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

            placement.RealizeWith(mesh);

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

    public const string NodeStackTopId = "top";
    public const string NodeStackBottomId = "bottom";
    public const string NodeSurfaceId = "srfAttach";

    public static string NodeDynamicTag(Cap position) => position switch
    {
        Cap.Top => "__ATDynTop",
        Cap.Bottom => "__ATDynBottom",
        _ => throw new ArgumentOutOfRangeException(nameof(position))
    };

    protected AttachNode nodeTop = null!;
    protected AttachNode nodeBottom = null!;
    protected AttachNode nodeSurface => part.srfAttachNode;

    protected readonly Dictionary<Cap, List<AttachNode>> nodeDynamicPool = new()
    {
        [Cap.Top] = [],
        [Cap.Bottom] = []
    };

    protected void BuildStackAttachNodes()
    {
        if (!PartUtils.IsLoadingPrefab)
        {
            Debug.LogError("cannot generate attach nodes outside of prefab");
            return;
        }

        part.attachNodes.Add(
            AttachNodeUtils.New(AttachNode.NodeType.Stack, NodeStackTopId, Vector3.up, part));
        part.attachNodes.Add(
            AttachNodeUtils.New(AttachNode.NodeType.Stack, NodeStackBottomId, Vector3.down, part));

        part.srfAttachNode =
            AttachNodeUtils.New(AttachNode.NodeType.Surface, NodeSurfaceId, Vector3.right, part);

        foreach (var position in nodeDynamicPool.Keys)
        {
            var maxNodeCount = Library<StyleDefSkin>
                .GetAll(skinStyles)
                .SelectMany(style => style.Segments[position.AsRoleTerminator()])
                .Select(seg => seg.ExtraNodeCount)
                .Max();

            for (var i = 0; i < maxNodeCount; ++i)
            {
                part.attachNodes.Add(AttachNodeUtils.New(
                    AttachNode.NodeType.Stack, $"{NodeDynamicTag(position)}{i}", Vector3.up, part));
            }

            Debug.Log($"generated {maxNodeCount} dynamic {position} nodes");
        }
    }

    protected void FindStackAttachNodes()
    {
        foreach (var node in part.attachNodes)
        {
            if (node.id == NodeStackTopId)
                nodeTop = node;
            else if (node.id == NodeStackBottomId)
                nodeBottom = node;
            else
                foreach (var role in nodeDynamicPool.Keys)
                {
                    if (!node.id.StartsWith(NodeDynamicTag(role))) continue;
                    nodeDynamicPool[role].Add(node);
                    break;
                }
        }
    }

    protected int CalculateAttachNodeSize() =>
        Math.Min((int)(diameter / attachNodeSizeIncrementFactor), maxAttachNodeSize);

    protected bool StackNodeIsEnabled(Cap position) =>
        !Segment(SegmentLayer.Skin, position.AsRoleTerminator()).terminatorDisableStackNode;

    protected void UpdateAttachNodes(bool pushParts)
    {
        nodeSurface.MoveTo(Vector3.right * diameter * 0.5f, pushParts);

        var halfHeight = segmentStacks!.HalfHeight;
        nodeTop.MoveTo(Vector3.up * halfHeight, pushParts);
        nodeBottom.MoveTo(Vector3.down * halfHeight, pushParts);

        nodeTop.SetVisibility(StackNodeIsEnabled(Cap.Top));
        nodeBottom.SetVisibility(StackNodeIsEnabled(Cap.Bottom));

        var nodeSize = CalculateAttachNodeSize();
        nodeTop.size = nodeBottom.size = nodeSurface.size = nodeSize;

        foreach (var position in nodeDynamicPool.Keys)
            UpdateExtraAttachNodes(position, nodeSize, pushParts);
    }

    protected void UpdateExtraAttachNodes(Cap position, int nodeSize, bool pushParts)
    {
        // Shrink the sizes of extra nodes by 1.
        nodeSize = Math.Max(0, nodeSize - 1);

        var pool = nodeDynamicPool[position];
        var terminator = segmentStacks!.Skin.GetTerminator(position);
        var segmentMesh = terminator.RealizedMesh;
        var numExtraNodes = 0;

        if (segmentMesh != null)
        {
            foreach (var extraNode in terminator.Asset.extraNodes)
            {
                var node = pool[numExtraNodes];

                var worldNodePos = segmentMesh.TransformPoint(extraNode.position);
                var localNodePos = transform.InverseTransformPoint(worldNodePos);
                node.MoveTo(localNodePos, pushParts);

                var worldNodeOrient = segmentMesh.TransformDirection(extraNode.orientation);
                // Account for mesh flipping manually.
                if (terminator.Scale.y < 0f) worldNodeOrient.y *= -1f;
                var localNodeOrient = transform.InverseTransformDirection(worldNodeOrient);
                // Let's not rotate parts with the node orientation. That way probably lies madness.
                node.SetOrientation(localNodeOrient);

                node.Show();
                node.size = nodeSize;

                ++numExtraNodes;
            }
        }

        for (; numExtraNodes < pool.Count; ++numExtraNodes)
        {
            // Nodes are used sequentially. All subsequent ones will already be hidden.
            if (pool[numExtraNodes].IsHidden()) break;
            pool[numExtraNodes].Hide();
        }
    }

    protected void UpdateStackSymmetry()
    {
        var symmetryTop = Segment(SegmentLayer.Skin, Cap.Top).terminatorStackSymmetry;
        var symmetryBottom = Segment(SegmentLayer.Skin, Cap.Bottom).terminatorStackSymmetry;
        part.stackSymmetry = Math.Max(symmetryTop, symmetryBottom);
    }

    protected void MoveSurfaceAttachedChildren(float oldDiameter)
    {
        var deltaRadius = (diameter - oldDiameter) / 2f;

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
