using System.IO;
using System.Text.Json;
using NoteTray.Models;

namespace NoteTray.Services;

public class NoteStorageService
{
    private static readonly string AppDataPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NoteTray");
    private static readonly string IndexPath = Path.Combine(AppDataPath, "index.json");
    private static readonly string NotesDir = Path.Combine(AppDataPath, "notes");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private NoteIndex _index = new();

    public IReadOnlyList<Folder> Folders => _index.Folders.AsReadOnly();
    public IReadOnlyList<Note> Notes => _index.Notes.AsReadOnly();

    public void Initialize()
    {
        Directory.CreateDirectory(AppDataPath);
        Directory.CreateDirectory(NotesDir);

        if (File.Exists(IndexPath))
        {
            var json = File.ReadAllText(IndexPath);
            _index = JsonSerializer.Deserialize<NoteIndex>(json, JsonOptions) ?? new NoteIndex();
        }

        if (_index.Folders.Count == 0)
        {
            _index.Folders.Add(new Folder { Name = "General", SortOrder = 0 });
            SaveIndex();
        }
    }

    public List<Note> GetNotesForFolder(string folderId)
    {
        return _index.Notes
            .Where(n => n.FolderId == folderId)
            .OrderBy(n => n.SortOrder)
            .ToList();
    }

    public Note CreateNote(string folderId)
    {
        var maxSort = _index.Notes
            .Where(n => n.FolderId == folderId)
            .Select(n => n.SortOrder)
            .DefaultIfEmpty(-1)
            .Max();

        var note = new Note
        {
            FolderId = folderId,
            Title = "New Note",
            SortOrder = maxSort + 1
        };

        _index.Notes.Add(note);
        SaveIndex();

        var notePath = GetNotePath(note.Id);
        File.WriteAllText(notePath, string.Empty);

        return note;
    }

    public void DeleteNote(string noteId)
    {
        _index.Notes.RemoveAll(n => n.Id == noteId);
        SaveIndex();

        var notePath = GetNotePath(noteId);
        if (File.Exists(notePath))
            File.Delete(notePath);
    }

    public string LoadNoteContent(string noteId)
    {
        var notePath = GetNotePath(noteId);
        return File.Exists(notePath) ? File.ReadAllText(notePath) : string.Empty;
    }

    public void SaveNoteContent(string noteId, string content)
    {
        var note = _index.Notes.FirstOrDefault(n => n.Id == noteId);
        if (note == null) return;

        note.ModifiedAt = DateTime.UtcNow;

        var lines = content.Split('\n');
        var firstLine = lines.FirstOrDefault()?.Trim() ?? "";
        note.Title = string.IsNullOrWhiteSpace(firstLine) ? "New Note" : firstLine;
        if (note.Title.Length > 50)
            note.Title = note.Title[..50] + "...";

        SaveIndex();
        File.WriteAllText(GetNotePath(noteId), content);
    }

    public Folder CreateFolder(string name)
    {
        var maxSort = _index.Folders.Select(f => f.SortOrder).DefaultIfEmpty(-1).Max();
        var folder = new Folder { Name = name, SortOrder = maxSort + 1 };
        _index.Folders.Add(folder);
        SaveIndex();
        return folder;
    }

    public void DeleteFolder(string folderId)
    {
        var notesInFolder = _index.Notes.Where(n => n.FolderId == folderId).ToList();
        foreach (var note in notesInFolder)
        {
            var path = GetNotePath(note.Id);
            if (File.Exists(path)) File.Delete(path);
        }

        _index.Notes.RemoveAll(n => n.FolderId == folderId);
        _index.Folders.RemoveAll(f => f.Id == folderId);
        SaveIndex();
    }

    public void RenameFolder(string folderId, string newName)
    {
        var folder = _index.Folders.FirstOrDefault(f => f.Id == folderId);
        if (folder != null)
        {
            folder.Name = newName;
            SaveIndex();
        }
    }

    private void SaveIndex()
    {
        var json = JsonSerializer.Serialize(_index, JsonOptions);
        File.WriteAllText(IndexPath, json);
    }

    private string GetNotePath(string noteId) => Path.Combine(NotesDir, $"{noteId}.md");
}
