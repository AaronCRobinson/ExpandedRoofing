using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using Harmony;

namespace ExpandedRoofing
{
    static class Helper
    {
        static float num;
        static public float CheckTransparency(GlowGrid gg, Map map, IntVec3 c)
        {
            num = 0;
            TransparentRoofExtension transparentRoofExt = map.roofGrid.RoofAt(c)?.GetModExtension<TransparentRoofExtension>();
            if (transparentRoofExt != null)
            {
                num = map.skyManager.CurSkyGlow * transparentRoofExt.transparency;
                if (num == 1f) return num;
            }
            return num;
        }

    }

    [StaticConstructorOnStartup]
    internal class HarmonyPatches
    {
        static MethodInfo MI_CheckTransparency = AccessTools.Method(typeof(Helper), nameof(Helper.CheckTransparency));
        static FieldInfo FI_GlowGrid_map = AccessTools.Field(typeof(GlowGrid), "map");

        static HarmonyPatches()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.whyisthat.expandedroofing.main");
            harmony.Patch(AccessTools.Method(typeof(GlowGrid), nameof(GlowGrid.GameGlowAt)), null, null, new HarmonyMethod(typeof(HarmonyPatches), nameof(GameGlowTranspiler)));
        }

        public static IEnumerable<CodeInstruction> GameGlowTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            bool oneTime = true;

            List<CodeInstruction> instructionList = instructions.ToList();
            int i;
            for (i = 0; i < instructionList.Count; i++)
            {
                yield return instructionList[i];
                if (instructionList[i].opcode == OpCodes.Ret && oneTime)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0) { labels = instructionList[++i].labels };
                    yield return new CodeInstruction(OpCodes.Dup);
                    yield return new CodeInstruction(OpCodes.Ldfld, FI_GlowGrid_map);
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Call, MI_CheckTransparency);
                    yield return new CodeInstruction(OpCodes.Stloc_0);
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Ldc_R4, 1f);
                    Label @continue = il.DefineLabel();
                    yield return new CodeInstruction(OpCodes.Bne_Un, @continue);
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Ret);
                    yield return new CodeInstruction(instructionList[i].opcode, instructionList[i].operand) { labels = { @continue } };
                    break;
                }
            }
            for (i += 1 ; i < instructionList.Count; i++) yield return instructionList[i]; // finish off instructions
        }

    }
}
