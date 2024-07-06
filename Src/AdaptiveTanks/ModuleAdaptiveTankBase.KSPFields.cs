using System.Linq;
using AdaptiveTanks.Utils;

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
    [UI_FloatEdit(sigFigs = 4, useSI = true, unit = "m")]
    public float diameter;

    [KSPField(isPersistant = true, guiName = "Height", guiActiveEditor = true)]
    [UI_FloatEdit(sigFigs = 4, useSI = true, unit = "m")]
    public float height;

    [KSPField(guiName = "Worst distortion", guiActiveEditor = true)] [UI_Label]
    public string sWorstDistortion = "";

    [KSPField] public float minDiameter = 0.5f;
    [KSPField] public float maxDiameter = 10f;
    [KSPField] public float minHeight = 0.5f;
    [KSPField] public float maxHeight = 50f;

    [KSPField(guiActiveEditor = true, guiName = "Volume", guiUnits = "L", guiFormat = "f0")]
    [UI_Label]
    public float volumeL;

    #endregion

    #region styling

    public string[] availableCoreStyles;

    [KSPField(isPersistant = true, guiName = "Skin Style")] [UI_ChooseOption]
    public string skinStyle;

    [KSPField(isPersistant = true, guiName = "Core Style")] [UI_ChooseOption]
    public string coreStyle;

    [KSPField(isPersistant = true, guiName = "Skin Nose")] [UI_ChooseOption]
    public string skinNoseVariant;

    [KSPField(isPersistant = true, guiName = "Core Nose")] [UI_ChooseOption]
    public string coreNoseVariant;

    [KSPField(isPersistant = true, guiName = "Nose Alignment")]
    [UI_Toggle(enabledText = "staggered", disabledText = "flushed")]
    public bool noseAlignInteriorEnd;

    [KSPField(isPersistant = true, guiName = "Skin Body")] [UI_ChooseOption]
    public string skinBodyVariant;

    [KSPField(isPersistant = true, guiName = "Core Body")] [UI_ChooseOption]
    public string coreBodyVariant;

    [KSPField(isPersistant = true, guiName = "Skin Intertank")] [UI_ChooseOption]
    public string skinIntertankVariant;

    [KSPField(isPersistant = true, guiName = "Core Intertank")] [UI_ChooseOption]
    public string coreIntertankVariant;

    [KSPField(isPersistant = true, guiName = "Skin Mount")] [UI_ChooseOption]
    public string skinMountVariant;

    [KSPField(isPersistant = true, guiName = "Core Mount")] [UI_ChooseOption]
    public string coreMountVariant;

    [KSPField(isPersistant = true, guiName = "Mount Alignment")]
    [UI_Toggle(enabledText = "staggered", disabledText = "flushed")]
    public bool mountAlignInteriorEnd;

    [KSPField] public bool allowIntertank = true;
    [KSPField] public bool enforceIntertank = false;
    [KSPField] public float maxIntertankVolumetricDeviation = 0.05f;

    [KSPField(isPersistant = true, guiName = "Intertank")]
    [UI_Toggle(enabledText = "enabled", disabledText = "disabled")]
    public bool useIntertank;

    #endregion
}
