namespace JWLMerge.BackupFileServices.Models.DatabaseModels
{
    public class PlaylistItemMarkerParagraphMap
    {
        public int PlaylistItemMarkerId { get; set; }
        public int MepsDocumentId { get; set; }
        public int ParagraphIndex { get; set; }
        public int MarkerIndexWithinParagraph { get; set; }
    }
}
