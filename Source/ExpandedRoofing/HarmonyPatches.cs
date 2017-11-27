using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using RimWorld;
using Harmony;

namespace ExpandedRoofing
{

    static class TraspileHelper
    {
        public static bool CheckTransparency(GlowGrid gg, Map map, IntVec3 c, ref float num)
        {
            RoofExtension transparentRoofExt = map.roofGrid.RoofAt(c)?.GetModExtension<RoofExtension>();
            if (transparentRoofExt != null)
            {
                num = map.skyManager.CurSkyGlow * transparentRoofExt.transparency;
                if (num == 1f) return true;
            }
            return false;
        }

        private static int KillFinalize(int count) => GenMath.RoundRandom((float)count * 0.5f);

        // NOTE: consider destruction mode for better spawning
        public static void DoLeavings(RoofDef curRoof, ThingDef spawnerDef, Map map, CellRect leavingsRect)
        {
            // TODO: remove next release -> cleans-up bug.
            if (curRoof.defName == "ThickStoneRoof") return;

            ThingOwner<Thing> thingOwner = new ThingOwner<Thing>();
            ThingDef stuff = null;
            string stuffDefName = curRoof.defName.Replace("ThickStoneRoof", "");
            if(stuffDefName == "Jade") stuff = DefDatabase<ThingDef>.GetNamed(stuffDefName, false);
            else stuff = DefDatabase<ThingDef>.GetNamed($"Blocks{stuffDefName}", false);

            List<ThingCountClass> thingCounts = spawnerDef.CostListAdjusted(stuff, true);

            foreach (ThingCountClass curCntCls in thingCounts)
            {
                int val = KillFinalize(curCntCls.count);
                if (val > 0)
                {
                    Thing thing = ThingMaker.MakeThing(curCntCls.thingDef, null);
                    thing.stackCount = val;
                    thingOwner.TryAdd(thing, true);
                }
            }

            List<IntVec3> list = leavingsRect.Cells.InRandomOrder(null).ToList<IntVec3>();
            int num = 0;
            while (thingOwner.Count > 0)
            {
                if (!thingOwner.TryDrop(thingOwner[0], list[num], map, ThingPlaceMode.Near, out Thing thing, null))
                {
                    Log.Warning($"Failed to place all leavings for destroyed thing {curRoof} at {leavingsRect.CenterCell}");
                    return;
                }
                if (++num >= list.Count) num = 0;
            }
        }

        public static bool SkipRoofRendering(RoofDef roofDef) => (roofDef == RoofDefOf.RoofTransparent);

        // NOTE: do not need to check if `isThickRoof` b\c we already know it is
        // TODO: look at consolidating this method
        public static bool IsBuildableThickRoof(IntVec3 cell, Map map) => (cell.GetRoof(map) != RimWorld.RoofDefOf.RoofRockThick);

    }

    [StaticConstructorOnStartup]
    internal class HarmonyPatches
    {
        public static FieldInfo FI_RoofGrid_map = AccessTools.Field(typeof(RoofGrid), "map");

        static HarmonyPatches()
        {
#if DEBUG
            HarmonyInstance.DEBUG = true;
#endif
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.whyisthat.expandedroofing.main");

            // correct lighting for plant growth
            harmony.Patch(AccessTools.Method(typeof(GlowGrid), nameof(GlowGrid.GameGlowAt)), null, null, new HarmonyMethod(typeof(HarmonyPatches).GetMethod(nameof(PlantLightingFix))));

            // set roof to return materials
            harmony.Patch(AccessTools.Method(typeof(RoofGrid), nameof(RoofGrid.SetRoof)), new HarmonyMethod(typeof(HarmonyPatches), nameof(RoofLeavings)), null);

            // fix lighting inside rooms with transparent roof  
            harmony.Patch(AccessTools.Method(typeof(SectionLayer_LightingOverlay), nameof(SectionLayer_LightingOverlay.Regenerate)), null, null, new HarmonyMethod(typeof(HarmonyPatches), nameof(TransparentRoofLightingOverlayFix)));

            // Fix infestation under buildable thick roofs
            harmony.Patch(AccessTools.Method(typeof(InfestationCellFinder), "GetScoreAt"), null, null, new HarmonyMethod(typeof(HarmonyPatches), nameof(ThickRoofInfestationFix)));

            // Reset CompMaintainable when building repaired
            harmony.Patch(AccessTools.Method(typeof(ListerBuildingsRepairable), nameof(ListerBuildingsRepairable.Notify_BuildingRepaired)), null, new HarmonyMethod(typeof(HarmonyPatches), nameof(BuildingRepairedPostfix)));

            // Set clearBuildingArea flag in BlocksConstruction to be respected before large plant check (trees mostly)
            harmony.Patch(AccessTools.Method(typeof(GenConstruct), nameof(GenConstruct.BlocksConstruction)), null, null, new HarmonyMethod(typeof(HarmonyPatches), nameof(FixClearBuildingArea)));
        }

        public static IEnumerable<CodeInstruction> PlantLightingFix(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            FieldInfo FI_GlowGrid_map = AccessTools.Field(typeof(GlowGrid), "map");
            MethodInfo MI_CheckTransparency = AccessTools.Method(typeof(TraspileHelper), nameof(TraspileHelper.CheckTransparency));

            List<CodeInstruction> instructionList = instructions.ToList();
            int i;
            for (i = 0; i < instructionList.Count; i++)
            {
                if (instructionList[i].opcode == OpCodes.Ldarg_2)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0) { labels = instructionList[i].labels };
                    yield return new CodeInstruction(OpCodes.Dup);
                    yield return new CodeInstruction(OpCodes.Ldfld, FI_GlowGrid_map);
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldloca, 0);
                    yield return new CodeInstruction(OpCodes.Call, MI_CheckTransparency);
                    Label @continue = il.DefineLabel();
                    yield return new CodeInstruction(OpCodes.Brfalse, @continue);
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Ret);
                    yield return new CodeInstruction(instructionList[i].opcode, instructionList[i].operand) { labels = { @continue } };
                    break;
                }
                yield return instructionList[i];
            }
            for (i += 1; i < instructionList.Count; i++) yield return instructionList[i]; // finish off instructions
        }

        public static void RoofLeavings(RoofGrid __instance, IntVec3 c, RoofDef def)
        {
            RoofDef curRoof = __instance.RoofAt(c);
            if (curRoof != null && def != curRoof)
            {
                RoofExtension roofExt = curRoof.GetModExtension<RoofExtension>();
                if (roofExt != null) TraspileHelper.DoLeavings(curRoof, roofExt.spawnerDef, FI_RoofGrid_map.GetValue(__instance) as Map, GenAdj.OccupiedRect(c, Rot4.North, roofExt.spawnerDef.size));
            }
        }

        public static IEnumerable<CodeInstruction> TransparentRoofLightingOverlayFix(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            MethodInfo MI_RoofAt = AccessTools.Method(typeof(RoofGrid), nameof(RoofGrid.RoofAt), new[] { typeof(int), typeof(int) });
            MethodInfo MI_SkipRoofRendering = AccessTools.Method(typeof(TraspileHelper), nameof(TraspileHelper.SkipRoofRendering));

            List<CodeInstruction> instructionList = instructions.ToList();
            for (int i = 0; i < instructionList.Count; i++)
            {
                yield return instructionList[i];
                if (instructionList[i].opcode == OpCodes.Callvirt && instructionList[i].operand == MI_RoofAt)
                {
                    // NOTE: consider finding a better way to locate this...
                    // make sure state by checking ops a few times
                    yield return instructionList[++i];
                    if (instructionList[i].opcode != OpCodes.Stloc_S) break;

                    yield return instructionList[++i];
                    if (instructionList[i].opcode != OpCodes.Ldloc_S) break;

                    CodeInstruction load = new CodeInstruction(instructionList[i].opcode, instructionList[i].operand);

                    yield return instructionList[++i];
                    if (instructionList[i].opcode != OpCodes.Brfalse) break;

                    yield return load;
                    yield return new CodeInstruction(OpCodes.Call, MI_SkipRoofRendering);
                    Label @continue = il.DefineLabel();
                    yield return new CodeInstruction(OpCodes.Brtrue, @continue);
                    while (instructionList[++i].opcode != OpCodes.Stloc_S) { yield return instructionList[i]; } // yield block
                    yield return instructionList[i++];
                    instructionList[i].labels.Add(@continue);
                    yield return instructionList[i];
                }
            }
        }

        public static IEnumerable<CodeInstruction> ThickRoofInfestationFix(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            MethodInfo MI_IsBuildableThickroof = AccessTools.Method(typeof(TraspileHelper), nameof(TraspileHelper.IsBuildableThickRoof));
            List<CodeInstruction> instructionList = instructions.ToList();
            int i;
            for (i = 0; i < instructionList.Count - 2; i++)
            {
                if (instructionList[i + 2].opcode == OpCodes.Ldc_I4_6)
                    break;
                yield return instructionList[i];
            }

            yield return new CodeInstruction(OpCodes.Ldarg_0); // cell
            yield return new CodeInstruction(OpCodes.Ldarg_1); // map
            yield return new CodeInstruction(OpCodes.Call, MI_IsBuildableThickroof);
            Label @continue = il.DefineLabel();
            yield return new CodeInstruction(OpCodes.Brfalse, @continue);
            yield return new CodeInstruction(OpCodes.Ldc_R4, 0f);
            yield return new CodeInstruction(OpCodes.Ret);
            instructionList[i].labels.Add(@continue);

            for (; i < instructionList.Count; i++)
                yield return instructionList[i];
        }
        
        public static void BuildingRepairedPostfix(Building b)
        {
            CompMaintainable comp = b.GetComp<CompMaintainable>();
            if (comp != null)
                comp.ticksSinceMaintain = 0;
        }

        public static IEnumerable<CodeInstruction> FixClearBuildingArea(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionList = instructions.ToList();

            int i, j;
            int insertIndex = 0, startIndex = 0, endIndex = 0 ;
            for (i = 0; i < instructionList.Count; i++)
            {
                if (startIndex == 0 && instructionList[i].opcode == OpCodes.Ldc_I4_3)
                {
                    j = i;
                    while (instructionList[--j].opcode != OpCodes.Ldarg_1) continue;
                    startIndex = j;
                    j = i;
                    while (instructionList[j++].opcode != OpCodes.Blt_Un) continue;
                    endIndex = j+4; // values and returns
                }
                if (insertIndex == 0 && instructionList[i].opcode == OpCodes.Stloc_1)
                {
                    j = i;
                    while (instructionList[--j].opcode != OpCodes.Ldloc_0) continue;
                    insertIndex = j;
                }
                if (insertIndex != 0 && startIndex != 0)
                    break;
            }

            int count = endIndex - startIndex;
            List<CodeInstruction> swapRange = instructionList.GetRange(startIndex, count);
            instructionList.RemoveRange(startIndex, count);

            // handle labels
            List<Label> swapLabels = swapRange[0].labels;
            swapRange[0].labels = instructionList[startIndex].labels;
            instructionList[startIndex].labels = swapLabels;

            instructionList.InsertRange(insertIndex - count, swapRange);

            return instructionList;
        }

    }
}
