using System.Linq;
using AdaptiveTanks.Extensions;
using UnityEngine;

namespace AdaptiveTanks;

public class ModuleAdaptiveTank : PartModule
{
    #region constants

    public const string CoreStackAnchorName = "__ATCoreStack";

    #endregion

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

    #region internal state

    public SegmentStacker stacker = new();
    protected SegmentStack currentStack;

    # endregion

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

    protected void UpdateStacker()
    {
        stacker.NormalizedHeight = height / diameter;
        stacker.CoreSegmentDef = Library.Segments.Values.First(); // TODO slider
    }

    public void Restack()
    {
        UpdateStacker();
        var newStack = stacker.Build();
        newStack.RealizeGeometry(part, CoreStackAnchorName, diameter, currentStack);
        currentStack = newStack;
        RecenterStack();
    }

    protected void RecenterStack()
    {
        var extentCenter = (currentStack.NormalizedExtent.x + currentStack.NormalizedExtent.y) /
            2f * diameter;
        part.GetOrCreateRootAnchor().localPosition = Vector3.down * extentCenter;
    }

    #endregion
}
