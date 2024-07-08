﻿using System;
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

    private static readonly Dictionary<string, List<GameObject>> segmentMeshCache = new();

    protected bool RealizeGeometry(
        SegmentStack newStack, float diam, string anchorName, string materialId)
    {
        var anchor = part.GetOrCreateAnchor(anchorName);

        foreach (Transform segmentMesh in anchor)
        {
            segmentMeshCache.GetOrCreateValue(segmentMesh.name).Add(segmentMesh.gameObject);
        }

        var didInstantiateGO = false;

        foreach (var (asset, transformation) in newStack.IterSegments(diam))
        {
            if (!segmentMeshCache.GetOrCreateValue(asset.mu).TryPop(out var segmentMesh))
            {
                if (asset.Prefab == null) continue;
                segmentMesh = Instantiate(asset.Prefab);
                segmentMesh.name = asset.mu;
                didInstantiateGO = true;
            }

            segmentMesh.SetActive(true);
            segmentMesh.transform.NestToParent(anchor);
            segmentMesh.transform.SetLayerRecursive(part.gameObject.layer);
            transformation.ApplyTo(segmentMesh);

            if (asset.materials.Contains(materialId))
                asset.materials[materialId].ApplyTo(segmentMesh);
        }

        foreach (var entry in segmentMeshCache)
        {
            while (entry.Value.TryPop(out var segmentMesh)) Destroy(segmentMesh);
        }

        return didInstantiateGO;
    }

    protected void RealizeGeometry()
    {
        var didInstantiateGO = RealizeGeometry(
            segmentStacks!.Skin,
            segmentStacks.Diameter,
            SkinStackAnchorName,
            skinLinkedMaterial);
        didInstantiateGO |= RealizeGeometry(
            segmentStacks.Core,
            segmentStacks.Diameter,
            CoreStackAnchorName,
            coreLinkedMaterial);

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
        // TODO: skin transparency somehow?
        // part.GetOrCreateAnchor(SkinStackAnchorName).localPosition +=
        //     Vector3.forward * diameter * 1.5f;
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

        RealizeGeometry();
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
}
