using Avalonia;
using Avalonia.Controls.Primitives;

namespace AvaloniaLoudnessMeter.Controls;

public class LargeLabelControl : TemplatedControl
{
    public static readonly StyledProperty<string> LargeTextProperty = AvaloniaProperty.Register<LargeLabelControl, string>(
        nameof(LargeText), "Large Text");

    public string LargeText
    {
        get => GetValue(LargeTextProperty);
        set => SetValue(LargeTextProperty, value);
    }

    public static readonly StyledProperty<string> SmallTextProperty = AvaloniaProperty.Register<LargeLabelControl, string>(
        nameof(SmallText), "small text");

    public string SmallText
    {
        get => GetValue(SmallTextProperty);
        set => SetValue(SmallTextProperty, value);
    }
}