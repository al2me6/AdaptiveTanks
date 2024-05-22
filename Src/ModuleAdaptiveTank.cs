using System.Linq;
using AdaptiveTanks.Extensions;
using KSP.UI;
using UnityEngine;

namespace AdaptiveTanks;

#nullable enable

public class ModuleAdaptiveTank : PartModule
{
    #region PAW

    public const string PAWName = "AdaptiveTanks";
    public const string PAWDispName = PAWName;

    [KSPField(isPersistant = true, guiName = "Diameter", guiActiveEditor = true,
        groupName = PAWName, groupDisplayName = PAWDispName)]
    [UI_FloatEdit(sigFigs = 4, useSI = true, unit = "m", scene = UI_Scene.Editor)]
    public float diameter;

    [KSPField(isPersistant = true, guiName = "Height", guiActiveEditor = true,
        groupName = PAWName, groupDisplayName = PAWDispName)]
    [UI_FloatEdit(sigFigs = 4, useSI = true, unit = "m", scene = UI_Scene.Editor)]
    public float height;

    #endregion

    #region configuration

    [KSPField] public float dimensionIncrementLarge = 1f;
    [KSPField] public float dimensionIncrementSmall = 0.25f;
    [KSPField] public float dimensionIncrementSlide = 0.01f;

    #endregion

    #region lifecycle

    public override void OnLoad(ConfigNode node)
    {
    }

    public override void OnIconCreate() => InitializeConfigurationAndModel();

    public override void OnStart(StartState state)
    {
        InitializeConfigurationAndModel();
        InitializeUI();
    }

    public void OnDestroy()
    {
    }

    protected void InitializeConfigurationAndModel()
    {
        FindStackAttachNodes();
        Restack();
    }

    protected void InitializeUI()
    {
        LinkPAWElements();
        InitializeConfigurablePAWParameters();
        UpdatePAWLimits();
    }

    #endregion

    #region PAW management

    protected void LinkPAWElements()
    {
        if (!HighLogic.LoadedSceneIsEditor) return;
        Fields[nameof(diameter)].AddSelfAndSymmetryListener(OnSizeModified);
        Fields[nameof(height)].AddSelfAndSymmetryListener(OnSizeModified);
    }

    protected void InitializeConfigurablePAWParameters()
    {
        if (!HighLogic.LoadedSceneIsEditor) return;
        Fields[nameof(diameter)].AsEditor<UI_FloatEdit>().SetIncrements(dimensionIncrementLarge,
            dimensionIncrementSmall, dimensionIncrementSlide);
        Fields[nameof(height)].AsEditor<UI_FloatEdit>().SetIncrements(dimensionIncrementLarge,
            dimensionIncrementSmall, dimensionIncrementSlide);
    }

    protected void UpdatePAWLimits()
    {
        if (!HighLogic.LoadedSceneIsEditor) return;
        Fields[nameof(diameter)].AsEditor<UI_FloatEdit>().SetMinMax(0.5f, 5f);
        Fields[nameof(height)].AsEditor<UI_FloatEdit>().SetMinMax(0.5f, 10f);
    }

    protected void OnSizeModified(BaseField f, object obj)
    {
        Restack();
    }

    #endregion

    #region stack generation

    public const string CoreStackAnchorName = "__ATCoreStack";

    public SegmentStacker stacker = new();
    protected SegmentStack? currentStack;

    protected SegmentStack? RunStacker()
    {
        stacker.TrueHeight = height;
        stacker.Diameter = diameter;
        stacker.CoreSegmentDef = Library.Segments.Values.First(); // TODO slider
        var oldStack = currentStack;
        currentStack = stacker.Build();
        return oldStack;
    }

    protected void RealizeGeometryFromScratch(SegmentStack stack, Transform anchor)
    {
        foreach (var (mu, transformation) in stack.IterSegments())
        {
            var segmentMesh = GameDatabase.Instance.GetModel(mu);
            segmentMesh.SetActive(true);
            segmentMesh.transform.NestToParent(anchor);
            transformation.ApplyTo(segmentMesh);
        }
    }

    protected void RealizeGeometry(SegmentStack? previous)
    {
        var anchor = part.GetOrCreateAnchor(CoreStackAnchorName);

        // TODO: adjust existing stack instead of spawning new stack.
        anchor.ClearChildren();
        RealizeGeometryFromScratch(currentStack!, anchor);
    }

    protected void RecenterStack()
    {
        part.GetOrCreateRootAnchor().localPosition = Vector3.down * currentStack!.ExtentCenter;
    }

    public void Restack()
    {
        var oldStack = RunStacker();
        RealizeGeometry(oldStack);
        RecenterStack();
        UpdateAttachNodes();
        MoveSurfaceAttachedChildren(oldStack);
    }

    #endregion

    #region attach node management

    [KSPField] public string nodeStackTopId = "top";
    [KSPField] public string nodeStackBottomId = "bottom";

    #nullable disable
    protected AttachNode nodeTop;
    protected AttachNode nodeBottom;
    #nullable enable
    protected AttachNode nodeSurface => part.srfAttachNode;

    protected void FindStackAttachNodes()
    {
        nodeTop = part.attachNodes.Find(node => node.id == nodeStackTopId);
        nodeBottom = part.attachNodes.Find(node => node.id == nodeStackBottomId);
    }

    protected void UpdateAttachNodes()
    {
        nodeTop.MoveTo(Vector3.up * currentStack!.HalfExtent);
        nodeBottom.MoveTo(Vector3.down * currentStack.HalfExtent);
        nodeSurface.MoveTo(Vector3.right * currentStack.Diameter / 2f);
    }

    protected void MoveSurfaceAttachedChildren(SegmentStack? oldStack)
    {
        if (oldStack == null) return;

        var deltaRadius = (diameter - oldStack.Diameter) / 2f;

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
