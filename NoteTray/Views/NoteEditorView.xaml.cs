using System.Windows;
using System.Windows.Controls;

namespace NoteTray.Views;

public partial class NoteEditorView : UserControl
{
    public NoteEditorView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue != null)
        {
            EditorTextBox.Focus();
            EditorTextBox.CaretIndex = EditorTextBox.Text.Length;
        }
    }
}
