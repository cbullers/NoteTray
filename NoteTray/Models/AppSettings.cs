namespace NoteTray.Models;

public class AppSettings
{
    public string Hotkey { get; set; } = "Ctrl+Alt+Space";
    public bool GrabBarEnabled { get; set; } = true;
    public string GrabBarSide { get; set; } = "Right";
    public int PanelWidth { get; set; } = 350;
    public int AnimationDurationMs { get; set; } = 200;
    public bool StartWithWindows { get; set; } = false;
}
