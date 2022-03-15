using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Newtonsoft.Json;
using SupineSnail.JobGaugeAdjustments.Abstractions;

namespace SupineSnail.JobGaugeAdjustments.Configuration;

public class ConfigurationWindow : Window
{
    private static readonly Vector2 DefaultPosition = new(100, 100);
    private static readonly Vector2 DefaultSize = new(500, 500);
    private static readonly Vector2 MinSize = new(500, 500);
    
    private readonly ILocalizationService _localization;
    private readonly IPluginLog _pluginLog;
    private ConfigurationModel _configuration;

    public ConfigurationWindow(ILocalizationService localization, IPluginLog pluginLog) : base("JobGaugeAdjustments::ConfigRoot")
    {
        _localization = localization;
        _pluginLog = pluginLog;
             
        Flags = ImGuiWindowFlags.None;
        Position = DefaultPosition * ImGuiHelpers.GlobalScale;
        Size = DefaultSize * ImGuiHelpers.GlobalScale;
        IsOpen = false;
        
        WindowName = _localization["Configuration_WindowTitle", "Job Gauge Adjustments Configuration"];
        
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = MinSize * ImGuiHelpers.GlobalScale,
            MaximumSize = ImGui.GetMainViewport().Size
        };
        //
        _pluginLog.Debug("Created Config Window");
    }

    public override void Draw()
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = MinSize * ImGuiHelpers.GlobalScale,
            MaximumSize = ImGui.GetMainViewport().Size
        };

        Position = null;
        Size = null;

        DrawConfiguration();
    }

    public delegate void Trigger();
    public event Trigger OnChanged;

    private uint? _selectedJob;

    public void SetConfiguration(ConfigurationModel currentConfiguration)
    {
        _configuration = currentConfiguration;
    }

    private void DrawConfiguration()
    {
        if (_configuration == null)
            return;

        var hasChanged = false;
        var available = ImGui.GetContentRegionAvail();
        var padding = ImGui.GetStyle().CellPadding.X;
        
        var allHeight = available.Y * ImGuiHelpers.GlobalScale;
        var jobSelectorWidth = 150 * ImGuiHelpers.GlobalScale;
        var configWidth = available.X - padding * 2 - jobSelectorWidth;
        
        // Job List (Left)
        ImGui.BeginChild("JobGaugeAdjustments::Configuration::JobList", new Vector2(jobSelectorWidth, allHeight), false);

        foreach (var job in JobMap.Map.Values.OrderBy(j => j.Name))
        {
            if (ImGui.Selectable(_localization[job.Name, job.Name], _selectedJob == job.Id))
                _selectedJob = job.Id;
        }

        ImGui.EndChild();
        ImGui.SameLine();

        // Job Configuration (Right)
        var jobMap = JobMap.GetJobMap(_selectedJob);
        var rightFlags = jobMap == null
             ? ImGuiWindowFlags.None
             : ImGuiWindowFlags.MenuBar;
        
        ImGui.BeginChild("JobGaugeAdjustments::Configuration::JobParameters", new Vector2(configWidth, allHeight),
            false, rightFlags);
        if (ImGui.BeginMenuBar())
        {
            ImGui.TextWrapped(_localization[jobMap!.Name, jobMap.Name]);
            ImGui.EndMenuBar();
        }
        
        if (jobMap != null)
        {
            var jobConfig = GetJobConfig(jobMap.Id);
            hasChanged |= DrawConfigJobSection(jobMap, jobConfig);
        }
        else
        {
            ImGui.TextWrapped(_localization["No_Job_Selected", "Select a Job!"]);
        }
        
        ImGui.EndChild();

        if (!hasChanged)
            return;
    
        OnChanged!.Invoke();
    }

    private bool DrawConfigJobSection(JobGaugeMap job, JobConfiguration jobConfig)
    {
        if (jobConfig == null)
            return false;
        
        var hasChanged = false;

        if (job.ComingSoon)
        {
            ImGui.Text(_localization["Coming_Soon", "Coming Soon!"]);
            return false;
        }

        // Draw name + Enable flag, expand as needed
        hasChanged |= ImGui.Checkbox(_localization["Config_Enable_Job", "Enable", "Button to enable the job"], ref jobConfig.Enabled);
        if (!jobConfig.Enabled) 
            return hasChanged;

        var components = job.Addons.SelectMany(a => a.Value).ToArray();
        for (var i = 0; i < components.Length; i++)
        {
            var componentConfig = GetComponentConfig(jobConfig, components[i].Key);
            if (componentConfig == null)
            {
                _pluginLog.Error($"Component in config is null for job {job.Name} component {components[i].Key}");
                continue;
            }

            hasChanged |=
                DrawEditorForPiece(
                    _localization["GaugePiece_" + job.Name + "_" + components[i].Key, components[i].ConfigName],
                    componentConfig, components[i]);

            if (i < components.Length - 1)
                ImGui.Separator();
        }

        return hasChanged;
    }

    private bool DrawEditorForPiece(string label, GaugeComponentConfig gaugeConfig, AddonComponentPart componentPart) 
    {
        var hasChanged = false;
            
        var xAvail = ImGui.GetContentRegionAvail().X;
        ImGui.Text(label);
        ImGui.SameLine();

        ImGui.SetCursorPosX(Math.Min(320 * ImGuiHelpers.GlobalScale, xAvail - 30 * ImGuiHelpers.GlobalScale));
        ImGui.PushFont(UiBuilder.IconFont);
        if (ImGui.Button($"{(char) FontAwesomeIcon.CircleNotch}##resetOffsetX_{label}")) 
        {
            gaugeConfig.OffsetX = 0;
            gaugeConfig.OffsetY = 0;
            gaugeConfig.Rotation = 0;
            gaugeConfig.Hide = false;
            gaugeConfig.LeftAlign = false;
            hasChanged = true;
        }
        ImGui.PopFont();
            
        hasChanged |= ImGui.Checkbox(_localization["Hide_Element", "Hide", "Hide Button Text"] + $"###hide_{label}", ref gaugeConfig.Hide);
        if (gaugeConfig.Hide) 
            return hasChanged;
            
        ImGui.Text(_localization["X_Offset", "X Offset: "]);
        ImGui.SameLine();
            
        ImGui.SetNextItemWidth(150 * ImGuiHelpers.GlobalScale);
        hasChanged |= ImGui.InputInt($"##offsetX_{label}", ref gaugeConfig.OffsetX);
            
        ImGui.Text(_localization["Y_Offset", "Y Offset: "]);
        ImGui.SameLine();
            
        ImGui.SetNextItemWidth(150 * ImGuiHelpers.GlobalScale);
        hasChanged |= ImGui.InputInt($"##offsetY_{label}", ref gaugeConfig.OffsetY);

        if (componentPart.CanRotate)
        {
            ImGui.Text(_localization["Rotation", "Rotation: "]);
            ImGui.SameLine();

            ImGui.SetNextItemWidth(145 * ImGuiHelpers.GlobalScale);
            hasChanged |= ImGui.DragInt($"##rotation_{label}", ref gaugeConfig.Rotation, 1, -180, 180, "%d", ImGuiSliderFlags.AlwaysClamp);
        }

        if (componentPart.CanLeftAlign)
        {
            hasChanged |= ImGui.Checkbox(_localization["Left_Align", "Left Align", "Left Align Checkbox Text"] + $"###leftAlign_{label}", ref gaugeConfig.LeftAlign);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(_localization["Left_Align_Tooltip", "Will left align text instead of the default right alignment."]);
        }
        
        return hasChanged;
    }

    private JobConfiguration GetJobConfig(uint jobId) =>
        _configuration.Jobs.TryGetValue(jobId, out var jobConfig) 
            ? jobConfig 
            : null;

    private GaugeComponentConfig GetComponentConfig(JobConfiguration jobConfig, string pieceKey) =>
        jobConfig.Components.TryGetValue(pieceKey, out var componentConfig)
            ? componentConfig
            : null;
}