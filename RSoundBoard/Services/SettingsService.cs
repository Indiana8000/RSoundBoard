using System.Text.Json;
using TestApp1.Models;

namespace TestApp1.Services;

public class SettingsService
{
    private readonly string _settingsFilePath;
    private AppSettings _settings;

    public SettingsService()
    {
        var appDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RSoundBoard");
        
        Directory.CreateDirectory(appDataFolder);
        _settingsFilePath = Path.Combine(appDataFolder, "settings.json");
        
        _settings = LoadSettings();
    }

    private AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch
        {
            // Bei Fehler Standard-Einstellungen verwenden
        }

        return new AppSettings();
    }

    private void SaveSettings()
    {
        try
        {
            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            File.WriteAllText(_settingsFilePath, json);
        }
        catch
        {
            // Fehler beim Speichern ignorieren
        }
    }

    public int? GetSelectedAudioDevice()
    {
        return _settings.SelectedAudioDeviceNumber;
    }

    public void SetSelectedAudioDevice(int? deviceNumber)
    {
        _settings.SelectedAudioDeviceNumber = deviceNumber;
        SaveSettings();
    }
}
