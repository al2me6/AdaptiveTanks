﻿using System;
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
        UpdateAvailableAlignments();
        UpdateDimensionLimits();
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
        Fields[nameof(skinBodyVariant)].AddSelfAndSymmetryListener(OnSegmentModified);
        Fields[nameof(skinMountVariant)].AddSelfAndSymmetryListener(OnSegmentModified);
        Fields[nameof(coreNoseVariant)].AddSelfAndSymmetryListener(OnSegmentModified);
        Fields[nameof(coreBodyVariant)].AddSelfAndSymmetryListener(OnSegmentModified);
        Fields[nameof(coreMountVariant)].AddSelfAndSymmetryListener(OnSegmentModified);
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
        (Layer.Skin, Role.TerminatorBottom) => nameof(skinMountVariant),
        (Layer.Core, Role.TerminatorTop) => nameof(coreNoseVariant),
        (Layer.Core, Role.Tank) => nameof(coreBodyVariant),
        (Layer.Core, Role.TerminatorBottom) => nameof(coreMountVariant),
        _ => throw new ArgumentOutOfRangeException()
    };

    protected ref string SegmentName(Layer layer, Role role)
    {
        if (layer == Layer.Skin)
        {
            if (role == Role.TerminatorTop) return ref skinNoseVariant;
            if (role == Role.Tank) return ref skinBodyVariant;
            if (role == Role.TerminatorBottom) return ref skinMountVariant;
        }

        if (layer == Layer.Core)
        {
            if (role == Role.TerminatorTop) return ref coreNoseVariant;
            if (role == Role.Tank) return ref coreBodyVariant;
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
        intertank: SkinStyle.SegmentsByRole[Role.Intertank].First().name,
        tankCapInternalTop: TerminatorIsAccessory(Cap.Top) // TODO select
            ? SkinStyle.SegmentsByRole[Role.TankCapInternalTop].First().name
            : null,
        tankCapInternalBottom: TerminatorIsAccessory(Cap.Bottom)
            ? SkinStyle.SegmentsByRole[Role.TankCapInternalBottom].First().name
            : null,
        alignTop: noseAlign,
        alignBottom: mountAlign);

    public SelectedSegments CoreSegments() => new(
        tank: coreBodyVariant,
        terminatorTop: coreNoseVariant,
        terminatorBottom: coreMountVariant,
        intertank: CoreStyle.SegmentsByRole[Role.Intertank].First().name,
        tankCapInternalTop: TerminatorIsAccessory(Cap.Top) // TODO select
            ? CoreStyle.SegmentsByRole[Role.TankCapInternalTop].First().name
            : null,
        tankCapInternalBottom: TerminatorIsAccessory(Cap.Bottom)
            ? CoreStyle.SegmentsByRole[Role.TankCapInternalBottom].First().name
            : null,
        alignTop: noseAlign,
        alignBottom: mountAlign);

    public bool TerminatorIsAccessory(Cap position) =>
        Segment(Layer.Skin, position.AsRoleTerminator()).IsAccessory;

    protected ref SegmentAlignment Alignment(Cap position)
    {
        if (position == Cap.Top) return ref noseAlign;
        if (position == Cap.Bottom) return ref mountAlign;
        throw new ArgumentOutOfRangeException(nameof(position));
    }

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

    protected void OnSegmentModified(BaseField f, object obj)
    {
        // Note that changing the skin may result in changes in core variant availability due
        // to the coupling of terminator segment selection.
        switch (f.name)
        {
            case nameof(skinStyle):
                UpdateAvailableVariants(Layer.Skin);
                UpdateAvailableVariants(Layer.Core);
                break;
            case nameof(coreStyle) or nameof(skinNoseVariant) or nameof(skinMountVariant):
                UpdateAvailableVariants(Layer.Core);
                break;
        }

        UpdateDimensionLimits();
        UpdateAvailableAlignments();
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
        // Debug.LogWarning($"^^layer {layer}, role {role}");
        var variants = Style(layer).SegmentsByRole[role];
        ref var selection = ref SegmentName(layer, role);

        if (layer == Layer.Core
            && role.IsTerminator()
            && TerminatorIsAccessory(role.TryAsCapPosition()!.Value))
        {
            field.guiActiveEditor = false;
            selection = SegmentDef.CoreAccessorySurrogate.name;
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
        UpdateAvailableAlignmentImpl(Cap.Top);
        UpdateAvailableAlignmentImpl(Cap.Bottom);
    }

    protected void UpdateAvailableAlignmentImpl(Cap position)
    {
        var role = position.AsRoleTerminator();
        var skinTerminator = Segment(Layer.Skin, role);
        var coreTerminator = Segment(Layer.Core, role);
        var kspEvent = Events[AlignmentToggleEventName(position)];

        switch (skinTerminator.CanToggleAlignment, coreTerminator.CanToggleAlignment)
        {
            case (true, true):
                kspEvent.guiActiveEditor = true;
                UpdateAlignmentToggleText(position);
                break;
            case (false, true):
                kspEvent.guiActiveEditor = false;
                Alignment(position) = skinTerminator.TryGetOnlyAlignment()!.Value;
                break;
            case (true, false) or (false, false):
                // Note that in case of a mismatch, the core takes precedence.
                kspEvent.guiActiveEditor = false;
                Alignment(position) = coreTerminator.TryGetOnlyAlignment()!.Value;
                break;
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
