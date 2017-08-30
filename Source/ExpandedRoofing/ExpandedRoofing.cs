using System.Diagnostics.CodeAnalysis;
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
#if DEBUG
            if (!RoofCollapseUtility.WithinRangeOfRoofHolder(this.parent.InteractionCell, this.parent.Map) ||
                !RoofCollapseUtility.ConnectedToRoofHolder(this.parent.InteractionCell, this.parent.Map, true))
            {
                this.parent.Map.roofCollapseBuffer.MarkToCollapse(this.parent.InteractionCell);
                this.parent.Map.roofGrid.SetRoof(this.parent.Position, null); // NOTE: unsure why I need this... race condition?
            }
#endif
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
#pragma warning disable CS0649 // Set in the xml
        public ThingDef spawnerDef;
#pragma warning restore CS0649
    }
}
