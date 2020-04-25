using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using RimWorld;
using HarmonyLib;

namespace ExpandedRoofing
{

    static class TranspileHelper
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

            List<ThingDefCountClass> thingCounts = spawnerDef.CostListAdjusted(stuff, true);

            foreach (ThingDefCountClass curCntCls in thingCounts)
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

        /*public static bool GridsUtilityRoofedFix(IntVec3 c, Map map)
        {
            RoofDef roofDef = map.roofGrid.RoofAt(c);
            if (roofDef != null && roofDef != RoofDefOf.RoofTransparent)
                return true;
            return false;
        }*/

        // NOTE: do not need to check if `isThickRoof` b\c we already know it is
        // TODO: look at consolidating this method
        public static bool IsBuildableThickRoof(IntVec3 cell, Map map) => (cell.GetRoof(map) != RimWorld.RoofDefOf.RoofRockThick);

        // TODO: the current fix here does not reduce light that is traveling through transparet roofs
        public static void FixRoofedPowerOutputFactor(CompPowerPlantSolar comp, IntVec3 c, ref int coveredCells)
        {
            RoofDef roofDef = comp.parent.Map.roofGrid.RoofAt(c);
            if (roofDef != null && roofDef != RoofDefOf.RoofTransparent)
                ++coveredCells;
        }
    }

    [StaticConstructorOnStartup]
    internal class HarmonyPatches
    {
        public static FieldInfo FI_RoofGrid_map = AccessTools.Field(typeof(RoofGrid), "map");
        public static FieldInfo FI_RoofGrid_roofGrid = AccessTools.Field(typeof(RoofDef[]), "roofGrid");

        private static int SectionLayer_LightingOverlay__Regenerate__RoofDef__LocalIndex = AccessTools.Method(typeof(SectionLayer_LightingOverlay), nameof(SectionLayer_LightingOverlay.Regenerate)).GetMethodBody().LocalVariables.First(lvi => lvi.LocalType == typeof(RoofDef)).LocalIndex;

        static HarmonyPatches()
        {
#if DEBUG
            Harmony.DEBUG = true;
#endif
            Harmony harmony = new Harmony("rimworld.whyisthat.expandedroofing.main");

            // correct lighting for plant growth
#if DEBUG
            Log.Message("correct lighting for plant growth");
#endif
            harmony.Patch(AccessTools.Method(typeof(GlowGrid), nameof(GlowGrid.GameGlowAt)), null, null, new HarmonyMethod(typeof(HarmonyPatches).GetMethod(nameof(PlantLightingFix))));

            // set roof to return materials
#if DEBUG
            Log.Message("set roof to return materials");
#endif
            harmony.Patch(AccessTools.Method(typeof(RoofGrid), nameof(RoofGrid.SetRoof)), new HarmonyMethod(typeof(HarmonyPatches), nameof(SetRoof_Prefix)), null);

            // fix lighting inside rooms with transparent roof  
#if DEBUG
            Log.Message("fix lighting inside rooms with transparent roof");
#endif
            harmony.Patch(AccessTools.Method(typeof(SectionLayer_LightingOverlay), nameof(SectionLayer_LightingOverlay.Regenerate)), null, null, new HarmonyMethod(typeof(HarmonyPatches), nameof(TransparentRoofLightingOverlayFix)));

            //harmony.Patch(AccessTools.Method(AccessTools.TypeByName("SectionLayer_IndoorMask"), "Regenerate"), null, null, new HarmonyMethod(typeof(HarmonyPatches), nameof(TransparentRoofIndoorMaskFix)));

            harmony.Patch(AccessTools.Method(typeof(RoofGrid), nameof(RoofGrid.Roofed), new Type[] { typeof(int) }), null, new HarmonyMethod(typeof(HarmonyPatches), nameof(Roofed_Postfix)));

            // Fix infestation under buildable thick roofs
#if DEBUG
            Log.Message("Fix infestation under buildable thick roofs");
#endif
            harmony.Patch(AccessTools.Method(typeof(InfestationCellFinder), "GetScoreAt"), null, null, new HarmonyMethod(typeof(HarmonyPatches), nameof(ThickRoofInfestationFix)));

            // Reset CompMaintainable when building repaired
#if DEBUG
            Log.Message("Reset CompMaintainable when building repaired");
#endif
            harmony.Patch(AccessTools.Method(typeof(ListerBuildingsRepairable), nameof(ListerBuildingsRepairable.Notify_BuildingRepaired)), null, new HarmonyMethod(typeof(HarmonyPatches), nameof(BuildingRepaired_Postfix)));

            // Set clearBuildingArea flag in BlocksConstruction to be respected before large plant check (trees mostly)
#if DEBUG
            Log.Message("Set clearBuildingArea flag in BlocksConstruction to be respected before large plant check (trees mostly)");
#endif
            harmony.Patch(AccessTools.Method(typeof(GenConstruct), nameof(GenConstruct.BlocksConstruction)), null, null, new HarmonyMethod(typeof(HarmonyPatches), nameof(FixClearBuildingArea)));

            harmony.Patch(AccessTools.Property(typeof(CompPowerPlantSolar), "RoofedPowerOutputFactor").GetGetMethod(true), null, null, new HarmonyMethod(typeof(HarmonyPatches), nameof(TransparentRoofOutputFactorFix)));

            // NOTE: look for a better injection point
            harmony.Patch(AccessTools.Method(typeof(Game), nameof(Game.FinalizeInit)), null, new HarmonyMethod(typeof(HarmonyPatches), nameof(GameInited)));
#if DEBUG
            Harmony.DEBUG = false;
#endif
        }

        public static IEnumerable<CodeInstruction> PlantLightingFix(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            FieldInfo FI_GlowGrid_map = AccessTools.Field(typeof(GlowGrid), "map");
            MethodInfo MI_CheckTransparency = AccessTools.Method(typeof(TranspileHelper), nameof(TranspileHelper.CheckTransparency));

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

        public static bool SetRoof_Prefix(RoofGrid __instance, RoofDef[] ___roofGrid, IntVec3 c, RoofDef def)
        {
            RoofDef curRoof = __instance.RoofAt(c);
            Map map = FI_RoofGrid_map.GetValue(__instance) as Map;

            if (curRoof == def) return false;

            // handle lighting special case
            if (def == RoofDefOf.RoofTransparent)
            {
                Log.Message("Transparent roof being set");
                if (curRoof != null)
                    __instance.SetRoof(c, null);
                //RoofDef[] roofGrid = FI_RoofGrid_roofGrid.GetValue(__instance) as RoofDef[];
                Log.Message(___roofGrid.ToString());
                ___roofGrid[map.cellIndices.CellToIndex(c)] = def; // set roof
                return false;
            }

            // handle roof leavings
            if (curRoof != null)
            {
                RoofExtension roofExt = curRoof.GetModExtension<RoofExtension>();
                if (roofExt != null)
                    TranspileHelper.DoLeavings(curRoof, roofExt.spawnerDef, map, GenAdj.OccupiedRect(c, Rot4.North, roofExt.spawnerDef.size));

                if (curRoof == RoofDefOf.RoofSolar) // removing solar roofing
                    map.GetComponent<SolarRoofing_MapComponent>().tracker.RemoveSolarCell(c);
            }

            if (def == RoofDefOf.RoofSolar) // adding solar roofing
                map.GetComponent<SolarRoofing_MapComponent>().tracker.AddSolarCell(c);

            return true;
        }

        public static IEnumerable<CodeInstruction> TransparentRoofLightingOverlayFix(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            //MethodInfo MI_RoofAt = AccessTools.Method(typeof(RoofGrid), nameof(RoofGrid.RoofAt), new[] { typeof(int), typeof(int) });
            MethodInfo MI_SkipRoofRendering = AccessTools.Method(typeof(TranspileHelper), nameof(TranspileHelper.SkipRoofRendering));

            List<CodeInstruction> instructionList = instructions.ToList();
            for (int i = 0; i < instructionList.Count; i++)
            {
                yield return instructionList[i];
                if (instructionList[i].opcode == OpCodes.Stloc_S && instructionList[i].operand is LocalBuilder lb && lb.LocalIndex == SectionLayer_LightingOverlay__Regenerate__RoofDef__LocalIndex)
                {
                    // make sure state by checking ops a few times
                    yield return instructionList[++i];
                    if (instructionList[i].opcode != OpCodes.Ldloc_S) break;

                    CodeInstruction load = new CodeInstruction(instructionList[i].opcode, instructionList[i].operand);

                    yield return instructionList[++i];
                    if (instructionList[i].opcode != OpCodes.Brfalse_S) break;
#if DEBUG
                    Log.Message("Patching TransparentRoof");
#endif
                    yield return load;
                    yield return new CodeInstruction(OpCodes.Call, MI_SkipRoofRendering);

                    Label @continue = il.DefineLabel();
                    yield return new CodeInstruction(OpCodes.Brtrue_S, @continue);


                    //int start = ++i;
                    
                    
                    //for (int j = 0; j < 3; j++) // eat three blocks (uhm! cookie!)
                    while (instructionList[++i].opcode != OpCodes.Stloc_S) { yield return instructionList[i]; } // yield block


                    /*while (instructionList[++i].opcode != OpCodes.Stloc_S) { } // look for label
                    while (instructionList[++i].opcode != OpCodes.Brtrue_S) { } // look for label

                    Label l = (Label)instructionList[i].operand;

                    int end = i;

                    yield return new CodeInstruction(OpCodes.Brtrue_S, l);
                    for (int j = start; j < end; j++)
                    {
                        yield return instructionList[j];
                    }*/

                    yield return instructionList[i++];
                    instructionList[i].labels.Add(@continue);
                    yield return instructionList[i];
                }
            }
        }

        /*public static IEnumerable<CodeInstruction> TransparentRoofIndoorMaskFix(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo MI_Roofed = AccessTools.Method(typeof(GridsUtility), nameof(GridsUtility.Roofed));
            MethodInfo MI_GridsUtilityRoofedFix = AccessTools.Method(typeof(TraspileHelper), nameof(TraspileHelper.GridsUtilityRoofedFix));

            List<CodeInstruction> instructionList = instructions.ToList();
            int i;
            for (i = 0; i < instructionList.Count; i++)
            {
                if (instructionList[i].opcode == OpCodes.Call && instructionList[i].operand is MethodInfo mi && mi == MI_Roofed)
                {
                    Log.Message("Patching SectionLayer_IndoorMask");
                    yield return new CodeInstruction(OpCodes.Call, MI_GridsUtilityRoofedFix);
                }
                else
                    yield return instructionList[i];

            }
            for (i += 1; i < instructionList.Count; i++) yield return instructionList[i]; // finish off instructions
        }*/


        public static void Roofed_Postfix(RoofGrid __instance, ref bool __result, int index)
        {
            if (__instance.RoofAt(index) == RoofDefOf.RoofTransparent)
                __result = false;
        }

        public static IEnumerable<CodeInstruction> ThickRoofInfestationFix(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            MethodInfo MI_IsBuildableThickroof = AccessTools.Method(typeof(TranspileHelper), nameof(TranspileHelper.IsBuildableThickRoof));
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
        
        public static void BuildingRepaired_Postfix(Building b)
        {
            CompMaintainable comp = b.GetComp<CompMaintainable>();
            if (comp != null)
                comp.ticksSinceMaintain = 0;
        }

        // NOTE: this method is getting mighty ugly...
        public static IEnumerable<CodeInstruction> FixClearBuildingArea(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionList = instructions.ToList();

            int j;
            int insertIndex = 0, startIndex = 0, endIndex = 0 ;
#if DEBUG
            Log.Message($"instructions.Count -> {instructionList.Count}");
#endif 
            for (int i = 0; i < instructionList.Count; i++)
            {
                if (startIndex == 0 && instructionList[i].opcode == OpCodes.Ldc_I4_4)
                {
#if DEBUG
                    Log.Message($"Setting -> startIndex, endIndex @ {i}");
#endif 
                    j = i;
                    while (instructionList[--j].opcode != OpCodes.Ldarg_1) continue;
                    startIndex = j;
#if DEBUG
                    Log.Message($"startIndex: {startIndex}");
#endif 
                    j = i;
                    while (instructionList[j++].opcode != OpCodes.Ble_Un_S) continue;
                    j = j+4; // values and returns
                    // keep going...
                    while (instructionList[j++].opcode != OpCodes.Ldloc_0) continue;
                    endIndex = j-1; // values and returns
#if DEBUG
                    Log.Message($"endIndex: {endIndex}");
#endif 
                }
                if (insertIndex == 0 && instructionList[i].opcode == OpCodes.Stloc_1) // entityDefToBuild
                {
#if DEBUG
                    Log.Message($"Setting -> insertIndex @ {i}");
#endif 
                    j = i;
                    while (instructionList[--j].opcode != OpCodes.Ldloc_0) continue;
                    insertIndex = j;
#if DEBUG
                    Log.Message($"insertIndex: {insertIndex}");
#endif 
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

#if DEBUG
            Log.Message($"Insert Location: {instructionList[insertIndex - count]}");
#endif 

            instructionList.InsertRange(insertIndex - count, swapRange);

            return instructionList;
        }

        public static IEnumerable<CodeInstruction> TransparentRoofOutputFactorFix(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo MI_FixRoofedPowerOutputFactor = AccessTools.Method(typeof(TranspileHelper), nameof(TranspileHelper.FixRoofedPowerOutputFactor));
            List<CodeInstruction> instructionList = instructions.ToList();
            bool skipping = false;
            for (int i = 0; i < instructionList.Count; i++)
            {
                if (skipping)
                {
                    if (instructionList[i].opcode == OpCodes.Add && instructionList[i + 1].opcode == OpCodes.Stloc_1)
                    {
                        i++;
                        skipping = false;
                    }
                    continue;
                }
                else if (instructionList[i].opcode == OpCodes.Add && instructionList[i + 1].opcode == OpCodes.Stloc_0)
                {
                    yield return instructionList[i++];
                    yield return instructionList[i++];
                    yield return new CodeInstruction(OpCodes.Ldarg_0); // this (CompPowerPlantSolar)
                    yield return new CodeInstruction(OpCodes.Ldloc_2); // c
                    yield return new CodeInstruction(OpCodes.Ldloca, 1); // num2
                    yield return new CodeInstruction(OpCodes.Call, MI_FixRoofedPowerOutputFactor);
                    skipping = true;
                }
                else
                    yield return instructionList[i];
                
            }

        }

        public static void GameInited()
        {
            // Handle disabling maintenance
            if (!ExpandedRoofingMod.settings.roofMaintenance)
            {
                MethodInfo MI_DefDatabase_Remove = AccessTools.Method(typeof(DefDatabase<JobDef>), "Remove");
                MI_DefDatabase_Remove.Invoke(null, new object[] { JobDefOf.PerformRoofMaintenance });
            }

            // Handle Glass+Lights intergration
            if (ExpandedRoofingMod.GlassLights)
            {
                MethodInfo MI_DefDatabase_Remove = AccessTools.Method(typeof(DefDatabase<ThingDef>), "Remove");
                ThingDef glassDef = DefDatabase<ThingDef>.GetNamed("Glass"); //TODO
                ThingDef framingDef = ThingDefOf.RoofTransparentFraming;

                // error check
                if (glassDef == null)
                {
                    Log.Error("ExpandedRoofing: Error with configuring defs with Glass+Lights");
                    return;
                }

                MI_DefDatabase_Remove.Invoke(null, new object[] { ThingDefOf.RoofTransparentFraming });
                framingDef.costList = new List<ThingDefCountClass>() { new ThingDefCountClass(glassDef, 1) };
                DefDatabase<ThingDef>.Add(framingDef);

                Log.Message("ExpandedRoofing: Glass+Lights configuration done.");

                // TODO: easy way to avoid redoing this op. (fix this)
                ExpandedRoofingMod.GlassLights = true;
            }
           
        }

    }
}
