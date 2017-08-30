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
    }

    [DefOf]
    public static class ResearchProjectDefOf
    {
        public static ResearchProjectDef ThickStoneRoofRemoval;
    }
}
