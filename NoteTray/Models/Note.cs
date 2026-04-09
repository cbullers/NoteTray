namespace NoteTray.Models;

public class Note
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FolderId { get; set; } = string.Empty;
    public string Title { get; set; } = "New Note";
    public string? Color { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    public bool IsPinned { get; set; }
    public int SortOrder { get; set; }
}
