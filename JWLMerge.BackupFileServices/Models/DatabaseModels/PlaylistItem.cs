namespace JWLMerge.BackupFileServices.Models.DatabaseModels
{
    public class PlaylistItem
    {
        public int PlaylistItemId { get; set; }
        public string Label { get; set; } = null!;
        public int? StartTrimOffsetTicks { get; set; }
        public int? EndTrimOffsetTicks { get; set; }
        public int Accuracy { get; set; }
        public int EndAction { get; set; }
        public string? ThumbnailFilePath { get; set; }
    }
}
