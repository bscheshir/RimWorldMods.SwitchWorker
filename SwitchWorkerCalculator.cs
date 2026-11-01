using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace SwitchWorker
{
    public static class SwitchWorkerCalculator
    {
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

            if (totalWork > 0)
            {
                return true;
            }

            totalWork = 0f;
            return false;
        }

        // Расчет нового остатка работы с учетом штрафов и разницы навыков
        public static int CalculateNewWorkLeft(UnfinishedThing thing, Bill_ProductionWithUft bill, Pawn currentCreator, Pawn newWorker)
        {
            float workLeft = thing.workLeft;
            float workDone = 0f;
            float totalWork = 0f;

            if (!TryGetTotalWorkAmount(thing, bill, out totalWork))
            {
                // Если полную стоимость узнать нельзя (разборка), остаток не меняется
                return Mathf.RoundToInt(workLeft);
            }

            workDone = Mathf.Max(0f, totalWork - workLeft);

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

            // Корректная формула: базовые потери работы при переносе чертежей
            float baseLoss = workDone * kLoss;

            // Разница навыков влияет на то, сколько ЕЩЕ работы будет потеряно или сэкономлено
            // Максимальный штраф/бонус от навыка ограничен коэффициентом kSkill
            float skillFactor = (currentLevel - newLevel) / 20f; // Положительный, если новый хуже
            float skillLossModification = baseLoss * (skillFactor * kSkill);

            // Итоговый штраф в тиках работы
            float finalWorkLoss = Mathf.Max(0f, baseLoss + skillLossModification);

            // Новый остаток работы = старый остаток + итоговый штраф
            float targetWorkLeft = workLeft + finalWorkLoss;

            // Жесткое ограничение рамками полной стоимости задачи
            return Mathf.RoundToInt(Mathf.Clamp(targetWorkLeft, workLeft, totalWork));
        }
    }
}
