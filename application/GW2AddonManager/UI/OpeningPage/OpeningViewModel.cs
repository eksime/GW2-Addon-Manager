namespace GW2AddonManager
{
    using System.Collections.ObjectModel;
    using System.Windows.Input;
    using System.Linq;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Collections.Specialized;
    using CommunityToolkit.Mvvm.Input;
    using System.Windows.Data;
    using System.Collections;
    using System;
    using System.Diagnostics;

    public partial class OpeningViewModel : DependentObservableObject
    {
        #region Fields
        private readonly IAddonManager _addonManager;
        private readonly IAddonRepository _addonRepository;
        private readonly ILoaderManager _loaderManager;
        private readonly ICoreManager _coreManager;
        private readonly IAddonWatcher _addonWatcher;
        private readonly ObservableCollection<AddonViewModel> _addons;
        private readonly ReadOnlyObservableCollection<AddonViewModel> _addonsReadOnly;
        private readonly ObservableCollection<string> _logLines;
        private readonly ReadOnlyObservableCollection<string> _logLinesReadOnly;
        private readonly object _logSyncRoot;

        private TaskNotifier _addonRefreshTask;
        private IRelayCommand _launchGame;
        private IAsyncRelayCommand _disableSelected;
        private IAsyncRelayCommand _enableSelected;
        private IAsyncRelayCommand _deleteSelected;
        private IAsyncRelayCommand _installSelected;
        private IRelayCommand _checkAddon;
        private IAsyncRelayCommand _refreshAddons;
        private IRelayCommand _checkAllAddons;
        private IRelayCommand _uncheckAllAddons;
        private IRelayCommand _cleanInstall;
        private IAsyncRelayCommand _installManager;
        private IRelayCommand _uninstallManager;
        #endregion Fields

        #region Constructors

        public OpeningViewModel(IAddonManager addonManager, IAddonRepository addonRepository, ILoaderManager loaderManager, ICoreManager coreManager, IAddonWatcher addonWatcher)
        {
            _addonManager = addonManager;
            _addonRepository = addonRepository;
            _loaderManager = loaderManager;
            _coreManager = coreManager;
            _addonWatcher = addonWatcher;

            _addons = new ObservableCollection<AddonViewModel>();
            _addonsReadOnly = new ReadOnlyObservableCollection<AddonViewModel>(_addons);

            _logLines = new ObservableCollection<string>();
            _logLinesReadOnly = new ReadOnlyObservableCollection<string>(_logLines);
            _logSyncRoot = new object();
            BindingOperations.EnableCollectionSynchronization(_logLinesReadOnly, _logSyncRoot);

            _addons.CollectionChanged += OnAddonsCollectionChanged;
            _coreManager.Log += _coreManager_Log;

            RefreshAddons.Execute(null);
        }

        #endregion Constrcutors

        #region Properties
        public ReadOnlyObservableCollection<string> LogLines
        {
            get => _logLinesReadOnly;
        }

        public ReadOnlyObservableCollection<AddonViewModel> Addons
        {
            get => _addonsReadOnly;
        }

        public ICommand OpenExternalUri
        {
            get => new RelayCommand<string>(uriString =>
            {
                if (!string.IsNullOrWhiteSpace(uriString))
                {
                    try
                    {
                        Uri uri = new Uri(uriString, UriKind.Absolute);
                        if (uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) || uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase))
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = uri.AbsoluteUri,
                                UseShellExecute = true
                            });
                        }
                    }
                    catch (Exception)
                    {

                    }
                }
            });
        }

        // todo: fixme
        //public IEnumerable<AddonInfo> CheckedAddons
        //{
        //    get => Addons.Where(x => x.IsChecked).Select(x => x.Model);
        //}

        public IRelayCommand LaunchGame
        {
            get => _launchGame ??= new RelayCommand(
                execute: () => { });
        }

        // todo: fixme
        //public IAsyncRelayCommand DisableSelected
        //{
        //    get => _disableSelected ??= new AsyncRelayCommand(
        //        execute: () => _addonManager.Disable(CheckedAddons),
        //        canExecute: () => CheckedAddons.Any(x => _addonManager.IsEnabled(x).GetValueOrDefault(false)));
        //}

        // todo: fixme
        //public IAsyncRelayCommand EnableSelected
        //{
        //    get => _enableSelected ??= new AsyncRelayCommand(
        //        execute: () => _addonManager.Enable(CheckedAddons),
        //        canExecute: () => CheckedAddons.Any(x => !_addonManager.IsEnabled(x).GetValueOrDefault(true)));
        //}

        // todo: fixme
        //public IAsyncRelayCommand DeleteSelected
        //{
        //    get => _deleteSelected ??= new AsyncRelayCommand(
        //        execute: () => _addonManager.Delete(CheckedAddons),
        //        canExecute: () => CheckedAddons.Any(x => _addonManager.IsEnabled(x).HasValue));
        //}

        // todo: fixme
        //public IAsyncRelayCommand InstallSelected
        //{
        //    get => _installSelected ??= new AsyncRelayCommand(
        //        execute: () => _addonManager.Install(CheckedAddons),
        //        canExecute: () => CheckedAddons.Any(x => !_addonManager.IsEnabled(x).HasValue));
        //}

        public ICommand CleanInstall
        {
            get => _cleanInstall ??= new RelayCommand(() => _coreManager.Uninstall());
        }

        public ICommand InstallManager
        {
            get => _installManager ??= new AsyncRelayCommand(() => _loaderManager.Install());
        }

        public ICommand UninstallManager
        {
            get => _uninstallManager ??= new RelayCommand(() => _loaderManager.Uninstall());
        }

        public IRelayCommand CheckAllAddons
        {
            get => _checkAllAddons ??= new RelayCommand(
                execute: () =>
                {
                    foreach (AddonViewModel addon in Addons)
                    {
                        addon.IsChecked = true;
                    }

                    NotifyAddonsChanged();
                },
                canExecute: () => Addons.Any(addon => !addon.IsChecked));
        }

        public IRelayCommand UncheckAllAddons
        {
            get => _uncheckAllAddons ??= new RelayCommand(
                execute: () =>
                {
                    foreach (AddonViewModel addon in Addons)
                    {
                        addon.IsChecked = false;
                    }

                    NotifyAddonsChanged();
                },
                canExecute: () => Addons.Any(addon => addon.IsChecked));
        }

        public IRelayCommand CheckAddon
        {
            get => _checkAddon ??= new RelayCommand(() => NotifyAddonsChanged());
        }

        public IAsyncRelayCommand RefreshAddons
        {
            get => _refreshAddons ??= new AsyncRelayCommand(() => this.AddonRefreshTask = _addonRepository.Refresh());
        }

        public Task AddonRefreshTask
        {
            get => _addonRefreshTask;
            set => SetPropertyAndNotifyOnCompletion(ref _addonRefreshTask, value, t =>
            {
                _addons.Clear();

                foreach (AddonInfo addonInfo in _addonRepository.Addons.Values.OrderBy(x => x.AddonName))
                {
                    _addons.Add(new AddonViewModel(addonInfo, _addonManager));
                }
            });
        }
        #endregion Properties

        #region Methods
        private void OnAddonsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            NotifyAddonsChanged();
        }

        private void NotifyAddonsChanged()
        {
            UncheckAllAddons.NotifyCanExecuteChanged();
            CheckAllAddons.NotifyCanExecuteChanged();
            //EnableSelected.NotifyCanExecuteChanged();
            //DisableSelected.NotifyCanExecuteChanged();
            //DeleteSelected.NotifyCanExecuteChanged();
            //InstallSelected.NotifyCanExecuteChanged();
        }

        private void _coreManager_Log(object sender, LogEventArgs eventArgs)
        {
            lock (_logSyncRoot)
            {
                _logLines.Add(eventArgs.Message);
            }
        }
        #endregion Methods
    }
}
