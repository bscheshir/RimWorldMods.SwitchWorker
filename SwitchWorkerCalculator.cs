using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace SwitchWorker
{
    public static class SwitchWorkerCalculator
    {
        public static string BuildWorkerLabel(UnfinishedThing thing, Pawn currentCreator, Pawn pawn, out string reason)
        {
            RecipeDef recipe = thing?.Recipe;
            SkillDef skillDef = recipe?.workSkill;

            string text = pawn.LabelShortCap;
            if (pawn.story != null && !pawn.story.TitleShort.NullOrEmpty())
            {
                text += $", {pawn.story.TitleShort}";
            }

            if (pawn.IsSlave)
            {
                text = $"<color=#E5C565>{text}</color>";
            }

            if (skillDef != null && pawn.skills != null)
            {
                text += $" [{skillDef.LabelCap}: {pawn.skills.GetSkill(skillDef).Level}]";
            }

            reason = GetWorkerBlockReason(thing, currentCreator, pawn);

            if (reason == "SwitchWorker_CurrentCreator")
            {
                int currentWorkLeftSeconds = Mathf.CeilToInt(thing.workLeft / 60f);
                string currentCreatorLocalized = $"<color=#68BB68>{"SwitchWorker_CurrentCreator".Translate()}</color>";
                string workLeftLocalized = "SwitchWorker_ResultWorkLeft".Translate(currentWorkLeftSeconds);

                return $"{text}\n({currentCreatorLocalized}: {workLeftLocalized})";
            }

            if (reason == null)
            {
                int newWorkLeftTicks = CalculateNewWorkLeft(thing, currentCreator, pawn);
                int newWorkLeftSeconds = Mathf.CeilToInt((float)newWorkLeftTicks / 60f);
                string workLeftLocalized = "SwitchWorker_ResultWorkLeft".Translate(newWorkLeftSeconds);

                return $"{text}\n({workLeftLocalized})";
            }

            return $"{text}\n({reason})";
        }

        public static string GetWorkerBlockReason(UnfinishedThing thing, Pawn currentCreator, Pawn pawn)
        {
            // Безопасное сравнение по ThingID (работает для улетевших пешек)
            if (currentCreator != null && pawn != null && pawn.ThingID == currentCreator.ThingID)
            {
                return "SwitchWorker_CurrentCreator";
            }

            if (pawn == null || pawn.Downed || pawn.InMentalState || pawn.Dead)
            {
                return "SwitchWorker_CantWork_Incapacitated".Translate();
            }

            RecipeDef recipe = thing?.Recipe;
            if (recipe == null) return "Error";

            // Сначала смотрим тип работы в рецепте. Если там null, точечно ищем первый верстак в базе игры, который выполняет этот рецепт
            WorkTypeDef workType = recipe.requiredGiverWorkType
                                  ?? DefDatabase<WorkGiverDef>.AllDefs
                                     .FirstOrDefault(g => g.fixedBillGiverDefs != null && g.workType != null && g.fixedBillGiverDefs.Any(d => d.AllRecipes.Contains(recipe)))?.workType;

            SkillDef skillDef = recipe.workSkill;
            int minSkill = recipe.skillRequirements?.FirstOrDefault(x => x.skill == skillDef)?.minLevel ?? 0;

            if (workType != null && pawn.workSettings != null && !pawn.workSettings.WorkIsActive(workType))
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

        public static bool TryGetTotalWorkAmount(UnfinishedThing thing, out float totalWork)
        {
            totalWork = 0f;
            if (thing?.Recipe == null) return false;

            totalWork = thing.Recipe.workAmount;

            if (totalWork <= 0 && thing.Recipe.products != null && thing.Recipe.products.Count > 0)
            {
                ThingDefCountClass product = thing.Recipe.products[0];
                if (product?.thingDef != null)
                {
                    totalWork = product.thingDef.GetStatValueAbstract(StatDefOf.WorkToMake);
                }
            }

            return totalWork > 0;
        }

        public static int CalculateNewWorkLeft(UnfinishedThing thing, Pawn currentCreator, Pawn newWorker)
        {
            float workLeft = thing.workLeft;
            float workDone = 0f;
            float totalWork = 0f;

            if (TryGetTotalWorkAmount(thing, out totalWork))
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

            RecipeDef recipe = thing.Recipe;
            SkillDef skillDef = recipe?.workSkill;
            int currentLevel = 0;
            int newLevel = 0;

            if (skillDef != null)
            {
                if (currentCreator != null && !currentCreator.Destroyed && currentCreator.skills != null)
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
