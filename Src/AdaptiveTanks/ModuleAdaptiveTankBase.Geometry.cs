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

    protected SegmentStacks currentStacks;

    private static readonly Dictionary<string, List<GameObject>> segmentMeshCache = new();

    protected bool RealizeGeometry(SegmentStack newStack, float diam, string anchorName)
    {
        var anchor = part.GetOrCreateAnchor(anchorName);

        foreach (Transform segmentMesh in anchor)
        {
            segmentMeshCache.GetOrCreateValue(segmentMesh.name).Add(segmentMesh.gameObject);
        }

        var instantiatedNewGO = false;

        foreach (var (muPath, transformation) in newStack.IterSegments(diam))
        {
            if (!segmentMeshCache.GetOrCreateValue(muPath).TryPop(out var segmentMesh))
            {
                segmentMesh = GameDatabase.Instance.GetModel(muPath);

                if (segmentMesh == null)
                {
                    Debug.LogError($"model `{muPath}` was not found");
                    continue;
                }

                segmentMesh.name = muPath;
                instantiatedNewGO = true;
                Debug.Log($"instantiated new GO {muPath}");
            }

            segmentMesh.SetActive(true);
            segmentMesh.transform.NestToParent(anchor);
            segmentMesh.transform.SetLayerRecursive(part.gameObject.layer);
            transformation.ApplyTo(segmentMesh);
        }

        foreach (var entry in segmentMeshCache)
        {
            while (entry.Value.TryPop(out var segmentMesh))
            {
                Debug.Log($"destroyed GO instance {entry.Key}");
                Destroy(segmentMesh);
            }
        }

        return instantiatedNewGO;
    }

    protected void RealizeGeometry()
    {
        var didInstantiateGO =
            RealizeGeometry(currentStacks.Skin, currentStacks.Diameter, SkinStackAnchorName);
        didInstantiateGO |=
            RealizeGeometry(currentStacks.Core, currentStacks.Diameter, CoreStackAnchorName);

        if (didInstantiateGO) part.ResetAllRendererCaches();

        var skinDistortion = currentStacks.Skin.WorstDistortion();
        var coreDistortion = currentStacks.Core.WorstDistortion();
        sWorstDistortion = $"skin {skinDistortion:P1}; core {coreDistortion:P1}";
    }

    protected void RecenterStack()
    {
        part.GetOrCreateAnchor(SkinStackAnchorName).localPosition =
            part.GetOrCreateAnchor(CoreStackAnchorName).localPosition =
                Vector3.down * currentStacks.HalfHeight;
        // TODO: skin transparency somehow?
        // part.GetOrCreateAnchor(SkinStackAnchorName).localPosition +=
        //     Vector3.forward * diameter * 1.5f;
    }

    public void ReStack(bool isInitialize)
    {
        var oldDiameter = currentStacks?.Diameter;
        currentStacks = SegmentStacker.SolveStack(
            diameter,
            height,
            SkinSegments(),
            CoreSegments(),
            VolumetricMixtureRatio(),
            maxIntertankVolumetricDeviation);

        var solutionHeight = currentStacks.Height;
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

    protected AttachNode nodeTop;
    protected AttachNode nodeBottom;
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
        var halfHeight = currentStacks.HalfHeight;
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