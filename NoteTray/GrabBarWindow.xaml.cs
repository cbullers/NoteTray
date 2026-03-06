using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using NoteTray.Services;

namespace NoteTray;

public partial class GrabBarWindow : Window
{
    private readonly SettingsService _settingsService;
    private string _currentMonitorId = string.Empty;
    private IntPtr _mouseHookId = IntPtr.Zero;
    private LowLevelMouseProc? _mouseProc; // keep reference to prevent GC collection
    private long _lastCheckMs;

    public event EventHandler? GrabBarClicked;

    public GrabBarWindow(SettingsService settingsService)
    {
        _settingsService = settingsService;
        InitializeComponent();
        PositionOnCurrentMonitor();
    }

    public void StartTracking()
    {
        _mouseProc = MouseHookCallback;
        using var module = Process.GetCurrentProcess().MainModule!;
        _mouseHookId = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, GetModuleHandle(module.ModuleName!), 0);
    }

    public void StopTracking()
    {
        if (_mouseHookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_mouseHookId);
            _mouseHookId = IntPtr.Zero;
        }
    }

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (int)wParam == WM_MOUSEMOVE)
        {
            var nowMs = Environment.TickCount64;
            if (nowMs - _lastCheckMs >= 100) // throttle to ~10 checks/sec
            {
                _lastCheckMs = nowMs;
                var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                var monitorHandle = MonitorFromPoint(hookStruct.pt, MONITOR_DEFAULTTONEAREST);
                var monitorId = monitorHandle.ToString();
                if (monitorId != _currentMonitorId)
                {
                    _currentMonitorId = monitorId;
                    Dispatcher.BeginInvoke(PositionOnCurrentMonitor);
                }
            }
        }
        return CallNextHookEx(_mouseHookId, nCode, wParam, lParam);
    }

    private void PositionOnCurrentMonitor()
    {
        var monitor = MonitorService.GetCurrentMonitor();
        PositionOnMonitor(monitor);

        // Re-sync the ID using the same HMONITOR approach as the hook
        var cursorPos = System.Windows.Forms.Cursor.Position;
        _currentMonitorId = MonitorFromPoint(cursorPos, MONITOR_DEFAULTTONEAREST).ToString();
    }

    private void PositionOnMonitor(MonitorService.MonitorInfo monitor)
    {
        var isRight = _settingsService.Settings.GrabBarSide == "Right";
        double left = isRight ? monitor.WorkArea.Right - Width : monitor.WorkArea.Left;
        double top = monitor.WorkArea.Top + (monitor.WorkArea.Height - Height) / 2;
        MoveWindowAtomic(left, top);
    }

    // Moves X and Y in a single SetWindowPos call to prevent the snap caused by
    // two separate WPF property sets (Left then Top) producing two Win32 calls.
    private void MoveWindowAtomic(double left, double top)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero)
        {
            Left = left;
            Top = top;
            return;
        }

        var source = HwndSource.FromHwnd(hwnd);
        var toDevice = source?.CompositionTarget?.TransformToDevice ?? Matrix.Identity;
        var physical = toDevice.Transform(new System.Windows.Point(left, top));
        SetWindowPos(hwnd, IntPtr.Zero, (int)physical.X, (int)physical.Y, 0, 0,
            SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
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

    // ── Win32 P/Invokes ────────────────────────────────────────────────────────

    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public System.Drawing.Point pt;
        public uint mouseData, flags, time;
        public IntPtr dwExtraInfo;
    }

    private const int WH_MOUSE_LL = 14;
    private const int WM_MOUSEMOVE = 0x0200;
    private const uint MONITOR_DEFAULTTONEAREST = 2;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;

    [DllImport("user32.dll")] static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc fn, IntPtr hMod, uint threadId);
    [DllImport("user32.dll")] static extern bool UnhookWindowsHookEx(IntPtr hhk);
    [DllImport("user32.dll")] static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
    [DllImport("kernel32.dll")] static extern IntPtr GetModuleHandle(string lpModuleName);
    [DllImport("user32.dll")] static extern IntPtr MonitorFromPoint(System.Drawing.Point pt, uint dwFlags);
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);
}
