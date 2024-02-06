using System;
using System.Threading;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace AvaloniaLoudnessMeter.Controls;

public class AnimatedPopup : ContentControl
{
    #region Private Properties

    /// <summary>
    /// The underlay control for closing the popup
    /// </summary>
    private Control _underlayControl;
    /// <summary>
    /// Indicates if first time animating
    /// </summary>
    private bool _firstAnimation = true;
    /// <summary>
    /// Indicates if we have captured the opacity value yet
    /// </summary>
    private bool _opacityCaptured = false;
    /// <summary>
    /// Store control's original opacity value at startup
    /// </summary>
    private double _originalOpacity;
    /// <summary>
    /// Size of the final control after the animation
    /// </summary>
    private Size _desiredSize;
    /// <summary>
    /// Flag for state of animating
    /// </summary>
    private bool _animating;
    /// <summary>
    /// Keeps track of if we have found the desired 100% width/height auto size
    /// </summary>
    private bool _sizeFound;
    /// <summary>
    /// Animation UI Timer
    /// </summary>
    private DispatcherTimer _animationTimer;
    /// <summary>
    /// Timeout timer to detect when auto-sizing has finished
    /// </summary>
    private Timer _sizingTimer;
    /// <summary>
    /// The current position of the animation
    /// </summary>
    private int _animationCurrentTick;

    private TimeSpan _frameRate = TimeSpan.FromSeconds(1 / 60.0);
    private int TotalTicks => (int)(_animationTime.TotalSeconds / _frameRate.TotalSeconds);

    #endregion

    #region Public Properties

    #region Open

    private bool _open;

    public static readonly DirectProperty<AnimatedPopup, bool> OpenProperty = AvaloniaProperty.RegisterDirect<AnimatedPopup, bool>(
        nameof(Open), o => o.Open, (o, v) => o.Open = v);

    /// <summary>
    /// Whether the control should be open or closed
    /// </summary>
    public bool Open
    {
        get => _open;
        set
        {
            if (value == _open)
                return;

            if (value)
            {
                // Inject the underlay
                if (Parent is Grid grid)
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (grid.RowDefinitions?.Count > 0)
                            _underlayControl.SetValue(Grid.RowSpanProperty, grid.RowDefinitions?.Count);
                        if (grid.ColumnDefinitions?.Count > 0)
                            _underlayControl.SetValue(Grid.ColumnSpanProperty, grid.ColumnDefinitions?.Count);

                        if (!grid.Children.Contains(_underlayControl))
                            grid.Children.Insert(0, _underlayControl);
                    });
                }
            }
            // If closing
            else
            {
                // If the control is currently fully open
                if (IsOpened)
                    UpdateDesiredSize();
            }
            UpdateAnimation();
            SetAndRaise(OpenProperty, ref _open, value);
        }
    }

    #endregion

    #region AnimationTime

    private TimeSpan _animationTime = TimeSpan.FromSeconds(3);

    public static readonly DirectProperty<AnimatedPopup, TimeSpan> AnimationTimeProperty = AvaloniaProperty.RegisterDirect<AnimatedPopup, TimeSpan>(
        nameof(AnimationTime), o => o.AnimationTime, (o, v) => o.AnimationTime = v);

    public TimeSpan AnimationTime
    {
        get => _animationTime;
        set => SetAndRaise(AnimationTimeProperty, ref _animationTime, value);
    }

    #endregion

    #region UnderlayOpacity

    private double _underlayOpacity = 0.2;

    public static readonly DirectProperty<AnimatedPopup, double> UnderlayOpacityProperty = AvaloniaProperty.RegisterDirect<AnimatedPopup, double>(
        nameof(UnderlayOpacity), o => o.UnderlayOpacity, (o, v) => o.UnderlayOpacity = v);

    public double UnderlayOpacity
    {
        get => _underlayOpacity;
        set => SetAndRaise(UnderlayOpacityProperty, ref _underlayOpacity, value);
    }

    #endregion

    #region AnimateOpacity

    private bool _animateOpacity = true;

    public static readonly DirectProperty<AnimatedPopup, bool> AnimateOpacityProperty = AvaloniaProperty.RegisterDirect<AnimatedPopup, bool>(
        nameof(AnimateOpacity), o => o.AnimateOpacity, (o, v) => o.AnimateOpacity = v);

    public bool AnimateOpacity
    {
        get => _animateOpacity;
        set => SetAndRaise(AnimateOpacityProperty, ref _animateOpacity, value);
    }

    #endregion

    /// <summary>
    /// If the control is opened
    /// </summary>
    public bool IsOpened => _animationCurrentTick >= TotalTicks;

    #endregion

    #region Public Commands

    public void BeginOpen()
    {
        Open = true;
    }

    public void BeginClose()
    {
        Open = false;
    }

    #endregion

    #region Contructors

    public AnimatedPopup()
    {
        // Make a new underlay control
        _underlayControl = new Border
        {

            Background = Brushes.Black,
            Opacity = 0,
            ZIndex = 9,
        };

        // Close popup on press
        _underlayControl.PointerPressed += (sender, args) =>
        {
            BeginClose();
        };

        _animationTimer = new DispatcherTimer
        {
            Interval = _frameRate
        };

        _sizingTimer = new Timer(t =>
        {
            // If size is already calculated
            if (_sizeFound)
                return;

            _sizeFound = true;

            // Ensure we're on the UI Thread
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                UpdateDesiredSize();

                // Update animation
                UpdateAnimation();
            });
        });

        _animationTimer.Tick += (s, e) => AnimationTick();
    }

    #endregion

    #region Overrides

    public override void Render(DrawingContext context)
    {
        // If desired size not found yet
        if (!_sizeFound)
        {
            if (!_opacityCaptured)
            {
                _opacityCaptured = true;
                // Remember original control's opacity
                _originalOpacity = Opacity;
                Dispatcher.UIThread.InvokeAsync(() => Opacity = 0);
            }

            _sizingTimer.Change(100, int.MaxValue);
        }

        base.Render(context);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Should be called when an open or closed transition has complete.
    /// </summary>
    private void AnimationComplete()
    {
        if (_open)
        {
            Width = double.NaN;
            Height = double.NaN;

            Opacity = _originalOpacity;
        }
        else
        {
            Width = 0;
            Height = 0;

            if (Parent is Grid grid)
            {
                _underlayControl.Opacity = 0;

                if (grid.Children.Contains(_underlayControl))
                    grid.Children.Remove(_underlayControl);
            }
        }

    }

    /// <summary>
    /// Update control's sizes based on next tick of animation
    /// </summary>
    private void AnimationTick()
    {
        // If first call after calculating desired size
        if (_firstAnimation)
        {
            // Clear the flag
            _firstAnimation = false;

            _animationTimer.Stop();

            // Reset opacity
            Opacity = _originalOpacity;

            // Set final size
            AnimationComplete();

            // Finish tick
            return;
        }

        // If at end of animation
        if ((_open && _animationCurrentTick >= TotalTicks) || (!_open && _animationCurrentTick == 0))
        {
            _animationTimer.Stop();

            // Set final size
            AnimationComplete();

            _animating = false;
            return;
        }

        _animating = true;

        // Increment or decrement the tick if opening or closing
        _animationCurrentTick += _open ? 1 : -1;

        // Get percentage of animation progress
        var percentageAnimated = (float)_animationCurrentTick / TotalTicks;

        // Make an easing animation
        var quadraticEasing = new QuadraticEaseIn();
        var linearEasing = new LinearEasing();

        // Calculate final width & height using easing
        var finalWidth = _desiredSize.Width * quadraticEasing.Ease(percentageAnimated);
        var finalHeight = _desiredSize.Height * quadraticEasing.Ease(percentageAnimated);

        Width = finalWidth;
        Height = finalHeight;

        // Animate opacity
        if (AnimateOpacity)
            Opacity = _originalOpacity * linearEasing.Ease(percentageAnimated);

        // Animate underlay
        _underlayControl.Opacity = _underlayOpacity * quadraticEasing.Ease(percentageAnimated);


        Console.WriteLine($"Current tick: {_animationCurrentTick}");
    }

    /// <summary>
    /// Calculate and start any new animation
    /// </summary>
    private void UpdateAnimation()
    {

        // Do nothing if initial size still not found
        if (!_sizeFound)
            return;

        // Start animation thread again
        _animationTimer.Start();
    }

    /// <summary>
    /// Updates the animation desired size based on current visuals
    /// </summary>
    private void UpdateDesiredSize() => _desiredSize = DesiredSize - Margin;

    #endregion


}