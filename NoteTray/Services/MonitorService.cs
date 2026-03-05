using System.Windows;
using WinForms = System.Windows.Forms;

namespace NoteTray.Services;

public class MonitorService
{
    public class MonitorInfo
    {
        public required Rect WorkArea { get; init; }
        public required Rect Bounds { get; init; }
        public bool IsPrimary { get; init; }
    }

    public static MonitorInfo GetCurrentMonitor()
    {
        var cursorPos = WinForms.Cursor.Position;
        var screen = WinForms.Screen.FromPoint(cursorPos);
        return FromScreen(screen);
    }

    public static MonitorInfo GetMonitorAt(double x, double y)
    {
        var point = new System.Drawing.Point((int)x, (int)y);
        var screen = WinForms.Screen.FromPoint(point);
        return FromScreen(screen);
    }

    private static MonitorInfo FromScreen(WinForms.Screen screen)
    {
        return new MonitorInfo
        {
            WorkArea = new Rect(
                screen.WorkingArea.X,
                screen.WorkingArea.Y,
                screen.WorkingArea.Width,
                screen.WorkingArea.Height),
            Bounds = new Rect(
                screen.Bounds.X,
                screen.Bounds.Y,
                screen.Bounds.Width,
                screen.Bounds.Height),
            IsPrimary = screen.Primary
        };
    }
}
