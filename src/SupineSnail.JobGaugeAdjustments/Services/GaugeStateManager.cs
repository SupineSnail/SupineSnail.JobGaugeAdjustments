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
    private Hook<AddonOnUpdate> _addonUpdateHook;
    
    private unsafe delegate void* AddonOnFinalize(AtkUnitBase* atkUnitBase);
    private Hook<AddonOnFinalize> _addonFinalizeHook;

    private readonly Dictionary<ushort, string> _atkBaseIdToAddonMap = new();

    private uint? _currentJob;
    private short? _jobChangeUpdateCount;
    private bool _lastWasEnabled;
    private bool _wasFinalized;

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
            {
                if (!_currentJob.HasValue)
                    return;
                
                // Handles logging out and back in
                ResetGauges();
                _currentJob = null;
                _wasFinalized = false;
                
                return;
            }

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
            
            // If finalized but still has a job, probably the aesthetician so we should keep checking until it's ready again
            if (_currentJob != null && _currentJob == job && _wasFinalized && !_jobChangeUpdateCount.HasValue)
            {
                if (CheckGaugesAvailable(_currentJob.Value))
                {
                    _pluginLog.Debug("UI is ready! Treating like new job!");
                    
                    // If ready, treat as if a new job change (wait appropriate frame count)
                    ResetGauges();
                    _currentJob = null;
                }
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
        
        _pluginLog.Debug("Removing Update Hook");
        _addonUpdateHook?.Disable();
        _addonUpdateHook = null;
        
        _pluginLog.Debug("Removing Finalize Hook");
        _addonFinalizeHook?.Disable();
        _addonFinalizeHook = null;
        
        _atkBaseIdToAddonMap.Clear();
        _wasFinalized = false;
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
        
        _pluginLog.Debug("Job found.");
        if (forceReset)
            _pluginLog.Debug("Resetting Gauge to default state.");
        else
            _pluginLog.Debug("Updating Gauges");
            
        var reset = forceReset || !jobConfig.Enabled;

        foreach (var (addonName, components) in map.Addons)
        {
            var hudAddon = GetUnitBase(addonName);
            if (hudAddon == null)
            {
                _pluginLog.Debug($"Could not get base addon {addonName} for job {job} in render.");
                continue;
            }

            if (!reset)
                AttachAddonHooks(addonName, hudAddon);

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

    private unsafe bool CheckGaugesAvailable(uint job)
    {
        var map = JobMap.GetJobMap(job);
        if (map == null)
        {
            _pluginLog.Debug($"Job with Id {job} not found in map. Cannot check.");
            return false;
        }

        var addonName = map.Addons.First().Key;
        if (string.IsNullOrWhiteSpace(addonName))
            return false;
        
        return GetUnitBase(addonName) != null;
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
            if (Math.Abs(node->Rotation - tracking.DefaultRotation) > 0.01)
                ApplyNodeRotationRad(node, tracking.DefaultRotation);
            return;
        }

        if (componentConfig.Hide && node->Color.A != 0)
            node->Color.A = 0;
        else if (!componentConfig.Hide && node->Color.A == 0)
            node->Color.A = 255;

        var intendedX = tracking.DefaultX + tracking.AdditionalX + componentConfig.OffsetX;
        var intendedY = tracking.DefaultY + tracking.AdditionalY + componentConfig.OffsetY;

        // Undo additional X movement if left aligned
        if (componentConfig.LeftAlign)
            intendedX -= tracking.AdditionalX;
        
        node->SetPositionFloat(intendedX, intendedY);
        tracking.LastX = intendedX;
        tracking.LastY = intendedY;

        var radRotation = GetRadian(componentConfig.Rotation);
        if (Math.Abs(node->Rotation - radRotation) > 0.01)
            ApplyNodeRotationRad(node, radRotation);
    }

    private float GetRadian(int deg) => (float) (Math.PI / 180) * deg;

    private unsafe void ApplyNodeRotationRad(AtkResNode* node, float rad)
    {
        _pluginLog.Debug($"Applying rotation {rad}R");
        node->Rotation = rad;
        if ((node->Flags_2 & 0x4) != 0x4)
            node->Flags_2 |= 0x4;
        if ((node->Flags_2 & 0x1) != 0x1)
            node->Flags_2 |= 0x1;
    }

    private unsafe GaugeComponentTracking GetTrackedNode(string nodeKey, AtkResNode* node) {
        if (node == null)
            return null;

        if (_trackedNodeValues.TryGetValue(nodeKey, out var tracking))
            return tracking;

        tracking = new GaugeComponentTracking
        {
            DefaultX = (int) node->X,
            DefaultY = (int) node->Y,
            DefaultRotation = (int) node->Rotation
        };
        _trackedNodeValues[nodeKey] = tracking;

        return tracking;
    }

    private unsafe AtkUnitBase* GetUnitBase(string name) => (AtkUnitBase*) _gameGui.GetAddonByName(name, 1);

    private unsafe void AttachAddonHooks(string addonName, AtkUnitBase* hudAddon)
    {
        if (hudAddon == null)
            return;

        if (!_atkBaseIdToAddonMap.ContainsKey(hudAddon->ID))
            _atkBaseIdToAddonMap[hudAddon->ID] = addonName;
        
        // Only one hook is needed, all updates / finalizes go through the same hook
        if (_addonUpdateHook == null)
        {
            _pluginLog.Debug("Attaching Update");
            _addonUpdateHook = new Hook<AddonOnUpdate>(new IntPtr(hudAddon->AtkEventListener.vfunc[39]), OnUpdate);
            _pluginLog.Debug("Update Enable");
            _addonUpdateHook?.Enable();
        }

        if (_addonFinalizeHook == null)
        {
            _pluginLog.Debug("Attaching Finalize");
            _addonFinalizeHook = new Hook<AddonOnFinalize>(new IntPtr(hudAddon->AtkEventListener.vfunc[38]), OnFinalize);
            _pluginLog.Debug("Finalize Enable");
            _addonFinalizeHook?.Enable();
        }
    }

    private unsafe byte OnUpdate(AtkUnitBase* atkunitbase)
    {
        var result = _addonUpdateHook.Original(atkunitbase);
        
        // Same hook for all updates, make sure we're only triggering for the current job
        if (!_atkBaseIdToAddonMap.ContainsKey(atkunitbase->ID))
            return result;
        
        if (_currentJob.HasValue && !_jobChangeUpdateCount.HasValue)
        {
            UpdateTrackedNodes(_atkBaseIdToAddonMap[atkunitbase->ID]);
        }

        return result;
    }

    private unsafe void* OnFinalize(AtkUnitBase* atkunitbase)
    {
        // Same hook for all finalize, make sure we're only triggering for the current job (switch will remove previous)
        if (!_atkBaseIdToAddonMap.ContainsKey(atkunitbase->ID))
            return _addonFinalizeHook.Original(atkunitbase);

        var addonName = _atkBaseIdToAddonMap[atkunitbase->ID];
        _pluginLog.Debug("Finalize for addon " + atkunitbase->ID + " - " + addonName);

        _atkBaseIdToAddonMap.Remove(atkunitbase->ID);
        var result = _addonFinalizeHook.Original(atkunitbase);
        if (_atkBaseIdToAddonMap.Count == 0)
        {
            _pluginLog.Debug("Removing Update Hook");
            _addonUpdateHook?.Disable();
            _addonUpdateHook = null;

            _pluginLog.Debug("Removing Finalize Hook");
            _addonFinalizeHook?.Disable();
            _addonFinalizeHook = null;
            
            // If we got this far, the addon is removing for logout or for aesthetician
            // Logout will be handled by detecting job change. Aesthetician will be detected with checking if the addon becomes available
            _pluginLog.Debug("Checking each loop if job is ready for UI");
            _wasFinalized = true;
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
        var config = jobConfig.Components[addonPart!.Key];
        if (config.Hide && node->Color.A != 0)
            node->Color.A = 0;
        
        // If we are left aligning, undo shift and reset lastX so we can detect next shift
        if (config.LeftAlign && xDiff != 0)
        {
            _pluginLog.Debug("Undoing X shift because this element is left aligned");
            node->SetPositionFloat(node->X - xDiff, node->Y);
            tracking.LastX = (int) node->X;
        }
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