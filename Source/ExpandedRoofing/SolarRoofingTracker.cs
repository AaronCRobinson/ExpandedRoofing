using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ExpandedRoofing
{
    // TODO: This will not work with multiple maps.
    // MapComponent...
    public static class SolarRoofingTracker
    {
        private static Dictionary<int, SolarGridSet> cellSets = new Dictionary<int, SolarGridSet>();
        private static List<Thing> isolatedControllers = new List<Thing>();

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

        // used to add new SolarGridSets to cellSets. (Simple incremental keying)
        public static int Add(this Dictionary<int, SolarGridSet> d, SolarGridSet s)
        {
            int idx = cellSets.Count;
            d.Add(idx, s);
            return idx;
        }

        public static void Add(IntVec3 cell)
        {
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
            Log.Message($"{idx} -> case {found.Count()}");
            switch (found.Count)
            {
                case 0:
                    idx = cellSets.Add(new SolarGridSet(cell));
                    break;
                case 1:
                    idx = found.First();
                    cellSets[idx].set.Add(cell);
                    break;
                default: // merger
                    int mergerKey = found.ElementAt(idx);
                    for (int i = 1; i < found.Count; i++)
                    {
                        cellSets[mergerKey].UnionWith(cellSets[found.ElementAt(i)]);
                        cellSets.Remove(i);
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
                            controller.TryGetComp<CompPowerPlantSolarController>().SetNetId(idx);
                            @break = true;
                        }
                    }
                }
            }
            foreach (Thing d in del)
                isolatedControllers.Remove(d);
        }

        // TODO: move common logic to method.
        public static void Remove(IntVec3 cell)
        {
            IntVec3 pos;
            foreach (KeyValuePair<int, SolarGridSet> pair in cellSets)
            {
                // using cardinal because it will be faster...
                for (int i = 0; i < 5; i++)
                {
                    pos = cell + GenAdj.CardinalDirectionsAndInside[i];
                    if (pair.Value.set.Contains(pos))
                    {
                        //cellSets.Remove(pos);
                        pair.Value.set.Remove(pos);
                        return;
                    }
                }
            }

            Log.Error($"ExpandedRoofing: SolarRoofingTracker.Remove on a bad cell ({cell}).");
        }

        // NOTE: ignoring case of controller connects to two grids...
        public static void Add(Thing controller)
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
                    controller.TryGetComp<CompPowerPlantSolarController>()?.SetNetId(pair.Key);
                    return;
                }
            }
            //isolatedControllers[controller.Position] = controller;
            isolatedControllers.Add(controller);
            return;
        }

        public static SolarGridSet GetCellSets(int? netId) => netId != null ? cellSets[(int)netId] : null;

    }
}
