namespace ScumFreeBot.Models;

public sealed class UpdateInfo
{
    public string? Version { get; set; }
    public string? DownloadUrl { get; set; }
    public string? PatchNotesUrl { get; set; }
    public bool Mandatory { get; set; }
    public string? Sha256 { get; set; }
}