using System;
using System.Reflection;
using Verse;

namespace ExpandedRoofing
{
    // https://github.com/UnlimitedHugs/RimworldHugsLib/blob/62e1a12ff65f3a9cfcd230a0113848d92eabb99c/Source/Utils/InjectedDefHasher.cs
    public static class InjectedDefHasher
    {
        private delegate void GiveShortHash(Def def, Type defType);
        private static GiveShortHash giveShortHashDelegate;

        internal static void PrepareReflection()
        {
            var methodInfo = typeof(ShortHashGiver).GetMethod("GiveShortHash", BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(Def), typeof(Type) }, null);
            if (methodInfo == null)
            {
                //HugsLibController.Logger.Error("Failed to reflect ShortHashGiver.GiveShortHash");
                return;
            }
            giveShortHashDelegate = (GiveShortHash)Delegate.CreateDelegate(typeof(GiveShortHash), methodInfo);
        }

        public static void GiveShortHasToDef(Def newDef, Type defType)
        {
            if (giveShortHashDelegate == null) throw new Exception("Hasher not initalized");
            giveShortHashDelegate(newDef, defType);
        }
    }
}
