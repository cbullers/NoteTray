using System.Drawing;
using System.Windows;
using System.Windows.Input;
using Hardcodet.Wpf.TaskbarNotification;
using NHotkey;
using NHotkey.Wpf;
using NoteTray.Services;
using NoteTray.ViewModels;

namespace NoteTray;

public partial class App : Application
{
    private TaskbarIcon? _trayIcon;
    private MainWindow? _mainWindow;
    private GrabBarWindow? _grabBar;
    private NoteStorageService? _storage;
    private SettingsService? _settings;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Initialize services
        _settings = new SettingsService();
        _settings.Load();
        _settings.ApplyStartWithWindows();

        _storage = new NoteStorageService();
        _storage.Initialize();

        // Create the main view model and window
        var viewModel = new MainViewModel(_storage);
        _mainWindow = new MainWindow(viewModel, _settings);

        // Create the grab bar
        _grabBar = new GrabBarWindow(_settings);
        _grabBar.GrabBarClicked += OnGrabBarClicked;
        _mainWindow.Deactivated += OnMainWindowDeactivated;

        if (_settings.Settings.GrabBarEnabled)
        {
            _grabBar.Show();
            _grabBar.StartTracking();
        }

        // Set up system tray icon
        SetupTrayIcon();

        // Register global hotkey
        RegisterHotkey();
    }

    private void SetupTrayIcon()
    {
        _trayIcon = new TaskbarIcon
        {
            Icon = CreateTrayIcon(),
            ToolTipText = "NoteTray",
            ContextMenu = CreateContextMenu()
        };

        _trayIcon.TrayLeftMouseDown += (_, _) => TogglePanel();
    }

    private System.Windows.Controls.ContextMenu CreateContextMenu()
    {
        var menu = new System.Windows.Controls.ContextMenu();

        var showItem = new System.Windows.Controls.MenuItem { Header = "Show/Hide" };
        showItem.Click += (_, _) => TogglePanel();
        menu.Items.Add(showItem);

        menu.Items.Add(new System.Windows.Controls.Separator());

        var exitItem = new System.Windows.Controls.MenuItem { Header = "Exit" };
        exitItem.Click += (_, _) => ExitApplication();
        menu.Items.Add(exitItem);

        return menu;
    }

    private Icon CreateTrayIcon()
    {
        // Create a simple 16x16 icon programmatically
        var bitmap = new Bitmap(16, 16);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.Clear(Color.Transparent);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Draw a simple notepad icon
            using var pen = new Pen(Color.FromArgb(91, 156, 246), 1.5f);
            using var brush = new SolidBrush(Color.FromArgb(91, 156, 246));

            // Outer rectangle
            g.DrawRectangle(pen, 2, 1, 11, 13);
            // Lines
            g.DrawLine(pen, 4, 5, 11, 5);
            g.DrawLine(pen, 4, 8, 11, 8);
            g.DrawLine(pen, 4, 11, 8, 11);
        }

        var handle = bitmap.GetHicon();
        return Icon.FromHandle(handle);
    }

    private void RegisterHotkey()
    {
        try
        {
            var (modifiers, key) = ParseHotkey(_settings!.Settings.Hotkey);
            HotkeyManager.Current.AddOrReplace("TogglePanel", key, modifiers, OnHotkeyPressed);
        }
        catch (HotkeyAlreadyRegisteredException)
        {
            // Hotkey is in use by another app — silently skip
        }
        catch
        {
            // Invalid hotkey config — skip
        }

        try
        {
            var (modifiers, key) = ParseHotkey(_settings!.Settings.PasteHotkey);
            HotkeyManager.Current.AddOrReplace("PasteNote", key, modifiers, OnPasteHotkeyPressed);
        }
        catch (HotkeyAlreadyRegisteredException)
        {
            // Hotkey is in use by another app — silently skip
        }
        catch
        {
            // Invalid hotkey config — skip
        }
    }

    private void OnHotkeyPressed(object? sender, HotkeyEventArgs e)
    {
        TogglePanel();
        e.Handled = true;
    }

    private void OnPasteHotkeyPressed(object? sender, HotkeyEventArgs e)
    {
        var text = System.Windows.Clipboard.GetText();
        if (!string.IsNullOrEmpty(text))
        {
            _mainWindow?.ViewModel.CreateNoteFromClipboard(text);
            _trayIcon?.ShowBalloonTip("NoteTray", "Note captured from clipboard", BalloonIcon.Info);
        }
        e.Handled = true;
    }

    private void TogglePanel()
    {
        if (_mainWindow == null) return;

        if (_mainWindow.IsPanelVisible)
        {
            _mainWindow.SlideOut();
            if (_settings!.Settings.GrabBarEnabled)
            {
                _grabBar?.Show();
            }
        }
        else
        {
            _grabBar?.Hide();
            _mainWindow.SlideIn();
        }
    }

    private void OnGrabBarClicked(object? sender, EventArgs e)
    {
        TogglePanel();
    }

    private void OnMainWindowDeactivated(object? sender, EventArgs e)
    {
        if (_mainWindow!.IsPanelVisible)
        {
            _mainWindow.SlideOut();
            if (_settings!.Settings.GrabBarEnabled)
                _grabBar?.Show();
        }
    }

    private void ExitApplication()
    {
        _mainWindow?.ViewModel.SaveAll();
        _settings?.Save();
        _grabBar?.StopTracking();
        _grabBar?.Close();
        _mainWindow?.Close();
        _trayIcon?.Dispose();

        try
        {
            HotkeyManager.Current.Remove("TogglePanel");
        }
        catch { }

        try
        {
            HotkeyManager.Current.Remove("PasteNote");
        }
        catch { }

        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        base.OnExit(e);
    }

    public static (ModifierKeys modifiers, Key key) ParseHotkey(string hotkeyString)
    {
        var modifiers = ModifierKeys.None;
        var key = Key.None;

        var parts = hotkeyString.Split('+');
        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            switch (trimmed.ToLowerInvariant())
            {
                case "ctrl":
                case "control":
                    modifiers |= ModifierKeys.Control;
                    break;
                case "alt":
                    modifiers |= ModifierKeys.Alt;
                    break;
                case "shift":
                    modifiers |= ModifierKeys.Shift;
                    break;
                case "win":
                case "windows":
                    modifiers |= ModifierKeys.Windows;
                    break;
                default:
                    if (Enum.TryParse<Key>(trimmed, true, out var parsedKey))
                        key = parsedKey;
                    break;
            }
        }

        return (modifiers, key);
    }
}
