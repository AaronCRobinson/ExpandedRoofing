using Verse;

namespace ExpandedRoofing
{
    internal static class RoofHelper
    {
        public static bool IsBuildableThickRoof(this RoofDef roofDef) => roofDef != null && roofDef.isThickRoof && roofDef.HasModExtension<RoofExtension>();
    }
}
