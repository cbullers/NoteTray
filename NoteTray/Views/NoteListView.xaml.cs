using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NoteTray.ViewModels;
using WpfButton = System.Windows.Controls.Button;

namespace NoteTray.Views;

public partial class NoteListView : UserControl
{
    private System.Windows.Point _dragStartPoint;
    private bool _isDragging;

    public NoteListView()
    {
        InitializeComponent();
    }

    private void NoteList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(null);
        _isDragging = false;
    }

    private void NoteList_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;

        var diff = _dragStartPoint - e.GetPosition(null);
        if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;

        if (_isDragging) return;

        var item = FindAncestor<ListBoxItem>((DependencyObject)e.OriginalSource);
        if (item == null) return;

        // Don't start drag if clicking a button
        if (FindAncestor<WpfButton>((DependencyObject)e.OriginalSource) != null) return;

        _isDragging = true;
        var noteVm = item.DataContext as NoteViewModel;
        if (noteVm != null)
        {
            System.Windows.DragDrop.DoDragDrop(item, noteVm, System.Windows.DragDropEffects.Move);
        }
        _isDragging = false;
    }

    private void NoteList_Drop(object sender, System.Windows.DragEventArgs e)
    {
        var droppedNote = e.Data.GetData(typeof(NoteViewModel)) as NoteViewModel;
        if (droppedNote == null) return;

        var target = FindAncestor<ListBoxItem>((DependencyObject)e.OriginalSource);
        var targetNote = target?.DataContext as NoteViewModel;

        if (targetNote == null || targetNote == droppedNote) return;

        var vm = DataContext as MainViewModel;
        vm?.MoveNote(droppedNote, targetNote);
    }

    private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
    {
        while (current != null)
        {
            if (current is T t) return t;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }
}
