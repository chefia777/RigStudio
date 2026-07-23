using System;
using ReactiveUI;

namespace SpriteRigStudio.Desktop.ViewModels;

public class MainWindowViewModel : ReactiveObject
{
    private string _title = "Sprite Rig Studio";

    public string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    public MainWindowViewModel()
    {
    }
}
