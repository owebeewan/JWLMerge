namespace JWLMerge.BackupFileServices.Models.DatabaseModels
{
    public class PlaylistItemLocationMap
    {
        public int PlaylistItemId { get; set; }
        public int LocationId { get; set; }
        public int MajorMultimediaType { get; set; }
        public int? BaseDurationTicks { get; set; }
    }
}
