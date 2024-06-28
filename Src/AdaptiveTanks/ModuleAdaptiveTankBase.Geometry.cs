using System;
using AdaptiveTanks.Extensions;
using KSP.UI;
using UnityEngine;

namespace AdaptiveTanks;

public partial class ModuleAdaptiveTankBase
{
    public void InitializeModel()
    {
        FindStackAttachNodes();
        ReStack();
    }

    #region stacking

    public const string SkinStackAnchorName = "__ATSkinStack";
    public const string CoreStackAnchorName = "__ATCoreStack";

    protected SkinAndCore<SegmentStack> currentStacks;

    protected void RealizeGeometry(SegmentStack newStack, string anchorName)
    {
        // TODO: adjust existing stack instead of spawning new stack.
        var anchor = part.GetOrCreateAnchor(anchorName);
        anchor.ClearChildren();

        Debug.Log($"{anchorName} solution:\n{newStack.DebugPrint()}");

        foreach (var (mu, transformation) in newStack.IterSegments())
        {
            var segmentMesh = GameDatabase.Instance.GetModel(mu);
            if (segmentMesh == null)
            {
                Debug.LogError($"model {mu} was not found");
                continue;
            }

            segmentMesh.SetActive(true);
            segmentMesh.transform.NestToParent(anchor);
            segmentMesh.transform.SetLayerRecursive(part.gameObject.layer);
            transformation.ApplyTo(segmentMesh);
        }
    }

    protected void RealizeGeometry()
    {
        RealizeGeometry(currentStacks.Skin, SkinStackAnchorName);
        RealizeGeometry(currentStacks.Core, CoreStackAnchorName);

        var skinDistortion = currentStacks.Skin.WorstDistortion();
        var coreDistortion = currentStacks.Core.WorstDistortion();
        sWorstDistortion = $"skin {skinDistortion:P1}; core {coreDistortion:P1}";
    }

    protected void RecenterStack()
    {
        part.GetOrCreateAnchor(SkinStackAnchorName).localPosition =
            part.GetOrCreateAnchor(CoreStackAnchorName).localPosition =
                Vector3.down * currentStacks.HalfHeight();
        part.GetOrCreateAnchor(SkinStackAnchorName).localPosition +=
            Vector3.forward * diameter * 1.5f;
    }

    public void ReStack()
    {
        var oldDiameter = currentStacks?.Diameter();
        currentStacks = SegmentStacker.SolveStack(
            diameter,
            height,
            SkinSegments(),
            CoreSegments(),
            [intertankFraction, 1f - intertankFraction]);
        RealizeGeometry();
        RecenterStack();
        UpdateAttachNodes();
        MoveSurfaceAttachedChildren(oldDiameter);
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
        nodeTop.MoveTo(Vector3.up * currentStacks.HalfHeight());
        nodeBottom.MoveTo(Vector3.down * currentStacks.HalfHeight());
        nodeSurface.MoveTo(Vector3.right * currentStacks.Diameter() / 2f);
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
