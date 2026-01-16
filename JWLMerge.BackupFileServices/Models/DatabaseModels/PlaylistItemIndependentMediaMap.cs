namespace JWLMerge.BackupFileServices.Models.DatabaseModels;

public class PlaylistItemIndependentMediaMap
{
    /// <summary>
    /// The playlist item identifier.
    /// </summary>
    public int PlaylistItemId { get; set; }

    /// <summary>
    /// The playlist item indepentent media identifier.
    /// </summary>
    public int IndependentMediaId { get; set; }

    /// <summary>
    /// The playlist item independent media duration.
    /// </summary>
    public double DurationTicks { get; set; }

    public PlaylistItemIndependentMediaMap Clone() => (PlaylistItemIndependentMediaMap)MemberwiseClone();
}