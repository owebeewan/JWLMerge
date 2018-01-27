namespace JWLMerge.ViewModel
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using BackupFileServices;
    using GalaSoft.MvvmLight;
    using GalaSoft.MvvmLight.Command;
    using GalaSoft.MvvmLight.Messaging;
    using GalaSoft.MvvmLight.Threading;
    using Helpers;
    using Messages;
    using Models;
    using Services;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class MainViewModel : ViewModelBase
    {
        private readonly IDragDropService _dragDropService;
        private readonly IBackupFileService _backupFileService;
        private readonly IFileOpenSaveService _fileOpenSaveService;
        private readonly ObservableCollection<JwLibraryFile> _files = new ObservableCollection<JwLibraryFile>();

        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel(
            IDragDropService dragDropService, 
            IBackupFileService backupFileService,
            IFileOpenSaveService fileOpenSaveService)
        {
            _dragDropService = dragDropService;
            _backupFileService = backupFileService;
            _fileOpenSaveService = fileOpenSaveService;

            _files.CollectionChanged += FilesCollectionChanged;

            SetTitle();

            // subscriptions...
            Messenger.Default.Register<DragOverMessage>(this, OnDragOver);
            Messenger.Default.Register<DragDropMessage>(this, OnDragDrop);
            Messenger.Default.Register<MainWindowClosingMessage>(this, OnMainWindowClosing);

            AddDesignTimeItems();

            InitCommands();
        }

        private void OnMainWindowClosing(MainWindowClosingMessage message)
        {
            message.CancelEventArgs.Cancel = IsBusy;
        }

        private void FilesCollectionChanged(
            object sender, 
            System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            RaisePropertyChanged(nameof(FileListEmpty));
            MergeCommand?.RaiseCanExecuteChanged();
        }

        private void InitCommands()
        {
            CloseCardCommand = new RelayCommand<string>(RemoveCard, filePath => !IsBusy);
            MergeCommand = new RelayCommand(MergeFiles, () => GetMergableFileCount() > 1 && !IsBusy);
        }

        private void PrepareForMerge()
        {
            ReloadFiles();
            ApplyMergeParameters();
        }

        private void MergeFiles()
        {
            var destPath = _fileOpenSaveService.GetSaveFilePath();
            if (!string.IsNullOrWhiteSpace(destPath))
            {
                IsBusy = true;
                
                Task.Run(() =>
                {
                    DebugSleep();
                    PrepareForMerge();
                    try
                    {
                        var schemaFilePath = GetSuitableFilePathForSchema();

                        if (schemaFilePath != null)
                        {
                            var mergedFile = _backupFileService.Merge(_files.Select(x => x.BackupFile).ToArray());
                            _backupFileService.WriteNewDatabase(mergedFile, destPath, schemaFilePath);
                        }
                    }
                    finally
                    {
                        // we need to ensure the files are back to nnormal after 
                        // applying any merge parameters.
                        ReloadFiles();
                    }
                }).ContinueWith(previousTask =>
                {
                    DispatcherHelper.CheckBeginInvokeOnUI(() =>
                    {
                        IsBusy = false;
                    });
                });
            }
        }

        [Conditional("DEBUG")]
        private void DebugSleep()
        {
            Thread.Sleep(4000);
        }

        private void ApplyMergeParameters()
        {
            // ReSharper disable once StyleCop.SA1116
            // ReSharper disable once StyleCop.SA1115
            Parallel.ForEach(_files, file =>
            {
                if (File.Exists(file.FilePath))
                {
                    MergePreparation.ApplyMergeParameters(
                        _backupFileService,
                        file.BackupFile.Database, 
                        file.MergeParameters);
                }
            });
        }

        private void ReloadFiles()
        {
            // ReSharper disable once StyleCop.SA1116
            // ReSharper disable once StyleCop.SA1115
            Parallel.ForEach(_files, file =>
            {
                if (File.Exists(file.FilePath))
                {
                    file.BackupFile = _backupFileService.Load(file.FilePath);
                }
            });
        }

        private string GetSuitableFilePathForSchema()
        {
            foreach (var file in _files)
            {
                if (File.Exists(file.FilePath))
                {
                    return file.FilePath;
                }
            }
                
            return null;
        }

        private void RemoveCard(string filePath)
        {
            foreach (var file in _files)
            {
                if (IsSameFile(file.FilePath, filePath))
                {
                    _files.Remove(file);
                    break;
                }
            }
        }

        public ObservableCollection<JwLibraryFile> Files => _files;

        private void AddDesignTimeItems()
        {
            if (IsInDesignMode)
            {
                for (int n = 0; n < 3; ++n)
                {
                    _files.Add(DesignTimeFileCreation.CreateMockJwLibraryFile(_backupFileService, n));
                }
            }
        }
        
        private void SetTitle()
        {
            Title = IsInDesignMode
                ? "JWL Merge (design mode)"
                : "JWL Merge";
        }

        private void OnDragOver(DragOverMessage message)
        {
            message.DragEventArgs.Effects = !IsBusy && _dragDropService.CanAcceptDrop(message.DragEventArgs)
                ? DragDropEffects.Copy
                : DragDropEffects.None;

            message.DragEventArgs.Handled = true;
        }

        private void OnDragDrop(DragDropMessage message)
        {
            if (!IsBusy)
            {
                // ReSharper disable once StyleCop.SA1305
                var jwLibraryFiles = _dragDropService.GetDroppedFiles(message.DragEventArgs);

                var tmpFilesCollection = new ConcurrentBag<JwLibraryFile>();

                // ReSharper disable once StyleCop.SA1116
                // ReSharper disable once StyleCop.SA1115
                Parallel.ForEach(jwLibraryFiles, file =>
                {
                    var backupFile = _backupFileService.Load(file);

                    tmpFilesCollection.Add(new JwLibraryFile
                    {
                        BackupFile = backupFile,
                        FilePath = file
                    });
                });

                foreach (var file in tmpFilesCollection)
                {
                    if (_files.FirstOrDefault(x => IsSameFile(file.FilePath, x.FilePath)) == null)
                    {
                        file.PropertyChanged += FilePropertyChanged;
                        _files.Add(file);
                    }
                }
            }

            message.DragEventArgs.Handled = true;
        }

        private void FilePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // when the merge params are modified it can leave the number of mergeable items at less than 2.
            MergeCommand?.RaiseCanExecuteChanged();
        }

        private bool IsSameFile(string path1, string path2)
        {
            return Path.GetFullPath(path1).Equals(Path.GetFullPath(path2), StringComparison.OrdinalIgnoreCase);
        }

        private int GetMergableFileCount()
        {
            return _files.Count(file => file.MergeParameters.AnyIncludes());
        }

        public string Title { get; set; }
        
        public bool FileListEmpty => _files.Count == 0;

        private bool _isBusy;

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(IsNotBusy));

                    MergeCommand?.RaiseCanExecuteChanged();
                    CloseCardCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        public bool IsNotBusy => !IsBusy;

        public RelayCommand<string> CloseCardCommand { get; set; }
        
        public RelayCommand MergeCommand { get; set; }
    }
}