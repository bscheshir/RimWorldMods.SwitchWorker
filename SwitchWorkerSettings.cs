using Verse;

namespace SwitchWorker
{
    public class SwitchWorkerSettings : ModSettings
    {
        public float workLossMultiplier = 0.25f;
        public float skillInfluenceMultiplier = 0.75f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref workLossMultiplier, "workLossMultiplier", 0.25f);
            Scribe_Values.Look(ref skillInfluenceMultiplier, "skillInfluenceMultiplier", 0.75f);
        }
    }
}
