namespace GW2AddonManager
{
    public interface IAddonManager
    {
        Task Delete(IEnumerable<AddonInfo> addons);
        Task Disable(IEnumerable<AddonInfo> addons);
        Task Enable(IEnumerable<AddonInfo> addons);
        Task Install(IEnumerable<AddonInfo> addons);

        bool? IsEnabled(AddonInfo addon);
        string GetAddonDirectory(AddonInfo addon);
        string GetAddonPath(AddonInfo addon, bool isEnabled);

        string AddonsFolder { get; }
    }
}
