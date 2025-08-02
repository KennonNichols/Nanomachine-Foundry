using PipeSystem;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NanomachineFoundry.Utils;
using UnityEngine;
using Verse;
using Verse.AI;

namespace NanomachineFoundry
{
	[StaticConstructorOnStartup]
	public abstract class CompNaniteApplicator : ThingComp, ISuspendableThingHolder, IThingHolderWithDrawnPawn
	{
		protected ThingOwner InnerContainer;

		private int _ticks;
		
        public bool IsContentsSuspended => true;
        private static readonly Material BackgroundMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.082f, 0.078f, 0.063f));

		private static readonly Texture2D EjectTexture = ContentFinder<Texture2D>.Get("UI/Gizmos/NaniteInjectionLeave");
		public float HeldPawnDrawPos_Y => parent.DrawPos.y - 1f / 26f;

        public float HeldPawnBodyAngle => parent.Rotation.AsAngle;

        public PawnPosture HeldPawnPosture => PawnPosture.LayingOnGroundFaceUp;

        private CompPowerTrader PowerTrader => _compPowerTrader ??= _compPowerTrader = parent.GetComp<CompPowerTrader>();

        public IEnumerable<CompResource> Resources => _compResources ??= _compResources = parent.GetComps<CompResource>();

		private IEnumerable<CompResource> _compResources;
		private CompPowerTrader _compPowerTrader;

		protected NaniteTracker_Pawn OccupantTracker => NaniteTracker_Pawn.Get(Occupant);

		protected bool PowerOn => PowerTrader.PowerOn;
		protected Pawn Occupant => InnerContainer.OfType<Pawn>().FirstOrDefault();

		public void GetChildHolders(List<IThingHolder> outChildren)
		{
			ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Deep.Look(ref InnerContainer, "innerContainer", this);
		}

		protected void KillOccupant()
		{
			if (Occupant == null) return;
			Find.LetterStack.ReceiveLetter("THNMF.MechanizerDestroyEarly".Translate(), string.Format("THNMF.MechanizerDestroyEarlyDescription".Translate(), Occupant.NameShortColored, Occupant.Possessive()), LetterDefOf.ThreatSmall);
			//Inflict intense nanite rot
			Hediff decay = HediffMaker.MakeHediff(NMF_DefsOf.THNMF_DecayNaniteInfection, Occupant);
			decay.Severity = Rand.Range(.5f, .75f);
			Occupant.health.AddHediff(decay);
		}

		public ThingOwner GetDirectlyHeldThings()
		{
			return InnerContainer;
		}

		protected CompNaniteApplicator()
        {
			InnerContainer = new ThingOwner<Thing>(this);
		}

		public override void PostDestroy(DestroyMode mode, Map previousMap)
		{
			EjectContents(previousMap);
			InnerContainer.ClearAndDestroyContents();
			base.PostDestroy(mode, previousMap);
		}

		protected virtual void EjectContents(Map destMap = null)
		{
			OnEmpty();
			if (destMap == null)
			{
				destMap = parent.Map;
			}
			InnerContainer.TryDropAll(parent.InteractionCell, destMap ?? parent.Map, ThingPlaceMode.Near);
		}

		public override string CompInspectStringExtra()
		{
			StringBuilder stringBuilder = new StringBuilder();
			//No more base stuff
			//stringBuilder.AppendLineIfNotEmpty().Append(base.CompInspectStringExtra()).AppendLineIfNotEmpty();
			if (!parent.Spawned || Occupant == null) return stringBuilder.Length > 0 ? stringBuilder.ToString() : null;
			stringBuilder.AppendLineIfNotEmpty().Append("Contains".Translate()).Append(": ")
				.Append(Occupant.NameShortColored.Resolve());
			return stringBuilder.Length > 0 ? stringBuilder.ToString() : null;
		}

		public override void CompTick()
		{
			_ticks = (_ticks + 1) % 500;
		}

		public abstract bool CanAccept(Pawn pawn);

		public void TryAcceptPawn(Pawn pawn)
		{
			if (!CanAccept(pawn)) return;
			if (pawn.Spawned)
			{
				pawn.DeSpawn();
			}
			if (pawn.holdingOwner != null)
			{
				pawn.holdingOwner.TryTransferToContainer(pawn, InnerContainer);
			}
			else
			{
				InnerContainer.TryAdd(pawn);
				OnAccept();
			}
		}

		public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
			if (selPawn.IsQuestLodger())
			{
				yield return new FloatMenuOption("CannotUseReason".Translate("CryptosleepCasketGuestsNotAllowed".Translate()), null);
				yield break;
			}
			if (!selPawn.CanReach(parent, PathEndMode.InteractionCell, Danger.Deadly))
			{
				yield return new FloatMenuOption("CannotUseNoPath".Translate(), null);
				yield break;
			}
			if (!PowerOn)
			{
				yield return new FloatMenuOption("CannotUseNoPower".Translate(), null);
				yield break;
			}
			if (Occupant != null)
			{
				yield return new FloatMenuOption("CannotUseReason".Translate("THNMF.InjectorOccupied".Translate()), null);
				yield break;
			}
		}

		public override void PostDraw()
		{
			base.PostDraw();
			Vector3 s = new Vector3(0.8f, 1f, parent.def.graphicData.drawSize.y * 0.8f);
			Vector3 drawPos = parent.DrawPos;
			drawPos.y -= 1f / 13f;
			Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(drawPos, parent.Rotation.AsQuat, s), BackgroundMat, 0);
			if (Occupant == null) return;
			Vector3 drawPos2 = parent.DrawPos;
			drawPos2.y -= 1f / 26f;
			if (parent.Rotation == Rot4.South)
			{
				drawPos2.z -= VerticalOffset;
			}
			if (parent.Rotation == Rot4.West || parent.Rotation == Rot4.East)
			{
				drawPos2.z += VerticalOffset;
			}
			if (parent.Rotation == Rot4.West)
			{
				drawPos2.x -= HorizontalOffset;
			}
			if (parent.Rotation == Rot4.East)
			{
				drawPos2.x += HorizontalOffset;
			}

			drawPos2 += FloatingOffset(_ticks);
			Occupant.Drawer.renderer.RenderPawnAt(drawPos2, Rot4.South, neverAimWeapon: true);
		}

		protected virtual float HorizontalOffset => 0.3f;

		protected virtual float VerticalOffset => 0.1f;
		
		private static Vector3 FloatingOffset(int tick)
		{
			float num = tick / 500f;
			float num2 = Mathf.Sin((float)Math.PI * num);
			float z = num2 * num2 * 0.04f;
			return new Vector3(0f, 0f, z);
		}


		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			if (InnerContainer.Any)
			{
				foreach (Thing item in InnerContainer)
				{
					yield return Building.SelectContainedItemGizmo(parent, item);
				}
			}
			if (Occupant != null && PowerOn)
			{
				yield return new Command_Action
				{
					icon = EjectTexture,
					action = delegate
					{
						EjectContents();
					},
					defaultLabel = "THNMF.EjectPawn".Translate(),
					defaultDesc = "THNMF.EjectPawnDesc".Translate(Occupant.Named("PAWN"))
				};
			}
		}
		
    
		//protected void sendPawnToEnter(Pawn pawn) {}
		
		protected virtual void OnAccept() {}
		protected virtual void OnEmpty() {}
	}
}
