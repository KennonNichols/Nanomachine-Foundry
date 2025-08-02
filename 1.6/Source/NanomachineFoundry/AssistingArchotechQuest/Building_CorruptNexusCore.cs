using System.Collections.Generic;
using System.Linq;
using NanomachineFoundry.Utils;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace NanomachineFoundry.AssistingArchotechQuest
{
    public class Building_CorruptNexusCore : Building
    {
		public override IEnumerable<Gizmo> GetGizmos()
		{
			foreach (Gizmo gizmo in base.GetGizmos())
			{
				yield return gizmo;
			}
			if (DebugSettings.ShowDevGizmos)
			{
				Command_Action commandAction = new Command_Action
				{
					action = delegate {Activate(null);},
					defaultLabel = "DEV: Stabilize corrupt nexus core"
				};
				yield return commandAction;
			}
		}

		public void Activate(Pawn activator)
		{
			GameComponent_NanomachineFoundry comp = Current.Game.GetComponent<GameComponent_NanomachineFoundry>();
			SoundDefOf.Archotech_Invoked.PlayOneShot(this);
			if (activator != null) comp.StartReturnTimer(activator, 6f);
		}

		public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
		{
			if (!selPawn.CanReach(this, PathEndMode.InteractionCell, Danger.Deadly))
			{
				yield return new FloatMenuOption(
					"CannotInvoke".Translate("Power".Translate()) + ": " + "NoPath".Translate().CapitalizeFirst(),
					null);
				yield break;
			}

			yield return new FloatMenuOption("THNMF.ActivateCorruptNexus".Translate(), delegate
			{
				selPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(NMF_DefsOf.THNMF_StabilizeCorruptArchonexusCore, this), JobTag.Misc);
			});
		}
    }
}