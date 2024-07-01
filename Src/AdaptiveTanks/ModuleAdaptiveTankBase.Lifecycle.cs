﻿namespace AdaptiveTanks;

public abstract partial class ModuleAdaptiveTankBase : PartModule
{
    public static bool IsLoadingPrefab => !PartLoader.Instance.IsReady();

    public override void OnLoad(ConfigNode node)
    {
        if (IsLoadingPrefab) LoadCustomDataFromConfig(node);
    }

    // This gets called on a copy of the prefab, not the prefab itself.
    public override void OnIconCreate() => InitializeConfigurationAndModel();

    // This is for some reason necessary when loading a ship. Otherwise, Unity crashes.
    public override void OnInitialize() => InitializeConfigurationAndModel();

    public override string GetInfo()
    {
        // This is one of the few methods that gets called on the prefab PM. Initialize here so
        // the part info window has cost/propellant info.
        if (IsLoadingPrefab) InitializeConfigurationAndModel();

        return base.GetInfo();
    }

    public override void OnStart(StartState state)
    {
        InitializeConfigurationAndModel();
        InitializeEditorPAW();
    }

    private bool _configAndModelInitialized = false;

    protected void InitializeConfigurationAndModel()
    {
        if (_configAndModelInitialized) return;
        _configAndModelInitialized = true;
        if (!IsLoadingPrefab) RestoreCustomData();
        InitializeConfiguration();
        InitializeModel();
    }
}
