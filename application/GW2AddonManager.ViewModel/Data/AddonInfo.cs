namespace GW2AddonManager
{
    using System.Collections.Generic;

    public enum DownloadType
    {
        DLL,
        Archive,
    }

    public enum InstallMode
    {
        Binary,
        Arc,
        Loader,
    }

    public enum HostType
    {
        standalone,
        github,
    }

    public record AddonInfo(
        string Nickname,
        string Developer,
        string Website,
        string AddonName,
        string Description,
        string Tooltip,
        HostType HostType,
        string HostUrl,
        string VersionUrl,
        DownloadType DownloadType,
        InstallMode InstallMode,
        string PluginName,
        string PluginNamePattern,
        List<string> Files,
        List<string> Requires,
        List<string> Conflicts,
        string VersionId,
        bool VersionIdIsHumanReadable,
        string DownloadUrl,
        bool SelfUpdate);
}
