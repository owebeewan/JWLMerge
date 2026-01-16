namespace JWLMerge.BackupFileServices.Models.DatabaseModels;

public class PlaylistItemMarker
{
    /// <summary>
    /// The playlist item marker identifier.
    /// </summary>
    public int PlaylistItemMarkerId { get; set; }

    /// <summary>
    /// The playlist item identifier.
    /// </summary>
    public int PlaylistItemId { get; set; }

    /// <summary>
    /// The label of the playlist.
    /// </summary>
    public string Label { get; set; } = null!;

    /// <summary>
    /// The start time of the playlist.
    /// </summary>
    public int StartTimeTicks { get; set; }

    /// <summary>
    /// The duration time of the playlist.
    /// </summary>
    public int DurationTicks { get; set; }

    /// <summary>
    /// The end transition duration of the playlist.
    /// </summary>
    public int EndTransitionDurationTicks { get; set; }

    public PlaylistItemMarker Clone() => (PlaylistItemMarker)MemberwiseClone();
}