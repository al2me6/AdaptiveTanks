using System.Linq;
using AdaptiveTanks.Extensions;

namespace AdaptiveTanks;

public class ModuleAdaptiveTank : PartModule
{
    #region PAW

    public const string PAWName = "AdaptiveTanks";
    public const string PAWDispName = PAWName;

    [KSPField(isPersistant = true, guiName = "Diameter", guiActiveEditor = true,
        groupName = PAWName, groupDisplayName = PAWDispName)]
    [UI_FloatEdit(scene = UI_Scene.Editor)]
    public float diameter;

    [KSPField(isPersistant = true, guiName = "Height", guiActiveEditor = true,
        groupName = PAWName, groupDisplayName = PAWDispName)]
    [UI_FloatEdit(scene = UI_Scene.Editor)]
    public float height;

    #endregion

    #region internal state

    public const string CoreStackAnchorName = "__ATCoreStack";

    public SegmentStacker stacker = new();
    protected SegmentStack currentStack;

    # endregion

    #region lifecycle

    public override void OnLoad(ConfigNode node)
    {
    }

    public override void OnStart(StartState state)
    {
        Debug.Log("OnStart called");
        LinkPAWElements();
        UpdatePAWLimits();
        Restack();
    }

    public void OnDestroy()
    {
    }

    #endregion

    #region PAW management

    protected void LinkPAWElements()
    {
        Fields.AddSelfAndSymmetryListener(nameof(diameter), OnSizeModified);
        Fields.AddSelfAndSymmetryListener(nameof(height), OnSizeModified);
    }

    protected void UpdatePAWLimits()
    {
        var diamCtrl = Fields.AsEditorUICtrl<UI_FloatEdit>(nameof(diameter));
        diamCtrl.minValue = 0.5f;
        diamCtrl.maxValue = 5f;
        var heightCtrl = Fields.AsEditorUICtrl<UI_FloatEdit>(nameof(height));
        heightCtrl.minValue = 0.5f;
        heightCtrl.maxValue = 10f;
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
        stacker.CoreSegmentSet = Library.segments.Values.First(); // TODO slider
    }

    public void Restack()
    {
        UpdateStacker();
        var newStack = stacker.Build();
        newStack.RealizeGeometry(this, CoreStackAnchorName, currentStack);
        currentStack = newStack;
    }

    #endregion
}
