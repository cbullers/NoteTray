// Resolve ambiguities between WPF and WinForms
// We use WinForms only for Screen/Cursor APIs via MonitorService
global using Application = System.Windows.Application;
global using UserControl = System.Windows.Controls.UserControl;
global using MouseEventArgs = System.Windows.Input.MouseEventArgs;
global using KeyEventArgs = System.Windows.Input.KeyEventArgs;
