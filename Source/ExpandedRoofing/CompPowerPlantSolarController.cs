using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace ExpandedRoofing
{
    internal class CompPowerPlantSolarController : CompPowerPlant, ICellBoolGiver
    {
        public int RoofCount = 0;

        public bool collision = false;

        public CellBoolDrawer drawer;

        public bool[] SolarRoof;

        public bool[] SolarRoofLooked;

        protected override float DesiredPowerOutput
        {
            get
            {
                return Mathf.Lerp(0f, 50f, this.parent.Map.skyManager.CurSkyGlow) * (float)this.RoofCount;
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
            return this.SolarRoof[index];
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            this.SolarRoof = new bool[this.parent.Map.cellIndices.NumGridCells];
            this.SolarRoofLooked = new bool[this.parent.Map.cellIndices.NumGridCells];
        }

        public override void CompTick()
        {
            base.CompTick();
            if (Gen.IsHashIntervalTick(this.parent, 30))
            {
                Queue <IntVec3> lookQueue = new Queue<IntVec3>();
                for (int i = 0; i < this.SolarRoofLooked.Length; i++)
                {
                    this.SolarRoofLooked[i] = false;
                    this.SolarRoof[i] = false;
                    this.RoofCount = 0;
                    this.collision = false;
                }

                for(int i = -1; i < this.parent.RotatedSize.x + 1; i++)
                    for (int j = -1; j < this.parent.RotatedSize.x + 1; j++)
                        lookQueue.Enqueue(this.parent.Position + new IntVec3(i, 0, j));

                Map map = this.parent.Map;
                RoofGrid roofGrid = map.roofGrid;

                while (lookQueue.Count > 0)
                {
                    IntVec3 loc = lookQueue.Dequeue();
                    if (!loc.InBounds(map)) continue; // break if out of bounds
                    this.SolarRoofLooked[map.cellIndices.CellToIndex(loc)] = true; // tag looked

                    // NOTE: maybe a better way to do this...
                    if (loc.GetThingList(map).Any(t => t.def == ThingDefOf.SolarController))
                    {
                        this.collision = true;
                        this.RoofCount = 0;
                        lookQueue.Clear();
                        break;
                    }

                    if (roofGrid.RoofAt(loc) == RoofDefOf.RoofSolar)
                    {
                        this.RoofCount++;
                        this.SolarRoof[map.cellIndices.CellToIndex(loc)] = true;

                        foreach (IntVec3 cardinal in GenAdj.CardinalDirections)
                        {
                            if (GenGrid.InBounds(loc + cardinal, map) && !this.SolarRoofLooked[map.cellIndices.CellToIndex(loc + cardinal)])
                            {
                                lookQueue.Enqueue(loc + cardinal);
                            }
                        }

                    }
                }

                if (this.drawer != null)
                {
                    this.drawer.SetDirty();
                }
            }
        }

        public override void PostDrawExtraSelectionOverlays()
        {
            base.PostDrawExtraSelectionOverlays();
            if (!this.collision)
            {
                if (this.drawer == null)
                {
                    this.drawer = new CellBoolDrawer(this, this.parent.Map.Size.x, this.parent.Map.Size.z);
                }
                this.drawer.MarkForDraw();
                this.drawer.CellBoolDrawerUpdate();
            }
        }

        public override string CompInspectStringExtra()
        {
            string str;
            if (!this.collision)
            {
                str = Translator.Translate("SolarRoofArea") + ": " + this.RoofCount.ToString("###0");
            }
            else
            {
                str = Translator.Translate("SolarRoofCollision");
            }
            return str + "\n" + base.CompInspectStringExtra();
        }

        public Color GetCellExtraColor(int index)
        {
            return Color.white;
        }
    }
}
