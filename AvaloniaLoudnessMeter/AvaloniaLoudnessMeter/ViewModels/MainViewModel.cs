using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaLoudnessMeter.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _boldTitle = "AVALONIA";

    [ObservableProperty]
    private string _regularTitle = "LOUDNESS METER";
}