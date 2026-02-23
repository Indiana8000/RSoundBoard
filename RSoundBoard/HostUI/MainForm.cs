using System.Diagnostics;
using NAudio.Wave;
using TestApp1.Models;
using TestApp1.Services;

namespace TestApp1.HostUI;

public class MainForm : Form
{
    private readonly ButtonRepository _repository;
    private readonly SoundService _soundService;
    private readonly SettingsService _settingsService;
    private ListView _buttonListView = null!;
    private Button _addButton = null!;
    private Button _editButton = null!;
    private Button _deleteButton = null!;
    private Button _moveUpButton = null!;
    private Button _moveDownButton = null!;
    private Label _audioDeviceLabel = null!;
    private ComboBox _audioDeviceComboBox = null!;
    private Label _microphoneLabel = null!;
    private ComboBox _microphoneComboBox = null!;
    private Button _openWebButton = null!;
    private int _sortColumn = 0;
    private bool _sortAscending = true;
    private readonly string[] _columnNames = { "Gruppe", "Label", "Dateipfad", "Order" };

    public MainForm(ButtonRepository repository, SoundService soundService, SettingsService settingsService)
    {
        _repository = repository;
        _soundService = soundService;
        _settingsService = settingsService;
        InitializeUI();
        LoadButtons();

        var savedDevice = _settingsService.GetSelectedAudioDevice();
        if (savedDevice.HasValue)
        {
            _soundService.SetOutputDevice(savedDevice.Value);
        }

        var savedMicrophone = _settingsService.GetSelectedMicrophone();
        if (savedMicrophone.HasValue)
        {
            _soundService.SetMicrophoneDevice(savedMicrophone.Value);
        }
    }

    private void InitializeUI()
    {
        Text = "R Soundboard Manager";
        Width = 800;
        Height = 560;
        MinimumSize = new Size(800, 560);
        StartPosition = FormStartPosition.CenterScreen;

        try
        {
            Icon = new Icon("RSoundBoard.ico");
        }
        catch
        {
        }

        _buttonListView = new ListView
        {
            Left = 10,
            Top = 10,
            Width = 600,
            Height = 500,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            AllowDrop = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
        };

        // Spalten hinzufügen
        _buttonListView.Columns.Add("Gruppe", 100);
        _buttonListView.Columns.Add("Label", 150);
        _buttonListView.Columns.Add("Dateipfad", 250);
        _buttonListView.Columns.Add("Order", 80);

        _buttonListView.ColumnClick += ButtonListView_ColumnClick;
        _buttonListView.DoubleClick += ButtonListView_DoubleClick;
        _buttonListView.DragEnter += ButtonListBox_DragEnter;
        _buttonListView.DragDrop += ButtonListBox_DragDrop;

        _addButton = new Button
        {
            Text = "Hinzufügen",
            Left = 620,
            Top = 10,
            Width = 160,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _addButton.Click += AddButton_Click;

        _editButton = new Button
        {
            Text = "Bearbeiten",
            Left = 620,
            Top = 50,
            Width = 160,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _editButton.Click += EditButton_Click;

        _deleteButton = new Button
        {
            Text = "Löschen",
            Left = 620,
            Top = 90,
            Width = 160,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _deleteButton.Click += DeleteButton_Click;

        _moveUpButton = new Button
        {
            Text = "↑ Nach oben",
            Left = 620,
            Top = 140,
            Width = 160,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _moveUpButton.Click += MoveUpButton_Click;

        _moveDownButton = new Button
        {
            Text = "↓ Nach unten",
            Left = 620,
            Top = 180,
            Width = 160,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _moveDownButton.Click += MoveDownButton_Click;

        _audioDeviceLabel = new Label
        {
            Text = "Audio-Ausgabegerät:",
            Left = 620,
            Top = 390,
            Width = 160,
            Height = 20,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right
        };

        _audioDeviceComboBox = new ComboBox
        {
            Left = 620,
            Top = 415,
            Width = 160,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right
        };
        _audioDeviceComboBox.SelectedIndexChanged += AudioDeviceComboBox_SelectedIndexChanged;
        LoadAudioDevices();

        _microphoneLabel = new Label
        {
            Text = "Mikrofon:",
            Left = 620,
            Top = 340,
            Width = 160,
            Height = 20,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right
        };

        _microphoneComboBox = new ComboBox
        {
            Left = 620,
            Top = 365,
            Width = 160,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right
        };
        _microphoneComboBox.SelectedIndexChanged += MicrophoneComboBox_SelectedIndexChanged;
        LoadMicrophones();

        _openWebButton = new Button
        {
            Text = "Weboberfläche öffnen",
            Left = 620,
            Top = 470,
            Width = 160,
            Height = 40,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right
        };
        _openWebButton.Click += OpenWebButton_Click;

        Controls.Add(_buttonListView);
        Controls.Add(_addButton);
        Controls.Add(_editButton);
        Controls.Add(_deleteButton);
        Controls.Add(_moveUpButton);
        Controls.Add(_moveDownButton);
        Controls.Add(_microphoneLabel);
        Controls.Add(_microphoneComboBox);
        Controls.Add(_audioDeviceLabel);
        Controls.Add(_audioDeviceComboBox);
        Controls.Add(_openWebButton);
    }

    private async void LoadButtons()
    {
        var buttons = await _repository.GetAllAsync();
        _buttonListView.Items.Clear();

        // Standard-Sortierung: erst nach Gruppe, dann nach Order
        var sortedButtons = buttons.OrderBy(b => b.Group).ThenBy(b => b.Order).ToList();

        foreach (var button in sortedButtons)
        {
            var item = new ListViewItem(button.Group);
            item.SubItems.Add(button.Label);
            item.SubItems.Add(button.FilePath);
            item.SubItems.Add(button.Order.ToString());
            item.Tag = button;
            _buttonListView.Items.Add(item);
        }

        // Spaltenüberschriften aktualisieren, um aktuelle Sortierung anzuzeigen
        UpdateColumnHeaders();
    }

    private async void AddButton_Click(object? sender, EventArgs e)
    {
        var buttons = await _repository.GetAllAsync();
        var defaultGroup = "Default";

        // Bestimme die höchste Order-Nummer in der Default-Gruppe
        var maxOrder = buttons.Where(b => b.Group == defaultGroup).Any() 
            ? buttons.Where(b => b.Group == defaultGroup).Max(b => b.Order) 
            : -1;

        var newButton = new SoundButton
        {
            Order = maxOrder + 1,
            Group = defaultGroup
        };

        using var dialog = new ButtonEditDialog(newButton);
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            // Nach dem Hinzufügen die Gruppe normalisieren
            await _repository.AddAsync(dialog.Button);
            await NormalizeGroupOrders(dialog.Button.Group);
            LoadButtons();
        }
    }

    private async void EditButton_Click(object? sender, EventArgs e)
    {
        if (_buttonListView.SelectedItems.Count == 0 || _buttonListView.SelectedItems[0].Tag is not SoundButton button)
            return;

        var oldGroup = button.Group;

        using var dialog = new ButtonEditDialog(button);
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            var editedButton = dialog.Button;
            await _repository.UpdateAsync(button.Id, editedButton);

            // Beide Gruppen normalisieren (alte und neue, falls gewechselt)
            if (editedButton.Group != oldGroup)
            {
                await NormalizeGroupOrders(oldGroup);
            }
            await NormalizeGroupOrders(editedButton.Group);

            LoadButtons();
        }
    }

    private async void DeleteButton_Click(object? sender, EventArgs e)
    {
        if (_buttonListView.SelectedItems.Count == 0 || _buttonListView.SelectedItems[0].Tag is not SoundButton button)
            return;

        var result = MessageBox.Show(
            $"Button '{button.Label}' wirklich löschen?",
            "Bestätigung",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            var groupToNormalize = button.Group;
            await _repository.DeleteAsync(button.Id);
            await NormalizeGroupOrders(groupToNormalize);
            LoadButtons();
        }
    }

    private async void MoveUpButton_Click(object? sender, EventArgs e)
    {
        if (_buttonListView.SelectedItems.Count == 0 || _buttonListView.SelectedItems[0].Tag is not SoundButton selectedButton)
            return;

        var allButtons = await _repository.GetAllAsync();
        var button = allButtons.FirstOrDefault(b => b.Id == selectedButton.Id);
        if (button == null) return;

        // Alle Buttons der gleichen Gruppe, sortiert nach Order
        var groupButtons = allButtons
            .Where(b => b.Group == button.Group)
            .OrderBy(b => b.Order)
            .ToList();

        var currentIndex = groupButtons.FindIndex(b => b.Id == button.Id);
        if (currentIndex <= 0) return; // Schon am Anfang

        // Tausche Position mit dem vorherigen Button
        var previousButton = groupButtons[currentIndex - 1];
        (button.Order, previousButton.Order) = (previousButton.Order, button.Order);

        // Buttons aktualisieren
        await _repository.UpdateAsync(button.Id, button);
        await _repository.UpdateAsync(previousButton.Id, previousButton);
        await NormalizeGroupOrders(button.Group);

        LoadButtons();

        // Den verschobenen Button wieder auswählen
        SelectButtonById(button.Id);
    }

    private async void MoveDownButton_Click(object? sender, EventArgs e)
    {
        if (_buttonListView.SelectedItems.Count == 0 || _buttonListView.SelectedItems[0].Tag is not SoundButton selectedButton)
            return;

        var allButtons = await _repository.GetAllAsync();
        var button = allButtons.FirstOrDefault(b => b.Id == selectedButton.Id);
        if (button == null) return;

        // Alle Buttons der gleichen Gruppe, sortiert nach Order
        var groupButtons = allButtons
            .Where(b => b.Group == button.Group)
            .OrderBy(b => b.Order)
            .ToList();

        var currentIndex = groupButtons.FindIndex(b => b.Id == button.Id);
        if (currentIndex >= groupButtons.Count - 1) return; // Schon am Ende

        // Tausche Position mit dem nächsten Button
        var nextButton = groupButtons[currentIndex + 1];
        (button.Order, nextButton.Order) = (nextButton.Order, button.Order);

        // Buttons aktualisieren
        await _repository.UpdateAsync(button.Id, button);
        await _repository.UpdateAsync(nextButton.Id, nextButton);
        await NormalizeGroupOrders(button.Group);

        LoadButtons();

        // Den verschobenen Button wieder auswählen
        SelectButtonById(button.Id);
    }

    private void OpenWebButton_Click(object? sender, EventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "http://localhost:5000",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler beim Öffnen des Browsers: {ex.Message}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ButtonListBox_DragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
        {
            e.Effect = DragDropEffects.Copy;
        }
        else
        {
            e.Effect = DragDropEffects.None;
        }
    }

    private async void ButtonListBox_DragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is string[] files)
        {
            var defaultGroup = "Default";

            foreach (var filePath in files)
            {
                // Nur Audio-Dateien verarbeiten
                var extension = Path.GetExtension(filePath).ToLower();
                if (extension == ".wav" || extension == ".mp3" || extension == ".ogg" || extension == ".flac")
                {
                    var fileName = Path.GetFileNameWithoutExtension(filePath);

                    // Bestimme die höchste Order-Nummer in der Default-Gruppe
                    var buttons = await _repository.GetAllAsync();
                    var maxOrder = buttons.Where(b => b.Group == defaultGroup).Any() 
                        ? buttons.Where(b => b.Group == defaultGroup).Max(b => b.Order) 
                        : -1;

                    var newButton = new SoundButton
                    {
                        Label = fileName,
                        FilePath = filePath,
                        Group = defaultGroup,
                        Order = maxOrder + 1
                    };

                    await _repository.AddAsync(newButton);
                }
            }

            await NormalizeGroupOrders(defaultGroup);
            LoadButtons();
        }
    }

    private void ButtonListView_ColumnClick(object? sender, ColumnClickEventArgs e)
    {
        // Wenn auf die gleiche Spalte geklickt wird, Sortierreihenfolge umkehren
        if (e.Column == _sortColumn)
        {
            _sortAscending = !_sortAscending;
        }
        else
        {
            _sortColumn = e.Column;
            _sortAscending = true;
        }

        // Spaltenüberschriften aktualisieren
        UpdateColumnHeaders();

        var items = _buttonListView.Items.Cast<ListViewItem>().ToList();

        items.Sort((x, y) =>
        {
            var xButton = (SoundButton)x.Tag!;
            var yButton = (SoundButton)y.Tag!;

            int result = e.Column switch
            {
                0 => string.Compare(xButton.Group, yButton.Group, StringComparison.Ordinal),
                1 => string.Compare(xButton.Label, yButton.Label, StringComparison.Ordinal),
                2 => string.Compare(xButton.FilePath, yButton.FilePath, StringComparison.Ordinal),
                3 => xButton.Order.CompareTo(yButton.Order),
                _ => 0
            };

            return _sortAscending ? result : -result;
        });

        _buttonListView.Items.Clear();
        _buttonListView.Items.AddRange(items.ToArray());
    }

    private void UpdateColumnHeaders()
    {
        for (int i = 0; i < _buttonListView.Columns.Count; i++)
        {
            if (i == _sortColumn)
            {
                var arrow = _sortAscending ? " ↑" : " ↓";
                _buttonListView.Columns[i].Text = _columnNames[i] + arrow;
            }
            else
            {
                _buttonListView.Columns[i].Text = _columnNames[i];
            }
        }
    }

    /// <summary>
    /// Normalisiert die Order-Nummern einer Gruppe, sodass sie bei 0 beginnen und keine Lücken haben
    /// </summary>
    private async Task NormalizeGroupOrders(string groupName)
    {
        var allButtons = await _repository.GetAllAsync();
        var groupButtons = allButtons
            .Where(b => b.Group == groupName)
            .OrderBy(b => b.Order)
            .ToList();

        for (int i = 0; i < groupButtons.Count; i++)
        {
            if (groupButtons[i].Order != i)
            {
                groupButtons[i].Order = i;
                await _repository.UpdateAsync(groupButtons[i].Id, groupButtons[i]);
            }
        }
    }

    /// <summary>
    /// Wählt einen Button in der ListView anhand seiner ID aus
    /// </summary>
    private void SelectButtonById(Guid buttonId)
    {
        foreach (ListViewItem item in _buttonListView.Items)
        {
            if (item.Tag is SoundButton button && button.Id == buttonId)
            {
                item.Selected = true;
                item.Focused = true;
                item.EnsureVisible();
                break;
            }
        }
    }

    private async void ButtonListView_DoubleClick(object? sender, EventArgs e)
    {
        if (_buttonListView.SelectedItems.Count == 0 || _buttonListView.SelectedItems[0].Tag is not SoundButton button)
            return;

        try
        {
            await _soundService.PlayAsync(button.FilePath);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler beim Abspielen der Datei: {ex.Message}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void LoadAudioDevices()
    {
        _audioDeviceComboBox.Items.Clear();
        _audioDeviceComboBox.Items.Add("(Standard)");

        for (int i = 0; i < NAudio.Wave.WaveOut.DeviceCount; i++)
        {
            var capabilities = NAudio.Wave.WaveOut.GetCapabilities(i);
            _audioDeviceComboBox.Items.Add($"{capabilities.ProductName} (#{i})");
        }

        // Gespeichertes Gerät laden
        var savedDevice = _settingsService.GetSelectedAudioDevice();
        if (savedDevice.HasValue && savedDevice.Value >= 0 && savedDevice.Value < NAudio.Wave.WaveOut.DeviceCount)
        {
            _audioDeviceComboBox.SelectedIndex = savedDevice.Value + 1;
        }
        else
        {
            _audioDeviceComboBox.SelectedIndex = 0;
        }
    }

    private void AudioDeviceComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_audioDeviceComboBox.SelectedIndex <= 0)
        {
            _soundService.SetOutputDevice(null);
            _settingsService.SetSelectedAudioDevice(null);
        }
        else
        {
            var deviceNumber = _audioDeviceComboBox.SelectedIndex - 1;
            _soundService.SetOutputDevice(deviceNumber);
            _settingsService.SetSelectedAudioDevice(deviceNumber);
        }
    }

    private void LoadMicrophones()
    {
        _microphoneComboBox.Items.Clear();
        _microphoneComboBox.Items.Add("(Keines)");

        for (int i = 0; i < WaveInEvent.DeviceCount; i++)
        {
            var capabilities = WaveInEvent.GetCapabilities(i);
            _microphoneComboBox.Items.Add($"{capabilities.ProductName} (#{i})");
        }

        var savedMicrophone = _settingsService.GetSelectedMicrophone();
        if (savedMicrophone.HasValue && savedMicrophone.Value >= 0 && savedMicrophone.Value < WaveInEvent.DeviceCount)
        {
            _microphoneComboBox.SelectedIndex = savedMicrophone.Value + 1;
        }
        else
        {
            _microphoneComboBox.SelectedIndex = 0;
        }
    }

    private void MicrophoneComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_microphoneComboBox.SelectedIndex <= 0)
        {
            _soundService.SetMicrophoneDevice(null);
            _settingsService.SetSelectedMicrophone(null);
        }
        else
        {
            var deviceNumber = _microphoneComboBox.SelectedIndex - 1;
            _soundService.SetMicrophoneDevice(deviceNumber);
            _settingsService.SetSelectedMicrophone(deviceNumber);
        }
    }
}
