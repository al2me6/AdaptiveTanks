using B9PartSwitch;
using HarmonyLib;

namespace AdaptiveTanksStock;

[HarmonyPatch(typeof(ModuleB9PartSwitch))]
internal class B9PSPropellantSwitchCallback
{
    [HarmonyPostfix]
    [HarmonyPatch("UpdateSubtype")]
    internal static void Postfix_UpdateSubtype(ModuleB9PartSwitch __instance)
    {
        if (__instance.part.FindModuleImplementing<ModuleAdaptiveTankStock>() is
                ModuleAdaptiveTankStock moduleAT
            && __instance.moduleID == moduleAT.B9PSPropellantModuleID)
            moduleAT.OnPropellantMixtureUpdated();
    }
}
