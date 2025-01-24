using System.Runtime.Versioning;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;

namespace JWLMerge.ViewModel;

[SupportedOSPlatform("windows7.0")]
internal sealed class RedactNotesPromptViewModel : ObservableObject
{
    public RedactNotesPromptViewModel()
    {
        YesCommand = new RelayCommand(Yes);
        NoCommand = new RelayCommand(No);
    }

    public RelayCommand YesCommand { get; }

    public RelayCommand NoCommand { get; }

    public bool Result { get; private set; }

    private void No()
    {
        Result = false;
        DialogHost.CloseDialogCommand.Execute(null, null);
    }

    private void Yes()
    {
        Result = true;
        DialogHost.CloseDialogCommand.Execute(null, null);
    }
}