using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace SwitchWorker
{
    public class Command_SwitchWorker : Command
    {
        private readonly UnfinishedThing thing;

        public Command_SwitchWorker(UnfinishedThing thing)
        {
            this.thing = thing;
            this.defaultLabel = "SwitchWorker_ButtonLabel".Translate();
            this.defaultDesc = "SwitchWorker_ButtonDesc".Translate();
            this.icon = SwitchWorkerAssets.SwitchIcon;
            this.disabled = thing?.BoundBill == null || thing.Map == null;
        }

        public override void ProcessInput(Event ev)
        {
            base.ProcessInput(ev);

            Bill_ProductionWithUft bill = thing.BoundBill;
            Pawn currentCreator = thing.Creator;
            Map map = thing.Map;

            WorkTypeDef workType = BillUtility.GetWorkgiver(bill.billStack.billGiver).workType;
            SkillDef skillDef = bill.recipe.workSkill;
            int minSkill = bill.recipe.skillRequirements?.FirstOrDefault(x => x.skill == skillDef)?.minLevel ?? 0;

            List<FloatMenuOption> availableWorkers = new List<FloatMenuOption>();
            List<FloatMenuOption> currentWorkerOption = new List<FloatMenuOption>();
            List<FloatMenuOption> unavailableWorkers = new List<FloatMenuOption>();

            float workLeft = thing.workLeft;

            // Получение уровня навыка пешки
            int GetPawnSkill(Pawn p)
            {
                if (skillDef != null && p.skills != null)
                {
                    return p.skills.GetSkill(skillDef).Level;
                }
                return 0;
            }

            // Получение индекса отображения на панели колонистов
            int GetColonistBarOrder(Pawn p)
            {
                if (p.playerSettings != null)
                {
                    return p.playerSettings.displayOrder;
                }
                return 0;
            }

            // Сортировка по убыванию навыка, затем по порядку на панели
            var sortedPawns = map.mapPawns.FreeColonists
                .OrderByDescending(p => GetPawnSkill(p))
                .ThenBy(p => GetColonistBarOrder(p));

            foreach (Pawn pawn in sortedPawns)
            {
                // Стандартное ванильное имя (Кличка)
                string nameString = pawn.LabelShortCap;

                // Добавление краткой профессии
                if (pawn.story != null && !pawn.story.TitleShort.NullOrEmpty())
                {
                    nameString += $", {pawn.story.TitleShort}";
                }

                // Цветовое выделение рабов (желтый цвет)
                if (pawn.IsSlave)
                {
                    nameString = $"<color=#E5C565>{nameString}</color>";
                }

                // Формирование строки навыка
                string skillInfo = "";
                if (skillDef != null && pawn.skills != null)
                {
                    skillInfo = $" [{skillDef.LabelCap}: {pawn.skills.GetSkill(skillDef).Level}]";
                }

                Pawn localPawn = pawn;

                if (pawn == currentCreator)
                {
                    int currentWorkLeftSeconds = Mathf.CeilToInt(workLeft / 60f);

                    // Выделяем зеленым цветом (#68BB68) только само словосочетание "Текущий мастер"
                    string currentCreatorLocalized = $"<color=#68BB68>{"SwitchWorker_CurrentCreator".Translate()}</color>";
                    string currentLabel = $"{nameString}{skillInfo} ({currentCreatorLocalized}: {currentWorkLeftSeconds} ед.)";

                    var opt = new FloatMenuOption(currentLabel, null, localPawn, Color.white, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                    opt.Disabled = true;
                    currentWorkerOption.Add(opt);
                    continue;
                }

                // Проверка доступности пешки
                string reason = null;
                if (pawn.Downed || pawn.InMentalState || pawn.Dead)
                {
                    reason = "SwitchWorker_CantWork_Incapacitated".Translate();
                }
                else if (pawn.workSettings != null && !pawn.workSettings.WorkIsActive(workType))
                {
                    reason = "SwitchWorker_CantWork_WorkDisabled".Translate();
                }
                else if (skillDef != null && pawn.skills != null && pawn.skills.GetSkill(skillDef).Level < minSkill)
                {
                    reason = "SwitchWorker_CantWork_LowSkill".Translate(minSkill, pawn.skills.GetSkill(skillDef).Level);
                }
                else if (!pawn.CanReach(thing, PathEndMode.Touch, Danger.Some))
                {
                    reason = "SwitchWorker_CantWork_NoPath".Translate();
                }

                // Распределение по спискам доступности
                if (reason == null)
                {
                    int newWorkLeftTicks = SwitchWorkerCalculator.CalculateNewWorkLeft(thing, bill, currentCreator, pawn);
                    int newWorkLeftSeconds = Mathf.CeilToInt((float)newWorkLeftTicks / 60f);

                    string label = $"{nameString}{skillInfo} ({"SwitchWorker_ResultWorkLeft".Translate(newWorkLeftSeconds)})";

                    var opt = new FloatMenuOption(label, () =>
                    {
                        thing.Creator = localPawn;
                        thing.workLeft = newWorkLeftTicks;
                        SoundDefOf.Tick_High.PlayOneShotOnCamera();
                    }, localPawn, Color.white, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);

                    availableWorkers.Add(opt);
                }
                else
                {
                    string label = $"{nameString}{skillInfo} ({reason})";

                    var opt = new FloatMenuOption(label, null, localPawn, Color.white, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                    opt.Disabled = true;
                    unavailableWorkers.Add(opt);
                }
            }

            // Формирование и вывод итогового списка
            List<FloatMenuOption> finalMenu = new List<FloatMenuOption>();
            finalMenu.AddRange(availableWorkers);
            finalMenu.AddRange(currentWorkerOption);
            finalMenu.AddRange(unavailableWorkers);

            if (finalMenu.Any())
            {
                Find.WindowStack.Add(new FloatMenu(finalMenu));
            }
        }
    }
}
