using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using UnityEngine;
using Verse;
using Harmony;

namespace ExpandedRoofing
{
    class RoofGridCellBoolGiver
    {
        static readonly Color LightGray = new Color(0.4f, 0.4f, 0.4f);
        static readonly Color DarkGray = new Color(0.6f, 0.6f, 0.6f);
        static readonly Color LightGreen = new Color(0.3f, 1f, 0.4f); // default color

        static RoofGridCellBoolGiver()
        {
#if DEBUG
            HarmonyInstance.DEBUG = true;
#endif
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.whyisthat.expandedroofing.roofgridcellboolgiver");
            // Customize RoofGrid ICellBoolGiver
            harmony.Patch(AccessTools.Property(typeof(RoofGrid), nameof(RoofGrid.Color)).GetGetMethod(), new HarmonyMethod(typeof(RoofGridCellBoolGiver), nameof(RoofGridColorDetour)), null);
            harmony.Patch(AccessTools.Method(typeof(RoofGrid), nameof(RoofGrid.GetCellExtraColor)), null, null, new HarmonyMethod(typeof(RoofGridCellBoolGiver), nameof(RoofGridExtraColorTranspiler)));
        }

        static bool RoofGridColorDetour(RoofGrid __instance, ref Color __result)
        {
            __result = Color.white;
            return false;
        }

        // NOTE: detouring here via transpiler
        static IEnumerable<CodeInstruction> RoofGridExtraColorTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo MI_GetCellExtraColor = AccessTools.Method(typeof(RoofGridCellBoolGiver), nameof(RoofGridCellBoolGiver.GetCellExtraColor));
            List<CodeInstruction> instructionList = instructions.ToList();
            for (int i = 2; i < 6; i++)
                yield return instructionList[i];
            yield return new CodeInstruction(OpCodes.Call, MI_GetCellExtraColor);
            yield return new CodeInstruction(OpCodes.Ret);
        }

        static Color GetCellExtraColor(ushort roofCell)
        {
            if (roofCell == RimWorld.RoofDefOf.RoofConstructed.shortHash)
                return LightGreen; 
            if (roofCell == RoofDefOf.RoofTransparent.shortHash)
                return Color.yellow;
            if (roofCell == RoofDefOf.RoofSolar.shortHash)
                return Color.cyan;
            if (roofCell == RimWorld.RoofDefOf.RoofRockThin.shortHash)
                return LightGray; 
            if (roofCell == RimWorld.RoofDefOf.RoofRockThick.shortHash)
                return DarkGray;
            // Assuming all other roofs are ThickStoneRoof
            return Color.green;
        }
    }
}
