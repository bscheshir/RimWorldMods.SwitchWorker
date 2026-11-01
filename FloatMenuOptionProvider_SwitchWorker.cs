using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace SwitchWorker
{
    public class FloatMenuOptionProvider_SwitchWorker : FloatMenuOptionProvider
    {
        protected override bool Drafted => true;
        protected override bool Undrafted => true;
        protected override bool Multiselect => true;

        protected override bool AppliesInt(FloatMenuContext context)
        {
            return context?.FirstSelectedPawn != null && context.FirstSelectedPawn.Faction == Faction.OfPlayer;
        }

        protected override FloatMenuOption GetSingleOptionFor(Thing clickedThing, FloatMenuContext context)
        {
            if (!(clickedThing is UnfinishedThing targetThing) || targetThing.BoundBill == null)
            {
                return null;
            }

            Pawn pawn = context.FirstSelectedPawn;
            if (pawn == null || pawn.Map == null) return null;

            string label = SwitchWorkerCalculator.BuildWorkerLabel(targetThing, targetThing.BoundBill, targetThing.Creator, pawn, out string reason);

            Pawn localPawn = pawn;
            FloatMenuOption opt = null;

            if (reason == "SwitchWorker_CurrentCreator" || reason != null)
            {
                opt = new FloatMenuOption(label, null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                opt.Disabled = true;
            }
            else
            {
                opt = new FloatMenuOption(label, () =>
                {
                    int newWorkLeftTicks = SwitchWorkerCalculator.CalculateNewWorkLeft(targetThing, targetThing.BoundBill, targetThing.Creator, localPawn);
                    targetThing.Creator = localPawn;
                    targetThing.workLeft = newWorkLeftTicks;
                    SoundDefOf.Tick_High.PlayOneShotOnCamera();
                }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);

                opt.Disabled = false;
            }

            if (opt != null)
            {
                opt.extraPartWidth = 32f;
                opt.extraPartOnGUI = (Rect rect) =>
                {
                    Rect iconRect = new Rect(rect.x + 4f, rect.y + 4f, 24f, 24f);
                    Widgets.ThingIcon(iconRect, localPawn);
                    return false;
                };

                return FloatMenuUtility.DecoratePrioritizedTask(opt, localPawn, targetThing);
            }

            return null;
        }
    }
}
