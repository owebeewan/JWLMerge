namespace JWLMerge.BackupFileServices.Models.DatabaseModels;

public class PlaylistItem
{
    /// <summary>
    /// The playlist item identifier.
    /// </summary>
    public int PlaylistItemId { get; set; }

    /// <summary>
    /// The label of the playlist.
    /// </summary>
    public string Label { get; set; } = null!;

    /// <summary>
    /// The start trim offset of the playlist.
    /// </summary>
    public int? StartTrimOffsetTicks { get; set; }

    /// <summary>
    /// The end trim offset of the playlist.
    /// </summary>
    public int? EndTrimOffsetTicks { get; set; }

    /// <summary>
    /// The accuracy of the playlist.
    /// </summary>
    public int Accuracy { get; set; } = 1;

    /// <summary>
    /// The playlist end action.
    /// </summary>
    public int EndAction { get; set; }

    /// <summary>
    /// The file path of the playlist thumbnail.
    /// </summary>
    public string? ThumbnailFilePath { get; set; }

    public PlaylistItem Clone() => (PlaylistItem)MemberwiseClone();
}