using System.Collections.ObjectModel;
using ReactiveUI;
using SpriteRigStudio.Domain.Common;

namespace SpriteRigStudio.Desktop.ViewModels;

/// <summary>
/// View model for the project panel (left sidebar).
/// Displays the tree of skeletons, characters, animations, and export profiles.
/// </summary>
public class ProjectPanelViewModel : ReactiveObject
{
    private SkeletonItem? _selectedSkeleton;
    private CharacterItem? _selectedCharacter;
    private AnimationItem? _selectedAnimation;
    private ExportProfileItem? _selectedProfile;

    public ProjectPanelViewModel()
    {
        Skeletons = new ObservableCollection<SkeletonItem>();
        Characters = new ObservableCollection<CharacterItem>();
        Animations = new ObservableCollection<AnimationItem>();
        ExportProfiles = new ObservableCollection<ExportProfileItem>();
    }

    /// <summary>Skeletons in the project.</summary>
    public ObservableCollection<SkeletonItem> Skeletons { get; }

    /// <summary>Character rigs in the project.</summary>
    public ObservableCollection<CharacterItem> Characters { get; }

    /// <summary>Animation clips in the project.</summary>
    public ObservableCollection<AnimationItem> Animations { get; }

    /// <summary>Export profiles in the project.</summary>
    public ObservableCollection<ExportProfileItem> ExportProfiles { get; }

    /// <summary>Currently selected skeleton.</summary>
    public SkeletonItem? SelectedSkeleton
    {
        get => _selectedSkeleton;
        set => this.RaiseAndSetIfChanged(ref _selectedSkeleton, value);
    }

    /// <summary>Currently selected character.</summary>
    public CharacterItem? SelectedCharacter
    {
        get => _selectedCharacter;
        set => this.RaiseAndSetIfChanged(ref _selectedCharacter, value);
    }

    /// <summary>Currently selected animation.</summary>
    public AnimationItem? SelectedAnimation
    {
        get => _selectedAnimation;
        set => this.RaiseAndSetIfChanged(ref _selectedAnimation, value);
    }

    /// <summary>Currently selected export profile.</summary>
    public ExportProfileItem? SelectedProfile
    {
        get => _selectedProfile;
        set => this.RaiseAndSetIfChanged(ref _selectedProfile, value);
    }
}

/// <summary>
/// Display item for a skeleton in the project panel.
/// </summary>
public class SkeletonItem
{
    public SkeletonItem(SkeletonId id, string name)
    {
        Id = id;
        Name = name;
    }

    public SkeletonId Id { get; }
    public string Name { get; }

    public override string ToString() => Name;
}

/// <summary>
/// Display item for a character rig in the project panel.
/// </summary>
public class CharacterItem
{
    public CharacterItem(CharacterRigId id, string name)
    {
        Id = id;
        Name = name;
    }

    public CharacterRigId Id { get; }
    public string Name { get; }

    public override string ToString() => Name;
}

/// <summary>
/// Display item for an animation clip in the project panel.
/// </summary>
public class AnimationItem
{
    public AnimationItem(AnimationId id, string name)
    {
        Id = id;
        Name = name;
    }

    public AnimationId Id { get; }
    public string Name { get; }

    public override string ToString() => Name;
}

/// <summary>
/// Display item for an export profile in the project panel.
/// </summary>
public class ExportProfileItem
{
    public ExportProfileItem(ExportProfileId id, string name)
    {
        Id = id;
        Name = name;
    }

    public ExportProfileId Id { get; }
    public string Name { get; }

    public override string ToString() => Name;
}
