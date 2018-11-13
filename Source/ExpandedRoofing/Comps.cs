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

    // TODO: this netId is some bad caching...
    public class CompPowerPlantSolarController : CompPowerPlant//, ICellBoolGiver
    {
        private static readonly Color color = new Color(0.3f, 1f, 0.4f);

        //private CellBoolDrawer drawer; // TODO: consider static
        private float powerOut;
        private SolarRoofingTracker solarRoofingTracker;
        private int? netId;
        public int NetId { get => (int)this.netId; set => this.netId = value; }
        public float WattagePerSolarPanel { get => ExpandedRoofingMod.settings.solarController_wattagePerSolarPanel; }
        public float MaxOutput { get => netId == null ? 0 : ExpandedRoofingMod.settings.solarController_maxOutput; }
        public int RoofCount { get => netId == null ? 0 : this.solarRoofingTracker.GetCellSets(this.netId).RoofCount; }
        public int ControllerCount { get => this.solarRoofingTracker.GetCellSets(this.netId).ControllerCount; }

        protected override float DesiredPowerOutput
        {
            get
            {
                if (netId == null)
                    return 0;
                powerOut = Mathf.Lerp(0f, WattagePerSolarPanel, this.parent.Map.skyManager.CurSkyGlow) * ((float)this.RoofCount / this.ControllerCount);
                if (powerOut > MaxOutput)
                    return MaxOutput;
                return powerOut;
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            this.solarRoofingTracker = this.parent.Map.GetComponent<SolarRoofing_MapComponent>().tracker;
            this.solarRoofingTracker.AddController(this.parent);
            // NOTE: do we really need the full grid?
            //this.drawer = new CellBoolDrawer(this, this.parent.Map.Size.x, this.parent.Map.Size.z);
        }

        public override void PostDrawExtraSelectionOverlays()
        {
            base.PostDrawExtraSelectionOverlays();
            //drawer.MarkForDraw();
            //drawer.CellBoolDrawerUpdate();
        }

        public override string CompInspectStringExtra() => $"{"SolarRoofArea".Translate()}: {this.RoofCount.ToString("###0")}\n{base.CompInspectStringExtra()}";

        // ICellBoolGiver implementation
        public Color Color { get => CompPowerPlantSolarController.color; }
        //public bool GetCellBool(int index) => CompPowerPlantSolarController.solarRoof[index];
        public Color GetCellExtraColor(int index) => Color.white;

        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);
            map.GetComponent<SolarRoofing_MapComponent>().tracker.RemoveController(this.parent);
        }
    }

}
