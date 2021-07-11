using PublicInfos;
using System.Collections.Generic;

namespace GachaWebBackend.Helper
{
    public static class ConfigCache
    {
        public static readonly object ConfigLock = new();
        public static readonly object OrderConfigLock = new();
        public static Dictionary<long, Config> UserConfigs = new Dictionary<long, Config>();
        public static Dictionary<long, OrderConfig> UserOrderConfigs = new Dictionary<long, OrderConfig>();
    }
}
