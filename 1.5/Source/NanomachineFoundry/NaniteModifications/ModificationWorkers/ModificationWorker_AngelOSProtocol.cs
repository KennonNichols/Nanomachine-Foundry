using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace NanomachineFoundry.NaniteModifications.ModificationWorkers
{
    public class ModificationWorker_AngelOS: ModificationWorker_ResurrectionParent
    {
        public ModificationWorker_AngelOS(NaniteModificationDef def, Pawn pawn) : base(def, pawn)
        {
        }

        public override HediffDef ResurrectionHediffDef => NMF_DefsOf.THNMF_AngelOSResurrection;

        public override bool CanResurrect(out bool isPermanentlyDead)
        {
            if (BodySizeInRange() >= _bodySizeNeeded)
            {
                isPermanentlyDead = false;
                return true;
            }
            
            //They are permadead if the colony has no robots for them to consume.
            isPermanentlyDead = !ColonyHasRobots;
            return false;
        }

        public override void OnResurrect()
        {
            List<Pawn> killedRobots = KillUntilMet(_bodySizeNeeded);
            Messages.Message("THNMF.AngelOSConsumed".Translate(killedRobots.Count), killedRobots, MessageTypeDefOf.NeutralEvent);
            base.OnResurrect();
        }

        private List<Pawn> KillUntilMet(float amountRemaining)
        {
            List<Pawn> deadRobots = new List<Pawn>(); 
            List<Pawn> robotsCanKill = RobotsInRange().OrderByDescending(robot => robot.BodySize).ToList();
            while (amountRemaining > 0)
            {
                if (!robotsCanKill.Any()) continue;
                Pawn robot = robotsCanKill.First();
                robotsCanKill.RemoveAt(0);
                amountRemaining -= robot.BodySize;
                robot.Kill(null);
                deadRobots.Add(robot);
            }
            return deadRobots;
        }

        private float BodySizeInRange()
        {
            Pawn[] robots = RobotsInRange();
            float totalSize = robots.Sum(robot => robot.BodySize);
            //Log.Message($"Total size {totalSize} across {robots.Length} robots in range of {_range} tiles.");
            return totalSize;
        }

        private Pawn[] RobotsInRange()
        {
            return RobotsInMap().Where(pawnFocused => pawnFocused.Position.DistanceTo(pawn.Position) < _range).ToArray();
        }

        private bool ColonyHasRobots => RobotsInMap().Any();
        
        private IEnumerable<Pawn> RobotsInMap()
        {
            Map map = pawn.Map;
            if (pawn.Dead)
            {
                map = pawn.Corpse.Map;
            }
            return map.PlayerPawnsForStoryteller.Where(pawnFocused => pawnFocused != null).Where(pawnFocused =>
                pawnFocused.IsColonyMech);
        }

        private float _bodySizeNeeded;
        private float _range;
        
        public override void RecalculateStats(float naniteLevel, int intendedNaniteLevel, NaniteDef type = null)
        {
            base.RecalculateStats(naniteLevel, 0, type);
            _range = def.TryGetScaledValueSlow(naniteLevel, "Range");
            _bodySizeNeeded = def.TryGetScaledValueSlow(naniteLevel, "BodySize");
        }
        
    }
}