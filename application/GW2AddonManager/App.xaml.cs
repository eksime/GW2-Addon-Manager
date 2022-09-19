﻿using GW2AddonManager.UI.Dialogs;
using GW2AddonManager.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using System.Diagnostics;

namespace GW2AddonManager
{
    public partial class App : Application
    {
        private ServiceProvider _serviceProvider;
        private NamedMutex _mutex;

        public new static App Current => (App)Application.Current;

        public IServiceProvider Services => _serviceProvider;

        public App()
        {
            _mutex = new NamedMutex("GW2AddonManager", true);
            _serviceProvider = ConfigureServices();
        }

        private void ClearMutex()
        {
            _mutex?.Dispose();
            _mutex = null;
        }

        private ServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection()
                          .AddSingleton<IFileSystem, FileSystem>()
                          .AddSingleton<IHttpClientProvider, HttpClientProvider>()
                          .AddSingleton<IConfigurationProvider, ConfigurationProvider>()
                          .AddSingleton<IAddonRepository, AddonRepository>()
                          .AddSingleton<IAddonManager, AddonManager>()
                          .AddSingleton<ISelfManager, SelfManager>()
                          .AddSingleton<ILoaderManager, LoaderManager>()
                          .AddSingleton<ICoreManager, CoreManager>()
                          .AddSingleton<IDialogService, WPFDialogService>()
                          .AddSingleton<IAddonWatcher, AddonWatcher>()
                          .AddTransient<MainWindowViewModel>()
                          .AddTransient<OpeningViewModel>()
                          .AddTransient<UpdatingViewModel>()
                          .AddTransient<MainWindow>();

            return services.BuildServiceProvider();
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            Application.Current.DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(AppDispatcherUnhandledException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomainUnhandledException);

            _serviceProvider.GetService<ICoreManager>().UpdateCulture(_serviceProvider.GetService<IConfigurationProvider>().UserConfig.Culture);
            Application.Current.Exit += new ExitEventHandler((_, _) => ClearMutex());

            Application.Current.MainWindow = _serviceProvider.GetService<MainWindow>();
            Application.Current.MainWindow.Show();
        }

        private void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ClearMutex();
            ShowUnhandledException(e);
        }

        private void AppDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            ClearMutex();
            ShowUnhandledException(e);
        }

        [Conditional("DEBUG")]
        private void ShowUnhandledException(DispatcherUnhandledExceptionEventArgs e)
        {
            //LogError(logPath, e);
            string errmsg = "An unhandled exception occurred." + "\n" + e.Exception.Message + (e.Exception.InnerException != null ? "\n" + e.Exception.InnerException.Message : "");

            IDialogService dialogService = this._serviceProvider.GetRequiredService<IDialogService>();
            if (dialogService.ShowMessageBox(errmsg, "Critical Error", Core.Services.MessageBoxButton.OK, Core.Services.MessageBoxImage.Error) == Core.Services.MessageBoxResult.OK)
                Application.Current.Shutdown();
        }

        [Conditional("DEBUG")]
        private void ShowUnhandledException(UnhandledExceptionEventArgs e)
        {
            //LogError(logPath, e);
            Exception exc = (Exception)e.ExceptionObject;
            string errmsg = "An unhandled exception occurred." + "\n" + exc.Message + (exc.InnerException != null ? "\n" + exc.InnerException.Message : "");
            IDialogService dialogService = this._serviceProvider.GetRequiredService<IDialogService>();
            if (dialogService.ShowMessageBox(errmsg, "Critical Error", Core.Services.MessageBoxButton.OK, Core.Services.MessageBoxImage.Error) == Core.Services.MessageBoxResult.OK)
                Application.Current.Shutdown();
        }
    }
}
