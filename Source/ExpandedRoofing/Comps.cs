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

    // DEPRECATED: leave for a releases, as it will clean up any bad roofs created during different versions
    public class CompMaintainableRoof : ThingComp
    {
        // DEPRECATED - CLEAN UP: Destroy these things 
        public override void CompTick()
        {
            base.CompTick();
            // auto delete
            if (!this.parent.Destroyed) this.parent.Destroy();
        }
    }

    public class CompProperties_CustomRoof : CompProperties
    {
        public RoofDef roofDef;
        public CompProperties_CustomRoof() => this.compClass = typeof(CompCustomRoof);
    }

    class RoofExtension : DefModExtension
    {
        public float transparency = 0f;
#pragma warning disable CS0649 // Set in the xml
        public ThingDef spawnerDef;
#pragma warning restore CS0649
    }
}
