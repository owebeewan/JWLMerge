namespace JWLMerge.BackupFileServices.Models.DatabaseModels;

public class IndependentMedia
{
    /// <summary>
    /// The playlist item indepentent media identifier.
    /// </summary>
    public int IndependentMediaId { get; set; }

    /// <summary>
    /// The original file name.
    /// </summary>
    public required string OriginalFileName { get; set; }

    /// <summary>
    /// The file path.
    /// </summary>
    public required string FilePath { get; set; }

    /// <summary>
    /// The file's mime type
    /// </summary>
    public required string MimeType { get; set; }

    /// <summary>
    /// The file's hash.
    /// </summary>
    public required string Hash { get; set; }

    public IndependentMedia Clone() => (IndependentMedia)MemberwiseClone();
}