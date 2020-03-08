using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using static ExpandedRoofing.SolarRoofingTracker;

namespace ExpandedRoofing
{
    // TODO: consider consolidating MapComponents
    public class SolarRoofing_MapComponent : MapComponent
    {
        public SolarRoofingTracker tracker;
        public SolarRoofing_MapComponent(Map map) : base(map)
        {
            this.tracker = new SolarRoofingTracker(map);
        }
    }

    // NOTE: no need for ExposeData (just recount when map is loaded -- not a long operation)
    public class SolarRoofingTracker
    {
        private static readonly FieldInfo FI_RoofGrid_roofGrid = AccessTools.Field(typeof(RoofGrid), "roofGrid");
        private static int nextId=0;
        private int NextId { get => SolarRoofingTracker.nextId++; }

        public class SolarGridSet
        {
            public HashSet<IntVec3> set = new HashSet<IntVec3>();
            public HashSet<Thing> controllers = new HashSet<Thing>();

            public SolarGridSet() { }
            public SolarGridSet(IntVec3 cell) : base() => set.Add(cell);

            public void UnionWith(SolarGridSet other)
            {
                this.set.UnionWith(other.set);
                this.controllers.UnionWith(other.controllers);
            }

            public int RoofCount { get => this.set.Count; }
            public int ControllerCount { get => this.controllers.Count; }
        }

        private Dictionary<int, SolarGridSet> cellSets = new Dictionary<int, SolarGridSet>();
        private List<Thing> isolatedControllers = new List<Thing>();

        public SolarRoofingTracker(Map map)
        {
            // use map to init
            RoofDef[] roofGrid = FI_RoofGrid_roofGrid.GetValue(map.roofGrid) as RoofDef[];

            for (int i = 0; i < roofGrid.Length; i++)
                if (roofGrid[i] == RoofDefOf.RoofSolar)
                    this.AddSolarCell(map.cellIndices.IndexToCell(i));

            foreach (Thing controller in map.listerBuildings.AllBuildingsColonistOfDef(ThingDefOf.SolarController))
                this.AddController(controller);
        }

        public void AddSolarCell(IntVec3 cell)
        {
            // grids found that connect to the new cell
            HashSet<int> found = new HashSet<int>();
            foreach (KeyValuePair<int, SolarGridSet> pair in cellSets)
            {
                // using cardinal because it will be faster...
                for (int i = 0; i < 5; i++)
                {
                    if (pair.Value.set.Contains(cell + GenAdj.CardinalDirectionsAndInside[i]))
                    {
                        found.Add(pair.Key);
                        break; // return to main loop
                    }
                }
            }

            int idx = 0;
#if DEBUG
            Log.Message($"SolarRoofingTracker.AddSolarCell: {idx} -> case {found.Count()}");
#endif
            switch (found.Count)
            {
                case 0: // new grid
                    SolarGridSet s = new SolarGridSet(cell);
                    idx = NextId;
                    cellSets.Add(idx, s);
                    break;
                case 1: // 1 match
                    idx = found.First();
                    cellSets[idx].set.Add(cell);
                    break;
                default: // merger
                    int mergerKey = found.ElementAt(idx);
                    cellSets[mergerKey].set.Add(cell);
                    for (int i = 1; i < found.Count; i++)
                    {
                        foreach(Thing controller in cellSets[found.ElementAt(i)].controllers)
                        {
                            controller.TryGetComp<CompPowerPlantSolarController>().NetId = mergerKey;
                        }
                        cellSets[mergerKey].UnionWith(cellSets[found.ElementAt(i)]);
                        cellSets.Remove(found.ElementAt(i));
                    }
                    break;
            }

            //check isolated controllers
            List<Thing> del = new List<Thing>();
            foreach (Thing controller in isolatedControllers)
            {
                bool @break = false;
                for (int i = -1; i < controller.RotatedSize.x + 1 && !@break; i++)
                {
                    for (int j = -1; j < controller.RotatedSize.z + 1 && !@break; j++)
                    {
                        if (cell == controller.Position + new IntVec3(i, 0, j))
                        {

                            cellSets[idx].controllers.Add(controller);
                            del.Add(controller);
                            controller.TryGetComp<CompPowerPlantSolarController>().NetId = idx;
                            @break = true;
                        }
                    }
                }
            }
            foreach (Thing d in del)
                isolatedControllers.Remove(d);
        }

        // TODO: move common logic to method.
        public void RemoveSolarCell(IntVec3 cell)
        {
            foreach (SolarGridSet gs in cellSets.Values)
            {
                if (gs.set.Contains(cell))
                {
                    gs.set.Remove(cell);
                    return;
                }
            }
            Log.Error($"ExpandedRoofing: SolarRoofingTracker.Remove on a bad cell ({cell}).");
        }

        // NOTE: ignoring case of controller connects to two grids...
        public void AddController(Thing controller)
        {
            HashSet<IntVec3> connects = new HashSet<IntVec3>();

            for (int i = -1; i < controller.RotatedSize.x + 1; i++)
                for (int j = -1; j < controller.RotatedSize.z + 1; j++)
                    connects.Add(controller.Position + new IntVec3(i, 0, j));

            foreach (KeyValuePair<int, SolarGridSet> pair in cellSets)
            {
                if (connects.Any(iv3 => pair.Value.set.Contains(iv3)))
                {
                    pair.Value.controllers.Add(controller);
                    // this should never fail...
                    controller.TryGetComp<CompPowerPlantSolarController>().NetId = pair.Key;
                    return;
                }
            }
            isolatedControllers.Add(controller);
            return;
        }

        public void RemoveController(Thing controller) => cellSets[controller.TryGetComp<CompPowerPlantSolarController>().NetId].controllers.Remove(controller);

        public SolarGridSet GetCellSets(int? netId) => netId != null ? cellSets[(int)netId] : null;

    }
}
