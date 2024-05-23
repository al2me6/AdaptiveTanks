using System;
using AdaptiveTanks.Extensions;
using KSP.UI;
using UnityEngine;

namespace AdaptiveTanks;

public abstract class ModuleAdaptiveTankBase : PartModule
{
    #region lifecycle

    public override void OnIconCreate() => InitializeConfigurationAndModel();

    public override void OnStart(StartState state)
    {
        InitializeConfigurationAndModel();
        if (HighLogic.LoadedSceneIsEditor) InitializeEditorPAW();
    }

    protected virtual void InitializeConfigurationAndModel()
    {
        FindStackAttachNodes();
        Restack();
    }

    protected virtual void InitializeEditorPAW()
    {
        InitializeDimensionSelectors();
        UpdateDimensionLimits();
    }

    #endregion

    #region PAW

    public const string PAWName = "AdaptiveTanks";
    public const string PAWDispName = PAWName;

    [KSPField] public float dimensionIncrementLarge = 1f;
    [KSPField] public float dimensionIncrementSmall = 0.25f;
    [KSPField] public float dimensionIncrementSlide = 0.01f;

    [KSPField(isPersistant = true, guiName = "Diameter", guiActiveEditor = true,
        groupName = PAWName, groupDisplayName = PAWDispName)]
    [UI_FloatEdit(sigFigs = 4, useSI = true, unit = "m", scene = UI_Scene.Editor)]
    public float diameter;

    [KSPField(isPersistant = true, guiName = "Height", guiActiveEditor = true,
        groupName = PAWName, groupDisplayName = PAWDispName)]
    [UI_FloatEdit(sigFigs = 4, useSI = true, unit = "m", scene = UI_Scene.Editor)]
    public float height;

    protected void InitializeDimensionSelectors()
    {
        Fields[nameof(diameter)].AddSelfAndSymmetryListener(OnDimensionModified);
        Fields[nameof(height)].AddSelfAndSymmetryListener(OnDimensionModified);
        Fields[nameof(diameter)].AsEditor<UI_FloatEdit>().SetIncrements(dimensionIncrementLarge,
            dimensionIncrementSmall, dimensionIncrementSlide);
        Fields[nameof(height)].AsEditor<UI_FloatEdit>().SetIncrements(dimensionIncrementLarge,
            dimensionIncrementSmall, dimensionIncrementSlide);
    }

    protected abstract void UpdateDimensionLimits();

    protected virtual void OnDimensionModified(BaseField f, object obj)
    {
        Restack();
    }

    #endregion

    #region style management

    public abstract SelectedSegmentDefs GetSelectedSkinSegments();
    public abstract SelectedSegmentDefs GetSelectedCoreSegments();

    #endregion

    #region stack generation

    public const string SkinStackAnchorName = "__ATSkinStack";
    public const string CoreStackAnchorName = "__ATCoreStack";

    protected SkinAndCore<SegmentStack> currentStacks;

    protected void RealizeGeometry(SegmentStack current, string anchorName)
    {
        // TODO: adjust existing stack instead of spawning new stack.
        var anchor = part.GetOrCreateAnchor(anchorName);
        anchor.ClearChildren();
        foreach (var (mu, transformation) in current.IterSegments())
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
    }

    protected void RecenterStack()
    {
        part.GetOrCreateAnchor(SkinStackAnchorName).localPosition =
            Vector3.down * currentStacks.Skin.ExtentCenter;
        part.GetOrCreateAnchor(CoreStackAnchorName).localPosition =
            Vector3.down * currentStacks.Core.ExtentCenter;
    }

    public abstract SkinAndCore<SegmentStack> SolveStack(StackerParameters parameters);

    public void Restack()
    {
        var oldDiameter = currentStacks?.Diameter();
        var parameters = new StackerParameters(
            height,
            diameter,
            GetSelectedSkinSegments(),
            GetSelectedCoreSegments()
        );
        currentStacks = SolveStack(parameters);
        RealizeGeometry();
        RecenterStack();
        UpdateAttachNodes();
        MoveSurfaceAttachedChildren(oldDiameter);
    }

    #endregion

    #region attach node management

    // Nodes with these names (e.g. node_stack_top, _bottom) will be used as the stack nodes.
    [KSPField] public string nodeStackTopId = "top";
    [KSPField] public string nodeStackBottomId = "bottom";

    // For every increase in diameter by this amount, increment the size of the attachment node by 1.
    [KSPField] public float attachNodeSizeIncrementFactor = 1.25f;
    [KSPField] public int maxAttachNodeSize = 6;


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
                child.transform.position += worldPushNrm;
            }
            // TODO: take local geometry at position of attachment into account.
            // Current logic only works for cylindrical objects.
            // TODO: shift vertically on height change. This will depend on cap vs body.
        }
    }

    #endregion
}
