using System.Linq;
using AdaptiveTanks.Extensions;

namespace AdaptiveTanks;

using Layer = SegmentLayer;
using Role = SegmentRole;

public class ModuleAdaptiveTankStock : ModuleAdaptiveTankBase
{
    #region lifecycle

    public override void OnLoad(ConfigNode node)
    {
        skinStyles = node.LoadAllNamesFromNodes(SkinStyleNodeName).ToArray();
        coreStyles = node.LoadAllNamesFromNodes(CoreStyleNodeName).ToArray();

        if ((skinStyles.Length == 0 || coreStyles.Length == 0) &&
            part.partInfo?.partPrefab is Part prefab)
        {
            var prefabPM = prefab.FindModuleImplementing<ModuleAdaptiveTankStock>();
            skinStyles = prefabPM.skinStyles;
            coreStyles = prefabPM.coreStyles;
        }
    }

    protected override void InitializeConfigurationAndModelOverride()
    {
        if (string.IsNullOrEmpty(skinStyle) || !skinStyles.Contains(skinStyle))
            skinStyle = skinStyles[0];
        if (string.IsNullOrEmpty(coreStyle) || !coreStyles.Contains(coreStyle))
            coreStyle = coreStyles[0];

        UpdateAvailableVariants(Layer.Skin);
        UpdateAvailableVariants(Layer.Core);
    }

    #endregion

    #region PAW

    public const string SkinPAWName = "AdaptiveTanks Skin Style";
    public const string SkinPAWDispName = SkinPAWName;
    public const string CorePAWName = "AdaptiveTanks Core Style";
    public const string CorePAWDispName = CorePAWName;

    [KSPField(isPersistant = true, guiName = "Style", guiActiveEditor = true,
        groupName = SkinPAWName, groupDisplayName = SkinPAWDispName)]
    [UI_ChooseOption]
    public string skinStyle;

    [KSPField(isPersistant = true, guiName = "Nose", guiActiveEditor = true,
        groupName = SkinPAWName, groupDisplayName = SkinPAWDispName)]
    [UI_ChooseOption]
    public string skinNoseVariant;

    [KSPField(isPersistant = true, guiName = "Body", guiActiveEditor = true,
        groupName = SkinPAWName, groupDisplayName = SkinPAWDispName)]
    [UI_ChooseOption]
    public string skinBodyVariant;

    [KSPField(isPersistant = true, guiName = "Mount", guiActiveEditor = true,
        groupName = SkinPAWName, groupDisplayName = SkinPAWDispName)]
    [UI_ChooseOption]
    public string skinMountVariant;

    [KSPField(isPersistant = true, guiName = "Style", guiActiveEditor = true,
        groupName = CorePAWName, groupDisplayName = CorePAWDispName)]
    [UI_ChooseOption]
    public string coreStyle;

    [KSPField(isPersistant = true, guiName = "Nose", guiActiveEditor = true,
        groupName = CorePAWName, groupDisplayName = CorePAWDispName)]
    [UI_ChooseOption]
    public string coreNoseVariant;

    [KSPField(isPersistant = true, guiName = "Body", guiActiveEditor = true,
        groupName = CorePAWName, groupDisplayName = CorePAWDispName)]
    [UI_ChooseOption]
    public string coreBodyVariant;

    [KSPField(isPersistant = true, guiName = "Mount", guiActiveEditor = true,
        groupName = CorePAWName, groupDisplayName = CorePAWDispName)]
    [UI_ChooseOption]
    public string coreMountVariant;

    protected override void InitializeEditorPAW()
    {
        base.InitializeEditorPAW();
        InitializeStyleAndVariantSelectors();
        UpdateDimensionLimits();
    }

    protected override void UpdateDimensionLimits()
    {
        // TODO calculate.
        Fields[nameof(diameter)].AsEditor<UI_FloatEdit>().SetMinMax(0.5f, 5f);
        Fields[nameof(height)].AsEditor<UI_FloatEdit>().SetMinMax(0.5f, 10f);
    }

    protected void InitializeStyleAndVariantSelectors()
    {
        Fields[nameof(skinStyle)].AddSelfAndSymmetryListener(OnStyleModified);
        Fields[nameof(coreStyle)].AddSelfAndSymmetryListener(OnStyleModified);

        Fields[nameof(skinStyle)].AsEditor<UI_ChooseOption>().SetOptions(
            skinStyles,
            skinStyles.Select(skin => Library<StyleDefStockSkin>.Get(skin).DisplayName));

        Fields[nameof(coreStyle)].AsEditor<UI_ChooseOption>().SetOptions(
            coreStyles,
            coreStyles.Select(core => Library<StyleDefStockCore>.Get(core).DisplayName));

        Fields[nameof(skinStyle)].guiActiveEditor = skinStyles.Length > 1;
        Fields[nameof(coreStyle)].guiActiveEditor = coreStyles.Length > 1;

        Fields[nameof(skinNoseVariant)].AddSelfAndSymmetryListener(OnVariantModified);
        Fields[nameof(skinBodyVariant)].AddSelfAndSymmetryListener(OnVariantModified);
        Fields[nameof(skinMountVariant)].AddSelfAndSymmetryListener(OnVariantModified);
        Fields[nameof(coreNoseVariant)].AddSelfAndSymmetryListener(OnVariantModified);
        Fields[nameof(coreBodyVariant)].AddSelfAndSymmetryListener(OnVariantModified);
        Fields[nameof(coreMountVariant)].AddSelfAndSymmetryListener(OnVariantModified);

        UpdateAvailableVariants(Layer.Skin);
        UpdateAvailableVariants(Layer.Core);
    }

    protected void UpdateAvailableVariants(Layer layer)
    {
        if (layer == Layer.Skin)
        {
            UpdateVariantSlider(nameof(skinNoseVariant), SkinStyle, Role.Nose);
            UpdateVariantSlider(nameof(skinBodyVariant), SkinStyle, Role.Body);
            UpdateVariantSlider(nameof(skinMountVariant), SkinStyle, Role.Mount);
        }
        else
        {
            UpdateVariantSlider(nameof(coreNoseVariant), CoreStyle, Role.Nose);
            UpdateVariantSlider(nameof(coreBodyVariant), CoreStyle, Role.Body);
            UpdateVariantSlider(nameof(coreMountVariant), CoreStyle, Role.Mount);
        }
    }

    protected void UpdateVariantSlider(string fieldName, StyleDefStock style, Role role)
    {
        var variants = style.GetAvailableSegments(role);
        var field = Fields[fieldName];
        field.AsEditor<UI_ChooseOption>().SetOptions(
            variants.Select(v => v.name), variants.Select(v => v.DisplayName));
        field.guiActiveEditor = variants.Length > 1;
        field.FieldInfo.SetValue(this, variants[0].name);
    }

    #endregion

    #region style management

    public const string SkinStyleNodeName = "SkinStyle";
    public const string CoreStyleNodeName = "CoreStyle";

    public string[] skinStyles;
    public string[] coreStyles;

    public StyleDefStock SkinStyle => Library<StyleDefStockSkin>.Get(skinStyle);
    public StyleDefStock CoreStyle => Library<StyleDefStockCore>.Get(coreStyle);

    public override SelectedSegments SelectedSkinSegments() =>
        new(skinNoseVariant, skinBodyVariant, skinMountVariant);

    public override SelectedSegments SelectedCoreSegments() =>
        new(coreNoseVariant, coreBodyVariant, coreMountVariant);

    protected void OnStyleModified(BaseField f, object obj)
    {
        if (f.name == nameof(skinStyle)) UpdateAvailableVariants(Layer.Skin);
        else if (f.name == nameof(coreStyle)) UpdateAvailableVariants(Layer.Core);
        OnVariantModified(null, null);
    }

    protected void OnVariantModified(BaseField f, object obj)
    {
        UpdateDimensionLimits();
        ReStack();
    }

    #endregion

    #region stack generation

    public override SkinAndCore<SegmentStack> SolveStack(StackerParameters parameters) =>
        SegmentStacker.SolveSkinAndCoreSeparately(parameters);

    #endregion
}
