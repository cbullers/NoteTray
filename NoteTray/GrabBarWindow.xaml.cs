using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using NoteTray.Services;

namespace NoteTray;

public partial class GrabBarWindow : Window
{
    private readonly SettingsService _settingsService;
    private readonly DispatcherTimer _monitorTracker;
    private string _currentMonitorId = string.Empty;

    public event EventHandler? GrabBarClicked;

    public GrabBarWindow(SettingsService settingsService)
    {
        _settingsService = settingsService;
        InitializeComponent();

        _monitorTracker = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _monitorTracker.Tick += MonitorTracker_Tick;

        PositionOnCurrentMonitor();
    }

    public void StartTracking()
    {
        _monitorTracker.Start();
    }

    public void StopTracking()
    {
        _monitorTracker.Stop();
    }

    private void MonitorTracker_Tick(object? sender, EventArgs e)
    {
        var monitor = MonitorService.GetCurrentMonitor();
        var monitorId = $"{monitor.WorkArea.X},{monitor.WorkArea.Y},{monitor.WorkArea.Width},{monitor.WorkArea.Height}";

        if (monitorId != _currentMonitorId)
        {
            _currentMonitorId = monitorId;
            PositionOnMonitor(monitor);
        }
    }

    private void PositionOnCurrentMonitor()
    {
        var monitor = MonitorService.GetCurrentMonitor();
        PositionOnMonitor(monitor);
    }

    private void PositionOnMonitor(MonitorService.MonitorInfo monitor)
    {
        var isRight = _settingsService.Settings.GrabBarSide == "Right";

        if (isRight)
        {
            this.Left = monitor.WorkArea.Right - this.Width;
        }
        else
        {
            this.Left = monitor.WorkArea.Left;
        }

        this.Top = monitor.WorkArea.Top + (monitor.WorkArea.Height - this.Height) / 2;

        _currentMonitorId = $"{monitor.WorkArea.X},{monitor.WorkArea.Y},{monitor.WorkArea.Width},{monitor.WorkArea.Height}";
    }

    protected override void OnMouseEnter(MouseEventArgs e)
    {
        base.OnMouseEnter(e);
        var animation = new DoubleAnimation(0.7, TimeSpan.FromMilliseconds(150));
        this.BeginAnimation(OpacityProperty, animation);
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        var animation = new DoubleAnimation(0.3, TimeSpan.FromMilliseconds(150));
        this.BeginAnimation(OpacityProperty, animation);
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        GrabBarClicked?.Invoke(this, EventArgs.Empty);
    }
}
