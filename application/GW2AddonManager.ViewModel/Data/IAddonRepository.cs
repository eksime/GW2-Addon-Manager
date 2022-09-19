using System.Collections.Generic;
using System.Threading.Tasks;

namespace GW2AddonManager
{
    public interface IAddonRepository
    {
        IReadOnlyDictionary<string, AddonInfo> Addons { get; }
        LoaderInfo Loader { get; }
        ManagerInfo Manager { get; }
        Task Refresh();
    }
}
