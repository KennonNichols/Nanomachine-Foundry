using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NanomachineFoundry.Utils;
using RimWorld;
using Verse;
using Verse.AI;

namespace NanomachineFoundry
{
	/*public class CompProperties_NaniteOperator : CompProperties
	{
		
	}*/
	
    public class CompNaniteOperator: CompNaniteApplicator
    {
		private readonly HashSet<NaniteOperationDef> _possibleAdministrations = DefDatabase<NaniteOperationDef>.AllDefs.ToHashSet();
		private Dictionary<NaniteDef, int> _tempConfigs;

		private NaniteOperationDef _currentOperation;
		private int _ticksRemaining;

		public bool IsOperating => _currentOperation != null;
		
		public ref Dictionary<NaniteDef, int> TempConfigs {
			get
			{
				if (_tempConfigs == null)
				{
					SaveOccupantConfig();
				}
				return ref _tempConfigs;
			}
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Collections.Look(ref _tempConfigs, "thnmf_tempConfigs", LookMode.Def);
			Scribe_Values.Look(ref _ticksRemaining, "thnmf_ticksRemaining");
			Scribe_Defs.Look(ref _currentOperation, "thnmf_currentOperation");
		}

		protected virtual void CompleteOperation()
		{
			_currentOperation.AdministerToPawn(Occupant, this);
			Find.LetterStack.ReceiveLetter("THNMF.OperationComplete".Translate(), string.Format("THNMF.OperationCompleteDescription".Translate(), _currentOperation.label, Occupant.Name.ToStringShort), LetterDefOf.PositiveEvent);
			
			EjectContents();
			_currentOperation = null;
		}

		private void CancelOperation()
		{
			_currentOperation = null;
		}

		public void StartOperation(NaniteOperationDef operationDef)
		{
			_ticksRemaining = operationDef.operationDurationTicks;
			_currentOperation = operationDef;
		}

		public bool TryGetLinkedTracker(out NaniteTracker_Pawn tracker)
		{
			if (Occupant == null)
			{
				tracker = null;
				return false;
			}
			tracker = OccupantTracker;
			return true;
		}

		public override void CompTick()
		{
			base.CompTick();
			if (!IsOperating) return;
			_ticksRemaining--;
			if (_ticksRemaining <= 0)
			{
				CompleteOperation();
			}
		}

		protected override void OnEmpty()
		{
			CancelOperation();
		}

		public override void PostDestroy(DestroyMode mode, Map previousMap)
		{
			if (IsOperating)
			{
				KillOccupant();
			}
			EjectContents(previousMap);
			InnerContainer.ClearAndDestroyContents();
			base.PostDestroy(mode, previousMap);
		}

		public bool TryGetLinkedPawn(out Pawn pawn)
		{
			pawn = Occupant;
			return Occupant != null;
		}
		
		public override bool CanAccept(Pawn pawn)
		{
			if (PowerOn && Occupant == null)
			{
				//If there is either an operation to be done, or the pawn is mechanized
				return pawn.IsMechanized() || _possibleAdministrations.Any(op => op.CanAdministerToPawn(pawn, out _));
			}
			return false;
		}
		
		

		public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
		{
			foreach (FloatMenuOption menuOption in base.CompFloatMenuOptions(selPawn))
			{
				yield return menuOption;
				yield break;
			}

			if (!(selPawn.IsMechanized() || _possibleAdministrations.Any(op => op.CanAdministerToPawn(selPawn, out _))))
			{
				yield return new FloatMenuOption("CannotUseReason".Translate("THNMF.OperatorUnusable".Translate()), null);
				yield break;
			}
			yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("THNMF.EnterInjector".Translate(), delegate
			{
				selPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(NMF_DefsOf.THNMF_EnterNaniteInjector,    parent), JobTag.Misc);
			}), selPawn, parent);
		}

		protected override void OnAccept()
		{
			if (Occupant.IsMechanized())
			{
				SaveOccupantConfig();
			}
		}
		
		public override string CompInspectStringExtra()
		{
			StringBuilder stringBuilder = new StringBuilder();

			if (IsOperating)
			{
				stringBuilder.Append($"Operation: '{_currentOperation.label}' is in progress. {_ticksRemaining.ToStringTicksToPeriodVerbose()} remaining.");
			}
			
			stringBuilder.AppendLineIfNotEmpty().Append(base.CompInspectStringExtra() ?? "");
			return stringBuilder.Length > 0 ? stringBuilder.ToString() : null;
		}

		private void SaveOccupantConfig()
		{
			_tempConfigs = OccupantTracker.NaniteConfigRatios.ToDictionary(entry => entry.Key, entry => entry.Value);
		}

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			foreach (Gizmo gizmo in base.CompGetGizmosExtra())
			{
				yield return gizmo;
			}

			if (IsOperating && DebugSettings.ShowDevGizmos)
			{
				yield return new Command_Action
				{
					action = CompleteOperation,
					defaultLabel = "DEV: Complete Instantly",
					defaultDesc = "Instantly finish active operation."
				};
			}
		}
    }
}
