using AdaptiveTanks.Extensions;

namespace AdaptiveTanks;

public class ModuleAdaptiveTankStock : ModuleAdaptiveTankBase
{
    #region lifecycle

    public override void OnLoad(ConfigNode node)
    {
        Debug.Log("OnLoad called");
    }

    #endregion

    #region PAW

    [KSPField] public string skinStyle;
    [KSPField] public string coreStyle;

    public StyleDefStock SkinStyle => Library<StyleDefStockSkin>.Get(skinStyle);
    public StyleDefStock CoreStyle => Library<StyleDefStockCore>.Get(coreStyle);

    protected override void InitializeEditorPAW()
    {
        base.InitializeEditorPAW();
        InitializeStyleSelectors();
        UpdateDimensionLimits();
    }

    protected override void UpdateDimensionLimits()
    {
        if (!HighLogic.LoadedSceneIsEditor) return;
        Fields[nameof(diameter)].AsEditor<UI_FloatEdit>().SetMinMax(0.5f, 5f);
        Fields[nameof(height)].AsEditor<UI_FloatEdit>().SetMinMax(0.5f, 10f);
    }

    protected void InitializeStyleSelectors()
    {
    }

    #endregion

    #region style management

    public override SelectedSegmentDefs GetSelectedSkinSegments()
    {
        return GetSelectedCoreSegments();
    }

    public override SelectedSegmentDefs GetSelectedCoreSegments()
    {
        return new SelectedSegmentDefs(
            Library<SegmentDef>.Get("squad_cap_nose_125"),
            Library<SegmentDef>.Get("squad_body_125"),
            Library<SegmentDef>.Get("squad_cap_blank_125")
        );
    }

    #endregion

    #region stack generation

    public override SkinAndCore<SegmentStack> SolveStack(StackerParameters parameters) =>
        SegmentStacker.SolveSkinAndCoreSeparately(parameters);

    #endregion
}
