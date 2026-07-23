using System.Linq;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using SpriteRigStudio.Desktop.ViewModels;
using SpriteRigStudio.Domain.Common;

namespace SpriteRigStudio.Desktop.Dialogs;

/// <summary>
/// Dialog for creating a new character rig: choose a name, skeleton, and optional source artwork.
/// </summary>
public partial class NewCharacterDialog : Window
{
    /// <summary>Character name entered by the user.</summary>
    public string? CharacterName => NameBox.Text;

    /// <summary>Skeleton selected by the user.</summary>
    public SkeletonId? SelectedSkeletonId { get; private set; }

    /// <summary>Artwork file path selected by the user, if any.</summary>
    public string? ArtworkPath => ArtworkPathBox.Text;

    public NewCharacterDialog(IEnumerable<SkeletonItem> skeletons)
    {
        InitializeComponent();

        foreach (var s in skeletons)
            SkeletonCombo.Items.Add(s);

        if (SkeletonCombo.Items.Count > 0)
            SkeletonCombo.SelectedIndex = 0;
    }

    private async void OnBrowseArtwork(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var files = await this.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Character Artwork",
            AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("PNG Images") { Patterns = new[] { "*.png" } } }
        });

        var file = files?.FirstOrDefault();
        if (file != null)
            ArtworkPathBox.Text = file.Path.LocalPath;
    }

    private void OnCreate(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text) || SkeletonCombo.SelectedItem == null)
            return;

        SelectedSkeletonId = ((SkeletonItem)SkeletonCombo.SelectedItem).Id;
        Close(DialogResult.Ok);
    }

    private void OnCancel(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(DialogResult.Cancel);
    }
}
