using Dalamud.Configuration;

namespace SupineSnail.JobGaugeAdjustments.Configuration;

public class ConfigurationModel : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    public Dictionary<uint, JobConfiguration> Jobs = new();
}