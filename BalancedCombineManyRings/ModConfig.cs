using System;
namespace BalancedCombineManyRings
{
    public class ModConfig
    {
        public bool DestroyRingOnFailure { get; set; } = false;
        public int FailureChancePerExtraRing { get; set; } = 20;
        public int CostPerExtraRing { get; set; } = 100;
    }
}
