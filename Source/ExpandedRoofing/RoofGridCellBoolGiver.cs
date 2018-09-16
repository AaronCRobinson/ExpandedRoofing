using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using UnityEngine;
using Verse;
using Harmony;

namespace ExpandedRoofing
{
    [StaticConstructorOnStartup]
    public class RoofGridCellBoolGiver
    {
        static readonly Color ThinRockRoofColor = new Color(0.6f, 0.6f, 0.6f);
        static readonly Color ThickRockRoofColor = new Color(0.75f, 0.375f, 0.25f);
        static readonly Color LightGreen = new Color(0.3f, 1f, 0.4f); // default color

        static RoofGridCellBoolGiver()
        {
#if DEBUG
            HarmonyInstance.DEBUG = true;
#endif
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.whyisthat.expandedroofing.roofgridcellboolgiver");
            // Customize RoofGrid ICellBoolGiver
            harmony.Patch(AccessTools.Property(typeof(RoofGrid), nameof(RoofGrid.Color)).GetGetMethod(), new HarmonyMethod(typeof(RoofGridCellBoolGiver), nameof(RoofGridColorDetour)), null);
            harmony.Patch(AccessTools.Method(typeof(RoofGrid), nameof(RoofGrid.GetCellExtraColor)), null, null, new HarmonyMethod(typeof(RoofGridCellBoolGiver).GetMethod(nameof(RoofGridCellBoolGiver.RoofGridExtraColorDetour))));
        }

        public static bool RoofGridColorDetour(RoofGrid __instance, ref Color __result)
        {
            __result = Color.white;
            return false;
        }

        // NOTE: detouring here via transpiler
        public static IEnumerable<CodeInstruction> RoofGridExtraColorDetour(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            FieldInfo FI_RoofGrid_roofGrid = AccessTools.Field(typeof(RoofGrid), "roofGrid");
            MethodInfo MI_GetCellExtraColor = typeof(RoofGridCellBoolGiver).GetMethod(nameof(RoofGridCellBoolGiver.GetCellExtraColor));
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Ldfld, FI_RoofGrid_roofGrid);
            yield return new CodeInstruction(OpCodes.Ldarg_1);
            yield return new CodeInstruction(OpCodes.Ldelem_Ref);
            yield return new CodeInstruction(OpCodes.Call, MI_GetCellExtraColor);
            yield return new CodeInstruction(OpCodes.Ret);
        }

        public static Color GetCellExtraColor(RoofDef roofCell)
        {
            if (roofCell == RimWorld.RoofDefOf.RoofConstructed)
            {
                return LightGreen;
            }

            else if (roofCell == RoofDefOf.RoofTransparent)
            {
                return Color.yellow;
            }

            else if (roofCell == RoofDefOf.RoofSolar)
            {
                return Color.cyan;
            }

            else if (roofCell == RimWorld.RoofDefOf.RoofRockThin)
            {
                return ThinRockRoofColor;
            }

            else if (roofCell == RimWorld.RoofDefOf.RoofRockThick)
            {
                return ThickRockRoofColor;
            }
            // Assuming all other roofs are ThickStoneRoof
            return Color.magenta;
        }
    }
}
