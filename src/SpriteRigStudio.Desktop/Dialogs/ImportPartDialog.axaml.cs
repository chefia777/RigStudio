using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using SpriteRigStudio.Domain.Common;
using SpriteRigStudio.Domain.Geometry;
using SpriteRigStudio.Domain.Skeletons;

namespace SpriteRigStudio.Desktop.Dialogs;

public partial class ImportPartDialog : Window
{
    private string? _selectedFilePath;

    public string PartName => PartNameBox.Text ?? string.Empty;
    public string? ImagePath => _selectedFilePath;
    public BoneId BoundBoneId { get; private set; }
    public Vector2D PositionOffset => new(
        double.TryParse(OffsetXBox.Text, out var x) ? x : 0,
        double.TryParse(OffsetYBox.Text, out var y) ? y : 0);

    public ImportPartDialog(SkeletonDefinition skeleton)
    {
        InitializeComponent();

        // Populate bone combo from skeleton bones
        var boneItems = skeleton.Bones.Values
            .Select(b => new BoneItem(b.BoneId, b.Name))
            .ToList();

        BoneCombo.ItemsSource = boneItems;

        // Select root bone by default
        if (skeleton.GetRootBone() is { } rootBone)
        {
            var defaultItem = boneItems.FirstOrDefault(b => b.Id == rootBone.BoneId);
            if (defaultItem != null)
                BoneCombo.SelectedItem = defaultItem;
        }

        if (BoneCombo.SelectedItem == null && boneItems.Count > 0)
            BoneCombo.SelectedIndex = 0;

        // Track selection
        BoneCombo.SelectionChanged += (_, _) =>
        {
            if (BoneCombo.SelectedItem is BoneItem item)
                BoundBoneId = item.Id;
        };

        // Initialize BoundBoneId
        if (BoneCombo.SelectedItem is BoneItem initialItem)
            BoundBoneId = initialItem.Id;
    }

    private async void OnChooseFile(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Part PNG",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("PNG Images") { Patterns = new[] { "*.png" } }
            }
        });

        var file = files?.FirstOrDefault();
        if (file != null)
        {
            _selectedFilePath = file.TryGetLocalPath() ?? file.Path.ToString();
            FilePathText.Text = System.IO.Path.GetFileName(_selectedFilePath);
        }
    }

    private void OnImport(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(PartNameBox.Text))
            return;

        if (string.IsNullOrEmpty(_selectedFilePath))
            return;

        if (BoneCombo.SelectedItem == null)
            return;

        Close(DialogResult.Ok);
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Close(DialogResult.Cancel);
    }
}
