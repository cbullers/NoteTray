using System.Windows.Threading;
using NoteTray.Models;
using NoteTray.Services;

namespace NoteTray.ViewModels;

public class NoteViewModel : ViewModelBase
{
    private readonly NoteStorageService _storage;
    private readonly DispatcherTimer _autoSaveTimer;
    private string _content = string.Empty;
    private bool _isDirty;

    public Note Model { get; }

    public string Id => Model.Id;
    public string Title => Model.Title;
    public DateTime ModifiedAt => Model.ModifiedAt;

    public string Content
    {
        get => _content;
        set
        {
            if (SetProperty(ref _content, value))
            {
                _isDirty = true;
                _autoSaveTimer.Stop();
                _autoSaveTimer.Start();
            }
        }
    }

    public NoteViewModel(Note model, NoteStorageService storage)
    {
        Model = model;
        _storage = storage;
        _content = storage.LoadNoteContent(model.Id);

        _autoSaveTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _autoSaveTimer.Tick += (_, _) =>
        {
            _autoSaveTimer.Stop();
            if (_isDirty)
            {
                Save();
            }
        };
    }

    public void Save()
    {
        if (!_isDirty) return;
        _storage.SaveNoteContent(Model.Id, _content);
        _isDirty = false;
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(ModifiedAt));
    }
}
