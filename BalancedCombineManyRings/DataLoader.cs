using System;
using StardewModdingAPI;
using StardewValley;
namespace BalancedCombineManyRings
{
    public class DataLoader
    {
        public static IModHelper Helper;
        public static ModConfig ModConfig;
        public DataLoader(IModHelper helper)
        {
            Helper = helper;
            ModConfig = helper.ReadConfig<ModConfig>();
        }
    }
}
