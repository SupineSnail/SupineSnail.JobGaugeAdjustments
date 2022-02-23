using System.IO;
using Newtonsoft.Json;
using SupineSnail.JobGaugeAdjustments.Abstractions;

namespace SupineSnail.JobGaugeAdjustments.Services;


public class LocalizedString {
    [JsonProperty("message")]
    public string Message { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }
}

public class LocalizationService : ILocalizationService
{
    private SortedDictionary<string, LocalizedString> _localizationStrings = new();
    private readonly IFileSystemService _fileService;
    private readonly DalamudPluginInterface _pluginInterface;

    public LocalizationService(IFileSystemService fileService, DalamudPluginInterface pluginInterface)
    {
        _fileService = fileService;
        _pluginInterface = pluginInterface;
    }

    public void Load(ClientLanguage language) 
    {
        _localizationStrings = new SortedDictionary<string, LocalizedString>();

        var locDir = _pluginInterface.GetPluginLocDirectory();
        if (string.IsNullOrWhiteSpace(locDir)) 
            return;

        var languageCode = language switch
        {
            ClientLanguage.Japanese => "jp",
            ClientLanguage.English => "en",
            ClientLanguage.German => "de",
            ClientLanguage.French => "fr",
            _ => throw new ArgumentOutOfRangeException(nameof(language), language, null)
        };

        var langFile = Path.Combine(locDir, $"{languageCode}/strings.json");
        if (!_fileService.Exists(langFile))
            return;
        
        var json = _fileService.ReadFileText(langFile);
        _localizationStrings = JsonConvert.DeserializeObject<SortedDictionary<string, LocalizedString>>(json);
    }

    public string Localize(string key, string fallbackValue, string description = null) {
        if (_localizationStrings.ContainsKey(key))
            return _localizationStrings[key].Message;
        
        _localizationStrings[key] = new LocalizedString
        {
            Message = fallbackValue,
            Description = description ?? $"{key} - {fallbackValue}"
        };
        return fallbackValue;
    }

    public string this[string key, string fallbackValue, string description] =>
        Localize(key, fallbackValue, description);

    public string this[string key, string fallbackValue] => this[key, fallbackValue, string.Empty];
}