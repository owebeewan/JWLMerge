﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace JWLMerge.ViewModel;

using System.Collections.Generic;
using Models;
using MaterialDesignThemes.Wpf;

internal sealed class BackupFileFormatErrorViewModel : ObservableObject
{
    public BackupFileFormatErrorViewModel()
    {
        OkCommand = new RelayCommand(Ok);
    }

    public List<FileFormatErrorListItem> Errors { get; } = new();

    public RelayCommand OkCommand { get; }

    private void Ok()
    {
        DialogHost.CloseDialogCommand.Execute(null, null);
    }
}