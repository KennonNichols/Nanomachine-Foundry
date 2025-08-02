using NanomachineFoundry.Utils;
using PipeSystem;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace NanomachineFoundry
{
	[StaticConstructorOnStartup]
	public class CompNaniteInjector: CompNaniteApplicator
	{
		protected override float HorizontalOffset => -0.25f;

		protected override float VerticalOffset => -0.25f;

	
		
		public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
		{
			foreach (FloatMenuOption menuOption in base.CompFloatMenuOptions(selPawn))
            {
				yield return menuOption;
            }
			if (!selPawn.IsMechanized())
			{
				yield return new FloatMenuOption("CannotUseReason".Translate("THNMF.MustBeMechanized".Translate()), null);
				yield break;
			}
			if (!NaniteTracker_Pawn.Get(selPawn).GetRefillableTypes().Any())
            {
				yield return new FloatMenuOption("CannotUseReason".Translate("THNMF.PawnNanitesFull".Translate()), null);
				yield break;
            }
			yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("THNMF.EnterInjector".Translate(), delegate
			{
				selPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(NMF_DefsOf.THNMF_EnterNaniteInjector, parent), JobTag.Misc);
			}), selPawn, parent);
		}
		
		public override bool CanAccept(Pawn pawn)
		{
			return PowerOn && Occupant == null && pawn.IsMechanized();
		}

        public override void CompTick()
        {
	        if (!parent.IsHashIntervalTick(1500)) return;
	        if (Occupant == null) return;
	        NaniteTracker_Pawn tracker = OccupantTracker;
	        bool fillDone = false;

	        IEnumerable<NaniteDef> refillables = tracker.GetRefillableTypes();
	        var naniteTypes = refillables as NaniteDef[] ?? refillables.ToArray();
	        
	        foreach (CompResource compResource in Resources)
	        {
		        foreach (NaniteDef naniteType in naniteTypes)
		        {
			        if (naniteType.pipeNet != compResource.PipeNet.def) continue;
			        if (compResource.PipeNet.Stored > 1)
			        {
				        if (tracker.TryChangeNanitesLevel(naniteType, 1,
					            tracker.NaniteForceMode(naniteType)))
				        {
					        fillDone = true;
					        compResource.PipeNet.DrawAmongStorage(1, compResource.PipeNet.storages);
				        }
			        }
			        break;
		        }
	        }

	        if (!fillDone)
	        {
		        EjectContents();
	        }
        }
    }
}
