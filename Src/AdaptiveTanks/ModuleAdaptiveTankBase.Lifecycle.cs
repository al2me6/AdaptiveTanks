using AdaptiveTanks.Utils;

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
        // This is more or less the only method that gets called on the prefab PM.
        // Initialize here to set up attachment nodes and provide cost/propellant info
        // for the part info hover.
        if (PartUtils.IsLoadingPrefab) InitializeConfigurationAndModel();

        return base.GetInfo();
    }

    // Called when dropping a new part in the editor and when loading a `ShipConstruct`.
    // In the latter case, we must initialize here to provide mass/cost info.
    public override void OnInitialize() => InitializeConfigurationAndModel();

    public override void OnStart(StartState state)
    {
        // OnInitialize is not called when unpacking an existing vessel. Initialize here instead.
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
