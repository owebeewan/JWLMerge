namespace JWLMerge.BackupFileServices.Models.DatabaseModels;

public class PlaylistItemMarkerParagraphMap
{
    /// <summary>
    /// The playlist item marker identifier.
    /// </summary>
    public int PlaylistItemMarkerId { get; set; }

    /// <summary>
    /// The meps document identifier.
    /// </summary>
    public int MepsDocumentId { get; set; }

    /// <summary>
    /// The paragraph index.
    /// </summary>
    public int ParagraphIndex { get; set; }

    /// <summary>
    /// The marker index within the paragraph.
    /// </summary>
    public int MarkerIndexWithinParagraph { get; set; }

    public PlaylistItemMarkerParagraphMap Clone() => (PlaylistItemMarkerParagraphMap)MemberwiseClone();
}