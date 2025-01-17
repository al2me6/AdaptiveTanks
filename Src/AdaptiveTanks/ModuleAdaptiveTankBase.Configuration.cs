﻿using System;
using System.Collections.Generic;
using System.Linq;
using AdaptiveTanks.Utils;
using UnityEngine;

namespace AdaptiveTanks;

using Layer = SegmentLayer;
using Role = SegmentRole;
using Cap = CapPosition;

public partial class ModuleAdaptiveTankBase
{
    #region initialization

    protected virtual void InitializeConfiguration()
    {
        ValidateNonEmptyStyles();
        if (string.IsNullOrEmpty(skinStyle) || !skinStyles.Contains(skinStyle))
            skinStyle = skinStyles[0];
        UpdateAvailableCoreStyles();

        // Requires: valid styles.
        UpdateAvailableVariants(Layer.Skin);
        UpdateAvailableVariants(Layer.Core);
        UpdateIntertankUseSelector();
        // Requires: valid segment selection.
        UpdateDimensionLimits();

        UpdateAvailableAlignments();

        UpdateAvailableLinkedMaterials();
    }

    protected void InitializeEditorPAW()
    {
        InitializeStyleAndVariantSelectors();
        InitializeDimensionSelectors();
    }

    protected void InitializeStyleAndVariantSelectors()
    {
        // This needs only be set once.
        Fields[nameof(skinStyle)].AsEditor<UI_ChooseOption>().SetOptions(
            skinStyles,
            skinStyles.Select(skin => Library<StyleDefSkin>.Get(skin).DisplayName));

        Fields[nameof(skinStyle)].AddSelfAndSymmetryListener(OnSegmentModified);
        Fields[nameof(coreStyle)].AddSelfAndSymmetryListener(OnSegmentModified);

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

        Fields[nameof(skinLinkedMaterial)].AddSelfAndSymmetryListener(OnLinkedMaterialModified);
        Fields[nameof(coreLinkedMaterial)].AddSelfAndSymmetryListener(OnLinkedMaterialModified);
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

    public StyleDefSkin SkinStyle => Library<StyleDefSkin>.Get(skinStyle);
    public StyleDefCore CoreStyle => Library<StyleDefCore>.Get(coreStyle);

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

    protected SegmentDef Segment(Layer layer, Cap position) =>
        Segment(layer, position.AsRoleTerminator());

    // TODO: this needs caching.

    public SelectedSegments SkinSegments() => new(
        tank: skinBodyVariant,
        terminatorTop: skinNoseVariant,
        terminatorBottom: skinMountVariant,
        intertank: IntertankDecision() ? skinIntertankVariant : null,
        tankCapInternalTop: TerminatorIsAccessory(Cap.Top) // TODO select
            ? SkinStyle.Segments[Role.TankCapInternalTop].First().name
            : null,
        tankCapInternalBottom: TerminatorIsAccessory(Cap.Bottom)
            ? SkinStyle.Segments[Role.TankCapInternalBottom].First().name
            : null,
        alignTop: Alignment(Cap.Top),
        alignBottom: Alignment(Cap.Bottom));

    public SelectedSegments CoreSegments() => new(
        tank: coreBodyVariant,
        terminatorTop: coreNoseVariant,
        terminatorBottom: coreMountVariant,
        intertank: IntertankDecision() ? coreIntertankVariant : null,
        tankCapInternalTop: TerminatorIsAccessory(Cap.Top) // TODO select
            ? CoreStyle.Segments[Role.TankCapInternalTop].First().name
            : null,
        tankCapInternalBottom: TerminatorIsAccessory(Cap.Bottom)
            ? CoreStyle.Segments[Role.TankCapInternalBottom].First().name
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

    // Only allow biprop intertanks for now.
    protected bool IntertankPossible() =>
        allowIntertank &&
        CoreStyle.SupportsIntertank && SkinStyle.SupportsIntertank &&
        VolumetricMixtureRatio().Length == 2;

    protected bool IntertankDecision() =>
        IntertankPossible() && (enforceIntertank ? allowIntertank : useIntertank);

    protected string LinkedMaterialFieldName(Layer layer) => layer switch
    {
        Layer.Skin => nameof(skinLinkedMaterial),
        Layer.Core => nameof(coreLinkedMaterial),
        _ => throw new ArgumentException(nameof(layer))
    };

    protected ref string LinkedMaterial(Layer layer)
    {
        if (layer == Layer.Skin) return ref skinLinkedMaterial;
        if (layer == Layer.Core) return ref coreLinkedMaterial;
        throw new ArgumentException(nameof(layer));
    }

    protected abstract float[] VolumetricMixtureRatio();

    #endregion

    #region update callbacks

    protected void OnDimensionModified(BaseField f, object obj)
    {
        // Note that increasing the diameter may force the height to increase.
        if (f.name == nameof(diameter)) UpdateDimensionLimits();
        ReStack(false);
    }

    protected void OnSegmentModified(BaseField f, object obj)
    {
        // Note that changing the skin may result in changes in core variant availability due
        // to the coupling of terminator segment selection.
        switch (f.name)
        {
            case nameof(skinStyle):
                UpdateAvailableCoreStyles();
                UpdateAvailableVariants(Layer.Skin);
                UpdateAvailableVariants(Layer.Core);
                UpdateIntertankUseSelector();
                UpdateAvailableLinkedMaterials();
                break;
            case nameof(coreStyle):
                UpdateAvailableVariants(Layer.Core);
                UpdateIntertankUseSelector();
                UpdateAvailableLinkedMaterials();
                break;
            case nameof(skinNoseVariant) or nameof(skinMountVariant):
                UpdateAvailableVariants(Layer.Core);
                break;
        }

        UpdateDimensionLimits();
        UpdateAvailableAlignments();
        ReStack(false);
    }

    protected void OnAlignmentModified(BaseField f, object obj)
    {
        ReStack(false);
    }

    protected void OnIntertankModified(BaseField f, object obj) => OnIntertankModified();

    protected void OnIntertankModified()
    {
        UpdateAvailableVariant(Layer.Skin, Role.Intertank);
        UpdateAvailableVariant(Layer.Core, Role.Intertank);
        ReStack(false);
    }

    protected void OnLinkedMaterialModified(BaseField f, object obj)
    {
        ReStack(false);
    }

    #endregion

    #region constraints

    protected void UpdateUIChooseOption(
        BaseField field, ref string backing,
        IEnumerable<string> names, IEnumerable<string> displays)
    {
        var namesArr = names.ToArray();
        field.AsEditor<UI_ChooseOption>().SetOptions(namesArr, displays);
        field.guiActiveEditor = namesArr.Length > 1;
        if (!namesArr.Contains(backing)) backing = namesArr[0];
    }

    protected void ValidateNonEmptyStyles()
    {
        if (skinStyles.Length == 0)
        {
            Debug.LogWarning($"part `{part.name}`: must contain at least one skin style");
            skinStyles = [Library<StyleDefSkin>.Items.Keys.First()];
        }

        if (coreStyles.Length == 0)
        {
            Debug.LogWarning($"part `{part.name}`: must contain at least one core style");
            coreStyles = [Library<StyleDefCore>.Items.Keys.First()];
        }
    }

    protected void UpdateAvailableCoreStyles()
    {
        availableCoreStyles = SkinStyle.GetAllowedCores(coreStyles);
        if (availableCoreStyles.Length == 0)
        {
            Debug.LogError($"part `{part.name}`: skin style `{skinStyle}` has no compatible cores");
            availableCoreStyles =
                [SkinStyle.GetAllowedCores(Library<StyleDefCore>.ItemNames).First()];
        }

        UpdateUIChooseOption(Fields[nameof(coreStyle)], ref coreStyle, availableCoreStyles,
            availableCoreStyles.Select(v => Library<StyleDefCore>.Get(v).DisplayName));
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
        var variants = Style(layer).Segments[role];
        ref var selection = ref SegmentName(layer, role);

        if (layer == Layer.Core
            && role.IsTerminator()
            && TerminatorIsAccessory(role.TryAsCapPosition()!.Value))
        {
            field.guiActiveEditor = false;
            selection = BuiltinItems.EmptyAccessorySegmentName;
        }
        else if (role == Role.Intertank && !IntertankDecision())
        {
            field.guiActiveEditor = false;
        }
        else
        {
            UpdateUIChooseOption(
                field, ref selection,
                variants.Select(v => v.name), variants.Select(v => v.DisplayName));
        }
    }

    public void UpdateIntertankUseSelector()
    {
        Fields[nameof(useIntertank)].guiActiveEditor = !enforceIntertank && IntertankPossible();
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

        switch (skinTerminator.TryGetOnlyAlignment(), coreTerminator.TryGetOnlyAlignment())
        {
            case (null, null):
                field.guiActiveEditor = true;
                break;
            case (SegmentAlignment align, null):
                field.guiActiveEditor = false;
                AlignInteriorEnd(position) = align.IsInteriorEnd();
                break;
            // Note that in case of a mismatch, the core takes precedence.
            case (_, SegmentAlignment align):
                field.guiActiveEditor = false;
                AlignInteriorEnd(position) = align.IsInteriorEnd();
                break;
        }
    }

    protected void UpdateDimensionLimits()
    {
        // TODO: what happens if this ends up empty?
        var diameterRange = Enumerable
            .Concat(SkinSegments(), CoreSegments())
            .Select(seg => seg.SupportedDiameters)
            .IntersectionOfIntervals();

        var minDiameterClamped =
            MathUtils.RoundUpTo(Mathf.Max(minDiameter, diameterRange.x), dimensionIncrementSmall);
        var maxDiameterClamped =
            MathUtils.RoundDownTo(Mathf.Min(maxDiameter, diameterRange.y), dimensionIncrementSmall);

        Fields[nameof(diameter)].AsEditor<UI_FloatEdit>()
            .SetMinMax(minDiameterClamped, maxDiameterClamped);
        diameter.Clamp(minDiameterClamped, maxDiameterClamped);

        var minStackHeight = SegmentStacker.MinHeight(diameter, SkinSegments(), CoreSegments());
        var minHeightClamped =
            MathUtils.RoundUpTo(Mathf.Max(minHeight, minStackHeight), dimensionIncrementSmall);

        Fields[nameof(height)].AsEditor<UI_FloatEdit>()
            .SetMinMax(minHeightClamped, maxHeight);
        height.Clamp(minHeightClamped, maxHeight);

        MonoUtilities.RefreshPartContextWindow(part);
    }

    protected void UpdateAvailableLinkedMaterials()
    {
        UpdateAvailableLinkedMaterials(Layer.Skin);
        UpdateAvailableLinkedMaterials(Layer.Core);
    }

    protected void UpdateAvailableLinkedMaterials(Layer layer)
    {
        var field = Fields[LinkedMaterialFieldName(layer)];
        var materials = Style(layer).LinkedMaterials;
        ref var materialId = ref LinkedMaterial(layer);

        if (materials.IsEmpty())
        {
            materialId = "";
            field.guiActiveEditor = false;
            return;
        }

        UpdateUIChooseOption(
            field, ref materialId,
            materials.Select(mat => mat.Id), materials.Select(mat => mat.DisplayName));
    }

    #endregion
}
