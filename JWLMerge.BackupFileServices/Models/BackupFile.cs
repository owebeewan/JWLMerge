using JWLMerge.BackupFileServices.Models.DatabaseModels;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using JWLMerge.BackupFileServices.Models.ManifestFile;

namespace JWLMerge.BackupFileServices.Models;

/// <summary>
/// The Backup file.
/// </summary>
/// <remarks>We implement INotifyPropertyChanged to prevent the common "WPF binding leak".</remarks>
public sealed class BackupFile(Manifest manifest, Database database, string filePath) : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public Manifest Manifest { get; } = manifest;

    public Database Database { get; } = database;

    public string FilePath { get; } = filePath;

#pragma warning disable IDE0051 // Remove unused private members
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
#pragma warning restore IDE0051 // Remove unused private members
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}