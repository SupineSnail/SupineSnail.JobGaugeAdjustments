using System.Linq;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Component.GUI;
using SupineSnail.DependencyInjection;
using SupineSnail.JobGaugeAdjustments.Configuration;

namespace SupineSnail.JobGaugeAdjustments.Services;

public class GaugeStateManager : IPluginDisposable
{
    private ConfigurationModel _configuration;
    private readonly ClientState _clientState;
    private readonly IPluginLog _pluginLog;
    private readonly GameGui _gameGui;
    
    private readonly Dictionary<string, GaugeComponentTracking> _trackedNodeValues = new();
    private readonly Dictionary<string, List<IntPtr>> _trackedNodes = new();
        
    private unsafe delegate byte AddonOnUpdate(AtkUnitBase* atkUnitBase);
    private readonly Dictionary<string, Hook<AddonOnUpdate>> _addonUpdateHooks = new();

    private uint? _currentJob;
    private short? _jobChangeUpdateCount;
    private bool _lastWasEnabled;

    public GaugeStateManager(ClientState clientState, IPluginLog pluginLog, GameGui gameGui)
    {
        _clientState = clientState;
        _pluginLog = pluginLog;
        _gameGui = gameGui;
    }

    public void Initialize(ConfigurationModel configuration)
    {
        _configuration = configuration;
        EnsureConfigurationSetup();
    }

    public void UpdateState(bool hasConfigurationChange)
    {
        if (_configuration == null)
            return;

        try
        {
            var job = _clientState.LocalPlayer?.ClassJob.Id;
            if (job == null)
                return;

            if (_jobChangeUpdateCount.HasValue)
                _jobChangeUpdateCount++;

            if (_currentJob == job && hasConfigurationChange && !_jobChangeUpdateCount.HasValue)
            {
                _pluginLog.Debug("Configuration changed on current job");
                if (_lastWasEnabled && !_configuration.Jobs[job.Value].Enabled)
                    ResetGauges();
                else
                    UpdateGauges(job.Value, false);
            }
            
            // After a job changes, the gauges do not update instantly. We must wait a bit.
            // 1 tick is enough for them to show up, but need a few more for correct element visibility
            if (_currentJob == job || _jobChangeUpdateCount is < 5)
                return;

            if (_jobChangeUpdateCount.HasValue)
            {
                _pluginLog.Debug("Job change frame wait complete. Beginning Initialization.");
                _currentJob = job;

                UpdateGauges(job.Value, false);
                
                _jobChangeUpdateCount = null;
            }
            else
            {
                _pluginLog.Debug("Job change detected, waiting frames");
                ResetGauges();
                _jobChangeUpdateCount = 0;
            }
        }
        catch (Exception ex)
        {
            _pluginLog.Error(ex, "Unknown error when updating gauges");
        }
    }

    private void ResetGauges()
    {
        if (_currentJob == null)
            return;
        
        if (_lastWasEnabled)
            UpdateGauges(_currentJob.Value, true);
        
        _pluginLog.Debug("Resetting job gauges");

        _trackedNodes.Clear();
        _trackedNodeValues.Clear();

        foreach (var hook in _addonUpdateHooks.Values)
        {
            hook?.Disable();
        }

        _addonUpdateHooks.Clear();
    }

    private unsafe void UpdateGauges(uint job, bool forceReset)
    {
        var map = JobMap.GetJobMap(job);
        if (map == null)
        {
            _pluginLog.Debug($"Job with Id {job} not found in map. Cannot update.");
            return;
        }
        
        var jobConfig = _configuration.Jobs[job];
        _lastWasEnabled = jobConfig.Enabled;
        
        if (!jobConfig.Enabled && !forceReset) {
            _pluginLog.Debug("Job is not enabled, not updating gauges.");
            return;
        }
        
        _pluginLog.Debug("Job found. Proceeding to update gauges.");
        if (forceReset)
            _pluginLog.Debug("Resetting Gauge to default state.");
            
        var reset = forceReset || !jobConfig.Enabled;

        foreach (var (addonName, components) in map.Addons)
        {
            var hudAddon = GetUnitBase(addonName);
            if (hudAddon == null)
            {
                _pluginLog.Debug($"Could not get base addon {addonName} for job {job} in render.");
                return;
            }
            HookAddonUpdate(addonName, hudAddon);

            foreach (var componentPart in components)
            {
                if (!_trackedNodes.TryGetValue(addonName, out var addonNodes))
                {
                    addonNodes = new List<IntPtr>();
                    _trackedNodes[addonName] = addonNodes;
                }
                
                var componentConfig = jobConfig.Components[componentPart.Key];
                foreach (var nodeId in componentPart.NodeIds)
                {
                    var node = hudAddon->GetNodeById(nodeId);
                    if (node == null)
                        continue;

                    if (!addonNodes.Contains((IntPtr) node))
                        addonNodes.Add((IntPtr) node);

                    UpdateNode($"{addonName}_{nodeId}", node, componentConfig, reset);
                }
            }
        }
    }

    private unsafe void UpdateNode(string nodeKey, AtkResNode* node, GaugeComponentConfig componentConfig, bool reset)
    {
        if (node == null)
        {
            _pluginLog.Error("Node not found to update.");
            return;
        }

        var tracking = GetTrackedNode(nodeKey, node);
        if (reset)
        {
            if (node->Color.A == 0)
                node->Color.A = 255;

            node->SetPositionFloat(tracking.DefaultX + tracking.AdditionalX, tracking.DefaultY + tracking.AdditionalY);
            return;
        }

        if (componentConfig.Hide && node->Color.A != 0)
            node->Color.A = 0;
        else if (!componentConfig.Hide && node->Color.A == 0)
            node->Color.A = 255;

        var intendedX = tracking.DefaultX + tracking.AdditionalX + componentConfig.OffsetX;
        var intendedY = tracking.DefaultY + tracking.AdditionalY + componentConfig.OffsetY;

        node->SetPositionFloat(intendedX, intendedY);
        tracking.LastX = intendedX;
        tracking.LastY = intendedY;
    }

    private unsafe GaugeComponentTracking GetTrackedNode(string nodeKey, AtkResNode* node) {
        if (node == null)
            return null;

        if (_trackedNodeValues.TryGetValue(nodeKey, out var tracking))
            return tracking;

        tracking = new GaugeComponentTracking
        {
            DefaultX = (int) node->X,
            DefaultY = (int) node->Y
        };
        _trackedNodeValues[nodeKey] = tracking;

        return tracking;
    }

    private unsafe AtkUnitBase* GetUnitBase(string name) => (AtkUnitBase*) _gameGui.GetAddonByName(name, 1);

    private unsafe void HookAddonUpdate(string addonName, AtkUnitBase* hudAddon)
    {
        if (hudAddon == null || _addonUpdateHooks.ContainsKey(addonName))
            return;

        _addonUpdateHooks[addonName] = new Hook<AddonOnUpdate>(new IntPtr(hudAddon->AtkEventListener.vfunc[39]),
            atkunitbase => OnUpdate(addonName, atkunitbase));
        _addonUpdateHooks[addonName]?.Enable();
    }

    private unsafe byte OnUpdate(string addonName, AtkUnitBase* atkunitbase)
    {
        var result = _addonUpdateHooks[addonName].Original(atkunitbase);
        if (_currentJob.HasValue && !_jobChangeUpdateCount.HasValue)
        {
            UpdateTrackedNodes(addonName);
        }

        return result;
    }

    private unsafe void UpdateTrackedNodes(string addonName)
    {
        if (_trackedNodes == null)
            return;

        if (!_trackedNodes.ContainsKey(addonName))
            return;

        foreach (var node in _trackedNodes[addonName])
        {
            UpdateTrackedNode(addonName, (AtkResNode*) node);
        }
    }

    private unsafe void UpdateTrackedNode(string addonName, AtkResNode* node)
    {
        if (node == null)
            return;

        var tracking = GetTrackedNode($"{addonName}_{node->NodeID}", node);

        var xDiff = (int) node->X - tracking.LastX;
        var yDiff = (int) node->Y - tracking.LastY;

        if (xDiff != 0)
        {
            tracking.AdditionalX += xDiff;
            _pluginLog.Debug($"X Shifted for Node ID {node->NodeID} by {xDiff}. New offset {tracking.AdditionalX}");

            tracking.LastX = (int) node->X;
            _pluginLog.Debug($"New LastX for Node ID {node->NodeID}: {tracking.LastX}");
        }

        if (yDiff != 0)
        {
            tracking.AdditionalY += yDiff;
            _pluginLog.Debug($"Y Shifted for Node ID {node->NodeID} by {yDiff}. New offset {tracking.AdditionalY}");

            tracking.LastY = (int) node->Y;
            _pluginLog.Debug($"New LastY for Node ID {node->NodeID}: {tracking.LastY}");
        }

        var map = JobMap.GetJobMap(_currentJob);
        if (map == null)
            return;

        var jobConfig = GetJobConfig(_currentJob!.Value);
        var addonPart = map.Addons[addonName].FirstOrDefault(p => p.NodeIds.Contains(node->NodeID));
        if (jobConfig.Components[addonPart!.Key].Hide && node->Color.A != 0)
            node->Color.A = 0;
    }

    private void EnsureConfigurationSetup()
    {
        foreach (var job in JobMap.Map.Values)
        {
            var jobConfig = GetJobConfig(job.Id);
            foreach (var piece in job.Addons.SelectMany(a => a.Value))
            {
                EnsureComponentConfig(jobConfig, piece.Key);
            }
        }
    }

    private JobConfiguration GetJobConfig(uint jobId)
    {
        if (_configuration.Jobs.TryGetValue(jobId, out var jobConfig))
            return jobConfig;
        
        jobConfig = new JobConfiguration();
        _configuration.Jobs[jobId] = jobConfig;

        return jobConfig;
    }

    private void EnsureComponentConfig(JobConfiguration jobConfig, string pieceKey)
    {
        if (jobConfig.Components.TryGetValue(pieceKey, out var componentConfig))
            return;
        
        componentConfig = new GaugeComponentConfig();
        jobConfig.Components[pieceKey] = componentConfig;
    }

    public void Dispose()
    {
        ResetGauges();
    }
}