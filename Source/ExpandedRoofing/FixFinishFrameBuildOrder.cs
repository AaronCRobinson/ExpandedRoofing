using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using Verse.AI;
using RimWorld;
using HarmonyLib;

namespace ExpandedRoofing
{
    static class ClosestThingReachableHelper
    {
        public static Thing ClosestThingReachableWrapper(IntVec3 root, Map map, ThingRequest thingReq, PathEndMode peMode, TraverseParms traverseParams, float maxDistance, Predicate<Thing> validator, IEnumerable<Thing> customGlobalSearchSet, int searchRegionsMin, int searchRegionsMax, bool forceGlobalSearch, RegionType traversableRegionTypes, bool ignoreEntirelyForbiddenRegions)
        {
            if (thingReq.group == ThingRequestGroup.BuildingFrame)
            {
                Predicate<Thing> predicate = validator;
                Predicate<Thing> extra = (Thing t) => {
                    if (t.def.defName.EndsWith("Framing_Frame"))
                    {
                        return RoofCollapseUtility.WithinRangeOfRoofHolder(t.Position, t.Map) &&
                               RoofCollapseUtility.ConnectedToRoofHolder(t.Position, t.Map, true);
                    }
                    return true;
                };
                Predicate<Thing> newValidator = new Predicate<Thing>(s => (predicate(s) && extra(s)));
                validator = newValidator;
            }
            return GenClosest.ClosestThingReachable(root, map, thingReq, peMode, traverseParams, maxDistance, validator, customGlobalSearchSet, searchRegionsMin, searchRegionsMax, forceGlobalSearch, traversableRegionTypes, ignoreEntirelyForbiddenRegions);
        }
    }

    [StaticConstructorOnStartup]
    internal class FixFinishFrameBuildOrder
    {
        static MethodInfo MI_ClosestThingReachable = AccessTools.Method(typeof(GenClosest), nameof(GenClosest.ClosestThingReachable));
        static MethodInfo MI_ClosestThingReachableWrapper = AccessTools.Method(typeof(ClosestThingReachableHelper), nameof(ClosestThingReachableHelper.ClosestThingReachableWrapper));

        static FixFinishFrameBuildOrder()
        {
#if DEBUG
            Harmony.DEBUG = true;
            Log.Message("FixFinishFrameBuildOrder");
#endif
            Harmony harmony = new Harmony("rimworld.whyisthat.expandedroofing.fixbuildorder");

            harmony.Patch(AccessTools.Method(typeof(JobGiver_Work), nameof(JobGiver_Work.TryIssueJobPackage)), null, null, new HarmonyMethod(typeof(FixFinishFrameBuildOrder), nameof(Transpiler)));
#if DEBUG
            Harmony.DEBUG = false;
#endif
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Call && (MethodInfo)instruction.operand == MI_ClosestThingReachable)
                    yield return new CodeInstruction(OpCodes.Call, MI_ClosestThingReachableWrapper);
                else
                    yield return instruction;
            }
        }

    }
}
