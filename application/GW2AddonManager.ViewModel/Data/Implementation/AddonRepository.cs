using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using GW2AddonManager.Data.Serialization;

namespace GW2AddonManager
{
    public record LoaderInfo(string VersionId, string DownloadUrl, string WrapperNickname)
    {
        [JsonIgnore]
        public AddonInfo Wrapper { get; set; }
    }

    public record ManagerInfo(string VersionId, string DownloadUrl);

    internal record AddonRepositoryInfo(Dictionary<string, AddonInfo> Addons, LoaderInfo Loader, ManagerInfo Manager);

    public class AddonRepository : IAddonRepository
    {
        private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            Converters =
                {
                    new JsonStringEnumConverter(new SnakeCaseNamingPolicy()),
                },
            PropertyNamingPolicy = new SnakeCaseNamingPolicy(),
        };

        // Master URL
        private readonly IHttpClientProvider _httpClientProvider;
        private readonly IConfigurationProvider _configurationProvider;

        private readonly Dictionary<string, AddonInfo> _addons;
        private LoaderInfo _loader;
        private ManagerInfo _manager;

        public AddonRepository(IHttpClientProvider httpClientProvider, IConfigurationProvider configurationProvider)
        {
            _httpClientProvider = httpClientProvider;
            _configurationProvider = configurationProvider;

            _addons = new Dictionary<string, AddonInfo>(StringComparer.OrdinalIgnoreCase);
        }

        public IReadOnlyDictionary<string, AddonInfo> Addons
        {
            get => _addons;
        }

        public LoaderInfo Loader
        {
            get => _loader;
        }

        public ManagerInfo Manager
        {
            get => _manager;
        }

        public async Task Refresh()
        {
            foreach (Uri repoUri in _configurationProvider.UserConfig.AddonRepos)
            {
                using var raw = await _httpClientProvider.Client.GetStreamAsync(repoUri);
                AddonRepositoryInfo? repositoryInfo = await JsonSerializer.DeserializeAsync<AddonRepositoryInfo>(raw, SerializerOptions);

                foreach (KeyValuePair<string, AddonInfo> pair in repositoryInfo.Addons)
                {
                    if (_addons.TryGetValue(pair.Key, out AddonInfo oldValue))
                    {
                        if (string.Compare(pair.Value.VersionId, oldValue.VersionId) > 0)
                        {
                            _addons[pair.Key] = pair.Value;
                        }
                    }
                    else
                    {
                        _addons.Add(pair.Key, pair.Value);
                    }
                }

                _loader = repositoryInfo.Loader;

                AddonInfo loaderinfo = new AddonInfo(
                    Nickname: "addon-loader",
                    Developer: "gw2 addon loader contributors",
                    Website: "https://github.com/gw2-addon-loader/loader-core",
                    AddonName: "Addon Loader",
                    Description: "Core addon loading library.",
                    Tooltip: string.Empty,
                    HostType: HostType.github,
                    HostUrl: "https://github.com/gw2-addon-loader/loader-core/releases/latest",
                    VersionUrl: string.Empty,
                    DownloadType: DownloadType.Archive,
                    InstallMode: InstallMode.Loader,
                    PluginName: string.Empty,
                    PluginNamePattern: string.Empty,
                    Files: new List<string> { },
                    Requires: new List<string> { },
                    Conflicts: new List<string> { },
                    VersionId: _loader.VersionId,
                    VersionIdIsHumanReadable: false,
                    DownloadUrl: _loader.DownloadUrl,
                    false);

                foreach (var pair in _addons)
                {
                    AddonInfo addon = pair.Value;

                    if (addon.Requires == null)
                    {
                        addon = pair.Value with { Requires = new List<string>() };
                    }

                    if (!addon.Requires.Contains(loaderinfo.AddonName, StringComparer.OrdinalIgnoreCase))
                    {
                        addon.Requires.Add(loaderinfo.AddonName);
                    }

                    _addons[pair.Key] = addon;
                }

                _addons.Add(loaderinfo.AddonName, loaderinfo);

                _manager = repositoryInfo.Manager;
            }
        }
    }
}
