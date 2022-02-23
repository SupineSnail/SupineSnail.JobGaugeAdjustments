namespace SupineSnail.JobGaugeAdjustments.Abstractions;

public interface ILocalizationService
{
    void Load(ClientLanguage language);

    string Localize(string key, string fallbackValue, string description);
    
    string this[string key, string fallbackValue, string description] { get; }
    string this[string key, string fallbackValue] { get; }
}