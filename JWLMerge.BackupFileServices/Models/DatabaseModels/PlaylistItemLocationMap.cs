namespace JWLMerge.BackupFileServices.Models.DatabaseModels;

public class PlaylistItemLocationMap
{
    /// <summary>
    /// The playlist item identifier.
    /// </summary>
    public int PlaylistItemId { get; set; }

    /// <summary>
    /// The playlist item location map identifier.
    /// </summary>
    public int LocationId { get; set; }

    /// <summary>
    /// The playlist item location map multimedia type.
    /// </summary>
    public int MajorMultimediaType { get; set; }

    /// <summary>
    /// The playlist item base media duration.
    /// </summary>
    public double? BaseDurationTicks { get; set; }

    public PlaylistItemLocationMap Clone() => (PlaylistItemLocationMap)MemberwiseClone();
}