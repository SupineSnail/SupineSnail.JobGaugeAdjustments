namespace SupineSnail.JobGaugeAdjustments.Configuration;

public class JobConfiguration
{
    public bool Enabled;
    public Dictionary<string, GaugeComponentConfig> Components = new();
}