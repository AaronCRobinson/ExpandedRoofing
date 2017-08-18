using Verse;

namespace ExpandedRoofing
{
    public class CompAddRoof : ThingComp
    {
        public CompProperties_AddRoof Props
        {
            get
            {
                return (CompProperties_AddRoof)this.props;
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            RoofDef roofDef = this.Props.roofDef;
            ThingDef stuff = this.parent.Stuff; 
            if (stuff != null) roofDef = DefDatabase<RoofDef>.GetNamed($"{stuff.defName.Replace("Blocks", "")}ThickStoneRoof", false);
            this.parent.Map.roofGrid.SetRoof(this.parent.Position, roofDef);
            if (!this.parent.Destroyed) this.parent.Destroy(); // auto delete
        }
    }

    public class CompProperties_AddRoof : CompProperties
    {
        public RoofDef roofDef;

        public CompProperties_AddRoof()
        {
            this.compClass = typeof(CompAddRoof);
        }
    }

    class RoofExtension : DefModExtension
    {
        public float transparency = 0f;
        public ThingDef spawnerDef;
    }
}
