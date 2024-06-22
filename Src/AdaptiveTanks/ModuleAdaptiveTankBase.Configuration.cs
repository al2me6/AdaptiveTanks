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

        UpdateDimensionLimits();
        UpdateIntertankAvailability();
        UpdateAvailableVariants(Layer.Skin);
        UpdateAvailableVariants(Layer.Core);
        UpdateAvailableAlignments();
    }

    protected void InitializeEditorPAW()
    {
        InitializeStyleAndVariantSelectors();
        InitializeDimensionSelectors();
    }

    protected void InitializeStyleAndVariantSelectors()
    {
        Fields[nameof(skinStyle)].AddSelfAndSymmetryListener(OnSegmentModified);
        Fields[nameof(coreStyle)].AddSelfAndSymmetryListener(OnSegmentModified);

        Fields[nameof(skinStyle)].AsEditor<UI_ChooseOption>().SetOptions(
            skinStyles,
            skinStyles.Select(skin => Library<StyleDefSkin>.Get(skin).DisplayName));

        Fields[nameof(coreStyle)].AsEditor<UI_ChooseOption>().SetOptions(
            coreStyles,
            coreStyles.Select(core => Library<StyleDefCore>.Get(core).DisplayName));

        Fields[nameof(skinStyle)].guiActiveEditor = skinStyles.Length > 1;
        Fields[nameof(coreStyle)].guiActiveEditor = coreStyles.Length > 1;

        Fields[nameof(skinNoseVariant)].AddSelfAndSymmetryListener(OnSegmentModified);
        Fields[nameof(coreNoseVariant)].AddSelfAndSymmetryListener(OnSegmentModified);
        Fields[nameof(skinBodyVariant)].AddSelfAndSymmetryListener(OnSegmentModified);
        Fields[nameof(coreBodyVariant)].AddSelfAndSymmetryListener(OnSegmentModified);
        Fields[nameof(skinIntertankVariant)].AddSelfAndSymmetryListener(OnSegmentModified);
        Fields[nameof(coreIntertankVariant)].AddSelfAndSymmetryListener(OnSegmentModified);
        Fields[nameof(skinMountVariant)].AddSelfAndSymmetryListener(OnSegmentModified);
        Fields[nameof(coreMountVariant)].AddSelfAndSymmetryListener(OnSegmentModified);

        Fields[nameof(noseAlignInteriorEnd)].AddSelfAndSymmetryListener(OnAlignmentModified);
        Fields[nameof(mountAlignInteriorEnd)].AddSelfAndSymmetryListener(OnAlignmentModified);

        Fields[nameof(useIntertank)].AddSelfAndSymmetryListener(OnIntertankModified);
    }

    protected void InitializeDimensionSelectors()
    {
        Fields[nameof(diameter)].AddSelfAndSymmetryListener(OnDimensionModified);
        Fields[nameof(height)].AddSelfAndSymmetryListener(OnDimensionModified);
        Fields[nameof(intertankFraction)].AddSelfAndSymmetryListener(OnDimensionModified);

        Fields[nameof(diameter)].AsEditor<UI_FloatEdit>().SetIncrements(dimensionIncrementLarge,
            dimensionIncrementSmall, dimensionIncrementSlide);
        Fields[nameof(height)].AsEditor<UI_FloatEdit>().SetIncrements(dimensionIncrementLarge,
            dimensionIncrementSmall, dimensionIncrementSlide);
    }

    #endregion

    #region queries

    public StyleDef SkinStyle => Library<StyleDefSkin>.Get(skinStyle);
    public StyleDef CoreStyle => Library<StyleDefCore>.Get(coreStyle);

    public StyleDef Style(Layer layer) => layer switch
    {
        Layer.Skin => SkinStyle,
        Layer.Core => CoreStyle,
        _ => throw new ArgumentOutOfRangeException(nameof(layer))
    };

    protected static string SegmentFieldName(Layer layer, Role role) => (layer, role) switch
    {
        (Layer.Skin, Role.TerminatorTop) => nameof(skinNoseVariant),
        (Layer.Skin, Role.Tank) => nameof(skinBodyVariant),
        (Layer.Skin, Role.Intertank) => nameof(skinIntertankVariant),
        (Layer.Skin, Role.TerminatorBottom) => nameof(skinMountVariant),
        (Layer.Core, Role.TerminatorTop) => nameof(coreNoseVariant),
        (Layer.Core, Role.Tank) => nameof(coreBodyVariant),
        (Layer.Core, Role.Intertank) => nameof(coreIntertankVariant),
        (Layer.Core, Role.TerminatorBottom) => nameof(coreMountVariant),
        _ => throw new ArgumentOutOfRangeException()
    };

    protected ref string SegmentName(Layer layer, Role role)
    {
        if (layer == Layer.Skin)
        {
            if (role == Role.TerminatorTop) return ref skinNoseVariant;
            if (role == Role.Tank) return ref skinBodyVariant;
            if (role == Role.Intertank) return ref skinIntertankVariant;
            if (role == Role.TerminatorBottom) return ref skinMountVariant;
        }

        if (layer == Layer.Core)
        {
            if (role == Role.TerminatorTop) return ref coreNoseVariant;
            if (role == Role.Tank) return ref coreBodyVariant;
            if (role == Role.Intertank) return ref coreIntertankVariant;
            if (role == Role.TerminatorBottom) return ref coreMountVariant;
        }

        throw new ArgumentOutOfRangeException();
    }

    protected SegmentDef Segment(Layer layer, Role role) =>
        Library<SegmentDef>.Get(SegmentName(layer, role));

    public SelectedSegments SkinSegments() => new(
        tank: skinBodyVariant,
        terminatorTop: skinNoseVariant,
        terminatorBottom: skinMountVariant,
        intertank: useIntertank ? skinIntertankVariant : null,
        tankCapInternalTop: TerminatorIsAccessory(Cap.Top) // TODO select
            ? SkinStyle.SegmentsByRole[Role.TankCapInternalTop].First().name
            : null,
        tankCapInternalBottom: TerminatorIsAccessory(Cap.Bottom)
            ? SkinStyle.SegmentsByRole[Role.TankCapInternalBottom].First().name
            : null,
        alignTop: Alignment(Cap.Top),
        alignBottom: Alignment(Cap.Bottom));

    public SelectedSegments CoreSegments() => new(
        tank: coreBodyVariant,
        terminatorTop: coreNoseVariant,
        terminatorBottom: coreMountVariant,
        intertank: useIntertank ? coreIntertankVariant : null,
        tankCapInternalTop: TerminatorIsAccessory(Cap.Top) // TODO select
            ? CoreStyle.SegmentsByRole[Role.TankCapInternalTop].First().name
            : null,
        tankCapInternalBottom: TerminatorIsAccessory(Cap.Bottom)
            ? CoreStyle.SegmentsByRole[Role.TankCapInternalBottom].First().name
            : null,
        alignTop: Alignment(Cap.Top),
        alignBottom: Alignment(Cap.Bottom));

    public bool TerminatorIsAccessory(Cap position) =>
        Segment(Layer.Skin, position.AsRoleTerminator()).IsAccessory;

    protected static string AlignmentFieldName(Cap position) => position switch
    {
        Cap.Top => nameof(noseAlignInteriorEnd),
        Cap.Bottom => nameof(mountAlignInteriorEnd),
        _ => throw new ArgumentOutOfRangeException(nameof(position))
    };

    protected ref bool AlignInteriorEnd(Cap position)
    {
        if (position == Cap.Top) return ref noseAlignInteriorEnd;
        if (position == Cap.Bottom) return ref mountAlignInteriorEnd;
        throw new ArgumentOutOfRangeException(nameof(position));
    }

    protected SegmentAlignment Alignment(Cap position) => position switch
    {
        Cap.Top => noseAlignInteriorEnd,
        Cap.Bottom => mountAlignInteriorEnd,
        _ => throw new ArgumentOutOfRangeException(nameof(position))
    }
        ? SegmentAlignment.PinInteriorEnd
        : SegmentAlignment.PinBothEnds;

    #endregion

    #region update callbacks

    protected void OnDimensionModified(BaseField f, object obj)
    {
        ReStack();
    }

    protected void OnSegmentModified(BaseField f, object obj)
    {
        // Note that changing the skin may result in changes in core variant availability due
        // to the coupling of terminator segment selection.
        switch (f.name)
        {
            case nameof(skinStyle):
                UpdateIntertankAvailability();
                UpdateAvailableVariants(Layer.Skin);
                UpdateAvailableVariants(Layer.Core);
                break;
            case nameof(coreStyle):
                UpdateIntertankAvailability();
                UpdateAvailableVariants(Layer.Core);
                break;
            case nameof(skinNoseVariant) or nameof(skinMountVariant):
                UpdateAvailableVariants(Layer.Core);
                break;
        }

        UpdateDimensionLimits();
        UpdateAvailableAlignments();
        ReStack();
    }

    protected void OnAlignmentModified(BaseField f, object obj)
    {
        ReStack();
    }

    protected void OnIntertankModified(BaseField f, object obj)
    {
        UpdateAvailableVariant(Layer.Skin, Role.Intertank);
        UpdateAvailableVariant(Layer.Core, Role.Intertank);
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

    public void UpdateIntertankAvailability()
    {
        var field = Fields[nameof(useIntertank)];
        if (SkinStyle.SupportsIntertank && CoreStyle.SupportsIntertank)
            field.guiActiveEditor = true;
        else
            field.guiActiveEditor = useIntertank = false;
    }

    protected void UpdateAvailableVariants(Layer layer)
    {
        UpdateAvailableVariant(layer, Role.TerminatorTop);
        UpdateAvailableVariant(layer, Role.Tank);
        UpdateAvailableVariant(layer, Role.Intertank);
        UpdateAvailableVariant(layer, Role.TerminatorBottom);
    }

    protected void UpdateAvailableVariant(Layer layer, Role role)
    {
        var field = Fields[SegmentFieldName(layer, role)];
        var variants = Style(layer).SegmentsByRole[role];
        ref var selection = ref SegmentName(layer, role);

        if (layer == Layer.Core
            && role.IsTerminator()
            && TerminatorIsAccessory(role.TryAsCapPosition()!.Value))
        {
            field.guiActiveEditor = false;
            selection = BuiltinItems.EmptyAccessorySegmentName;
        }
        else if (role == Role.Intertank && !useIntertank)
        {
            field.guiActiveEditor = false;
        }
        else
        {
            var names = variants.Select(v => v.name).ToArray();
            var displays = variants.Select(v => v.DisplayName);
            field.AsEditor<UI_ChooseOption>().SetOptions(names, displays);
            field.guiActiveEditor = variants.Count > 1;
            if (!names.Contains(selection)) selection = names[0];
        }
    }

    protected void UpdateAvailableAlignments()
    {
        UpdateAvailableAlignment(Cap.Top);
        UpdateAvailableAlignment(Cap.Bottom);
    }

    protected void UpdateAvailableAlignment(Cap position)
    {
        var role = position.AsRoleTerminator();
        var skinTerminator = Segment(Layer.Skin, role);
        var coreTerminator = Segment(Layer.Core, role);
        var field = Fields[AlignmentFieldName(position)];

        switch (skinTerminator.CanToggleAlignment, coreTerminator.CanToggleAlignment)
        {
            case (true, true):
                field.guiActiveEditor = true;
                break;
            case (false, true):
                field.guiActiveEditor = false;
                AlignInteriorEnd(position) =
                    skinTerminator.TryGetOnlyAlignment()!.Value.IsInteriorEnd();
                break;
            case (true, false) or (false, false):
                // Note that in case of a mismatch, the core takes precedence.
                field.guiActiveEditor = false;
                AlignInteriorEnd(position) =
                    coreTerminator.TryGetOnlyAlignment()!.Value.IsInteriorEnd();
                break;
        }
    }

    #endregion
}
