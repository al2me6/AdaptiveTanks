using System;
using System.Linq;
using AdaptiveTanks.Extensions;

namespace AdaptiveTanks;

using Layer = SegmentLayer;
using Role = SegmentRole;
using Cap = CapPosition;

public partial class ModuleAdaptiveTankBase
{
    #region initialization

    protected void InitializeConfiguration()
    {
        if (string.IsNullOrEmpty(skinStyle) || !skinStyles.Contains(skinStyle))
            skinStyle = skinStyles[0];
        if (string.IsNullOrEmpty(coreStyle) || !coreStyles.Contains(coreStyle))
            coreStyle = coreStyles[0];

        UpdateAvailableVariants(Layer.Skin);
        UpdateAvailableVariants(Layer.Core);
    }

    protected void InitializeEditorPAW()
    {
        InitializeStyleAndVariantSelectors();
        InitializeDimensionSelectors();
        UpdateDimensionLimits();
    }

    protected void InitializeStyleAndVariantSelectors()
    {
        Fields[nameof(skinStyle)].AddSelfAndSymmetryListener(OnStyleModified);
        Fields[nameof(coreStyle)].AddSelfAndSymmetryListener(OnStyleModified);

        Fields[nameof(skinStyle)].AsEditor<UI_ChooseOption>().SetOptions(
            skinStyles,
            skinStyles.Select(skin => Library<StyleDefSkin>.Get(skin).DisplayName));

        Fields[nameof(coreStyle)].AsEditor<UI_ChooseOption>().SetOptions(
            coreStyles,
            coreStyles.Select(core => Library<StyleDefCore>.Get(core).DisplayName));

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
        UpdateAvailableAlignment();
    }

    protected void InitializeDimensionSelectors()
    {
        Fields[nameof(diameter)].AddSelfAndSymmetryListener(OnDimensionModified);
        Fields[nameof(height)].AddSelfAndSymmetryListener(OnDimensionModified);

        Fields[nameof(diameter)].AsEditor<UI_FloatEdit>().SetIncrements(dimensionIncrementLarge,
            dimensionIncrementSmall, dimensionIncrementSlide);
        Fields[nameof(height)].AsEditor<UI_FloatEdit>().SetIncrements(dimensionIncrementLarge,
            dimensionIncrementSmall, dimensionIncrementSlide);
    }

    #endregion

    #region queries

    public StyleDef SkinStyle => Library<StyleDefSkin>.Get(skinStyle);
    public StyleDef CoreStyle => Library<StyleDefCore>.Get(coreStyle);

    public StyleDef LayerStyle(Layer layer) => layer switch
    {
        Layer.Skin => SkinStyle,
        Layer.Core => CoreStyle,
        _ => throw new ArgumentOutOfRangeException(nameof(layer))
    };

    public SelectedSegments SkinSegments() => new(
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

    public SelectedSegments CoreSegments() => new(
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

    protected ref SegmentAlignment Alignment(Cap position)
    {
        if (position == Cap.Top) return ref noseAlign;
        if (position == Cap.Bottom) return ref mountAlign;
        throw new ArgumentOutOfRangeException(nameof(position));
    }

    protected static string SegmentFieldName(Layer layer, Role role) => (layer, role) switch
    {
        (Layer.Skin, Role.TerminatorTop) => nameof(skinNoseVariant),
        (Layer.Skin, Role.Tank) => nameof(skinBodyVariant),
        (Layer.Skin, Role.TerminatorBottom) => nameof(skinMountVariant),
        (Layer.Core, Role.TerminatorTop) => nameof(coreNoseVariant),
        (Layer.Core, Role.Tank) => nameof(coreBodyVariant),
        (Layer.Core, Role.TerminatorBottom) => nameof(coreMountVariant),
        _ => throw new ArgumentOutOfRangeException()
    };

    protected static string AlignmentToggleEventName(Cap position) => position switch
    {
        Cap.Top => nameof(ToggleNoseAlignment),
        Cap.Bottom => nameof(ToggleMountAlignment),
        _ => throw new ArgumentOutOfRangeException(nameof(position))
    };

    #endregion

    #region update callbacks

    protected void OnDimensionModified(BaseField f, object obj)
    {
        ReStack();
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
        UpdateAvailableAlignment();
        ReStack();
    }

    #endregion

    #region constraints

    protected void UpdateDimensionLimits()
    {
        // TODO calculate
        Fields[nameof(diameter)].AsEditor<UI_FloatEdit>().SetMinMax(0.5f, 5f);
        Fields[nameof(height)].AsEditor<UI_FloatEdit>().SetMinMax(0.5f, 10f);
    }

    protected void UpdateAvailableVariants(Layer layer)
    {
        UpdateAvailableVariantImpl(layer, Role.TerminatorTop);
        UpdateAvailableVariantImpl(layer, Role.Tank);
        UpdateAvailableVariantImpl(layer, Role.TerminatorBottom);
    }

    protected void UpdateAvailableVariantImpl(Layer layer, Role role)
    {
        var field = Fields[SegmentFieldName(layer, role)];
        var variants = LayerStyle(layer).SegmentsByRole[role];
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

    protected void UpdateAvailableAlignment()
    {
        UpdateAvailableAlignmentImpl(Cap.Top);
        UpdateAvailableAlignmentImpl(Cap.Bottom);
    }

    protected void UpdateAvailableAlignmentImpl(Cap position)
    {
        var skinTerminator = TerminatorSegment(Layer.Skin, position);
        var coreTerminator = TerminatorSegment(Layer.Core, position);
        var kspEvent = Events[AlignmentToggleEventName(position)];
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
            Alignment(position) = align;
            kspEvent.guiActiveEditor = false;
        }
    }

    #endregion

    #region update execution

    protected void ToggleAlignment(Cap position)
    {
        ref var align = ref Alignment(position);
        align = align.Toggle();
        UpdateAlignmentToggleText(position);
        ReStack();
    }

    protected void UpdateAlignmentToggleText(Cap position)
    {
        Events[AlignmentToggleEventName(position)].guiName = Alignment(position) switch
        {
            SegmentAlignment.PinBothEnds => $"{position.AsNoseMount()}: flushed",
            SegmentAlignment.PinInteriorEnd => $"{position.AsNoseMount()}: staggered",
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    #endregion
}
