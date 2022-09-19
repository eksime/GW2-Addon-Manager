namespace GW2AddonManager
{
    using GW2AddonManager.Core.Localization;

    public record Configuration(
        bool LaunchGame,
        string Culture,
        string GamePath,
        List<Uri> AddonRepos)
    {
        private static Uri DefaultRepoUri { get; } = new Uri("https://gw2-addon-loader.github.io/addon-repo/addons.json");
        public static Configuration Default => new Configuration(false, CultureConstants.English, string.Empty, new List<Uri> { DefaultRepoUri });
    }
}