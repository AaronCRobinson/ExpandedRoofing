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

    public class CompMaintainableRoof : ThingComp
    {
        public CompProperties_CustomRoof Props { get => (CompProperties_CustomRoof)this.props; }
        private ThingDef Stuff { get => this.parent.Stuff; }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                RoofDef roofDef = this.Props.roofDef;
                // set roofdef for thickroofs
                if (this.Stuff != null)
                    roofDef = DefDatabase<RoofDef>.GetNamed($"{this.Stuff.defName.Replace("Blocks", "")}ThickStoneRoof", false);
                this.parent.Map.roofGrid.SetRoof(this.parent.Position, roofDef);
                MoteMaker.PlaceTempRoof(this.parent.Position, this.parent.Map);
            }
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            this.parent.Map.roofCollapseBuffer.MarkToCollapse(this.parent.InteractionCell);
            //this.parent.Map.roofGrid.SetRoof(this.parent.Position, null);
        }

    }

    public class CompProperties_CustomRoof : CompProperties
    {
        public RoofDef roofDef;
        //public CompProperties_AddRoof() => this.compClass = typeof(CompAddRoof);
    }

    class RoofExtension : DefModExtension
    {
        public float transparency = 0f;
#pragma warning disable CS0649 // Set in the xml
        public ThingDef spawnerDef;
#pragma warning restore CS0649
    }
}
