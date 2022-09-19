using System.Reflection;
using System.Text.Json;
using System.IO.Abstractions;

namespace GW2AddonManager
{

    public class ConfigurationProvider : IConfigurationProvider
    {
        public string ConfigFileName => "config.json";

        public string ApplicationVersion
        {
            get
            {
                var currentAppVersion = Assembly.GetExecutingAssembly().GetName().Version;
                return $"v{currentAppVersion.Major}.{currentAppVersion.Minor}.{currentAppVersion.Build}";
            }
        }

        private Configuration _userConfig;
        private readonly IFileSystem _fileSystem;

        public Configuration UserConfig
        {
            get => _userConfig;
            set {
                _userConfig = value;
                Save();
            }
        }

        public ConfigurationProvider(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
            _userConfig = Load();
        }

        private void Save()
        {
            using var fs = _fileSystem.File.OpenWrite(ConfigFileName);
            JsonSerializer.Serialize(fs, (object)UserConfig);
        }

        private Configuration Load()
        {
            if (!_fileSystem.File.Exists(ConfigFileName))
                return Configuration.Default;

            using var fs = _fileSystem.File.OpenRead(ConfigFileName);
            return JsonSerializer.Deserialize<Configuration>(fs) ?? Configuration.Default;
        }
    }
}