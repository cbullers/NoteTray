namespace NoteTray.Models;

public class NoteIndex
{
    public List<Folder> Folders { get; set; } = new();
    public List<Note> Notes { get; set; } = new();
}
