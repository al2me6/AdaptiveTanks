using System.Linq;
using AdaptiveTanks.Extensions;
using KSP.UI;
using UnityEngine;

namespace AdaptiveTanks;

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
        BuildAttachNodes();
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
    protected SegmentStack currentStack;

    protected void UpdateStacker()
    {
        stacker.TrueHeight = height;
        stacker.Diameter = diameter;
        stacker.CoreSegmentDef = Library.Segments.Values.First(); // TODO slider
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

    protected void RealizeGeometry(SegmentStack previous)
    {
        var anchor = part.GetOrCreateAnchor(CoreStackAnchorName);

        // TODO: adjust existing stack instead of spawning new stack.
        anchor.ClearChildren();
        RealizeGeometryFromScratch(currentStack, anchor);
    }

    protected void RecenterStack()
    {
        part.GetOrCreateRootAnchor().localPosition = Vector3.down * currentStack.ExtentCenter;
    }

    public void Restack()
    {
        UpdateStacker();
        var oldStack = currentStack;
        currentStack = stacker.Build();
        RealizeGeometry(oldStack);
        RecenterStack();
        UpdateAttachNodes();
    }

    #endregion

    #region attach node management

    public const string NodeStackTopId = "top";
    public const string NodeStackBottomId = "bottom";

    protected AttachNode nodeTop;
    protected AttachNode nodeBottom;
    protected AttachNode nodeSurface => part.srfAttachNode;

    protected void BuildAttachNodes()
    {
        part.attachNodes.Clear();
        nodeTop = part.AddStackAttachNode(NodeStackTopId, Vector3.up, Vector3.up);
        nodeBottom = part.AddStackAttachNode(NodeStackBottomId, Vector3.down, Vector3.down);
        part.AddSurfaceAttachNode(Vector3.forward, Vector3.back);
    }

    protected void UpdateAttachNodes()
    {
        nodeTop.MoveTo(Vector3.up * currentStack.HalfExtent);
        nodeBottom.MoveTo(Vector3.down * currentStack.HalfExtent);
    }

    #endregion
}
