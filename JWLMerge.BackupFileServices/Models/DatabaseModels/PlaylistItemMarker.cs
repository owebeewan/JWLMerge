using JWLMerge.BackupFileServices.Models.DatabaseModels;

namespace JWLMerge.BackupFileServices.Models.DatabaseModels
{
    public class PlaylistItemMarker
    {
        public int PlaylistItemMarkerId { get; set; }
        public int PlaylistItemId { get; set; }
        public string Label { get; set; } = null!;
        public int StartTimeTicks { get; set; }
        public int DurationTicks { get; set; }
        public int EndTransitionDurationTicks { get; set; }
    }
}
