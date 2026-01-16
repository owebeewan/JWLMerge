using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using JWLMerge.Models;
using MaterialDesignThemes.Wpf;
using System.Runtime.Versioning;

namespace JWLMerge.ViewModel;

[SupportedOSPlatform("windows7.0")]
internal sealed class BackupFileFormatErrorViewModel : ObservableObject
{
    public BackupFileFormatErrorViewModel()
    {
        OkCommand = new RelayCommand(Ok);
    }

    public List<FileFormatErrorListItem> Errors { get; } = [];

    public RelayCommand OkCommand { get; }

    private void Ok()
    {
        DialogHost.CloseDialogCommand.Execute(null, null);
    }
}