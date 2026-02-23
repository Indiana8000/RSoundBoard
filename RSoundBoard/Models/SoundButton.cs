namespace TestApp1.Models;

public class SoundButton
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Label { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string Group { get; set; } = "Default";
    public int Order { get; set; }
}
