using System;
using System.Collections.Generic;
using System.Linq;
using AdaptiveTanks;
using B9PartSwitch;

namespace AdaptiveTanksStock;

public class ModuleAdaptiveTankStock : ModuleAdaptiveTankBase
{
    #region fields

    [KSPField] public string B9PSPropellantModuleID;

    [KSPField] public float B9PSPropUnitsPerL = 0.2f; // Why does this exist...
    [KSPField] public float tankUtilization = 0.85f;

    #endregion

    #region configuration

    private ModuleB9PartSwitch B9PSModule;

    protected override void InitializeConfiguration()
    {
        foreach (var module in part.Modules)
        {
            if (module is not ModuleB9PartSwitch b9ps) continue;
            if (b9ps.moduleID != B9PSPropellantModuleID) continue;
            B9PSModule = b9ps;
            break;
        }

        if (B9PSModule == null)
            Debug.LogError($"B9PS propellant switcher `{B9PSPropellantModuleID}` not found");

        if (part.Modules.IndexOf(B9PSModule) < part.Modules.IndexOf(this))
            Debug.LogError("B9PS module must be declared after this module");

        base.InitializeConfiguration();
    }

    #endregion

    #region update callbacks

    public void OnPropellantMixtureUpdated()
    {
        UpdateIntertankUseSelector();
        // Note that this eventually restacks and calls `ApplyVolume`, which pokes the B9PS
        // module. Thus, any volume changes incurred by the intertank appearing/disappearing are
        // accounted for.
        OnIntertankModified();
    }

    #endregion

    #region stats

    protected static readonly Dictionary<string, float[]> mixtureRatioCache = new();

    protected override float[] VolumetricMixtureRatio()
    {
        var tankType = B9PSModule.CurrentSubtype.tankType;

        if (mixtureRatioCache.TryGetValue(tankType.tankName, out var mr)) return mr;

        // 1 unit-volume = 1/5 SI liter. 1 unit-quantity = 1 in-game propellant unit.
        // `TankResource.UnitsPerVolume` = unit-quantity propellant per unit-volume of mixture.
        // `ResourceDefinition.volume` = unit-volumes occupied by 1 unit-quantity of propellant.
        // `UnitsPerVolume` * `volume` = unit-volumes occupied by requested propellant.
        // All such unit-volumes may not sum to unity, but oh well. This system is not sane.
        mr = tankType
            .resources
            .Select(res => res.unitsPerVolume * res.resourceDefinition.volume)
            .ToArray();
        // `ResourceDefinition.density` = mass (in tons) per unit-quantity.
        // Divide by `volume` to get mass per unit-volume, i.e. true density.
        var volumetricDensities = tankType
            .resources
            .Select(res => res.resourceDefinition.density / res.resourceDefinition.volume)
            .ToArray();
        // Order by least-dense at bottom.
        // TODO: make this configurable.
        Array.Sort(volumetricDensities, mr);

        Debug.Log($"{tankType.tankName}: MR {string.Join(":", mr.Select(val => $"{val:f2}"))}");

        mixtureRatioCache[tankType.tankName] = mr;
        return mr;
    }

    // This must not call `ModuleB9PartSwitch.UpdateSubtype` to avoid infinite recursion!
    protected override void ApplyVolume(bool isInitialize)
    {
        if (!isInitialize && HighLogic.LoadedSceneIsFlight)
        {
            Debug.LogError("tried to update volume past initialization in flight");
            return;
        }

        B9PSModule.baseVolume = volumeL * B9PSPropUnitsPerL * tankUtilization;

        // We initialize earlier than B9PS. Setting the value is sufficient.
        if (isInitialize) return;

        B9PSModule.UpdateVolume();
        MonoUtilities.RefreshPartContextWindow(part);
    }

    // Propellant is handled by B9PS.
    public override float GetTankCost() => 0f;
    public override float GetTankMass() => 0f;

    #endregion
}
