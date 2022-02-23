namespace SupineSnail.JobGaugeAdjustments.Configuration;

internal class JobGaugeMap
{
    internal JobGaugeMap(uint id, string name, bool comingSoon = false) {
        Id = id;
        Name = name;
        ComingSoon = comingSoon;
    }
            
    internal uint Id { get; }
    internal string Name { get; }
    internal bool ComingSoon { get; }

    internal Dictionary<string, AddonComponentPart[]> Addons { get; set; } = new();
}

internal class AddonComponentPart {
    internal AddonComponentPart(string key, string configName, params uint[] nodeIds) {
        Key = key;
        ConfigName = configName;
        NodeIds = nodeIds;
    }

    internal string Key { get; }
    internal string ConfigName { get; }
    internal uint[] NodeIds { get; }
}