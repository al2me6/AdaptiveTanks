using AdaptiveTanks.Utils;

namespace AdaptiveTanks;

public partial class ModuleAdaptiveTankBase : IPartCostModifier, IPartMassModifier
{
    protected void UpdateVolume(bool isInitialize)
    {
        volumeL = currentStacks.Core.EvaluateFueledVolume(diameter) * MathUtils.M3toL;
        ApplyVolume(isInitialize);
    }

    protected abstract void ApplyVolume(bool isInitialize);

    public ModifierChangeWhen GetModuleCostChangeWhen() => ModifierChangeWhen.FIXED;
    public abstract float GetModuleCost(float defaultCost, ModifierStagingSituation sit);

    public ModifierChangeWhen GetModuleMassChangeWhen() => ModifierChangeWhen.FIXED;
    public abstract float GetModuleMass(float defaultMass, ModifierStagingSituation sit);
}
