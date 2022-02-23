using System.IO;
using Dalamud.Interface.Windowing;
using Newtonsoft.Json;
using SupineSnail.DependencyInjection;
using SupineSnail.JobGaugeAdjustments.Abstractions;
using SupineSnail.JobGaugeAdjustments.Configuration;

namespace SupineSnail.JobGaugeAdjustments.Services;

/// <summary>
/// Not behind an interface, purely for hooking together non-interfaced Dalamud stuff with interfaced SupineSnail stuff in one place.
/// </summary>
/// <remarks>
/// Separated here, this doesn't need unit tests as badly
/// </remarks>
public class PluginManager : IPluginDisposable
{
    private readonly IPluginLog _logger;

    private readonly DalamudPluginInterface _pluginInterface;
    private readonly CommandManager _commandManager;
    private readonly ILocalizationService _localization;
    private readonly ConfigurationWindow _configWindow;
    private readonly GaugeStateManager _stateManager;
    private readonly Framework _framework;
    
    private ConfigurationModel _configuration;
    private WindowSystem _windowSystem;

    public PluginManager(
        IPluginLog pluginLog,
        DalamudPluginInterface pluginInterface,
        Framework framework,
        CommandManager commandManager,
        ILocalizationService localizationService,
        ConfigurationWindow configWindow,
        GaugeStateManager stateManager)
    {
        _logger = pluginLog;

        _pluginInterface = pluginInterface;
        _framework = framework;
        _commandManager = commandManager;
        _localization = localizationService;
        _configWindow = configWindow;
        _stateManager = stateManager;
    }

    public void Initialize()
    {
        // Register commands
        _commandManager.AddHandler(Constants.ConfigCommand, new CommandInfo(OnCommand)
        {
            HelpMessage = _localization["Config_Command_Help", "Show the Job Gauge Adjustments configuration window", "Helper text for the command to open settings."],
            ShowInHelp = true
        });
        
        _configuration = LoadConfiguration() ?? new ConfigurationModel();
        _windowSystem = new WindowSystem("JobGaugeAdjustments.Configuration");
        _stateManager.Initialize(_configuration);
        
        _logger.Debug("Initializing config and adding window");
        _configWindow.SetConfiguration(_configuration);
        _configWindow.OnChanged += OnConfigurationChanged;
        _windowSystem.AddWindow(_configWindow);
        
        // Add events
        _framework.Update += OnFrameworkUpdate;
        _pluginInterface.UiBuilder.OpenConfigUi += OnOpenConfigUi;
        _pluginInterface.UiBuilder.Draw += OnDraw;
    }

    private void OnConfigurationChanged()
    {
        _stateManager.UpdateState(true);
        _pluginInterface.SavePluginConfig(_configuration);
    }

    private void OnFrameworkUpdate(Framework framework)
    {
        _stateManager.UpdateState(false);
    }

    private ConfigurationModel LoadConfiguration()
    {
        try
        {
            return _pluginInterface.GetPluginConfig() as ConfigurationModel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not read in plugin configuration");
            return null;
        }
    }

    private void OnCommand(string command, string arguments)
    {
        _logger.LogDebug("Command handling: {command} {arguments}", command, arguments);
        switch (command)
        {
            case Constants.ConfigCommand:
                OnOpenConfigUi();
                break;
            default:
                _logger.LogDebug("Command not recognized. Is it not implemented yet?");
                break;
        }
    }

    private void OnOpenConfigUi()
    {
        _configWindow.IsOpen = !_configWindow.IsOpen;
    }

    private void OnDraw()
    {
        if (_configWindow.IsOpen)
            _windowSystem.Draw();
    }

    public void Dispose()
    {
        _logger.Debug("Disposing of PluginManager");

        _configWindow.OnChanged -= OnConfigurationChanged;
        _windowSystem.RemoveWindow(_configWindow);

        _pluginInterface.UiBuilder.Draw -= OnDraw;
        _pluginInterface.UiBuilder.OpenConfigUi -= OnOpenConfigUi;
        _framework.Update -= OnFrameworkUpdate;
        _commandManager.RemoveHandler(Constants.ConfigCommand);
        
        GC.SuppressFinalize(this);
    }
}