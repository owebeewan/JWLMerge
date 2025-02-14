namespace JWLMerge.BackupFileServices.Models.DatabaseModels
{
    public class PlaylistItemIndependentMediaMap
    {
        public int PlaylistItemId { get; set; }
        public int LocationId { get; set; }
        public int MajorMultimediaType { get; set; }
        public int? BaseDurationTicks { get; set; }
    }
}
