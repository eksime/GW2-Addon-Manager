namespace GW2AddonManager
{
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Threading;

    public partial class AddonViewModel : ObservableRecipient
    {
        private readonly IAddonManager _addonManager;
        private readonly IAddonWatcher _addonWatcher;

        private IAsyncRelayCommand _installCommand;
        private IAsyncRelayCommand _uninstallCommand;

        public AddonViewModel(AddonInfo addonInfo, IAddonManager addonManager)
        {
            _addonManager = addonManager;

            _model = addonInfo;
            IsChecked = _addonManager.IsEnabled(addonInfo).HasValue;

            this.Messenger.Register<AddonViewModel, AddonChanged, AddonInfo>(this, this._model, this.OnAddonChanged);
        }

        void OnAddonChanged(AddonViewModel receiver, AddonChanged message )
        {

        }

        private AddonInfo _model;

        [ObservableProperty]
        private bool _isChecked;

        public string Name
        {
            get => _model.AddonName;
        }

        public string Description
        {
            get => _model.Description;
        }

        public string Developer
        {
            get => _model.Developer;
        }

        public string Website
        {
            get => _model.Website;
        }

        public IEnumerable<string> Dependencies
        {
            get => _model.Requires;
        }

        [RelayCommand(CanExecute = nameof(InstallCanExecute))]
        public async Task Install()
        {
            await _addonManager.Install(Enumerable.Repeat(_model, 1));
        }

        public bool InstallCanExecute()
        {
            return !_addonManager.IsEnabled(_model).HasValue;
        }

        [RelayCommand(CanExecute = nameof(UninstallCanExecute))]
        public async Task Uninstall()
        {
            await _addonManager.Delete(Enumerable.Repeat(_model, 1));
        }

        public bool UninstallCanExecute()
        {
            return _addonManager.IsEnabled(_model).HasValue;
        }

        private void NotifyAddonStateChanged()
        {
            if (App.Current.Dispatcher.CheckAccess())
            {
                InstallCommand.NotifyCanExecuteChanged();
                UninstallCommand.NotifyCanExecuteChanged();
            }
            else
            {
                App.Current.Dispatcher.Invoke(NotifyAddonStateChanged);
            }
        }
    }
}
