namespace JWLMerge.BackupFileServices.Models.DatabaseModels;

public class PlaylistItemMarkerBibleVerseMap
{
    /// <summary>
    /// The playlist item marker identifier.
    /// </summary>
    public int PlaylistItemMarkerId { get; set; }

    /// <summary>
    /// The verse identifier.
    /// </summary>
    public int VerseId { get; set; }

    public PlaylistItemMarkerBibleVerseMap Clone() => (PlaylistItemMarkerBibleVerseMap)MemberwiseClone();
}