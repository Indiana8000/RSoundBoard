using System.Diagnostics;
using TestApp1.Models;
using TestApp1.Services;

namespace TestApp1.HostUI;

public class MainForm : Form
{
    private readonly ButtonRepository _repository;
    private ListView _buttonListView;
    private Button _addButton;
    private Button _editButton;
    private Button _deleteButton;
    private Button _moveUpButton;
    private Button _moveDownButton;
    private Button _openWebButton;
    private int _sortColumn = -1;
    private bool _sortAscending = true;
    private readonly string[] _columnNames = { "Gruppe", "Label", "Dateipfad", "Order" };

    public MainForm(ButtonRepository repository)
    {
        _repository = repository;
        InitializeUI();
        LoadButtons();
    }

    private void InitializeUI()
    {
        Text = "Soundboard Manager";
        Width = 800;
        Height = 600;
        StartPosition = FormStartPosition.CenterScreen;

        _buttonListView = new ListView
        {
            Left = 10,
            Top = 10,
            Width = 600,
            Height = 500,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            AllowDrop = true
        };

        // Spalten hinzufügen
        _buttonListView.Columns.Add("Gruppe", 100);
        _buttonListView.Columns.Add("Label", 150);
        _buttonListView.Columns.Add("Dateipfad", 250);
        _buttonListView.Columns.Add("Order", 80);

        _buttonListView.ColumnClick += ButtonListView_ColumnClick;
        _buttonListView.DragEnter += ButtonListBox_DragEnter;
        _buttonListView.DragDrop += ButtonListBox_DragDrop;

        _addButton = new Button
        {
            Text = "Hinzufügen",
            Left = 620,
            Top = 10,
            Width = 160
        };
        _addButton.Click += AddButton_Click;

        _editButton = new Button
        {
            Text = "Bearbeiten",
            Left = 620,
            Top = 50,
            Width = 160
        };
        _editButton.Click += EditButton_Click;

        _deleteButton = new Button
        {
            Text = "Löschen",
            Left = 620,
            Top = 90,
            Width = 160
        };
        _deleteButton.Click += DeleteButton_Click;

        _moveUpButton = new Button
        {
            Text = "↑ Nach oben",
            Left = 620,
            Top = 140,
            Width = 160
        };
        _moveUpButton.Click += MoveUpButton_Click;

        _moveDownButton = new Button
        {
            Text = "↓ Nach unten",
            Left = 620,
            Top = 180,
            Width = 160
        };
        _moveDownButton.Click += MoveDownButton_Click;

        _openWebButton = new Button
        {
            Text = "Weboberfläche öffnen",
            Left = 620,
            Top = 470,
            Width = 160,
            Height = 40
        };
        _openWebButton.Click += OpenWebButton_Click;

        Controls.Add(_buttonListView);
        Controls.Add(_addButton);
        Controls.Add(_editButton);
        Controls.Add(_deleteButton);
        Controls.Add(_moveUpButton);
        Controls.Add(_moveDownButton);
        Controls.Add(_openWebButton);
    }

    private async void LoadButtons()
    {
        var buttons = await _repository.GetAllAsync();
        _buttonListView.Items.Clear();

        foreach (var button in buttons.OrderBy(b => b.Group).ThenBy(b => b.Order))
        {
            var item = new ListViewItem(button.Group);
            item.SubItems.Add(button.Label);
            item.SubItems.Add(button.FilePath);
            item.SubItems.Add(button.Order.ToString());
            item.Tag = button;
            _buttonListView.Items.Add(item);
        }
    }

    private async void AddButton_Click(object? sender, EventArgs e)
    {
        // Bestimme die höchste Order-Nummer
        var buttons = await _repository.GetAllAsync();
        var maxOrder = buttons.Any() ? buttons.Max(b => b.Order) : -1;

        var newButton = new SoundButton
        {
            Order = maxOrder + 1,
            Group = "Default"
        };

        using var dialog = new ButtonEditDialog(newButton);
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            await _repository.AddAsync(dialog.Button);
            LoadButtons();
        }
    }

    private async void EditButton_Click(object? sender, EventArgs e)
    {
        if (_buttonListView.SelectedItems.Count == 0 || _buttonListView.SelectedItems[0].Tag is not SoundButton button)
            return;

        var oldGroup = button.Group;
        var oldOrder = button.Order;

        using var dialog = new ButtonEditDialog(button);
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            var allButtons = await _repository.GetAllAsync();
            var editedButton = dialog.Button;

            // Wenn Gruppe oder Order geändert wurde, Order-Nummern neu organisieren
            if (editedButton.Group != oldGroup || editedButton.Order != oldOrder)
            {
                // Alle Buttons der Zielgruppe, außer dem bearbeiteten
                var targetGroupButtons = allButtons
                    .Where(b => b.Group == editedButton.Group && b.Id != button.Id)
                    .OrderBy(b => b.Order)
                    .ToList();

                // Wenn ein anderer Button bereits die gewünschte Order-Nummer hat
                if (targetGroupButtons.Any(b => b.Order == editedButton.Order))
                {
                    // Alle Buttons ab der neuen Position nach hinten verschieben
                    foreach (var btn in targetGroupButtons.Where(b => b.Order >= editedButton.Order))
                    {
                        btn.Order++;
                        await _repository.UpdateAsync(btn.Id, btn);
                    }
                }

                // Alte Gruppe aufräumen, falls die Gruppe gewechselt wurde
                if (editedButton.Group != oldGroup)
                {
                    var oldGroupButtons = allButtons
                        .Where(b => b.Group == oldGroup && b.Id != button.Id && b.Order > oldOrder)
                        .OrderBy(b => b.Order)
                        .ToList();

                    foreach (var btn in oldGroupButtons)
                    {
                        btn.Order--;
                        await _repository.UpdateAsync(btn.Id, btn);
                    }
                }
            }

            await _repository.UpdateAsync(button.Id, editedButton);
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
            await _repository.DeleteAsync(button.Id);
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

        LoadButtons();
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

        LoadButtons();
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
            foreach (var filePath in files)
            {
                // Nur Audio-Dateien verarbeiten
                var extension = Path.GetExtension(filePath).ToLower();
                if (extension == ".wav" || extension == ".mp3" || extension == ".ogg" || extension == ".flac")
                {
                    var fileName = Path.GetFileNameWithoutExtension(filePath);

                    // Bestimme die höchste Order-Nummer
                    var buttons = await _repository.GetAllAsync();
                    var maxOrder = buttons.Any() ? buttons.Max(b => b.Order) : -1;

                    var newButton = new SoundButton
                    {
                        Label = fileName,
                        FilePath = filePath,
                        Group = "Default",
                        Order = maxOrder + 1
                    };

                    await _repository.AddAsync(newButton);
                }
            }

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
}
