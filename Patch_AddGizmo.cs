using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace SwitchWorker
{
    [HarmonyPatch(typeof(UnfinishedThing), nameof(UnfinishedThing.GetGizmos))]
    public static class Patch_AddGizmo
    {
        [HarmonyPostfix]
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, UnfinishedThing __instance)
        {
            foreach (Gizmo gizmo in __result)
            {
                yield return gizmo;
            }

            if (__instance != null && !__instance.Destroyed && __instance.Recipe != null)
            {
                yield return new Command_SwitchWorker(__instance);
            }
        }
    }
}
