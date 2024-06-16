using System.Linq;
using AdaptiveTanks.Extensions;

namespace AdaptiveTanks;

public partial class ModuleAdaptiveTankBase
{
    #region configuration

    [KSPField] public float dimensionIncrementLarge = 1f;
    [KSPField] public float dimensionIncrementSmall = 0.25f;
    [KSPField] public float dimensionIncrementSlide = 0.01f;

    // Nodes with these names (e.g. node_stack_top, _bottom) will be used as the stack nodes.
    [KSPField] public string nodeStackTopId = "top";
    [KSPField] public string nodeStackBottomId = "bottom";

    // For every increase in diameter by this amount, increment the size of the attachment node by 1.
    [KSPField] public float attachNodeSizeIncrementFactor = 1.25f;
    [KSPField] public int maxAttachNodeSize = 6;

    #endregion

    #region non-serializable configuration

    public string[] skinStyles;
    public string[] coreStyles;

    public const string SkinStyleNodeName = "SkinStyle";
    public const string CoreStyleNodeName = "CoreStyle";

    protected void LoadCustomDataFromConfig(ConfigNode node)
    {
        skinStyles = node.LoadAllNamesFromNodes(SkinStyleNodeName).ToArray();
        coreStyles = node.LoadAllNamesFromNodes(CoreStyleNodeName).ToArray();
    }

    protected void RestoreCustomData()
    {
        var prefabPM = part.partInfo.partPrefab.FindModuleImplementing<ModuleAdaptiveTankBase>();
        skinStyles = prefabPM.skinStyles;
        coreStyles = prefabPM.coreStyles;
    }

    #endregion

    #region dimensions

    [KSPField(isPersistant = true, guiName = "Diameter", guiActiveEditor = true)]
    [UI_FloatEdit(sigFigs = 4, useSI = true, unit = "m", scene = UI_Scene.Editor)]
    public float diameter;

    [KSPField(isPersistant = true, guiName = "Height", guiActiveEditor = true)]
    [UI_FloatEdit(sigFigs = 4, useSI = true, unit = "m", scene = UI_Scene.Editor)]
    public float height;

    [KSPField(guiName = "Worst distortion", guiActiveEditor = true)] [UI_Label]
    public string sWorstDistortion = "";

    #endregion

    #region styling

    [KSPField(isPersistant = true, guiName = "Skin Style", guiActiveEditor = true)]
    [UI_ChooseOption]
    public string skinStyle;

    [KSPField(isPersistant = true, guiName = "Skin Nose", guiActiveEditor = true)] [UI_ChooseOption]
    public string skinNoseVariant;

    [KSPField(isPersistant = true, guiName = "Skin Body", guiActiveEditor = true)] [UI_ChooseOption]
    public string skinBodyVariant;

    [KSPField(isPersistant = true, guiName = "Skin Mount", guiActiveEditor = true)]
    [UI_ChooseOption]
    public string skinMountVariant;

    [KSPField(isPersistant = true, guiName = "Core Style", guiActiveEditor = true)]
    [UI_ChooseOption]
    public string coreStyle;

    [KSPField(isPersistant = true, guiName = "Core Nose", guiActiveEditor = true)] [UI_ChooseOption]
    public string coreNoseVariant;

    [KSPField(isPersistant = true, guiName = "Core Body", guiActiveEditor = true)] [UI_ChooseOption]
    public string coreBodyVariant;

    [KSPField(isPersistant = true, guiName = "Core Mount", guiActiveEditor = true)]
    [UI_ChooseOption]
    public string coreMountVariant;

    [KSPField(isPersistant = true, guiName = "Intertank Fraction", guiActiveEditor = true)]
    [UI_FloatRange(minValue = 0f, maxValue = 1f, stepIncrement = 0.025f)]
    public float intertankFraction = 0.5f;

    [KSPField(isPersistant = true)] public SegmentAlignment noseAlign;
    [KSPField(isPersistant = true)] public SegmentAlignment mountAlign;

    #endregion

    #region events

    [KSPEvent(guiActiveEditor = true)]
    protected void ToggleNoseAlignment() => ToggleAlignment(CapPosition.Top);

    [KSPEvent(guiActiveEditor = true)]
    protected void ToggleMountAlignment() => ToggleAlignment(CapPosition.Bottom);

    #endregion
}
