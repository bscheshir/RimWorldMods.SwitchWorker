using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
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

            // Кнопка активна, если заготовка существует
            this.disabled = thing == null || thing.Destroyed || thing.Recipe == null;
        }

        public override void ProcessInput(Event ev)
        {
            base.ProcessInput(ev);

            Pawn currentCreator = thing.Creator;
            Map map = thing.Map ?? Find.CurrentMap; // Берем карту предмета или текущую карту игрока
            SkillDef skillDef = thing.Recipe?.workSkill;

            List<FloatMenuOption> availableWorkers = new List<FloatMenuOption>();
            List<FloatMenuOption> currentWorkerOption = new List<FloatMenuOption>();
            List<FloatMenuOption> unavailableWorkers = new List<FloatMenuOption>();

            if (map == null || map.mapPawns == null) return;

            int GetPawnSkill(Pawn p) => (skillDef != null && p.skills != null) ? p.skills.GetSkill(skillDef).Level : 0;
            int GetColonistBarOrder(Pawn p) => p.playerSettings != null ? p.playerSettings.displayOrder : 0;

            var sortedPawns = map.mapPawns.FreeColonists
                .OrderByDescending(p => GetPawnSkill(p))
                .ThenBy(p => GetColonistBarOrder(p));

            foreach (Pawn pawn in sortedPawns)
            {
                Pawn localPawn = pawn;
                string reason;

                string label = SwitchWorkerCalculator.BuildWorkerLabel(thing, currentCreator, pawn, out reason);

                if (reason == "SwitchWorker_CurrentCreator")
                {
                    var opt = new FloatMenuOption(label, null, localPawn, Color.white, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0) { Disabled = true };
                    currentWorkerOption.Add(opt);
                }
                else if (reason == null)
                {
                    var opt = new FloatMenuOption(label, () =>
                    {
                        int newWorkLeftTicks = SwitchWorkerCalculator.CalculateNewWorkLeft(thing, currentCreator, localPawn);
                        thing.Creator = localPawn;
                        thing.workLeft = newWorkLeftTicks;
                        SoundDefOf.Tick_High.PlayOneShotOnCamera();
                    }, localPawn, Color.white, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);

                    availableWorkers.Add(opt);
                }
                else
                {
                    var opt = new FloatMenuOption(label, null, localPawn, Color.white, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0) { Disabled = true };
                    unavailableWorkers.Add(opt);
                }
            }

            List<FloatMenuOption> finalMenu = new List<FloatMenuOption>();
            finalMenu.AddRange(availableWorkers);
            finalMenu.AddRange(currentWorkerOption);
            finalMenu.AddRange(unavailableWorkers);

            if (finalMenu.Any()) Find.WindowStack.Add(new FloatMenu(finalMenu));
        }
    }
}
