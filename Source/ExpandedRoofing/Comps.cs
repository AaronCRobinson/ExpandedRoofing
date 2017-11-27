using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace ExpandedRoofing
{
    /// <summary>
    /// Basic comp for custom roof. Auto deletes thing (leaving roof).
    /// </summary>
    public class CompCustomRoof : ThingComp
    {
        public CompProperties_CustomRoof Props { get => (CompProperties_CustomRoof)this.props; }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                this.parent.Map.roofGrid.SetRoof(this.parent.Position, this.Props.roofDef);
                MoteMaker.PlaceTempRoof(this.parent.Position, this.parent.Map);
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            // auto delete
            if (!this.parent.Destroyed) this.parent.Destroy();
        }

    }

    public class CompMaintainableRoof : CompCustomRoof
    {
        public override void CompTick()
        {
            if (this.parent.Stuff != null)
            {
                RoofDef roofDef = DefDatabase<RoofDef>.GetNamed($"{this.parent.Stuff.defName.Replace("Blocks", "")}ThickStoneRoof", false);
                this.parent.Map.roofGrid.SetRoof(this.parent.Position, roofDef);
            }
            base.CompTick(); // auto deletes
        }
    }

    public class CompProperties_CustomRoof : CompProperties
    {
        public RoofDef roofDef;
        //public CompProperties_CustomRoof() => this.compClass = typeof(CompCustomRoof);
    }

    public class CompPowerPlantSolarController : CompPowerPlant, ICellBoolGiver
    {
        private static readonly Color color = new Color(0.3f, 1f, 0.4f);
        public static bool[] solarRoof; // NOTE: find a way to stop reseting this...

        private CellBoolDrawer drawer; // TODO: consider static
        private int roofCount = 0;
        private HashSet<int> controllers;
        public HashSet<int> solarRoofLooked;
        private float powerOut;

        public float WattagePerSolarPanel { get => ExpandedRoofingMod.settings.solarController_wattagePerSolarPanel; }
        public float MaxOutput { get => ExpandedRoofingMod.settings.solarController_maxOutput; }

        protected override float DesiredPowerOutput
        {
            get
            {
                powerOut = 0;
                if (this.controllers.Count > 0)
                    powerOut = Mathf.Lerp(0f, WattagePerSolarPanel, this.parent.Map.skyManager.CurSkyGlow) * ((float)this.roofCount / this.controllers.Count);
                if (powerOut > MaxOutput)
                    return MaxOutput;
                return powerOut;
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            // NOTE: do we really need the full grid?
            solarRoof = new bool[this.parent.Map.cellIndices.NumGridCells];
            this.solarRoofLooked = new HashSet<int>();
            this.controllers = new HashSet<int>();
            this.drawer = new CellBoolDrawer(this, this.parent.Map.Size.x, this.parent.Map.Size.z);
        }

        public override void CompTick()
        {
            base.CompTick();
            if (Gen.IsHashIntervalTick(this.parent, 30)) CalculateSolarGrid();
        }

        private void CalculateSolarGrid(bool draw = false)
        {
            this.solarRoofLooked.Clear();
            this.controllers.Clear();
            Queue<IntVec3> lookQueue = new Queue<IntVec3>();
            this.roofCount = 0;
            for (int i = 0; i < solarRoof.Length; i++) solarRoof[i] = false; // TODO: fix the need for this...

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
                    solarRoof[map.cellIndices.CellToIndex(loc)] = true;

                    foreach (IntVec3 cardinal in GenAdj.CardinalDirections)
                    {
                        IntVec3 iv3 = loc + cardinal;
                        if (!this.solarRoofLooked.Contains(map.cellIndices.CellToIndex(iv3)))
                            lookQueue.Enqueue(iv3);
                    }

                }
                this.solarRoofLooked.Add(map.cellIndices.CellToIndex(loc));
            }

            if (draw) drawer.SetDirty();
        }

        public override void PostDrawExtraSelectionOverlays()
        {
            base.PostDrawExtraSelectionOverlays();
            CalculateSolarGrid(true);
            drawer.MarkForDraw();
            drawer.CellBoolDrawerUpdate();
        }

        public override string CompInspectStringExtra()
        {
            return $"{"SolarRoofArea".Translate()}: {this.roofCount.ToString("###0")}\n{base.CompInspectStringExtra()}";
        }

        // ICellBoolGiver implementation
        public Color Color { get => CompPowerPlantSolarController.color; }
        public bool GetCellBool(int index) => CompPowerPlantSolarController.solarRoof[index];
        public Color GetCellExtraColor(int index) => Color.white;

    }

}
