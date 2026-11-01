using UnityEngine;
using Verse;

namespace SwitchWorker
{
    public static class SwitchWorkerSettingsUI
    {
        public static void DrawSettings(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            // Первый ползунок (Прямая потеря)
            listing.Label("SwitchWorker_WorkLossMultiplier".Translate((SwitchWorkerMod.Settings.workLossMultiplier * 100f).ToString("F0")));
            SwitchWorkerMod.Settings.workLossMultiplier = listing.Slider(SwitchWorkerMod.Settings.workLossMultiplier, 0.0f, 0.9f);

            listing.Gap();

            // Второй ползунок (Влияние навыка)
            listing.Label("SwitchWorker_SkillInfluenceMultiplier".Translate((SwitchWorkerMod.Settings.skillInfluenceMultiplier * 100f).ToString("F0")));
            SwitchWorkerMod.Settings.skillInfluenceMultiplier = listing.Slider(SwitchWorkerMod.Settings.skillInfluenceMultiplier, 0.0f, 1.0f);

            listing.End();
        }
    }
}
