using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SpriteRigStudio.Desktop.ViewModels;
using SpriteRigStudio.Domain.Common;
using SpriteRigStudio.Domain.Exporting;
using SpriteRigStudio.Domain.Projects;

namespace SpriteRigStudio.Desktop.Dialogs;

/// <summary>
/// Dialog for selecting an export profile and triggering animation export.
/// </summary>
public partial class ExportDialog : Window
{
    /// <summary>Export profile identifier selected by the user.</summary>
    public ExportProfileId SelectedProfileId { get; private set; }

    /// <summary>Whether to export individual frame images.</summary>
    public bool ExportIndividualFrames => ExportFramesCheck?.IsChecked ?? false;

    public ExportDialog(
        System.Collections.Generic.IEnumerable<ExportProfile> profiles,
        string characterName,
        string animationName)
    {
        InitializeComponent();

        CharacterNameBox.Text = characterName;
        AnimationNameBox.Text = animationName;

        var profileList = profiles.ToList();
        if (profileList.Count > 0)
        {
            // Wrap profiles for display
            var profileItems = profileList
                .Select(p => new ExportProfileItem(p.ExportProfileId, p.Name))
                .ToList();

            ProfileCombo.ItemsSource = profileItems;
            ProfileCombo.SelectedIndex = 0;

            // Track selection
            ProfileCombo.SelectionChanged += (_, _) =>
            {
                if (ProfileCombo.SelectedItem is ExportProfileItem item)
                    SelectedProfileId = item.Id;
            };

            // Initialize SelectedProfileId
            if (ProfileCombo.SelectedItem is ExportProfileItem initialItem)
                SelectedProfileId = initialItem.Id;
        }
    }

    private void OnExport(object? sender, RoutedEventArgs e)
    {
        if (ProfileCombo.SelectedItem == null)
            return;

        Close(DialogResult.Ok);
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Close(DialogResult.Cancel);
    }
}
