using System;
using System.Linq;
using AdaptiveTanks.Extensions;

namespace AdaptiveTanks;

using Layer = SegmentLayer;
using Role = SegmentRole;
using Cap = CapPosition;

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

    [KSPField(isPersistant = true, guiName = "Skin Style", guiActiveEditor = true)]
    [UI_ChooseOption]
    public string skinStyle;

    [KSPField(isPersistant = true, guiName = "Skin Nose", guiActiveEditor = true)] [UI_ChooseOption]
    public string skinNoseVariant;

    [KSPField(isPersistant = true)] public SegmentAlignment noseAlign;

    [KSPField(isPersistant = true, guiName = "Skin Body", guiActiveEditor = true)] [UI_ChooseOption]
    public string skinBodyVariant;

    [KSPField(isPersistant = true, guiName = "Skin Mount", guiActiveEditor = true)]
    [UI_ChooseOption]
    public string skinMountVariant;


    [KSPField(isPersistant = true)] public SegmentAlignment mountAlign;

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

    protected static string FieldNameForLayerRole(Layer layer, Role role) => (layer, role) switch
    {
        (Layer.Skin, Role.TerminatorTop) => nameof(skinNoseVariant),
        (Layer.Skin, Role.Tank) => nameof(skinBodyVariant),
        (Layer.Skin, Role.TerminatorBottom) => nameof(skinMountVariant),
        (Layer.Core, Role.TerminatorTop) => nameof(coreNoseVariant),
        (Layer.Core, Role.Tank) => nameof(coreBodyVariant),
        (Layer.Core, Role.TerminatorBottom) => nameof(coreMountVariant),
        _ => throw new ArgumentOutOfRangeException()
    };

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
        UpdateAlignmentModes();
    }

    protected void UpdateAvailableVariants(Layer layer)
    {
        UpdateAvailableVariantImpl(layer, Role.TerminatorTop);
        UpdateAvailableVariantImpl(layer, Role.Tank);
        UpdateAvailableVariantImpl(layer, Role.TerminatorBottom);
    }

    protected void UpdateAvailableVariantImpl(Layer layer, Role role)
    {
        var style = LayerStyle(layer);
        var fieldName = FieldNameForLayerRole(layer, role);
        var field = Fields[fieldName];
        var variants = style.SegmentsByRole[role];
        if (layer == Layer.Core
            && role.IsTerminator()
            && TerminatorIsAccessory(role.TryAsCapPosition()!.Value))
        {
            field.guiActiveEditor = false;
            field.FieldInfo.SetValue(this, SegmentDef.CoreAccessorySurrogate.name);
        }
        else
        {
            field.AsEditor<UI_ChooseOption>().SetOptions(
                variants.Select(v => v.name), variants.Select(v => v.DisplayName));
            field.guiActiveEditor = variants.Count > 1;
            field.FieldInfo.SetValue(this, variants[0].name);
        }
    }

    protected ref SegmentAlignment AlignmentForTerminator(Cap position)
    {
        if (position == Cap.Top) return ref noseAlign;
        if (position == Cap.Bottom) return ref mountAlign;
        throw new ArgumentOutOfRangeException(nameof(position));
    }

    protected static string EventNameForTerminator(Cap position) => position switch
    {
        Cap.Top => nameof(ToggleNoseAlignment),
        Cap.Bottom => nameof(ToggleMountAlignment),
        _ => throw new ArgumentOutOfRangeException(nameof(position))
    };

    [KSPEvent(guiActiveEditor = true)]
    protected void ToggleNoseAlignment() => ToggleTerminatorAlignment(Cap.Top);

    [KSPEvent(guiActiveEditor = true)]
    protected void ToggleMountAlignment() => ToggleTerminatorAlignment(Cap.Bottom);

    protected void ToggleTerminatorAlignment(Cap position)
    {
        ref var align = ref AlignmentForTerminator(position);
        align = align.Toggle();
        UpdateAlignmentToggleText(position);
        OnVariantModified(null, null);
    }

    protected void UpdateAlignmentToggleText(Cap position)
    {
        Events[EventNameForTerminator(position)].guiName = AlignmentForTerminator(position) switch
        {
            SegmentAlignment.PinBothEnds => $"{position.AsNoseMount()}: flushed",
            SegmentAlignment.PinInteriorEnd => $"{position.AsNoseMount()}: staggered",
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    protected void UpdateAlignmentModes()
    {
        UpdateAlignmentModeImpl(Cap.Top);
        UpdateAlignmentModeImpl(Cap.Bottom);
    }

    protected void UpdateAlignmentModeImpl(Cap position)
    {
        var skinTerminator = TerminatorSegment(Layer.Skin, position);
        var coreTerminator = TerminatorSegment(Layer.Core, position);
        var kspEvent = Events[EventNameForTerminator(position)];
        if (skinTerminator.CanToggleAlignment && coreTerminator.CanToggleAlignment)
        {
            kspEvent.guiActiveEditor = true;
            UpdateAlignmentToggleText(position);
        }
        else
        {
            var skinAlign = skinTerminator.TryGetOnlyAlignment()!.Value;
            var coreAlign = coreTerminator.TryGetOnlyAlignment()!.Value;
            var align = skinAlign == coreAlign
                ? skinAlign
                : SegmentAlignment.PinBothEnds; // TODO: this needs better handling.
            AlignmentForTerminator(position) = align;
            kspEvent.guiActiveEditor = false;
        }
    }

    #endregion

    #region style management

    public const string SkinStyleNodeName = "SkinStyle";
    public const string CoreStyleNodeName = "CoreStyle";

    public string[] skinStyles;
    public string[] coreStyles;

    public StyleDefStock SkinStyle => Library<StyleDefStockSkin>.Get(skinStyle);
    public StyleDefStock CoreStyle => Library<StyleDefStockCore>.Get(coreStyle);

    public StyleDefStock LayerStyle(Layer layer) => layer switch
    {
        Layer.Skin => SkinStyle,
        Layer.Core => CoreStyle,
        _ => throw new ArgumentOutOfRangeException(nameof(layer))
    };

    public SegmentDef TerminatorSegment(Layer layer, Cap position) =>
        Library<SegmentDef>.Get((layer, position) switch
        {
            (Layer.Skin, Cap.Top) => skinNoseVariant,
            (Layer.Skin, Cap.Bottom) => skinMountVariant,
            (Layer.Core, Cap.Top) => coreNoseVariant,
            (Layer.Core, Cap.Bottom) => coreMountVariant,
            _ => throw new ArgumentOutOfRangeException()
        });

    public bool TerminatorIsAccessory(Cap position) =>
        TerminatorSegment(Layer.Skin, position).IsAccessory;

    public bool NoseIsAccessory() => TerminatorIsAccessory(Cap.Top);
    public bool MountIsAccessory() => TerminatorIsAccessory(Cap.Bottom);

    public override SelectedSegments SelectedSkinSegments() => new(
        tank: skinBodyVariant,
        terminatorTop: skinNoseVariant,
        terminatorBottom: skinMountVariant,
        intertank: null,
        tankCapInternalTop: NoseIsAccessory() // TODO select
            ? SkinStyle.SegmentsByRole[Role.TankCapInternalTop].First().name
            : null,
        tankCapInternalBottom: MountIsAccessory()
            ? SkinStyle.SegmentsByRole[Role.TankCapInternalBottom].First().name
            : null,
        alignTop: noseAlign,
        alignBottom: mountAlign);

    public override SelectedSegments SelectedCoreSegments() => new(
        tank: coreBodyVariant,
        terminatorTop: coreNoseVariant,
        terminatorBottom: coreMountVariant,
        intertank: null,
        tankCapInternalTop: NoseIsAccessory() // TODO select
            ? CoreStyle.SegmentsByRole[Role.TankCapInternalTop].First().name
            : null,
        tankCapInternalBottom: MountIsAccessory()
            ? CoreStyle.SegmentsByRole[Role.TankCapInternalBottom].First().name
            : null,
        alignTop: noseAlign,
        alignBottom: mountAlign);

    protected void OnStyleModified(BaseField f, object obj)
    {
        if (f.name == nameof(skinStyle)) UpdateAvailableVariants(Layer.Skin);
        else if (f.name == nameof(coreStyle)) UpdateAvailableVariants(Layer.Core);

        OnVariantModified(null, null);
    }

    protected void OnVariantModified(BaseField f, object obj)
    {
        UpdateDimensionLimits();
        UpdateAlignmentModes();
        ReStack();
    }

    #endregion

    #region stack generation

    public override SkinAndCore<SegmentStack> SolveStack(StackerParameters parameters) =>
        SegmentStacker.SolveStack(parameters);

    #endregion
}
