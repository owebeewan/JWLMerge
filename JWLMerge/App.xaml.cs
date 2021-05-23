﻿using System.Windows.Threading;
using JWLMerge.Helpers;

namespace JWLMerge
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Windows;
    using System.Windows.Interop;
    using System.Windows.Media;
    using Serilog;
    using JWLMerge.BackupFileServices;
    using JWLMerge.ExcelServices;
    using JWLMerge.Services;
    using JWLMerge.ViewModel;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Toolkit.Mvvm.DependencyInjection;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly string _appString = "JWLMergeAC";
        private Mutex _appMutex;

        protected override void OnExit(ExitEventArgs e)
        {
            _appMutex?.Dispose();
            Log.Logger.Information("==== Exit ====");
        }


        protected override void OnStartup(StartupEventArgs e)
        {
            ConfigureServices();

            if (AnotherInstanceRunning())
            {
                Shutdown();
            }

            if (ForceSoftwareRendering())
            {
                // disable hardware (GPU) rendering so that it's all done by the CPU...
                RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
                ConfigureLogger();
            }

            Current.DispatcherUnhandledException += CurrentDispatcherUnhandledException;
        }

        private void CurrentDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // unhandled exceptions thrown from UI thread
            e.Handled = true;
            Log.Logger.Fatal(e.Exception, "Unhandled exception");
            Current.Shutdown();
        }

        private void ConfigureServices()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton<IDragDropService, DragDropService>();
            serviceCollection.AddSingleton<IBackupFileService, BackupFileService>();
            serviceCollection.AddSingleton<IFileOpenSaveService, FileOpenSaveService>();
            serviceCollection.AddSingleton<IWindowService, WindowService>();
            serviceCollection.AddSingleton<IDialogService, DialogService>();
            serviceCollection.AddSingleton<ISnackbarService, SnackbarService>();
            serviceCollection.AddSingleton<IExcelService, ExcelService>();

            serviceCollection.AddSingleton<ISnackbarService, SnackbarService>();

            serviceCollection.AddSingleton<MainViewModel>();
            serviceCollection.AddSingleton<BackupFileFormatErrorViewModel>();
            serviceCollection.AddSingleton<RedactNotesPromptViewModel>();
            serviceCollection.AddSingleton<RemoveFavouritesPromptViewModel>();
            serviceCollection.AddSingleton<RemoveNotesByTagViewModel>();
            serviceCollection.AddSingleton<RemoveUnderliningByColourViewModel>();
            serviceCollection.AddSingleton<RemoveUnderliningByPubAndColourViewModel>();
            serviceCollection.AddSingleton<ImportBibleNotesViewModel >();
            serviceCollection.AddTransient<DetailViewModel>();
            
            var serviceProvider = serviceCollection.BuildServiceProvider();
            Ioc.Default.ConfigureServices(serviceProvider);
        }

        private bool ForceSoftwareRendering()
        {
            // https://blogs.msdn.microsoft.com/jgoldb/2010/06/22/software-rendering-usage-in-wpf/
            // renderingTier values:
            // 0 => No graphics hardware acceleration available for the application on the device
            //      and DirectX version level is less than version 7.0
            // 1 => Partial graphics hardware acceleration available on the video card. This 
            //      corresponds to a DirectX version that is greater than or equal to 7.0 and 
            //      less than 9.0.
            // 2 => A rendering tier value of 2 means that most of the graphics features of WPF 
            //      should use hardware acceleration provided the necessary system resources have 
            //      not been exhausted. This corresponds to a DirectX version that is greater 
            //      than or equal to 9.0.
            int renderingTier = RenderCapability.Tier >> 16;
            return renderingTier == 0;
        }

        private static void ConfigureLogger()
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "JWLMerge\\Logs");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(Path.Combine(folder, "log-{Date}.txt"), retainedFileCountLimit: 28)
                .CreateLogger();

            Log.Logger.Information("==== Launched ====");
            Log.Logger.Information($"Version {VersionDetection.GetCurrentVersion()}");
        }

        private bool AnotherInstanceRunning()
        {
            _appMutex = new Mutex(true, _appString, out var newInstance);
            return !newInstance;
        }
    }
}
