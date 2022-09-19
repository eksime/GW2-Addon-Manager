using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System.IO.Abstractions;

namespace GW2AddonManager
{
    public class AddonWatcher : ObservableRecipient, IAddonWatcher
    {
        private readonly IConfigurationProvider _configurationProvider;
        private readonly IAddonManager _addonManager;
        private readonly IAddonRepository _addonRepository;
        private readonly IFileSystem _fileSystem;
        private readonly SynchronizationContext? _context;

        private IFileSystemWatcher _watcher;

        public event EventHandler<AddonInfo> AddonChanged;

        public AddonWatcher(IConfigurationProvider configurationProvider, IAddonManager addonManager, IAddonRepository repository, IFileSystem fileSystem)
        {
            this._configurationProvider = configurationProvider;
            this._addonManager = addonManager;
            this._addonRepository = repository;
            this._fileSystem = fileSystem;
            this._context = SynchronizationContext.Current;
            this.SetWatcherPath(_configurationProvider.UserConfig.GamePath);
        }

        private void SetWatcherPath(string path)
        {
            if (!string.IsNullOrWhiteSpace(path) && _fileSystem.Directory.Exists(path))
            {
                if (_watcher is IFileSystemWatcher oldWatcher)
                {
                    _watcher.Path = path;
                }
                else
                {
                    _watcher = _fileSystem.FileSystemWatcher.CreateNew(path);
                    _watcher.IncludeSubdirectories = true;
                    _watcher.EnableRaisingEvents = true;
                    _watcher.Created += OnWatcherFileChanged;
                    _watcher.Deleted += OnWatcherFileChanged;
                    _watcher.Renamed += OnWatcherFileChanged;
                    _watcher.Changed += OnWatcherFileChanged;
                }
            }
            else if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
            }
        }

        private void OnWatcherFileChanged(object sender, FileSystemEventArgs e)
        {
            HashSet<string> pathsToConsider = new HashSet<string>() { e.FullPath };
            if (e is RenamedEventArgs renamedEventArgs)
            {
                pathsToConsider.Add(renamedEventArgs.OldFullPath);
            }

            foreach (AddonInfo info in this._addonRepository.Addons.Values)
            {
                string enabledPath = this._addonManager.GetAddonPath(info, true);
                string disabledPath = this._addonManager.GetAddonPath(info, false);
                string directory = this._addonManager.GetAddonDirectory(info);

                bool changed = false;

                foreach (string path in pathsToConsider)
                {
                    if (path.Equals(enabledPath, StringComparison.OrdinalIgnoreCase))
                    {
                        changed = true;
                        break;
                    }
                    else if (path.Equals(disabledPath, StringComparison.OrdinalIgnoreCase))
                    {
                        changed = true;
                        break;
                    }
                    else if (path.Equals(directory, StringComparison.OrdinalIgnoreCase))
                    {
                        changed = true;
                        break;
                    }
                    else
                    {
                        try
                        {
                            string fileName = this._fileSystem.Path.GetFileName(path);
                            IEnumerable<string> files = this._fileSystem.Directory.EnumerateFiles(directory, fileName, SearchOption.AllDirectories);
                            if (files.Any())
                            {
                                changed = true;
                                break;
                            }
                        }
                        catch (DirectoryNotFoundException _)
                        {
                            // ignore
                        }
                        catch (UnauthorizedAccessException _)
                        {
                            // ignore
                        }
                    }
                }

                if (changed)
                {
                    bool? enabled = this._addonManager.IsEnabled(info);
                    bool installed = enabled.HasValue;

                    AddonChanged changedMessage = new AddonChanged(installed, enabled);
                    
                    this.Messenger.Send(changedMessage, info);
                }
            }
        }
    }
}
