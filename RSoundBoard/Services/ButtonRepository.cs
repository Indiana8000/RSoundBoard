using System.Text.Json;
using TestApp1.Helpers;
using TestApp1.Models;

namespace TestApp1.Services;

public class ButtonRepository
{
    private readonly string _dataFilePath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private List<SoundButton> _buttons = new();

    public ButtonRepository()
    {
        var appDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RSoundBoard");

        Directory.CreateDirectory(appDataFolder);
        _dataFilePath = Path.Combine(appDataFolder, "soundboard_data.json");
        LoadData();
    }

    private void LoadData()
    {
        if (!File.Exists(_dataFilePath))
        {
            _buttons = new List<SoundButton>();
            SaveData();
            return;
        }

        try
        {
            var json = File.ReadAllText(_dataFilePath);
            _buttons = JsonSerializer.Deserialize<List<SoundButton>>(json) ?? new List<SoundButton>();

            foreach (var button in _buttons)
            {
                if (!string.IsNullOrEmpty(button.FilePath))
                {
                    var fullPath = PathHelper.GetFullPath(button.FilePath);
                    if (!File.Exists(fullPath))
                    {
                        button.Group = "⁉ Missing Files";
                    }
                }
            }
        }
        catch
        {
            _buttons = new List<SoundButton>();
        }
    }

    private void SaveData()
    {
        var json = JsonSerializer.Serialize(_buttons, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_dataFilePath, json);
    }

    public async Task<List<SoundButton>> GetAllAsync()
    {
        await _lock.WaitAsync();
        try
        {
            return new List<SoundButton>(_buttons);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<SoundButton?> GetByIdAsync(Guid id)
    {
        await _lock.WaitAsync();
        try
        {
            return _buttons.FirstOrDefault(b => b.Id == id);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<SoundButton> AddAsync(SoundButton button)
    {
        await _lock.WaitAsync();
        try
        {
            button.Id = Guid.NewGuid();
            _buttons.Add(button);
            SaveData();
            return button;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> UpdateAsync(Guid id, SoundButton updatedButton)
    {
        await _lock.WaitAsync();
        try
        {
            var button = _buttons.FirstOrDefault(b => b.Id == id);
            if (button == null) return false;

            button.Label = updatedButton.Label;
            button.FilePath = updatedButton.FilePath;
            button.Group = updatedButton.Group;
            button.Order = updatedButton.Order;

            SaveData();
            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        await _lock.WaitAsync();
        try
        {
            var button = _buttons.FirstOrDefault(b => b.Id == id);
            if (button == null) return false;

            _buttons.Remove(button);
            SaveData();
            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<List<string>> GetAllGroupsAsync()
    {
        await _lock.WaitAsync();
        try
        {
            return _buttons
                .Select(b => b.Group)
                .Where(g => !string.IsNullOrWhiteSpace(g))
                .Distinct()
                .OrderBy(g => g)
                .ToList();
        }
        finally
        {
            _lock.Release();
        }
    }
}
