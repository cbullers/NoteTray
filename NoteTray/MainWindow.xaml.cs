using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using FontAwesome.Sharp;
using NoteTray.Services;
using NoteTray.ViewModels;
using NoteTray.Views;

namespace NoteTray;

public partial class MainWindow : Window
{
    private readonly SettingsService _settingsService;
    private bool _isVisible;

    public MainViewModel ViewModel { get; }

    public MainWindow(MainViewModel viewModel, SettingsService settingsService)
    {
        _settingsService = settingsService;
        ViewModel = viewModel;
        DataContext = viewModel;

        InitializeComponent();
        PositionOffScreen();
    }

    // The window is always sized to the max resize width (transparent areas are
    // click-through on layered windows). The visible panel is positioned inside
    // via PanelBorder.Margin. This means resize only changes a margin — no
    // window move/resize ever happens while visible, so the right edge is rock-solid.

    private static double GetMaxWidth(MonitorService.MonitorInfo monitor) =>
        Math.Min(monitor.WorkArea.Width / 2, 1200);

    private void UpdatePanelLayout(double panelWidth)
    {
        var leftMargin = this.Width - panelWidth;
        PanelBorder.Margin = new Thickness(leftMargin, 8, 8, 8);
        ResizeGrip.Margin = new Thickness(leftMargin, 8, 0, 8);
    }

    public void PositionOffScreen()
    {
        var monitor = MonitorService.GetCurrentMonitor();
        var panelWidth = _settingsService.Settings.PanelWidth;
        var maxWidth = GetMaxWidth(monitor);

        this.Width = maxWidth;
        this.Height = monitor.WorkArea.Height;
        this.Top = monitor.WorkArea.Top;
        this.Left = monitor.WorkArea.Right - maxWidth;
        UpdatePanelLayout(panelWidth);
        SlideTransform.X = panelWidth;
        _isVisible = false;
    }

    public void TogglePanel()
    {
        if (_isVisible)
            SlideOut();
        else
            SlideIn();
    }

    public void SlideIn()
    {
        if (_isVisible) return;

        var monitor = MonitorService.GetCurrentMonitor();
        var panelWidth = _settingsService.Settings.PanelWidth;
        var maxWidth = GetMaxWidth(monitor);

        this.Height = monitor.WorkArea.Height;
        this.Top = monitor.WorkArea.Top;
        this.Width = maxWidth;
        this.Left = monitor.WorkArea.Right - maxWidth;
        UpdatePanelLayout(panelWidth);
        SlideTransform.X = panelWidth;

        this.Show();

        var animation = new DoubleAnimation
        {
            From = panelWidth,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(_settingsService.Settings.AnimationDurationMs),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        animation.Completed += (_, _) =>
        {
            _isVisible = true;
            this.Activate();
        };

        SlideTransform.BeginAnimation(TranslateTransform.XProperty, animation);
    }

    public void SlideOut()
    {
        if (!_isVisible) return;

        ViewModel.SaveAll();

        var panelWidth = _settingsService.Settings.PanelWidth;

        var animation = new DoubleAnimation
        {
            To = panelWidth,
            Duration = TimeSpan.FromMilliseconds(_settingsService.Settings.AnimationDurationMs),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };

        animation.Completed += (_, _) =>
        {
            _isVisible = false;
            this.Hide();
        };

        SlideTransform.BeginAnimation(TranslateTransform.XProperty, animation);
    }

    public bool IsPanelVisible => _isVisible;

    private bool _resizing;
    private double _resizeStartScreenX;
    private double _resizeStartWidth;

    private void ResizeGrip_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _resizing = true;
        _resizeStartScreenX = this.Left + e.GetPosition(this).X;
        _resizeStartWidth = _settingsService.Settings.PanelWidth;
        ((UIElement)sender).CaptureMouse();
        e.Handled = true;
    }

    private void ResizeGrip_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_resizing) return;
        var currentScreenX = this.Left + e.GetPosition(this).X;
        var delta = _resizeStartScreenX - currentScreenX;
        var newWidth = Math.Clamp(_resizeStartWidth + delta, 200, this.Width - 58);
        _settingsService.Settings.PanelWidth = (int)newWidth;
        UpdatePanelLayout(newWidth);
    }

    private void ResizeGrip_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_resizing) return;
        _resizing = false;
        ((UIElement)sender).ReleaseMouseCapture();
        _settingsService.Save();
    }

    private void NewFolderButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new InputDialog("New Folder", "Enter folder name:");
        dialog.Owner = this;
        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.ResponseText))
        {
            ViewModel.CurrentFolder = null; // force refresh
            ((RelayCommand<string>)ViewModel.NewFolderCommand).Execute(dialog.ResponseText);
        }
    }

    private void TogglePreviewButton_Click(object sender, RoutedEventArgs e)
    {
        EditorView.TogglePreview();
        TogglePreviewIcon.Icon = EditorView.IsPreviewMode ? IconChar.Edit : IconChar.Eye;
        TogglePreviewButton.ToolTip = EditorView.IsPreviewMode ? "Back to Editor" : "Toggle Preview";
    }

    private void ResetPreviewIcon()
    {
        TogglePreviewIcon.Icon = IconChar.Eye;
        TogglePreviewButton.ToolTip = "Toggle Preview";
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            if (ViewModel.IsEditing)
            {
                ViewModel.SelectedNote = null;
                ResetPreviewIcon();
            }
            else
                SlideOut();
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }
}
