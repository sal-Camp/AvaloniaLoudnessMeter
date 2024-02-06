using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaLoudnessMeter.ViewModels;

namespace AvaloniaLoudnessMeter.Views;

public partial class MainView : UserControl
{

    private readonly Control _channelConfigurationPopup;
    private readonly Control _channelConfigurationButton;
    private readonly Control _mainGrid;
    public MainView()
    {
        InitializeComponent();

        _channelConfigurationButton = this.FindControl<Control>("ChannelConfigurationButton") ?? throw new Exception("Cannot find Channel Configuration Button by name.");
        _channelConfigurationPopup = this.FindControl<Control>("ChannelConfigurationPopup") ?? throw new Exception("Cannot find Channel Configuration Popup by name.");
        _mainGrid = this.FindControl<Control>("MainGrid") ?? throw new Exception("Cannot find Main Grid by name.");
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        // Get relative position of button in relation to the main grid
        var position =
            _channelConfigurationButton.TranslatePoint(new Point(), _mainGrid) ?? throw new Exception("Cannot get TranslatePoint from Channel Configuration Button.");

        // Move the popup to top left of the button
        Dispatcher.UIThread.Post(() =>
            _channelConfigurationPopup.Margin = new Thickness(
                position.X,
                0,
                0,
                _mainGrid.Bounds.Height - position.Y)
        );
    }

    private void ChannelConfigurationPopupOutside_OnPointerPressed(object? sender, PointerPressedEventArgs e) =>
        ((MainViewModel)DataContext).ChannelConfigurationButtonPressedCommand.Execute(null);
}