using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SpriteRigStudio.Domain.Common;
using SpriteRigStudio.Domain.Skeletons;

namespace SpriteRigStudio.Desktop.Dialogs;

/// <summary>
/// Display item for a bone in the parent bone selector.
/// </summary>
internal class BoneItem
{
    public BoneItem(BoneId id, string name)
    {
        Id = id;
        Name = name;
    }

    public BoneId Id { get; }
    public string Name { get; }

    public override string ToString() => Name;
}

/// <summary>
/// Dialog for adding a new bone to a skeleton: name, role, and parent selection.
/// </summary>
public partial class AddBoneDialog : Window
{
    /// <summary>Bone name entered by the user.</summary>
    public string BoneName => NameBox.Text ?? string.Empty;

    /// <summary>Bone role selected by the user.</summary>
    public BoneRole Role => (BoneRole)RoleCombo.SelectedIndex;

    /// <summary>Parent bone identifier selected by the user.</summary>
    public BoneId ParentBoneId { get; private set; }

    public AddBoneDialog(SkeletonDefinition skeleton)
    {
        InitializeComponent();

        // Populate role combo with all BoneRole enum values
        RoleCombo.ItemsSource = System.Enum.GetValues<BoneRole>();
        RoleCombo.SelectedIndex = 0;

        // Populate parent bone combo from skeleton bones
        var boneItems = skeleton.Bones.Values
            .Select(b => new BoneItem(b.BoneId, b.Name))
            .ToList();

        ParentBoneCombo.ItemsSource = boneItems;

        // Select root bone by default
        if (skeleton.GetRootBone() is { } rootBone)
        {
            var defaultItem = boneItems.FirstOrDefault(b => b.Id == rootBone.BoneId);
            if (defaultItem != null)
                ParentBoneCombo.SelectedItem = defaultItem;
        }

        if (ParentBoneCombo.SelectedItem == null && boneItems.Count > 0)
            ParentBoneCombo.SelectedIndex = 0;

        // Track selection
        ParentBoneCombo.SelectionChanged += (_, _) =>
        {
            if (ParentBoneCombo.SelectedItem is BoneItem item)
                ParentBoneId = item.Id;
        };

        // Initialize ParentBoneId
        if (ParentBoneCombo.SelectedItem is BoneItem initialItem)
            ParentBoneId = initialItem.Id;
    }

    private void OnAdd(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text))
            return;

        if (ParentBoneCombo.SelectedItem == null)
            return;

        Close(DialogResult.Ok);
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Close(DialogResult.Cancel);
    }
}
