namespace GW2AddonManager
{
    public interface IConfigurationProvider
    {
        string ApplicationVersion { get; }

        Configuration UserConfig { get; set; }

        public string ConfigFileName { get; }
    }
}