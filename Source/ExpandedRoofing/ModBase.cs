using System.Reflection;
using Verse;
using RimWorld;
using Harmony;
using HugsLib;
using HugsLib.Utils;

namespace ExpandedRoofing
{
    public class ExpandedRoofingModBase : ModBase
    {
        private static MethodInfo MI_NewBlueprintDef_Thing = AccessTools.Method(typeof(ThingDefGenerator_Buildings), "NewBlueprintDef_Thing");
        private static MethodInfo MI_NewFrameDef_Thing = AccessTools.Method(typeof(ThingDefGenerator_Buildings), "NewFrameDef_Thing");

        public override string ModIdentifier
        {
            get { return "ExpandedRoofing"; }
        }

        public override void DefsLoaded()
        {
            ThingDef def;
            Log.Message(this.ModIdentifier + ": generating dynamic defs");

            // RoofTransparent Blueprint
            def = MI_NewBlueprintDef_Thing.Invoke(null, new object[] { ThingDefOf.RoofTransparentFraming, false, null }) as ThingDef;
            InjectedDefHasher.GiveShortHasToDef(def, typeof(ThingDef));
            DefDatabase<ThingDef>.Add(def);

            // RoofTransparent Frame
            def = MI_NewFrameDef_Thing.Invoke(null, new object[] { ThingDefOf.RoofTransparentFraming }) as ThingDef;
            InjectedDefHasher.GiveShortHasToDef(def, typeof(ThingDef));
            DefDatabase<ThingDef>.Add(def);

            // RoofSolarFraming Blueprint
            def = MI_NewBlueprintDef_Thing.Invoke(null, new object[] { ThingDefOf.RoofSolarFraming, false, null }) as ThingDef;
            InjectedDefHasher.GiveShortHasToDef(def, typeof(ThingDef));
            DefDatabase<ThingDef>.Add(def);

            // RoofSolarFraming Frame
            def = MI_NewFrameDef_Thing.Invoke(null, new object[] { ThingDefOf.RoofSolarFraming }) as ThingDef;
            InjectedDefHasher.GiveShortHasToDef(def, typeof(ThingDef));
            DefDatabase<ThingDef>.Add(def);
        }

    }
}
