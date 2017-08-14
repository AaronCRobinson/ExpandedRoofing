using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace ExpandedRoofing
{
    internal class CompPowerPlantSolarController : CompPowerPlant, ICellBoolGiver
    {
        private const float maxOutput = 2500f;
        // NOTE: unsure why this number needs to be magnitudes larger... (not really wattage)
        private const float wattagePerSolarPanel = 200f; 
        // NOTE: find a way to stop reseting this...
        public static bool[] SolarRoof;
        private int roofCount = 0;
        private HashSet<int> controllers;
        private float powerOut;
        public CellBoolDrawer drawer;
        public HashSet<int> solarRoofLooked;

        protected override float DesiredPowerOutput
        {   
            get
            {
                powerOut = 0;
                if(this.controllers.Count > 0)
                    powerOut = Mathf.Lerp(0f, wattagePerSolarPanel, this.parent.Map.skyManager.CurSkyGlow) * ((float)this.roofCount / this.controllers.Count);
                if (powerOut > maxOutput)
                    return maxOutput;
                return powerOut;
            }
        }

        public Color Color
        {
            get
            {
                return new Color(0.3f, 1f, 0.4f);
            }
        }

        public bool GetCellBool(int index)
        {
            return SolarRoof[index];
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            // NOTE: do we really need the full grid?
            SolarRoof = new bool[this.parent.Map.cellIndices.NumGridCells];
            //this.SolarRoofLooked = new bool[this.parent.Map.cellIndices.NumGridCells];
            this.solarRoofLooked = new HashSet<int>();
            this.controllers = new HashSet<int>();
        }

        public override void CompTick()
        {
            base.CompTick();
            if (Gen.IsHashIntervalTick(this.parent, 30))
            {
                this.solarRoofLooked.Clear();
                this.controllers.Clear();
                Queue <IntVec3> lookQueue = new Queue<IntVec3>();
                this.roofCount = 0;
                for (int i = 0; i < SolarRoof.Length; i++) SolarRoof[i] = false; // TODO: fix the need for this...

                this.controllers.Add(this.parent.thingIDNumber);
                for (int i = -1; i < this.parent.RotatedSize.x + 1; i++)
                    for (int j = -1; j < this.parent.RotatedSize.x + 1; j++)
                        lookQueue.Enqueue(this.parent.Position + new IntVec3(i, 0, j));

                Map map = this.parent.Map;
                RoofGrid roofGrid = map.roofGrid;

                while (lookQueue.Count > 0)
                {
                    IntVec3 loc = lookQueue.Dequeue();
                    if (!loc.InBounds(map)) continue; // skip ahead if out of bounds

                    Building building = loc.GetFirstBuilding(map);
                    if (building?.def == ThingDefOf.SolarController && building.thingIDNumber != this.parent.thingIDNumber)
                        if (!this.controllers.Contains(building.thingIDNumber))
                            this.controllers.Add(building.thingIDNumber);

                    if (roofGrid.RoofAt(loc) == RoofDefOf.RoofSolar && !this.solarRoofLooked.Contains(map.cellIndices.CellToIndex(loc)))
                    {
                        this.roofCount++;
                        SolarRoof[map.cellIndices.CellToIndex(loc)] = true;

                        foreach (IntVec3 cardinal in GenAdj.CardinalDirections)
                        {
                            IntVec3 iv3 = loc + cardinal;
                            if (!this.solarRoofLooked.Contains(map.cellIndices.CellToIndex(iv3)))
                                lookQueue.Enqueue(iv3);
                        }

                    }
                    this.solarRoofLooked.Add(map.cellIndices.CellToIndex(loc));
                }

                if (this.drawer != null)
                {
                    this.drawer.SetDirty();
                }

#if DEBUG
                Log.Message($"CompTick - RoofCount: {this.roofCount} ControllerCount: {this.controllers.Count}");
#endif
            }
        }

        public override void PostDrawExtraSelectionOverlays()
        {
            base.PostDrawExtraSelectionOverlays();
            if (this.drawer == null)
            {
                this.drawer = new CellBoolDrawer(this, this.parent.Map.Size.x, this.parent.Map.Size.z);
            }
            this.drawer.MarkForDraw();
            this.drawer.CellBoolDrawerUpdate();
        }

        public override string CompInspectStringExtra()
        {
            return Translator.Translate("SolarRoofArea") + ": " + this.roofCount.ToString("###0") + "\n" + base.CompInspectStringExtra();
        }

        public Color GetCellExtraColor(int index)
        {
            return Color.white;
        }
    }
}
