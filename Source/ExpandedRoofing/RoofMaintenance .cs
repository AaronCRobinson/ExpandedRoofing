using Harmony;
using System;
using System.Collections.Generic;
using System.Text;
using Verse;

namespace ExpandedRoofing
{
    [StaticConstructorOnStartup]
    static class RoofMaintenance
    {
        // Verse.TickList.TickInterval
        const int long_TickInterval = 2000;

        static Dictionary<int, RoofMaintenceGrid> roofMaintenceGrids = new Dictionary<int, RoofMaintenceGrid>();

        static RoofMaintenance()
        {
#if DEBUG
            HarmonyInstance.DEBUG = true;
#endif
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.whyisthat.expandedroofing.roofmaintenance");

            harmony.Patch(AccessTools.Method(typeof(Map), nameof(Map.MapPostTick)), null, new HarmonyMethod(typeof(RoofMaintenance), nameof(RoofMaintenanceTick)));
            harmony.Patch(AccessTools.Method(typeof(Map), nameof(Map.ConstructComponents)), null, new HarmonyMethod(typeof(RoofMaintenance), nameof(ConstructComponentsPostfix)));

            harmony.Patch(AccessTools.Method(typeof(RoofGrid), nameof(RoofGrid.SetRoof)), null, new HarmonyMethod(typeof(RoofMaintenance), nameof(SetRoofPostfix)));
        }

        static void RoofMaintenanceTick(Map __instance)
        {
            if (Find.TickManager.TicksGame % long_TickInterval == 0) // NOTE: consider hashTick?
            {
                Log.Message("RoofMaintenanceTick");

                //roofMaintenceGrids[__instance.uniqueID]
            }
        }

        static void ConstructComponentsPostfix(Map __instance)
        {
            roofMaintenceGrids.Add(__instance.uniqueID, new RoofMaintenceGrid(__instance));
        }

        // NOTE: need to handle removing thickroofs...
        static void SetRoofPostfix(RoofGrid __instance, IntVec3 c, RoofDef roofDef)
        {
            // handle adding thick roofs to tracker. 
            if (roofDef.isThickRoof) // SetRoof & isThickRoof :. buildable thickRoof found.
            {
                Map map = (Map)HarmonyPatches.FI_RoofGrid_map.GetValue(__instance);
                roofMaintenceGrids[map.uniqueID].AddMaintainableRoof(c);
            }
        }

    }

    internal sealed class RoofMaintenceGrid : IExposable
    {
        const ushort minTimeBeforeDamage = 5000;

        private Map map;

        // NOTE: int => Cell index, ushort => time to breakdown
        Dictionary<int, ushort> roofMaintenceGrid;

        public RoofMaintenceGrid(Map map)
        {
            this.map = map;
            this.roofMaintenceGrid = new Dictionary<int, ushort>();
        }

        public void ExposeData()
        {
            throw new NotImplementedException();
        }

        public void AddMaintainableRoof(IntVec3 c)
        {
            this.roofMaintenceGrid.Add(map.cellIndices.CellToIndex(c), 0);
        }

        public void RoofMaintenceTick()
        {
            //Dictionary<int, ushort>.KeyCollection keys = this.roofMaintenceGrid.Keys;

            foreach (int key in this.roofMaintenceGrid.Keys)
            {
                this.roofMaintenceGrid[key] += 1;
                if (this.roofMaintenceGrid[key] > minTimeBeforeDamage)
                {

                }
            }

        }
    }

}
