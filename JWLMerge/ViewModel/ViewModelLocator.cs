using System.Runtime.Versioning;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace JWLMerge.ViewModel;

/// <summary>
/// This class contains static references to all the view models in the
/// application and provides an entry point for the bindings.
/// </summary>
[SupportedOSPlatform("windows7.0")]
internal sealed class ViewModelLocator
{
    public static MainViewModel Main => Ioc.Default.GetService<MainViewModel>()!;

#pragma warning disable CA1822 // Mark members as static
    public RedactNotesPromptViewModel RedactNotesPromptDialog => Ioc.Default.GetService<RedactNotesPromptViewModel>()!;

    public RemoveNotesByTagViewModel RemoveNotesByTagDialog => Ioc.Default.GetService<RemoveNotesByTagViewModel>()!;

    public RemoveUnderliningByColourViewModel RemoveUnderliningByColourDialog => Ioc.Default.GetService<RemoveUnderliningByColourViewModel>()!;

    public RemoveUnderliningByPubAndColourViewModel RemoveUnderliningByPubAndColourDialog => Ioc.Default.GetService<RemoveUnderliningByPubAndColourViewModel>()!;

    public RemoveFavouritesPromptViewModel RemoveFavouritesPromptDialog => Ioc.Default.GetService<RemoveFavouritesPromptViewModel>()!;

    public ImportBibleNotesViewModel ImportBibleNotesDialog => Ioc.Default.GetService<ImportBibleNotesViewModel>()!;

    public BackupFileFormatErrorViewModel BackupFileFormatErrorDialog => Ioc.Default.GetService<BackupFileFormatErrorViewModel>()!;

    public DetailViewModel Detail => Ioc.Default.GetService<DetailViewModel>()!;
#pragma warning restore CA1822 // Mark members as static
}