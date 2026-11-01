using RimWorld;
using UnityEngine;
using Verse;

namespace SwitchWorker
{
    public class SwitchWorkerMod : Mod
    {
        public static SwitchWorkerSettings Settings { get; private set; }

        public SwitchWorkerMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<SwitchWorkerSettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            SwitchWorkerSettingsUI.DrawSettings(inRect);
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory() => "SwitchWorker_ModName".Translate();
    }
}
