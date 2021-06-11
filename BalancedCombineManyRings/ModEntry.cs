using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Objects;
using StardewValley.Menus;
using Harmony;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace BalancedCombineManyRings
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        internal static IMonitor ModMonitor { get; set; }
        internal new static IModHelper Helper { get; set; }

        public override void Entry(IModHelper helper)
        {
            ModMonitor = Monitor;
            Helper = helper;

            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        }

        private void OnGameLaunched(object sender, EventArgs e)
        {
            var harmony = HarmonyInstance.Create("Arruda.BalancedCombineManyRings");

            harmony.Patch(
                original: AccessTools.Method(typeof(Ring), nameof(Ring.CanCombine)),
                prefix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.CanCombine_Prefix))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(CombinedRing), "loadDisplayFields"),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.LoadDisplayFields_Postfix))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(CombinedRing), nameof(CombinedRing.drawInMenu), new Type[] {typeof(SpriteBatch), typeof(Vector2), typeof(float), typeof(float), typeof(float), typeof(StackDrawType), typeof(Color), typeof(bool)}),
                prefix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.DrawInMenu_Prefix))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(ForgeMenu), nameof(ForgeMenu.GetForgeCost)),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.GetForgeCost_Postfix))
            );
        }
        public static int GetCombinedRingTotal(Ring ring)
        {
            if (ring is CombinedRing)
            {
                int count = 0;
                foreach (Ring r in (ring as CombinedRing).combinedRings)
                {
                    count += GetCombinedRingTotal(r);
                }
                return count;
            }
            else
            {
                return 1;
            }
        }
        public static SortedDictionary<string, int> GetCombinedRings(Ring ring)
        {
            SortedDictionary<string, int> result = new SortedDictionary<string, int>();
            Queue<Ring> to_process = new Queue<Ring>();
            to_process.Enqueue(ring);
            while (to_process.Count > 0) {
                Ring cur = to_process.Dequeue();
                ModMonitor.Log($"Processing {cur.DisplayName}", LogLevel.Trace);
                if (cur is CombinedRing)
                {
                    foreach(Ring r in (cur as CombinedRing).combinedRings)
                    {
                        to_process.Enqueue(r);
                    }
                }
                else
                {
                    string key = cur.displayName;
                    if (result.TryGetValue(key, out int val))
                    {
                        result.Add(key, val + 1);
                    }
                    else
                    {
                        result.Add(key, 1);
                    }
                }
            }
            return result;
        }
        public static void LoadDisplayFields_Postfix(CombinedRing __instance)
        {
            try
            {
                if (GetCombinedRingTotal(__instance) >= 8)
                {
                    string description = "Many Rings forged into one:\n\n";
                    foreach (KeyValuePair<string, int> entry in GetCombinedRings(__instance))
                    {
                        description += String.Format("{1}x {0}\n", entry.Key, entry.Value);
                    }
                    ModMonitor.Log($"Combined Ring description is {description}", LogLevel.Trace);
                    __instance.description = description.Trim();
                }
            }
            catch (Exception ex)
            {
                ModMonitor.Log($"Failed in {nameof(LoadDisplayFields_Postfix)}:\n{ex}", LogLevel.Error);
            }
        }
        public static bool CanCombine_Prefix(Ring __instance, Ring ring, ref bool __result)
        {
            try
            {
                __result = true;
                if (ring is CombinedRing)
                {
                    foreach (Ring combinedRing in (ring as CombinedRing).combinedRings)
                    {
                        if (!__instance.CanCombine(combinedRing))
                        {
                            __result = false;
                            break;
                        }
                    }
                }
                else if (__instance is CombinedRing)
                {
                    foreach (Ring combinedRing in (__instance as CombinedRing).combinedRings)
                    {
                        if (!combinedRing.CanCombine(ring))
                        {
                            __result = false;
                            break;
                        }
                    }
                }
                else if (__instance.ParentSheetIndex == ring.ParentSheetIndex)
                {
                    __result = false;
                }
                return false; // don't run original logic
            }
            catch (Exception ex)
            {
                ModMonitor.Log($"Failed in {nameof(CanCombine_Prefix)}:\n{ex}", LogLevel.Error);
                return true; // run original logic
            }
        }
        public static bool DrawInMenu_Prefix(CombinedRing __instance, SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
        {
            try
            {
                if (__instance.combinedRings.Count >= 2)
                {
                    // Always use base rings as the sprites to draw. The first pair that are combined on the left hand side get used as the sprite.
                    if (__instance.combinedRings[0] is CombinedRing)
                    {
                        __instance.combinedRings[0].drawInMenu(spriteBatch, location, scaleSize, transparency, layerDepth, drawStackNumber, color, drawShadow);
                        return false; // don't run original logic
                    }
                    else if (__instance.combinedRings[1] is CombinedRing)
                    {
                        __instance.combinedRings[1].drawInMenu(spriteBatch, location, scaleSize, transparency, layerDepth, drawStackNumber, color, drawShadow);
                        return false; // don't run original logic
                    }
                }
                return true; // run original logic
            }
            catch (Exception ex)
            {
                ModMonitor.Log($"Failed in {nameof(DrawInMenu_Prefix)}:\n{ex}", LogLevel.Error);
                return true; // run original logic
            }
        }

        //public virtual int GetForgeCost (Item left_item, Item right_item);
        public static void GetForgeCost_Postfix(ForgeMenu __instance, Item left_item, Item right_item, ref int __result)
        {
            if (left_item != null && right_item != null)
            {
                // if merging rings, then calculate different cost based on the total amount of rings being combined
                // if only two, rings, than keep normal cost of 20, otherwise, gets 100 per ring combined (max of 999)
                if (left_item.getCategoryName().Equals("Ring") && left_item.category == right_item.category)
                {
                    Ring left_ring = (Ring)left_item;
                    Ring right_ring = (Ring)right_item;

                    int total_rings = GetCombinedRingTotal(left_ring) + GetCombinedRingTotal(right_ring);
                    if (total_rings > 2)
                    {
                        int new_cost = Math.Min(total_rings * 100, 999);
                        __result = new_cost;

                    }

                }

            }

        }

        //public bool IsValidCraft(Item left_item, Item right_item);

        //public Item CraftItem(Item left_item, Item right_item, bool forReal = false);

        //public void SpendRightItem();

        //public void SpendLeftItem();
    }
}