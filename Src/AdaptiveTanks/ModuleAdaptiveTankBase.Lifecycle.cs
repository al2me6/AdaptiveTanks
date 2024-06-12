namespace AdaptiveTanks;

public abstract partial class ModuleAdaptiveTankBase : PartModule
{
    public override void OnLoad(ConfigNode node)
    {
        LoadDeclaredStyles(node);
    }

    public override void OnIconCreate() => InitializeConfigurationAndModel();

    public override void OnInitialize() => InitializeConfigurationAndModel();

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
        InitializeConfiguration();
        InitializeModel();
    }
}
