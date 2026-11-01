using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
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

            Pawn currentCreator = targetThing.Creator;
            Bill_ProductionWithUft bill = targetThing.BoundBill;
            SkillDef skillDef = bill.recipe.workSkill;

            string nameString = pawn.LabelShortCap;
            if (pawn.story != null && !pawn.story.TitleShort.NullOrEmpty())
            {
                nameString += $", {pawn.story.TitleShort}";
            }

            if (pawn.IsSlave)
            {
                nameString = $"<color=#E5C565>{nameString}</color>";
            }

            string skillInfo = "";
            if (skillDef != null && pawn.skills != null)
            {
                skillInfo = $" [{skillDef.LabelCap}: {pawn.skills.GetSkill(skillDef).Level}]";
            }

            Pawn localPawn = pawn;
            string reason = SwitchWorkerCalculator.GetWorkerBlockReason(targetThing, bill, currentCreator, pawn);

            FloatMenuOption opt = null;

            if (reason == "SwitchWorker_CurrentCreator")
            {
                int currentWorkLeftSeconds = Mathf.CeilToInt(targetThing.workLeft / 60f);
                string currentCreatorLocalized = $"<color=#68BB68>{"SwitchWorker_CurrentCreator".Translate()}</color>";

                // Перенос \n разделяет информацию на две строки
                string label = $"{nameString}{skillInfo}\n({currentCreatorLocalized}: {currentWorkLeftSeconds} ед.)";

                opt = new FloatMenuOption(label, null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                opt.Disabled = true;
            }
            else if (reason == null)
            {
                int newWorkLeftTicks = SwitchWorkerCalculator.CalculateNewWorkLeft(targetThing, bill, currentCreator, pawn);
                int newWorkLeftSeconds = Mathf.CeilToInt((float)newWorkLeftTicks / 60f);

                // Перенос \n перед текстом остатка работы для компактности интерфейса
                string label = "SwitchWorker_ButtonLabel".Translate() + $" {pawn.LabelShortCap}\n({"SwitchWorker_ResultWorkLeft".Translate(newWorkLeftSeconds)})";

                opt = new FloatMenuOption(label, () =>
                {
                    targetThing.Creator = localPawn;
                    targetThing.workLeft = newWorkLeftTicks;
                    SoundDefOf.Tick_High.PlayOneShotOnCamera();
                }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);

                opt.Disabled = false;
            }
            else
            {
                // Перенос \n перед выводом причин блокировок
                string label = $"{nameString}{skillInfo}\n({reason})";
                opt = new FloatMenuOption(label, null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                opt.Disabled = true;
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
