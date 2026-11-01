using HarmonyLib;
using System.Reflection;
using UnityEngine;
using Verse;

namespace SwitchWorker
{
    [StaticConstructorOnStartup]
    public static class HarmonyInitializer
    {
        static HarmonyInitializer()
        {
            var harmony = new Harmony("com.BSCheshir.SwitchWorker");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Log.Message("SwitchWorker_LogCoreInit".Translate());
        }
    }

    [StaticConstructorOnStartup]
    public static class SwitchWorkerAssets
    {
        // Соответствует пути: Textures/UI/Icons/SwitchIcon.png
        public static readonly Texture2D SwitchIcon = ContentFinder<Texture2D>.Get("UI/Icons/SwitchIcon", true);
    }
}
