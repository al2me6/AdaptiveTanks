using B9PartSwitch;

namespace AdaptiveTanks;

public class ModuleAdaptiveTankStock : ModuleAdaptiveTankBase
{
    [KSPField] public string B9PSPropellantModuleID;

    [KSPField] public float KSPPropUnitsPerL = 0.2f; // Why does this exist...
    [KSPField] public float tankUtilization = 0.85f;

    private ModuleB9PartSwitch B9PSModule;

    protected override void InitializeConfiguration()
    {
        base.InitializeConfiguration();

        foreach (var module in part.Modules)
        {
            if (module is not ModuleB9PartSwitch b9ps) continue;
            if (b9ps.moduleID != B9PSPropellantModuleID) continue;
            B9PSModule = b9ps;
            break;
        }

        if (B9PSModule == null)
        {
            Debug.LogError(
                $"propellant management B9PS module `{B9PSPropellantModuleID}` not found");
        }
    }

    protected override void ApplyVolume(bool isInitialize)
    {
        if (!isInitialize && HighLogic.LoadedSceneIsFlight)
        {
            Debug.LogError("tried to update volume past initialization in flight");
            return;
        }

        B9PSModule.baseVolume = volumeL * KSPPropUnitsPerL * tankUtilization;
        if (isInitialize) B9PSModule.CurrentSubtype.ActivateOnStart();
        else B9PSModule.UpdateVolume();
        MonoUtilities.RefreshPartContextWindow(part);
    }

    // Propellant is handled by B9PS.
    // TODO: fixed costs/masses.
    public override float GetModuleCost(float defaultCost, ModifierStagingSituation sit) => 0f;
    public override float GetModuleMass(float defaultMass, ModifierStagingSituation sit) => 0f;
}
