using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.IO;
using System;
using System.Threading.Tasks;
using System.IO.Abstractions;
using GW2AddonManager.Core.Services;
using GW2AddonManager.Core.Localization;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;

namespace GW2AddonManager
{

    public class AddonManager : ObservableRecipient, IAddonManager
    {
        private const string AddonPrefix = "gw2addon_";
        private const string ArcDPSFolder = "arcdps";
        private const string DisabledExtension = ".dll_disabled";
        private const string EnabledExtension = ".dll";

        private readonly IConfigurationProvider _configurationProvider;
        private readonly IFileSystem _fileSystem;
        private readonly IHttpClientProvider _httpClientProvider;
        private readonly ICoreManager _coreManager;
        private readonly IDialogService _dialogService;
        private readonly IAddonRepository _addonRepository;

        public string AddonsFolder => _fileSystem.Path.Combine(_configurationProvider.UserConfig.GamePath, "addons");

        public AddonManager(IConfigurationProvider configurationProvider, IFileSystem fileSystem, IHttpClientProvider httpClientProvider, ICoreManager coreManager, IDialogService dialogService, IAddonRepository addonRepository)
        {
            _configurationProvider = configurationProvider;
            _fileSystem = fileSystem;
            _httpClientProvider = httpClientProvider;
            _coreManager = coreManager;
            _dialogService = dialogService;
            _addonRepository = addonRepository;

            coreManager.Uninstalling += (_, _) => Uninstall();
        }

        public string GetAddonDirectory(AddonInfo addon)
        {
            switch (addon.InstallMode)
            {
            case InstallMode.Binary:
                return _fileSystem.Path.Combine(AddonsFolder, addon.Nickname);
            case InstallMode.Arc:
                return _fileSystem.Path.Combine(AddonsFolder, ArcDPSFolder);
            case InstallMode.Loader:
                return _configurationProvider.UserConfig.GamePath;
            default:
                throw new NotImplementedException($"Unknown install mode '{addon.InstallMode}'");
            }
        }

        public bool IsInstalled(AddonInfo addon)
        {
            return IsEnabled(addon).HasValue;
        }

        public bool? IsEnabled(AddonInfo addon)
        {
            //todo: check hashes here
            switch (addon.InstallMode)
            {
            case InstallMode.Arc:
            case InstallMode.Binary:
                if (_fileSystem.File.Exists(GetAddonPath(addon, true)))
                {
                    return true;
                }
                else if (_fileSystem.File.Exists(GetAddonPath(addon, false)))
                {
                    return false;
                }
                else
                {
                    return null;
                }
            case InstallMode.Loader:
                bool? result = null;
                string gamePath = _configurationProvider.UserConfig.GamePath;

                string[] paths =
                    {
                        _fileSystem.Path.Combine(gamePath, "addonLoader.dll"),
                        _fileSystem.Path.Combine(gamePath, "dxgi.dll"),
                        _fileSystem.Path.Combine(gamePath, "d3d11.dll"),
                        _fileSystem.Path.Combine(gamePath, "bin64", "d3d9.dll"),
                    };

                bool allExist = paths.All(path => _fileSystem.File.Exists(path));

                return allExist ? true : null;
            default:
                throw new NotImplementedException();
            }
        }

        public FileVersionInfo GetInstalledVersion(AddonInfo addon)
        {
            bool? enabled = IsEnabled(addon);
            if (!enabled.HasValue)
            {
                return null;
            }

            string dllPath = GetAddonPath(addon, enabled.Value);
            return FileVersionInfo.GetVersionInfo(dllPath);
        }

        public string GetAddonPath(AddonInfo addon, bool isEnabled)
        {
            var folderPath = GetAddonDirectory(addon);
            var extension = isEnabled ? EnabledExtension : DisabledExtension;
            if (addon.InstallMode == InstallMode.Binary)
            {
                return _fileSystem.Path.Combine(folderPath, AddonPrefix + addon.Nickname + extension);
            }
            else if (addon.InstallMode == InstallMode.Arc)
            {
                if (_fileSystem.Directory.Exists(folderPath))
                {
                    if (!string.IsNullOrWhiteSpace(addon.PluginName))
                    {
                        string path = _fileSystem.Path.Combine(folderPath, $"{addon.PluginName}{extension}");
                        if (_fileSystem.File.Exists(path))
                        {
                            return path;
                        }

                        // filthy hack for plugins with incorrect name patterns - shouldn't have to do this really.
                        path = _fileSystem.Path.Combine(folderPath, _fileSystem.Path.ChangeExtension(GetFilenameFromUrl(addon.DownloadUrl), extension));
                        if (_fileSystem.File.Exists(path))
                        {
                            return path;
                        }

                        return _fileSystem.Directory.EnumerateFiles(folderPath, addon.PluginNamePattern + extension).FirstOrDefault();
                    }
                }

                return _fileSystem.Path.Combine(folderPath, $"{addon.PluginName}{extension}");
            }
            else if (addon.InstallMode == InstallMode.Loader)
            {
                return null;
            }    
            else
            {
                throw new ArgumentException();
            }
        }

        private void DisableEnable(AddonInfo addon, bool enable)
        {
            bool? enabled = IsEnabled(addon);

            if (!enabled.HasValue || enabled.Value == enable)
            {
                _coreManager.AddLog($"Skipping {addon.AddonName}, not installed or already in desired state.");
                return;
            }

            var path = GetAddonPath(addon, enabled.Value);
            if (_fileSystem.File.Exists(path))
            {
                var newPath = _fileSystem.Path.ChangeExtension(path, enable ? EnabledExtension : DisabledExtension);
                _fileSystem.File.Move(path, newPath);
            }
            else
            {
                _coreManager.AddLog($"Could not {(enable ? "enable" : "disable")} {addon.AddonName}, expected addon path '{path}' does not exist!");
                return;
            }

            _coreManager.AddLog($"{(enable ? "Enabled" : "Disabled")} {addon.AddonName}.");
        }

        private void Delete(AddonInfo addon)
        {
            bool? enabled = IsEnabled(addon);
            
            if (!enabled.HasValue)
            {
                //_coreManager.AddLog($"Skipping {addon.AddonName}, not installed.");
                return;
            }

            foreach (AddonInfo dependent in _addonRepository.Addons.Values)
            {
                if (dependent.Requires.Contains(addon.AddonName))
                {
                    Delete(dependent);
                }
            }

            _coreManager.AddLog($"Deleting {addon.AddonName}...");

            var folderPath = GetAddonDirectory(addon);

            if (_fileSystem.Directory.Exists(folderPath))
            {
                if (addon.InstallMode == InstallMode.Arc)
                {
                    string addonPath = GetAddonPath(addon, enabled.Value);
                    _fileSystem.File.Delete(addonPath);
                    _coreManager.AddLog($"Deleting file '{addonPath}'...");
                }
                else if (addon.InstallMode == InstallMode.Binary)
                {
                    _fileSystem.Directory.Delete(folderPath, true);
                    _coreManager.AddLog($"Deleting dir '{folderPath}'...");
                }
                else if (addon.InstallMode == InstallMode.Loader)
                {
                    string gamePath = _configurationProvider.UserConfig.GamePath;

                    string[] paths =
                    {
                        _fileSystem.Path.Combine(gamePath, "addonLoader.dll"),
                        _fileSystem.Path.Combine(gamePath, "dxgi.dll"),
                        _fileSystem.Path.Combine(gamePath, "d3d11.dll"),
                        _fileSystem.Path.Combine(gamePath, "bin64", "d3d9.dll"),
                    };

                    foreach (string path in paths)
                    {
                        _coreManager.AddLog($"Deleting file '{path}'...");
                        _fileSystem.File.DeleteIfExists(path);
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }
                //var files = new List<string>
                //{
                //    GetAddonPath(addon, enabled.Value)
                //};

                //if (addon.Files is IEnumerable<string> addonFiles)
                //{
                //    foreach (var f in addonFiles)
                //    {
                //        files.Add(_fileSystem.Path.Combine(folderPath, f));
                //    }
                //}
                // todo: installed files
                //foreach (var f in state.InstalledFiles)
                //{
                //    files.Add(_fileSystem.Path.Combine(folderPath, f));
                //}

                //foreach (var f in files)
                //{
                //    if (_fileSystem.File.Exists(f))
                //    {
                //        _coreManager.AddLog($"Deleting file '{f}'...");
                //        _fileSystem.File.Delete(f);
                //    }
                //}

                //foreach (var dir in _fileSystem.Directory.EnumerateDirectories(folderPath, "*", SearchOption.AllDirectories).OrderByDescending(x => x.Length).Append(folderPath))
                //{
                //    _coreManager.AddLog($"Deleting directory '{dir}'...");
                //    try
                //    {
                //        _fileSystem.Directory.Delete(dir);
                //    }
                //    catch (Exception) { }
                //}
            }
            else
            {
                _coreManager.AddLog($"Directory '{folderPath}' does not exist, addon does not appear to be installed?");
            }

            _coreManager.AddLog($"Deleted {addon.AddonName}.");
        }

        public async Task Delete(IEnumerable<AddonInfo> addons)
        {
            if (addons == null)
            {
                this._dialogService.ShowMessageBox(StaticText.NoAddonsSelected, StaticText.CannotProceedTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (this._dialogService.ShowMessageBox(StaticText.DeleteAddonsPrompt, StaticText.DeleteTitle, MessageBoxButton.Yes | MessageBoxButton.No, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                int i = 0;
                int n = addons.Count();
                //OnProgressChanged(i, n);

                try
                {
                    foreach (AddonInfo addon in addons)
                    {
                        _coreManager.AddLog($"Deleting {addon.AddonName}...");
                        Delete(addon);
                        //OnProgressChanged(++i, n);
                    }
                }
                catch (Exception ex)
                {
                    _coreManager.AddLog($"Exception while deleting addons ({string.Join(", ", addons.Select(x => x.AddonName))}): {ex.Message}");

                    this._dialogService.ShowMessageBox("Error while deleting some addons: " + ex.Message, "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /* 
         * Credit: Fidel @ StackOverflow
         * Modified version of their answer at https://stackoverflow.com/a/54616044/9170673
         */
        private string GetFilenameFromUrl(string url)
        {
            Uri uri = new Uri(url, UriKind.Absolute);

            string result = uri.Segments.Last();
            if (result.EndsWith(".zip") || result.EndsWith(".dll"))
            {
                return result;
            }

            var req = System.Net.WebRequest.Create(url);
            req.Method = "GET";
            using (System.Net.WebResponse resp = req.GetResponse())
            {
                result = resp.ResponseUri.Segments.Last();
            }

            return result;
        }

        private async Task Install(AddonInfo addon)
        {
            foreach (string dependency in addon.Requires)
            {
                if (_addonRepository.Addons.TryGetValue(dependency, out AddonInfo dep))
                {
                    await Install(dep);
                }
                else
                {
                    throw new InvalidOperationException($"Unable to find dependency '{dependency}'");
                }
            }

            bool? enabled = IsEnabled(addon);

            // todo:versioning + selfupdate
            if (enabled.HasValue)
            {
                _coreManager.AddLog($"Skipping {addon.AddonName}, already installed and at the right version or self-updating.");
                return;
            }

            _coreManager.AddLog($"Installing {addon.AddonName}...");

            var url = addon.DownloadUrl;
            var destFolder = GetAddonDirectory(addon);

            List<string> relFiles;

            if (addon.DownloadType == DownloadType.Archive)
            {
                using MemoryStream ms = new MemoryStream();
                await _httpClientProvider.Client.DownloadAsync(url, ms);
                _coreManager.AddLog($"Downloaded {addon.AddonName}.");
                relFiles = Utils.ExtractArchiveWithFilesList(ms, destFolder, _fileSystem);
            }
            else
            {
                string fileName = GetFilenameFromUrl(url);
                string path = _fileSystem.Path.Join(destFolder, fileName);

                _fileSystem.Directory.CreateDirectory(destFolder);

                using Stream fs = _fileSystem.File.Create(path);
                await _httpClientProvider.Client.DownloadAsync(url, fs);
                _coreManager.AddLog($"Downloaded {addon.AddonName}.");

                relFiles = new List<string> { path };
            }

            _coreManager.AddLog($"Installed {addon.AddonName}.");
        }

        public async Task Install(IEnumerable<AddonInfo> addons)
        {
            if (addons == null)
            {
                this._dialogService.ShowMessageBox(StaticText.NoAddonsSelected, StaticText.CannotProceedTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int i = 0;
            int n = addons.Count();
            //todo:progress reporting
            //OnProgressChanged(i, n);

            try
            {
                foreach (AddonInfo addon in addons)
                {
                    _coreManager.AddLog($"Installing {addon.AddonName}...");
                    await Install(addon);
                    //OnProgressChanged(++i, n);
                }
            }
            catch (Exception ex)
            {
                _coreManager.AddLog($"Exception while installing addons ({string.Join(", ", addons.Select(x => x.AddonName))}): {ex.Message}");
                this._dialogService.ShowMessageBox("Error while installing some addons: " + ex.Message, "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DisableEnable(bool enable, IEnumerable<AddonInfo> addons)
        {
            int i = 0;
            int n = addons.Count();
            //OnProgressChanged(i, n);

            try
            {
                foreach (AddonInfo addon in addons)
                {
                    _coreManager.AddLog($"{(enable ? "Enabling" : "Disabling")} {addon.AddonName}...");
                    DisableEnable(addon, enable);
                    //OnProgressChanged(++i, n);
                }
            }
            catch (Exception ex)
            {
                _coreManager.AddLog($"Exception while {(enable ? "enabling" : "disabling")} addons ({string.Join(", ", addons.Select(x => x.AddonName))}): {ex.Message}");
                this._dialogService.ShowMessageBox($"Error while {(enable ? "enabling" : "disabling")} some addons: " + ex.Message, "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public Task Disable(IEnumerable<AddonInfo> addons)
        {
            if (addons == null)
            {
                this._dialogService.ShowMessageBox(StaticText.NoAddonsSelected, StaticText.CannotProceedTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
                return Task.CompletedTask;
            }

            if (this._dialogService.ShowMessageBox(StaticText.DisableAddonsPrompt, StaticText.DisableTitle, MessageBoxButton.Yes | MessageBoxButton.No, MessageBoxImage.Information) == MessageBoxResult.Yes)
            {
                DisableEnable(false, addons);
            }

            return Task.CompletedTask;
        }

        public Task Enable(IEnumerable<AddonInfo> addons)
        {
            if (addons == null)
            {
                this._dialogService.ShowMessageBox(StaticText.NoAddonsSelected, StaticText.CannotProceedTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
                return Task.CompletedTask;
            }

            if (this._dialogService.ShowMessageBox(StaticText.EnableAddonsPrompt, StaticText.EnableTitle, MessageBoxButton.Yes | MessageBoxButton.No, MessageBoxImage.Information) == MessageBoxResult.Yes)
            {
                DisableEnable(true, addons);
            }

            return Task.CompletedTask;
        }

        public void Uninstall()
        {
            if (_fileSystem.Directory.Exists(AddonsFolder))
                _fileSystem.Directory.Delete(AddonsFolder, true);
        }
    }
}
