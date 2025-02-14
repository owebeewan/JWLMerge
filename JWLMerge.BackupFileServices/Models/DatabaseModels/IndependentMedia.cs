﻿namespace JWLMerge.BackupFileServices.Models.DatabaseModels
{
    public class IndependentMedia
    {
        public int IndependentMediaId { get; set; }
        public string OriginalFilename { get; set; } = null!;
        public string FilePath { get; set; } = null!;
        public string MimeType { get; set; } = null!;
        public string Hash { get; set; } = null!;
    }
}
