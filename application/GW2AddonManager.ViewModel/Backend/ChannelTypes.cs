using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GW2AddonManager
{
    public record MessageChannel()
    {
        public static MessageChannel AddonInfoChanged = new MessageChannel();
    }

    public record AddonChanged(bool isInstalled, bool? isEnabled);
}
