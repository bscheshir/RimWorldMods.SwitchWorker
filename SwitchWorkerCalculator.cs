using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace SwitchWorker
{
    public static class SwitchWorkerCalculator
    {
        // Единая точка сборки двухстрочного текста для гизмо и ПКМ
        public static string BuildWorkerLabel(UnfinishedThing thing, Bill_ProductionWithUft bill, Pawn currentCreator, Pawn pawn, out string reason)
        {
            SkillDef skillDef = bill.recipe.workSkill;

            // Базовая строка: Кличка, Профессия
            string text = pawn.LabelShortCap;
            if (pawn.story != null && !pawn.story.TitleShort.NullOrEmpty())
            {
                text += $", {pawn.story.TitleShort}";
            }

            // Окрашивание раба в ванильный желтый цвет
            if (pawn.IsSlave)
            {
                text = $"<color=#E5C565>{text}</color>";
            }

            // Добавление навыка [Ремесло: Х]
            if (skillDef != null && pawn.skills != null)
            {
                text += $" [{skillDef.LabelCap}: {pawn.skills.GetSkill(skillDef).Level}]";
            }

            reason = GetWorkerBlockReason(thing, bill, currentCreator, pawn);

            if (reason == "SwitchWorker_CurrentCreator")
            {
                int currentWorkLeftSeconds = Mathf.CeilToInt(thing.workLeft / 60f);
                string currentCreatorLocalized = $"<color=#68BB68>{"SwitchWorker_CurrentCreator".Translate()}</color>";
                string workLeftLocalized = "SwitchWorker_ResultWorkLeft".Translate(currentWorkLeftSeconds);

                return $"{text}\n({currentCreatorLocalized}: {workLeftLocalized})";
            }

            if (reason == null)
            {
                int newWorkLeftTicks = CalculateNewWorkLeft(thing, bill, currentCreator, pawn);
                int newWorkLeftSeconds = Mathf.CeilToInt((float)newWorkLeftTicks / 60f);
                string workLeftLocalized = "SwitchWorker_ResultWorkLeft".Translate(newWorkLeftSeconds);

                return $"{text}\n({workLeftLocalized})";
            }

            // Если пешка недоступна — причина выводится на второй строке
            return $"{text}\n({reason})";
        }

        // Единые правила проверки доступности
        public static string GetWorkerBlockReason(UnfinishedThing thing, Bill_ProductionWithUft bill, Pawn currentCreator, Pawn pawn)
        {
            if (pawn == currentCreator) return "SwitchWorker_CurrentCreator";

            WorkTypeDef workType = BillUtility.GetWorkgiver(bill.billStack.billGiver).workType;
            SkillDef skillDef = bill.recipe.workSkill;
            int minSkill = bill.recipe.skillRequirements?.FirstOrDefault(x => x.skill == skillDef)?.minLevel ?? 0;

            if (pawn.Downed || pawn.InMentalState || pawn.Dead)
            {
                return "SwitchWorker_CantWork_Incapacitated".Translate();
            }
            if (pawn.workSettings != null && !pawn.workSettings.WorkIsActive(workType))
            {
                return "SwitchWorker_CantWork_WorkDisabled".Translate();
            }
            if (skillDef != null && pawn.skills != null && pawn.skills.GetSkill(skillDef).Level < minSkill)
            {
                return "SwitchWorker_CantWork_LowSkill".Translate(minSkill, pawn.skills.GetSkill(skillDef).Level);
            }
            if (!pawn.CanReach(thing, PathEndMode.Touch, Danger.Some))
            {
                return "SwitchWorker_CantWork_NoPath".Translate();
            }

            return null;
        }

        // Вычисление полной базовой стоимости рецепта
        public static bool TryGetTotalWorkAmount(UnfinishedThing thing, Bill_ProductionWithUft bill, out float totalWork)
        {
            totalWork = 0f;
            if (bill?.recipe == null) return false;

            totalWork = bill.recipe.workAmount;

            if (totalWork <= 0 && bill.recipe.products != null && bill.recipe.products.Count > 0)
            {
                ThingDefCountClass product = bill.recipe.products[0];
                if (product?.thingDef != null)
                {
                    totalWork = product.thingDef.GetStatValueAbstract(StatDefOf.WorkToMake);
                }
            }

            if (totalWork > 0) return true;

            totalWork = 0f;
            return false;
        }

        // Пропорциональный расчет нового остатка тиков работы
        public static int CalculateNewWorkLeft(UnfinishedThing thing, Bill_ProductionWithUft bill, Pawn currentCreator, Pawn newWorker)
        {
            float workLeft = thing.workLeft;
            float workDone = 0f;
            float totalWork = 0f;

            if (TryGetTotalWorkAmount(thing, bill, out totalWork))
            {
                workDone = Mathf.Max(0f, totalWork - workLeft);
            }
            else
            {
                totalWork = workLeft;
                workDone = 0f;
            }

            float kLoss = SwitchWorkerMod.Settings.workLossMultiplier;
            float kSkill = SwitchWorkerMod.Settings.skillInfluenceMultiplier;

            SkillDef skillDef = bill.recipe.workSkill;
            int currentLevel = 0;
            int newLevel = 0;

            if (skillDef != null)
            {
                if (currentCreator?.skills != null)
                {
                    currentLevel = Mathf.Clamp(currentCreator.skills.GetSkill(skillDef).Level, 0, 20);
                }
                else
                {
                    currentLevel = 0; // Защита от отсутствия старого мастера на карте
                }

                if (newWorker?.skills != null)
                {
                    newLevel = Mathf.Clamp(newWorker.skills.GetSkill(skillDef).Level, 0, 20);
                }
            }

            float baseLoss = workDone * kLoss;
            float skillFactor = (currentLevel - newLevel) / 20f;
            float skillLossModification = baseLoss * (skillFactor * kSkill);

            float finalWorkLoss = Mathf.Max(0f, baseLoss + skillLossModification);
            float targetWorkLeft = workLeft + finalWorkLoss;

            return Mathf.RoundToInt(Mathf.Clamp(targetWorkLeft, workLeft, totalWork));
        }
    }
}
