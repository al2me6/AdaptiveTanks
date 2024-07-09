using AdaptiveTanks.Utils;

namespace AdaptiveTanks;

public partial class ModuleAdaptiveTankBase : IPartCostModifier, IPartMassModifier
{
    protected void UpdateVolume(bool isInitialize)
    {
        volumeL = segmentStacks!.Core.EvaluateTankVolume() * MathUtils.M3toL;
        ApplyVolume(isInitialize);
    }

    protected abstract void ApplyVolume(bool isInitialize);

    public ModifierChangeWhen GetModuleCostChangeWhen() => ModifierChangeWhen.FIXED;

    public float GetModuleCost(float defaultCost, ModifierStagingSituation sit) =>
        GetTankCost() + GetStructuralCost();

    public ModifierChangeWhen GetModuleMassChangeWhen() => ModifierChangeWhen.FIXED;

    public float GetModuleMass(float defaultMass, ModifierStagingSituation sit) =>
        GetTankMass() + GetStructuralMass();

    public abstract float GetTankCost();
    public abstract float GetTankMass();

    public float GetStructuralCost() => segmentStacks!.Skin.EvaluateStructuralCost();
    public float GetStructuralMass() => segmentStacks!.Skin.EvaluateStructuralMass();
}
