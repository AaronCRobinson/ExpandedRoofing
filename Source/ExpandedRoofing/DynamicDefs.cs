using System.Reflection;
using Verse;
using RimWorld;
using HarmonyLib;

namespace ExpandedRoofing
{
    [StaticConstructorOnStartup]
    internal class DynamicDefs
    {
        private static MethodInfo MI_NewBlueprintDef_Thing = AccessTools.Method(typeof(ThingDefGenerator_Buildings), "NewBlueprintDef_Thing");
        private static MethodInfo MI_NewFrameDef_Thing = AccessTools.Method(typeof(ThingDefGenerator_Buildings), "NewFrameDef_Thing");

        static DynamicDefs()
        {
            Log.Message("ExpandedRoofing: generating dynamic defs");
            InjectedDefHasher.PrepareReflection();
            ImpliedBlueprintAndFrameDefs(ThingDefOf.RoofTransparentFraming);
            ImpliedBlueprintAndFrameDefs(ThingDefOf.RoofSolarFraming);
            ImpliedBlueprintAndFrameDefs(ThingDefOf.ThickStoneRoofFraming);
        }

        private static void ImpliedBlueprintAndFrameDefs(ThingDef thingDef)
        {
            ThingDef def;
            // Blueprint
            def = MI_NewBlueprintDef_Thing.Invoke(null, new object[] { thingDef, false, null }) as ThingDef;
            InjectedDefHasher.GiveShortHasToDef(def, typeof(ThingDef));
            DefDatabase<ThingDef>.Add(def);
            // Frame
            def = MI_NewFrameDef_Thing.Invoke(null, new object[] { thingDef }) as ThingDef;
            InjectedDefHasher.GiveShortHasToDef(def, typeof(ThingDef));
            if (thingDef.MadeFromStuff) def.stuffCategories = thingDef.stuffCategories;
            DefDatabase<ThingDef>.Add(def);
        }
    }
}
