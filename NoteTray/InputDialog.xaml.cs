using System.Windows;

namespace NoteTray;

public partial class InputDialog : Window
{
    public string ResponseText => InputTextBox.Text;

    public InputDialog(string title, string prompt)
    {
        InitializeComponent();
        Title = title;
        PromptText.Text = prompt;
        InputTextBox.Focus();
    }

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
