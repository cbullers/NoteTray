namespace NoteTray.Models;

public class Folder
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "General";
    public int SortOrder { get; set; }
}
