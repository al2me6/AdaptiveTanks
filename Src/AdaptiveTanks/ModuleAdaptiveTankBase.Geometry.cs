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

    protected SegmentStacks? stacks;

    protected readonly Dictionary<Transform, HashSet<Renderer>> stackRenderersCache = new();

    public void ReStack(bool isInitialize)
    {
        var surfaceAttachMaps = BuildSurfaceAttachMaps();

        stacks = SegmentStacker.SolveStack(
            diameter,
            height,
            SkinSegments(),
            CoreSegments(),
            VolumetricMixtureRatio(),
            maxIntertankVolumetricDeviation);

        var solutionHeight = stacks.Height;
        if (!MathUtils.ApproxEqAbs(height, solutionHeight, SegmentStacker.Tolerance))
        {
            Debug.LogError($"solution height ({solutionHeight}) differs from target ({height})");
            height = solutionHeight;
            MonoUtilities.RefreshPartContextWindow(part);
        }

        RealizeStacks(fullRebuild: isInitialize);
        RecenterStacks();

        // Only push parts when not initializing.
        // When dropping a new part, there's nothing to push.
        // When loading a vessel, the parts are already in the right places and pushing
        // will result in catastrophe.
        UpdateAttachNodes(pushParts: !isInitialize);
        UpdateStackSymmetry();
        if (!isInitialize) PushSurfaceAttachedChildren(surfaceAttachMaps!);
        CheckResetLayer();

        if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
            DragCubeTool.UpdateDragCubes(part);

        UpdateVolume(isInitialize);
    }

    protected void RealizeStacks(bool fullRebuild)
    {
        var didInstantiateGO = RealizeStackIncremental(
            stacks!.Skin,
            SkinStackAnchorName,
            skinLinkedMaterial,
            fullRebuild);
        didInstantiateGO |= RealizeStackIncremental(
            stacks.Core,
            CoreStackAnchorName,
            coreLinkedMaterial,
            fullRebuild);

        if (didInstantiateGO) part.ResetAllRendererCaches();

        var skinDistortion = stacks.Skin.WorstDistortion();
        var coreDistortion = stacks.Core.WorstDistortion();
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
                mesh.SetLayerExceptTriggers(gameObject.layer);

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
                mesh.SetActive(false);
                Destroy(mesh);
            }
        }

        return didInstantiateGO;
    }

    protected void RecenterStacks()
    {
        stacks!.ApplyAnchorPosition(part.GetOrCreateAnchor(SkinStackAnchorName));
        stacks!.ApplyAnchorPosition(part.GetOrCreateAnchor(CoreStackAnchorName));
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

        var halfHeight = stacks!.HalfHeight;
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
        var terminator = stacks!.Skin.GetTerminator(position);
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

    #endregion

    #region surface-attached children management

    private int? originalLayer = null;

    protected void OverrideLayer(UnityLayer layer)
    {
        if (originalLayer == (int)layer || gameObject.layer == (int)layer) return;
        originalLayer = gameObject.layer;
        gameObject.SetLayerExceptTriggers(layer);
    }

    protected void CheckResetLayer()
    {
        if (originalLayer == null) return;
        gameObject.SetLayerExceptTriggers(originalLayer.Value);
        originalLayer = null;
    }

    protected const UnityLayer RaycastLayer = UnityLayer.Water;

    protected readonly record struct SurfaceAttachMap(Cap? Region, Cylindrical NormalizedPosition);

    protected float GetLocalRadiusAtPosition(float theta, float y)
    {
        OverrideLayer(RaycastLayer);

        var nominalRad = stacks!.Diameter * 0.5f;
        var maxRad = nominalRad * 1.5f;
        var raycastTol = nominalRad * 0.005f;

        var localRayOrigin = (Vector3)new Cylindrical(maxRad, theta, y);
        var localRayDir = (Vector3)new Cylindrical(-1f, theta, 0f);
        Ray worldRay = new(
            transform.TransformPoint(localRayOrigin),
            transform.TransformDirection(localRayDir));

        if (Physics.SphereCast(worldRay, raycastTol, out var hit, maxRad, 1 << (int)RaycastLayer))
        {
            var hitVector = worldRay.direction * hit.distance;
            return maxRad - transform.InverseTransformVector(hitVector).magnitude;
        }

        return nominalRad;
    }

    protected Dictionary<Part, SurfaceAttachMap>? BuildSurfaceAttachMaps()
    {
        if (stacks == null) return null;

        Dictionary<Part, SurfaceAttachMap> attachMaps = new();

        foreach (var child in part.IterSurfaceAttachedChildren())
        {
            var worldPos = child.transform.TransformPoint(child.srfAttachNode.position);
            var localPos = (Cylindrical)transform.InverseTransformPoint(worldPos);

            var localRad = GetLocalRadiusAtPosition(localPos.theta, localPos.y);
            var radOffset = localPos.r - localRad;

            var attachHeight = localPos.y + stacks.HalfHeight;
            var (region, normHeight) = stacks.Skin.GetRegionNormalizedHeight(attachHeight);

            var normPos = new Cylindrical(radOffset, localPos.theta, normHeight);
            attachMaps[child] = new SurfaceAttachMap(region, normPos);
        }

        return attachMaps;
    }

    protected void PushSurfaceAttachedChildren(Dictionary<Part, SurfaceAttachMap> attachMaps)
    {
        foreach (var kvp in attachMaps)
        {
            var (child, (region, normPos)) = (kvp.Key, kvp.Value);
            if (child.srfAttachNode.attachedPart != part)
            {
                Debug.LogError(
                    $"surface attach map contained mismatched child `{child.persistentId}`");
                continue;
            }

            var attachHeight = stacks!.Skin.GetRealHeightFromNormalizedRegion(region, normPos.y);
            var localPosY = attachHeight - stacks.HalfHeight;
            var localRad = GetLocalRadiusAtPosition(normPos.theta, localPosY);

            var localPos = (Vector3)new Cylindrical(localRad + normPos.r, normPos.theta, localPosY);
            var worldPos = transform.TransformPoint(localPos);

            var currWorldPos = child.transform.TransformPoint(child.srfAttachNode.position);
            child.PushBy(worldPos - currWorldPos);
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
