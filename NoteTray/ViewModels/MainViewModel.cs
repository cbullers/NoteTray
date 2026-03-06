using System.Collections.ObjectModel;
using System.Windows.Input;
using NoteTray.Models;
using NoteTray.Services;

namespace NoteTray.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly NoteStorageService _storage;
    private Folder? _currentFolder;
    private NoteViewModel? _selectedNote;
    private bool _isEditing;

    public ObservableCollection<Folder> Folders { get; } = new();
    public ObservableCollection<NoteViewModel> Notes { get; } = new();

    public Folder? CurrentFolder
    {
        get => _currentFolder;
        set
        {
            if (SetProperty(ref _currentFolder, value))
                LoadNotesForCurrentFolder();
        }
    }

    public NoteViewModel? SelectedNote
    {
        get => _selectedNote;
        set
        {
            // Save the previously selected note before switching
            _selectedNote?.Save();
            if (SetProperty(ref _selectedNote, value))
            {
                IsEditing = value != null;
            }
        }
    }

    public bool IsEditing
    {
        get => _isEditing;
        set => SetProperty(ref _isEditing, value);
    }

    public ICommand NewNoteCommand { get; }
    public ICommand DeleteNoteCommand { get; }
    public ICommand NewFolderCommand { get; }
    public ICommand DeleteFolderCommand { get; }
    public ICommand BackToListCommand { get; }

    public MainViewModel(NoteStorageService storage)
    {
        _storage = storage;

        NewNoteCommand = new RelayCommand(CreateNewNote, () => CurrentFolder != null);
        DeleteNoteCommand = new RelayCommand<NoteViewModel>(DeleteNote, n => n != null);
        NewFolderCommand = new RelayCommand<string>(CreateNewFolder);
        DeleteFolderCommand = new RelayCommand(DeleteCurrentFolder, () => CurrentFolder != null && Folders.Count > 1);
        BackToListCommand = new RelayCommand(BackToList);

        LoadFolders();
    }

    private void LoadFolders()
    {
        Folders.Clear();
        foreach (var folder in _storage.Folders)
            Folders.Add(folder);

        CurrentFolder = Folders.FirstOrDefault();
    }

    private void LoadNotesForCurrentFolder()
    {
        // Save any open note first
        _selectedNote?.Save();
        SelectedNote = null;
        Notes.Clear();

        if (CurrentFolder == null) return;

        var notes = _storage.GetNotesForFolder(CurrentFolder.Id);
        foreach (var note in notes)
            Notes.Add(new NoteViewModel(note, _storage));
    }

    private void CreateNewNote()
    {
        if (CurrentFolder == null) return;

        var note = _storage.CreateNote(CurrentFolder.Id);
        var vm = new NoteViewModel(note, _storage);
        Notes.Insert(0, vm);
        SelectedNote = vm;
    }

    private void DeleteNote(NoteViewModel? note)
    {
        if (note == null) return;

        if (SelectedNote == note) SelectedNote = null;
        _storage.DeleteNote(note.Id);
        Notes.Remove(note);
    }

    private void CreateNewFolder(string? name)
    {
        var folderName = string.IsNullOrWhiteSpace(name) ? "New Folder" : name;
        var folder = _storage.CreateFolder(folderName);
        Folders.Add(folder);
        CurrentFolder = folder;
    }

    private void DeleteCurrentFolder()
    {
        if (CurrentFolder == null || Folders.Count <= 1) return;

        var folderToDelete = CurrentFolder;
        var idx = Folders.IndexOf(folderToDelete);
        CurrentFolder = Folders[idx > 0 ? idx - 1 : 1];
        _storage.DeleteFolder(folderToDelete.Id);
        Folders.Remove(folderToDelete);
    }

    private void BackToList()
    {
        SelectedNote?.Save();
        SelectedNote = null;
    }

    public void SaveAll()
    {
        _selectedNote?.Save();
    }
}

public class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;

    public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;
    public void Execute(object? parameter) => _execute((T?)parameter);
}
