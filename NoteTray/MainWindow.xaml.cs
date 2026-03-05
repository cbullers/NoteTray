using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using NoteTray.Services;
using NoteTray.ViewModels;

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

    public void PositionOffScreen()
    {
        var monitor = MonitorService.GetCurrentMonitor();
        var panelWidth = _settingsService.Settings.PanelWidth;

        this.Width = panelWidth;
        this.Height = monitor.WorkArea.Height;
        this.Top = monitor.WorkArea.Top;
        this.Left = monitor.WorkArea.Right;
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

        this.Height = monitor.WorkArea.Height;
        this.Top = monitor.WorkArea.Top;
        this.Left = monitor.WorkArea.Right;
        this.Width = panelWidth;

        this.Show();

        var animation = new DoubleAnimation
        {
            From = monitor.WorkArea.Right,
            To = monitor.WorkArea.Right - panelWidth,
            Duration = TimeSpan.FromMilliseconds(_settingsService.Settings.AnimationDurationMs),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        animation.Completed += (_, _) =>
        {
            _isVisible = true;
            this.Activate();
        };

        this.BeginAnimation(LeftProperty, animation);
    }

    public void SlideOut()
    {
        if (!_isVisible) return;

        ViewModel.SaveAll();

        var monitor = MonitorService.GetCurrentMonitor();

        var animation = new DoubleAnimation
        {
            To = monitor.WorkArea.Right,
            Duration = TimeSpan.FromMilliseconds(_settingsService.Settings.AnimationDurationMs),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };

        animation.Completed += (_, _) =>
        {
            _isVisible = false;
            this.Hide();
        };

        this.BeginAnimation(LeftProperty, animation);
    }

    public bool IsPanelVisible => _isVisible;

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

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            if (ViewModel.IsEditing)
                ViewModel.SelectedNote = null;
            else
                SlideOut();
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }
}
