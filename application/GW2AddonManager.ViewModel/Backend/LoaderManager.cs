using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Abstractions;
using System.IO.Compression;
using System.Net;
using System.Threading.Tasks;

namespace GW2AddonManager
{
    public interface ILoaderManager : IUpdateChangedEvents
    {
        Task Install();
        void Uninstall();
    }

    public class LoaderManager : UpdateChangedEvents, ILoaderManager
    {
        private readonly IConfigurationProvider _configurationProvider;
        private readonly IAddonRepository _addonRepository;
        private readonly IAddonManager _addonManager;
        private readonly IFileSystem _fileSystem;
        private readonly IHttpClientProvider _httpClientProvider;

        public string InstallPath => _configurationProvider.UserConfig.GamePath;

        public LoaderManager(IConfigurationProvider configurationProvider, IAddonRepository addonRepository, IAddonManager addonManager, IFileSystem fileSystem, IHttpClientProvider httpClientProvider, ICoreManager coreManager)
        {
            _configurationProvider = configurationProvider;
            _addonRepository = addonRepository;
            _addonManager = addonManager;
            _fileSystem = fileSystem;
            _httpClientProvider = httpClientProvider;
            coreManager.Uninstalling += (_, _) => Uninstall();
        }

        public async Task Install()
        {
            OnMessageChanged("Installing Addon Loader");
            Uninstall();

            using MemoryStream ms = new MemoryStream();
            await _httpClientProvider.Client.DownloadAsync(_addonRepository.Loader.DownloadUrl, ms, this);
            var relFiles = Utils.ExtractArchiveWithFilesList(ms, _configurationProvider.UserConfig.GamePath, _fileSystem);

            await _addonManager.Install(new List<AddonInfo> { _addonRepository.Loader.Wrapper });
        }

        public void Uninstall()
        {
            string gamePath = _configurationProvider.UserConfig.GamePath;
            // We don't know for sure what might be installed by the loader, but these files are consistently necessary, so remove those at least
            _fileSystem.File.DeleteIfExists(_fileSystem.Path.Combine(gamePath, "addonLoader.dll"));
            _fileSystem.File.DeleteIfExists(_fileSystem.Path.Combine(gamePath, "dxgi.dll"));
            _fileSystem.File.DeleteIfExists(_fileSystem.Path.Combine(gamePath, "d3d11.dll"));
            _fileSystem.File.DeleteIfExists(_fileSystem.Path.Combine(gamePath, "bin64", "d3d9.dll"));
        }
    }
}
