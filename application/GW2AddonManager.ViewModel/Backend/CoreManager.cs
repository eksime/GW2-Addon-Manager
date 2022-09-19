using GW2AddonManager.Core.Localization;
using GW2AddonManager.Core.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace GW2AddonManager
{
    public record LogEventArgs(string Message);
    public delegate void LogEventHandler(object sender, LogEventArgs eventArgs);

    public interface ICoreManager
    {
        void Uninstall();
        void UpdateCulture(string constant);

        event LogEventHandler Log;
        event EventHandler Uninstalling;
        void AddLog(string msg);
    }

    public class CoreManager : ICoreManager
    {
        private readonly IConfigurationProvider _configurationProvider;
        private readonly IFileSystem _fileSystem;
        private readonly IDialogService _dialogService;

        public event LogEventHandler Log;
        public event EventHandler Uninstalling;

        public CoreManager(IConfigurationProvider configurationProvider, IFileSystem fileSystem, IDialogService dialogService)
        {
            _configurationProvider = configurationProvider;
            _fileSystem = fileSystem;
            _dialogService = dialogService;
        }

        public void Uninstall()
        {
            if (_configurationProvider.UserConfig.GamePath is null || !_fileSystem.Directory.Exists(_configurationProvider.UserConfig.GamePath))
            {
                this._dialogService.ShowMessageBox(StaticText.NoGamePath, StaticText.ResetToCleanInstall, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (this._dialogService.ShowMessageBox(StaticText.ResetToCleanInstallWarning, StaticText.ResetToCleanInstall, MessageBoxButton.Yes | MessageBoxButton.No, MessageBoxImage.Hand) != MessageBoxResult.Yes)
                return;

            Uninstalling?.Invoke(this, new EventArgs());

            if (_fileSystem.File.Exists(_configurationProvider.ConfigFileName))
                _fileSystem.File.Delete(_configurationProvider.ConfigFileName);

            this._dialogService.ShowMessageBox(StaticText.ResetToCleanInstallDone, StaticText.ResetToCleanInstall, MessageBoxButton.OK);

            //todo: request shutdown
            //Application.Current.Shutdown();
        }

        public void UpdateCulture(string constant)
        {
            bool needsChange = constant != _configurationProvider.UserConfig.Culture;
            if (needsChange)
            {
                _configurationProvider.UserConfig = _configurationProvider.UserConfig with
                {
                    Culture = constant
                };
            }

            CultureInfo culture = new CultureInfo(_configurationProvider.UserConfig.Culture);
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;


            //todo: recreate main window
            //if (needsChange)
            //    App.Current.ReopenMainWindow();
        }

        public void AddLog(string msg)
        {
            Log?.Invoke(this, new LogEventArgs(msg));
        }
    }
}
