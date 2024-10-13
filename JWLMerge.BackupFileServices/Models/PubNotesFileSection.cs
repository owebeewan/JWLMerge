namespace JWLMerge.BackupFileServices.Models;

public class PubNotesFileSection(PubSymbolAndLanguage symbolAndLanguage)
{
    public PubSymbolAndLanguage SymbolAndLanguage { get; } = symbolAndLanguage;

    public int ContentStartLine { get; set; }

    public int ContentEndLine { get; set; }
}