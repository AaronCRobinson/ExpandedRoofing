using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace ExpandedRoofing
{
    public static class SolarRoofingTracker
    {
        private static Dictionary<int, SolarGridSet> cellSets = new Dictionary<int, SolarGridSet>();
        private static Dictionary<IntVec3, Thing> isolatedControllers = new Dictionary<IntVec3, Thing>();
        private static int cnt = 0;

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
        }

        public static void Add(this Dictionary<int, SolarGridSet> d, SolarGridSet s) => d.Add(cnt++, s);

        public static void Add(IntVec3 cell)
        {
            List<int> found = new List<int>();
            foreach (KeyValuePair<int, SolarGridSet> pair in cellSets)
            {
                // using cardinal because it will be faster...
                for (int i = 0; i < 5; i++)
                {
                    if (pair.Value.set.Contains(cell + GenAdj.CardinalDirectionsAndInside[i]))
                        found.Add(pair.Key);
                }
            }

            int idx = 0;
            switch (found.Count)
            {
                case 0:
                    cellSets.Add(new SolarGridSet(cell));
                    idx = cnt;
                    break;
                case 1:
                    cellSets[found[idx]].set.Add(cell);
                    break;
                default: // merger
                    int mergerKey = found[idx];
                    for (int i = 1; i < found.Count; i++)
                    {
                        cellSets[mergerKey].UnionWith(cellSets[i]);
                        cellSets.Remove(i);
                    }
                    break;
            }

            //check isolated controllers
            HashSet<IntVec3> connects = new HashSet<IntVec3>();
            foreach (IntVec3 controllerPos in isolatedControllers.Keys)
            {
                // NOTE: consider cardinal?
                for (int i = 0; i < 9; i++)
                {
                    if (cell == controllerPos + GenAdj.AdjacentCellsAndInside[i])
                    {
                        cellSets[idx].controllers.Add(isolatedControllers[controllerPos]);
                        break;
                    }
                }
                connects.Clear();
            }
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
                        cellSets.Remove(pos);
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
                for (int j = -1; j < controller.RotatedSize.x + 1; j++)
                    connects.Add(controller.Position + new IntVec3(i, 0, j));

            foreach (KeyValuePair<int, SolarGridSet> pair in cellSets)
            {
                if (connects.Any(iv3 => pair.Value.set.Contains(iv3)))
                {
                    pair.Value.controllers.Add(controller);
                    return; //break;
                }
            }
            // if still goin then we have an isolated controller
            isolatedControllers[controller.Position] = controller;
        }

    }
}
