using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace SwitchWorker
{
    public static class SwitchWorkerCalculator
    {
        // Определение причин недоступности (вынесено в единую точку для гизмо и ПКМ)
        public static string GetWorkerBlockReason(UnfinishedThing thing, Bill_ProductionWithUft bill, Pawn currentCreator, Pawn pawn)
        {
            if (pawn == currentCreator) return "SwitchWorker_CurrentCreator"; // Фильтр текущего мастера

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

            return null; // Ошибок нет, пешка может взяться за работу
        }

        // Определение полной базовой стоимости работы для рецепта
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

        // Расчет нового остатка работы с учетом штрафов и разницы навыков
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
