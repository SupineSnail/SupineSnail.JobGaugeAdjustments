using System;
using System.IO;
using System.Reflection;
using SupineSnail.DependencyInjection;
using SupineSnail.JobGaugeAdjustments.Abstractions;
using SupineSnail.JobGaugeAdjustments.Configuration;
using SupineSnail.JobGaugeAdjustments.Services;
using SupineSnail.StaticWrappers;
using SupineSnail.StaticWrappers.DependencyInjection;

namespace SupineSnail.JobGaugeAdjustments;

public class Plugin : IDalamudPlugin
{
    private readonly IServiceProvider _provider;
    private readonly PluginManager _manager;

    public Plugin(
        ClientState clientState,
        BuddyList buddyList,
        CommandManager commandManager,
        Condition condition,
        DalamudPluginInterface pluginInterface,
        DataManager dataManager,
        Framework framework,
        GameGui gameGui,
        JobGauges jobGauges,
        ObjectTable objectTable,
        PartyList partyList,
        SigScanner sigScanner,
        TargetManager targetManager
    )
    {
        _provider = InitializeDependencyInjection(clientState, buddyList, commandManager, condition,
            pluginInterface, dataManager, framework, gameGui, jobGauges, objectTable, partyList, sigScanner,
            targetManager);
        // Load the localization
        var localization = _provider.GetRequiredService<ILocalizationService>();
        var logger = _provider.GetRequiredService<IPluginLog>();
        localization.Load(clientState.ClientLanguage);
        logger.Debug("Localization loaded");

        // Get to the starting point
        _manager = _provider.GetRequiredService<PluginManager>();
        _manager.Initialize();
    }

    private IServiceProvider InitializeDependencyInjection(
        ClientState clientState,
        BuddyList buddyList,
        CommandManager commandManager,
        Condition condition,
        DalamudPluginInterface pluginInterface,
        DataManager dataManager,
        Framework framework,
        GameGui gameGui,
        JobGauges jobGauges,
        ObjectTable objectTable,
        PartyList partyList,
        SigScanner sigScanner,
        TargetManager targetManager
    )
    {
        var services = new ServiceCollection();

        services.AddDalamudStaticWrappers();
        services.AddLocalization();

        services.AddSingleton<ILocalizationService, LocalizationService>();
        services.AddSingleton<IFileSystemService, FileSystemService>();
        services.AddSingleton<GaugeStateManager>();
        services.AddSingleton<PluginManager>();
        services.AddSingleton<ConfigurationWindow>();

        services.AddSingleton(clientState);
        services.AddSingleton(buddyList);
        services.AddSingleton(commandManager);
        services.AddSingleton(condition);
        services.AddSingleton(pluginInterface);
        services.AddSingleton(dataManager);
        services.AddSingleton(framework);
        services.AddSingleton(gameGui);
        services.AddSingleton(jobGauges);
        services.AddSingleton(objectTable);
        services.AddSingleton(partyList);
        services.AddSingleton(sigScanner);
        services.AddSingleton(targetManager);

        return services.BuildProvider();
    }

    public void Dispose()
    {
        // DI Container will dispose of all resources
        _provider.Dispose();
        GC.SuppressFinalize(this);
    }

    public string Name => "Job Gauge Adjustments";
}