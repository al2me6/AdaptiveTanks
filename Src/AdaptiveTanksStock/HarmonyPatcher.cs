using HarmonyLib;
using UnityEngine;

namespace AdaptiveTanksStock;

[KSPAddon(KSPAddon.Startup.Instantly, true)]
public class HarmonyPatcher : MonoBehaviour
{
    public void Start() => new Harmony("AdaptiveTanksStock.HarmonyPatcher").PatchAll();
}
