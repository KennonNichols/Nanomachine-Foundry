using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace NanomachineFoundry.NaniteModifications.ModificationWorkers
{
    public class ModificationWorker_InstinctInhibitor: ModificationWorker
    {
        public float PainOffset { get; private set; }
        public float MinimumComfort { get; private set;  }

        
        private int _checksUntilInvalid;
        
        public bool CanAttackEnemy(bool forceCheck = false)
        {
	        if (pawn.IsAttacking())
	        {
		        _cachedCanAttack = true;
	        }
	        else
	        {
		        _checksUntilInvalid--;
		        if (_checksUntilInvalid <= 0 || forceCheck)
		        {
			        _checksUntilInvalid = 60;
			        _cachedCanAttack = CalculateCanAttackEnemy();
		        }
	        }
            return _cachedCanAttack;
        }
        
        private bool _cachedCanAttack;

        
        public ModificationWorker_InstinctInhibitor(NaniteModificationDef def, Pawn pawn) : base(def, pawn)
        {
	        
        }

        public override void RecalculateStats(float naniteLevel, int intendedNaniteLevel, NaniteDef type = null)
        {
            base.RecalculateStats(naniteLevel, 0);
            PainOffset = -.05f * naniteLevel;
            MinimumComfort = 0.02f * naniteLevel;
        }

        private bool CalculateCanAttackEnemy()
        {
	        if (!pawn.kindDef.canMeleeAttack || pawn.Downed || pawn.stances.FullBodyBusy || pawn.IsCarryingPawn() || (!pawn.IsPlayerControlled && pawn.IsPsychologicallyInvisible()))
			{
				return false;
			}
			bool flag = !pawn.WorkTagIsDisabled(WorkTags.Violent);
			bool flag2 = pawn.RaceProps.ToolUser && pawn.Faction == Faction.OfPlayer;
			if (!(flag || flag2))
			{
				return false;
			}
			//Melee calculation
			for (int i = 0; i < 9; i++)
			{
				IntVec3 c = pawn.Position + GenAdj.AdjacentCellsAndInside[i];
				if (!c.InBounds(pawn.Map))
				{
					continue;
				}
				List<Thing> thingList = c.GetThingList(pawn.Map);
				for (int j = 0; j < thingList.Count; j++)
				{
					if (flag && pawn.kindDef.canMeleeAttack && thingList[j] is Pawn pawn2 && !pawn2.ThreatDisabled(pawn) && pawn.HostileTo(pawn2))
					{
						CompActivity comp = pawn2.GetComp<CompActivity>();
						if ((comp == null || comp.IsActive) && !pawn.ThreatDisabledBecauseNonAggressiveRoamer(pawn2) && GenHostility.IsActiveThreatTo(pawn2, pawn.Faction))
						{
							return true;
						}
					}
				}
			}
			
			Verb currentEffectiveVerb = pawn.CurrentEffectiveVerb;
			if (currentEffectiveVerb != null && !currentEffectiveVerb.verbProps.IsMeleeAttack)
			{
				TargetScanFlags targetScanFlags = TargetScanFlags.NeedLOSToAll | TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable;
				if (currentEffectiveVerb.IsIncendiary_Ranged())
				{
					targetScanFlags |= TargetScanFlags.NeedNonBurning;
				}
				Thing thing = (Thing)AttackTargetFinder.BestShootTargetFromCurrentPosition(pawn, targetScanFlags);
				if (thing != null)
				{
					return true;
				}
			}
			return false;
			


			/*List<IAttackTarget> potentialTargetsFor = pawn.Map.attackTargetsCache.GetPotentialTargetsFor(pawn);
			for (int index = 0; index < potentialTargetsFor.Count; ++index)
			{
			    if (GenHostility.IsActiveThreatToPlayer(potentialTargetsFor[index]))
			    {
			        List<Verb> pawnMainAttacks = pawn.verbTracker.AllVerbs;

			        pawnMainAttacks.Do(verb => Log.Message($"{pawn.Name.ToStringShort}'s attack verb is: {verb.tool.label}"));

			        //Log.Message($"{pawn.Name.ToStringShort}'s main verb is: {pawnMainAttack.tool.label}");
			        if (pawnMainAttacks.Any(verb => verb.IsStillUsableBy(pawn) && verb.CanHitTarget(potentialTargetsFor[index].Thing))  )
			        {
			            return true;
			        }
			    }
			}
			return false;*/
        }
    }
}