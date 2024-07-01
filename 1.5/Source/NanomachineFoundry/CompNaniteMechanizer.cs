using NanomachineFoundry.Utils;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;

namespace NanomachineFoundry
{
	[StaticConstructorOnStartup]
	internal class CompNaniteMechanizer : CompNaniteApplicator
    {
		//12 hours for a tier
		private const int TicksPerCapacity = 30000;
		private const int FirstTimeMultiplier = 4;
		private int _currentTickAmount;
		//10 years gained from full mechanization

		public override void PostExposeData()
        {
            base.PostExposeData();
			Scribe_Values.Look(ref _currentTickAmount, "currentTickAmount");
        }

        public override void CompTick()
        {
            base.CompTick();
            if (Occupant == null) return;
            _currentTickAmount++;
			Occupant.ageTracker.AgeTickMothballed(NMFSettings.MechanizationAgingMultiplier - 1);
			if (_currentTickAmount >= TicksPerCapacity * (Occupant.IsMechanized() ? 1 : FirstTimeMultiplier))
			{
				IncreaseCapacity();
			}
        }

		private void IncreaseCapacity()
        {
			_currentTickAmount = 0;
			if (!OccupantTracker.IncreaseCapacity(1))
            {
				EjectContents();
            }
		}

        public override bool CanAccept(Pawn pawn)
		{
			if (PowerOn && Occupant == null)
			{
				if (pawn.IsMechanized())
				{
					return pawn.GetNaniteTracker().NaniteCapacity < NaniteTracker_Pawn.MaxCapacity;
				}
				return true;
			}
			return false;
		}

		public override void PostDestroy(DestroyMode mode, Map previousMap)
		{
			KillOccupant();
			EjectContents(previousMap);
			InnerContainer.ClearAndDestroyContents();
			base.PostDestroy(mode, previousMap);
		}

		protected override void EjectContents(Map destMap = null)
        {
	        if (Occupant == null) return;
	        if (destMap == null)
	        {
		        destMap = parent.Map;
	        }
	        Occupant.health.AddHediff(NMF_DefsOf.THNMF_MechanizationShock);
	        InnerContainer.TryDropAll(parent.InteractionCell, destMap ?? parent.Map, ThingPlaceMode.Near);
	        _currentTickAmount = 0;
        }

        public override string CompInspectStringExtra()
        {
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLineIfNotEmpty().Append(base.CompInspectStringExtra());
			if (Occupant != null)
			{
				stringBuilder.AppendLineIfNotEmpty().Append(string.Format("THNMF.MechanizerTimeRemaining".Translate(), _currentTickAmount.ToStringTicksToPeriodVerbose(), (TicksPerCapacity * (Occupant.IsMechanized() ? 1 : FirstTimeMultiplier)).ToStringTicksToPeriodVerbose()))
					.AppendLineIfNotEmpty().Append(string.Format("THNMF.MechanizerCapacityRemaining".Translate(), Occupant.NameShortColored, OccupantTracker.NaniteCapacity, NaniteTracker_Pawn.MaxCapacity));
			}
			return stringBuilder.Length > 0 ? stringBuilder.ToString() : null;
        }


        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
		{
			foreach (FloatMenuOption menuOption in base.CompFloatMenuOptions(selPawn))
			{
				yield return menuOption;
			}
			float adultAge = selPawn.RaceProps.lifeStageAges.First(age => age.def == LifeStageDefOf.HumanlikeAdult)
				.minAge;
			if (selPawn.ageTracker.AgeBiologicalYears < NMFSettings.MechanizationAge * adultAge)
			{
				yield return new FloatMenuOption("CannotUseReason".Translate("THNMF.PawnTooYoung".Translate(NMFSettings.MechanizationAge * adultAge)), null);
				yield break;
			}
			if (selPawn.IsMechanized())
            {
				if (NaniteTracker_Pawn.Get(selPawn).NaniteCapacity >= NaniteTracker_Pawn.MaxCapacity)
				{
					yield return new FloatMenuOption("CannotUseReason".Translate("THNMF.PawnNanitesAtCapacity".Translate()), null);
					yield break;
				}
            }
			yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("THNMF.EnterInjector".Translate(), delegate
			{
				selPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(NMF_DefsOf.THNMF_EnterNaniteInjector, parent), JobTag.Misc);
			}), selPawn, parent);
		}

    }
}
