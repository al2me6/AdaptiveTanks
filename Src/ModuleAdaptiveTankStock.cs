using System;
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
            UpdateVariantSlider(nameof(skinNoseVariant), Layer.Skin, Role.Nose);
            UpdateVariantSlider(nameof(skinBodyVariant), Layer.Skin, Role.Body);
            UpdateVariantSlider(nameof(skinMountVariant), Layer.Skin, Role.Mount);
        }
        else
        {
            UpdateVariantSlider(nameof(coreNoseVariant), Layer.Core, Role.Nose);
            UpdateVariantSlider(nameof(coreBodyVariant), Layer.Core, Role.Body);
            UpdateVariantSlider(nameof(coreMountVariant), Layer.Core, Role.Mount);
        }
    }

    protected void UpdateVariantSlider(string fieldName, Layer layer, Role role)
    {
        var style = layer switch
        {
            Layer.Skin => SkinStyle,
            Layer.Core => CoreStyle,
            _ => throw new ArgumentOutOfRangeException(nameof(layer))
        };
        var variants = style.GetAvailableSegments(role);
        var field = Fields[fieldName];
        field.AsEditor<UI_ChooseOption>().SetOptions(
            variants.Select(v => v.name), variants.Select(v => v.DisplayName));
        field.guiActiveEditor = variants.Length > 1;
        Debug.Log($"fieldinfo name `{field.FieldInfo.Name}`");
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

    public override SelectedSegmentDefs GetSelectedSkinSegments()
    {
        return new SelectedSegmentDefs(
            Library<SegmentDef>.Get(skinNoseVariant),
            Library<SegmentDef>.Get(skinBodyVariant),
            Library<SegmentDef>.Get(skinMountVariant)
        );
    }

    public override SelectedSegmentDefs GetSelectedCoreSegments()
    {
        return new SelectedSegmentDefs(
            Library<SegmentDef>.Get(coreNoseVariant),
            Library<SegmentDef>.Get(coreBodyVariant),
            Library<SegmentDef>.Get(coreMountVariant)
        );
    }

    protected void OnStyleModified(BaseField f, object obj)
    {
        if (f.name == nameof(skinStyle)) UpdateAvailableVariants(Layer.Skin);
        else if (f.name == nameof(coreStyle)) UpdateAvailableVariants(Layer.Core);
        OnVariantModified(null, null);
    }

    protected void OnVariantModified(BaseField f, object obj)
    {
        UpdateDimensionLimits();
        Restack();
    }

    #endregion

    #region stack generation

    public override SkinAndCore<SegmentStack> SolveStack(StackerParameters parameters) =>
        SegmentStacker.SolveSkinAndCoreSeparately(parameters);

    #endregion
}
