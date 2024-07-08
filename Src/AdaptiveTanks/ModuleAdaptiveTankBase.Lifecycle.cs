using AdaptiveTanks.Utils;
using UnityEngine;

namespace AdaptiveTanks;

public abstract partial class ModuleAdaptiveTankBase : PartModule
{
    public override void OnLoad(ConfigNode node)
    {
        if (PartUtils.IsLoadingPrefab) LoadCustomDataFromConfig(node);
    }

    // This gets called on a copy of the prefab, not the prefab itself.
    public override void OnIconCreate() => InitializeConfigurationAndModel();

    public override string GetInfo()
    {
        // This is one of the few methods that gets called on the prefab PM. Initialize here so
        // the part info window has cost/propellant info.
        if (PartUtils.IsLoadingPrefab) InitializeConfigurationAndModel();

        return base.GetInfo();
    }

    public override void OnInitialize() => InitializeConfigurationAndModel();

    public override void OnStart(StartState state)
    {
        // OnInitialize is not always called. This is a back-up.
        InitializeConfigurationAndModel();
        InitializeEditorPAW();
    }

    private bool configAndModelInitialized = false;

    protected void InitializeConfigurationAndModel()
    {
        if (configAndModelInitialized) return;
        configAndModelInitialized = true;
        if (!PartUtils.IsLoadingPrefab) RestoreCustomData();
        InitializeConfiguration();
        InitializeModel();
    }

    public void LateUpdate()
    {
        RefreshMPB();
    }
}
