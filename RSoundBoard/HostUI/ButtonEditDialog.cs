using TestApp1.Helpers;
using TestApp1.Models;
using TestApp1.Services;

namespace TestApp1.HostUI;

public class ButtonEditDialog : Form
{
    private TextBox _labelTextBox = null!;
    private TextBox _filePathTextBox = null!;
    private Button _browseButton = null!;
    private ComboBox _groupComboBox = null!;
    private NumericUpDown _orderNumeric = null!;
    private Button _okButton = null!;
    private Button _cancelButton = null!;
    private readonly ButtonRepository _repository;

    public SoundButton Button { get; private set; }

    public ButtonEditDialog(ButtonRepository repository, SoundButton? existingButton = null)
    {
        _repository = repository;
        Button = existingButton != null
            ? new SoundButton
            {
                Id = existingButton.Id,
                Label = existingButton.Label,
                FilePath = existingButton.FilePath,
                Group = existingButton.Group,
                Order = existingButton.Order
            }
            : new SoundButton();

        InitializeUI();
        LoadData();
    }

    private void InitializeUI()
    {
        Text = "Edit Button";
        Width = 500;
        Height = 280;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var labelLabel = new Label { Text = "Label:", Left = 10, Top = 20, Width = 100 };
        _labelTextBox = new TextBox { Left = 120, Top = 17, Width = 350 };

        var fileLabel = new Label { Text = "File:", Left = 10, Top = 60, Width = 100 };
        _filePathTextBox = new TextBox { Left = 120, Top = 57, Width = 270, AllowDrop = true };
        _filePathTextBox.DragEnter += FilePathTextBox_DragEnter;
        _filePathTextBox.DragDrop += FilePathTextBox_DragDrop;
        _browseButton = new Button { Text = "...", Left = 400, Top = 55, Width = 70 };
        _browseButton.Click += BrowseButton_Click;

        var groupLabel = new Label { Text = "Group:", Left = 10, Top = 100, Width = 100 };
        _groupComboBox = new ComboBox 
        { 
            Left = 120, 
            Top = 97, 
            Width = 350,
            DropDownStyle = ComboBoxStyle.DropDown
        };

        var orderLabel = new Label { Text = "Order:", Left = 10, Top = 140, Width = 100 };
        _orderNumeric = new NumericUpDown { Left = 120, Top = 137, Width = 100, Minimum = 0, Maximum = 9999 };

        _okButton = new Button
        {
            Text = "OK",
            Left = 270,
            Top = 200,
            Width = 100,
            DialogResult = DialogResult.OK
        };
        _okButton.Click += OkButton_Click;

        _cancelButton = new Button
        {
            Text = "Cancel",
            Left = 380,
            Top = 200,
            Width = 100,
            DialogResult = DialogResult.Cancel
        };

        Controls.AddRange(new Control[]
        {
            labelLabel, _labelTextBox,
            fileLabel, _filePathTextBox, _browseButton,
            groupLabel, _groupComboBox,
            orderLabel, _orderNumeric,
            _okButton, _cancelButton
        });

        AcceptButton = _okButton;
        CancelButton = _cancelButton;
    }

    private async void LoadData()
    {
        _labelTextBox.Text = Button.Label;
        _filePathTextBox.Text = Button.FilePath;
        _orderNumeric.Value = Button.Order;

        var existingGroups = await _repository.GetAllGroupsAsync();
        _groupComboBox.Items.Clear();
        foreach (var group in existingGroups)
        {
            _groupComboBox.Items.Add(group);
        }

        _groupComboBox.Text = Button.Group;
    }

    private void BrowseButton_Click(object? sender, EventArgs e)
    {
        using var openFileDialog = new OpenFileDialog
        {
            Filter = "Audio Files|*.wav;*.mp3|All Files|*.*",
            Title = "Select Sound File"
        };

        if (openFileDialog.ShowDialog() == DialogResult.OK)
        {
            _filePathTextBox.Text = PathHelper.ConvertToRelativePathIfPossible(openFileDialog.FileName);
        }
    }

    private void FilePathTextBox_DragEnter(object? sender, DragEventArgs e)
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

    private void FilePathTextBox_DragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
        {
            var filePath = files[0];
            _filePathTextBox.Text = PathHelper.ConvertToRelativePathIfPossible(filePath);

            // If label field is empty, set filename (without extension) as label
            if (string.IsNullOrWhiteSpace(_labelTextBox.Text))
            {
                _labelTextBox.Text = Path.GetFileNameWithoutExtension(filePath);
            }
        }
    }

    private async void OkButton_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_labelTextBox.Text))
        {
            MessageBox.Show("Please enter a label.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }

        if (string.IsNullOrWhiteSpace(_filePathTextBox.Text))
        {
            MessageBox.Show("Please select a file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }

        // Check if file path already exists (except for the current button being edited)
        if (await _repository.FilePathExistsAsync(_filePathTextBox.Text, Button.Id == Guid.Empty ? null : Button.Id))
        {
            MessageBox.Show("This file has already been added to the soundboard.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }

        Button.Label = _labelTextBox.Text;
        Button.FilePath = _filePathTextBox.Text;
        Button.Group = string.IsNullOrWhiteSpace(_groupComboBox.Text) ? "Default" : _groupComboBox.Text;
        Button.Order = (int)_orderNumeric.Value;
    }
}
