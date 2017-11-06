using RimWorld;
using Verse;

namespace ExpandedRoofing
{
    [DefOf]
    public static class ThingDefOf
    {
        // NOTE: fix naming later (maybe during RW version bump)
        public static ThingDef RoofSolarFraming;
        public static ThingDef RoofTransparentFraming;
        public static ThingDef ThickStoneRoofFraming;
        public static ThingDef SolarController;
    }

    [DefOf]
    public static class RoofDefOf
    {
        public static RoofDef RoofTransparent;
        public static RoofDef RoofSolar;
        public static RoofDef ThickStoneRoof;
        /*public static RoofDef JadeThickStoneRoof;
        public static RoofDef SandstoneThickStoneRoof;
        public static RoofDef GraniteThickStoneRoof;
        public static RoofDef LimestoneThickStoneRoof;
        public static RoofDef SlateThickStoneRoof;
        public static RoofDef MarbleThickStoneRoof;*/
    }

    [DefOf]
    public static class ResearchProjectDefOf
    {
        public static ResearchProjectDef ThickStoneRoofRemoval;
    }

    [DefOf]
    public static class JobDefOf
    {
        public static JobDef PerformRoofMaintenance;
    }
}
