using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PipeSystem;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace NanomachineFoundry.NaniteProduction
{
    [StaticConstructorOnStartup]
    public class CompMechaniteBreeder : ThingComp, IThingHolderWithDrawnPawn, ISuspendableThingHolder
    {
		public Job queuedEnterJob;

		public Pawn queuedPawn;

		public bool mechanitesBuilding;

		public int excessMechanitesCounter;

		public CompResource compResource;

		public ThingOwner innerContainer;

		private Gizmo _gizmo;


		public bool mechanoidCrippled;
		public Pawn Occupant => innerContainer.OfType<Pawn>().FirstOrDefault();

		public bool IsContentsSuspended => true;

		public PawnPosture HeldPawnPosture => PawnPosture.LayingOnGroundFaceUp;

		public float HeldPawnDrawPos_Y => parent.def.altitudeLayer.AltitudeFor(1f / 26f);

		public float HeldPawnBodyAngle => parent.Rotation.Opposite.AsAngle;
		private const int BaseWakeUpNanites = 60000;
		public const float DangerPercent = 0.8f;
		public const float AwakenChancePerTick = 0.03f;
		
		
		public bool PowerOn => parent.TryGetComp<CompPowerTrader>().PowerOn;
		public float PercentFull =>  Occupant == null ? 0: (float)excessMechanitesCounter / BaseWakeUpNanites * Occupant.BodySize;
		public int TotalWakeupTicks => Occupant == null ? BaseWakeUpNanites : (int)(BaseWakeUpNanites * Occupant.BodySize);
		public bool InDanger => Occupant != null && excessMechanitesCounter > TotalWakeupTicks * DangerPercent;
		public float PastDangerPercent => Occupant == null ? 0:
			(TotalWakeupTicks - excessMechanitesCounter) / ((1 - DangerPercent) * TotalWakeupTicks);

		
		public CompMechaniteBreeder()
		{
			innerContainer = new ThingOwner<Thing>(this);
		}

		public void GetChildHolders(List<IThingHolder> outChildren)
		{
			ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
		}

		public ThingOwner GetDirectlyHeldThings()
		{
			return innerContainer;
		}

		public override void Initialize(CompProperties props)
		{
			base.Initialize(props);
			compResource = parent.TryGetComp<CompResource>();
		}

		public override void CompTick()
		{
			if (parent.IsHashIntervalTick(15000) && Occupant != null && compResource != null)
			{
				compResource.PipeNet.DistributeAmongStorage(Occupant.BodySize / 4, out _);
			}

			if (!parent.IsHashIntervalTick(100)) return;
			if (Occupant == null) return;
			if (mechanoidCrippled) return;
			if (PowerOn)
			{
				excessMechanitesCounter -= 200;
				mechanitesBuilding = false;
			}
			else
			{
				mechanitesBuilding = true;
				excessMechanitesCounter += 200;
				if (InDanger)
				{
					if (Rand.Chance(AwakenChancePerTick))
					{
						Find.LetterStack.ReceiveLetter("THNMF.MechanoidsAwoken".Translate(), "THNMF.MechanoidsAwokenDescription".Translate(Occupant.RaceProps?.AnyPawnKind?.label ?? "mechanoid"), LetterDefOf.ThreatBig, Occupant);
						EjectContents(parent.Map);
					}
				}
			}
			excessMechanitesCounter = Mathf.Clamp(excessMechanitesCounter, 0, TotalWakeupTicks);
		}


		public override void PostDestroy(DestroyMode mode, Map previousMap)
		{
			EjectContents(previousMap);
			base.PostDestroy(mode, previousMap);
		}

		public void InsertPawn(Pawn pawn)
		{
			excessMechanitesCounter = 0;
			mechanitesBuilding = false;
			if (innerContainer.TryAddOrTransfer(pawn, canMergeWithExistingStacks: false)) HealToFull(pawn);
		}

		private void HealToFull(Pawn pawn)
		{
			List<Hediff_Injury> tmpMechInjuries = new List<Hediff_Injury>();
			pawn.health.hediffSet.GetHediffs(ref tmpMechInjuries,
				x => x != null && x.def.everCurableByItem && !x.IsPermanent());
			if (tmpMechInjuries.Count > 0)
			{
				float num = tmpMechInjuries.Sum(x => x.Severity) * 0.5f /
				            tmpMechInjuries.Count;
				foreach (var t in tmpMechInjuries)
					t.Severity = 0.2f;
				tmpMechInjuries.Clear();
			}
			pawn.health.hediffSet.hediffs.RemoveAll(x => x.def.everCurableByItem && x is Hediff_Injury && x.IsPermanent() && pawn.health.hediffSet.GetPartHealth(x.Part) <= 0.0);

			if (MechRepairUtility.IsMissingWeapon(pawn)) MechRepairUtility.GenerateWeapon(pawn);
			
			while (true)
			{
				Hediff_MissingPart hediffMissingPart = pawn.health.hediffSet.GetMissingPartsCommonAncestors().FirstOrDefault(x => !pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(x.Part));
				if (hediffMissingPart != null)
					pawn.health.RestorePart(hediffMissingPart.Part, checkStateChange: false);
				else
					break;
			}
		}

		public bool Accepts(Thing thing)
		{
			return innerContainer.CanAcceptAnyOf(thing);
		}

		public bool TryAcceptPawn(Pawn pawn)
		{
			innerContainer.ClearAndDestroyContents();
			bool flag = pawn.DeSpawnOrDeselect();
			if (pawn.holdingOwner != null)
			{
				pawn.holdingOwner.TryTransferToContainer(pawn, innerContainer);
			}
			else
			{
				innerContainer.TryAdd(pawn);
			}
			if (flag)
			{
				Find.Selector.Select(pawn, playSound: false, forceDesignatorDeselect: false);
			}
			ClearQueuedInformation();
			return true;
		}

		public virtual bool CanAcceptPawn(Pawn pawn)
		{
			return Occupant == null;
		}

		public static TargetingParameters GetTargetingParameters()
		{
			return new TargetingParameters
			{
				canTargetPawns = true,
				validator = delegate(TargetInfo x)
				{
					Pawn obj = x.Thing as Pawn;
					//Only downed ancient mechanoids can be used
					return obj is { Downed: true } && obj.RaceProps.IsMechanoid && obj.ageTracker.AgeBiologicalYears >= 100;
				}
			};
		}

		public static void AddCarryToJobs(List<FloatMenuOption> opts, Pawn pawn, Pawn target)
		{
			if (!pawn.CanReserveAndReach(target, PathEndMode.OnCell, Danger.Deadly, 1, -1, null, ignoreOtherReservations: true))
			{
				return;
			}
			string text = "";
			Action action = null;
			foreach (Thing item in PlatformsFor(pawn, target))
			{
				text = "THNMF.CarryToBreedingPlatform".Translate(target);
				if (!item.TryGetComp<CompMechaniteBreeder>(out var comp))
				{
					continue;
				}
				if (!target.RaceProps.IsMechanoid)
				{
					text += " (" + "THNMF.MustTargetMechanoid".Translate() + ")";
					continue;
				}
				if (!target.Downed)
				{
					text += " (" + "THNMF.CarryToBreedingPlatformDowned".Translate() + ")";
					continue;
				}
				if (target.ageTracker.AgeBiologicalYears < 100)
				{
					text += " (" + "THNMF.CarryToBreedingPlatformTooYoung".Translate() + ")";
					continue;
				}
				if (!comp.CanAcceptPawn(target))
				{
					text += " (" + "CryptosleepCasketOccupied".Translate() + ")";
					continue;
				}
				Thing pod = item;
				action = delegate
				{
					Job job = JobMaker.MakeJob(NMF_DefsOf.THNMF_CarryToMechaniteBreeder, target, pod);
					job.count = 1;
					pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
					pod.TryGetComp<CompMechaniteBreeder>().SetQueuedInformation(job, target);
				};
				break;
			}
			if (!text.NullOrEmpty())
			{
				opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text, action), pawn, target));
			}
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look(ref mechanitesBuilding, "mechanitesBuilding", defaultValue: false);
			Scribe_Values.Look(ref excessMechanitesCounter, "excessMechanites");
			Scribe_Values.Look(ref mechanoidCrippled, "mechanoidCrippled");
			Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
			Scribe_References.Look(ref queuedEnterJob, "queuedEnterJob");
			Scribe_References.Look(ref queuedPawn, "queuedPawn");
		}

		public override string CompInspectStringExtra()
		{
			StringBuilder builder = new StringBuilder(base.CompInspectStringExtra());
			if (Occupant != null)
			{
				builder.AppendLineIfNotEmpty().AppendLineIfNotEmpty().Append("THNMF.MechaniteProduction".Translate(Occupant.BodySize.ToString("0.0")));
			}
			return builder.ToString();
		}

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			foreach (Gizmo gizmo in base.CompGetGizmosExtra())
			{
				yield return gizmo;
			}

			if (mechanoidCrippled)
			{
				yield break;
			}

			if (Find.Selector.SingleSelectedThing != parent) yield break;
			_gizmo ??= new MechaniteBreederNaniteGizmo(this);
			_gizmo.Order = -100f;
			yield return _gizmo;
		}

		public static IEnumerable<Thing> PlatformsFor(Pawn pawn, Pawn target)
		{
			return from thing in DefDatabase<ThingDef>.AllDefs.Where(def => def.comps.Any(comp => comp.compClass == typeof(CompMechaniteBreeder))).SelectMany(thingDef => pawn.Map.listerThings.ThingsOfDef(thingDef))
				where thing != null && pawn.CanReach(thing, PathEndMode.InteractionCell, Danger.Some)
				select thing;
		}
		

		public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
		{
			foreach (FloatMenuOption item in base.CompFloatMenuOptions(selPawn))
			{
				yield return item;
			}
			//TODO should be able to cripple the mechanoid here
			if (innerContainer.Count != 0)
			{
				yield break;
			}
		}

		public void EjectContents(Map map)
		{
			excessMechanitesCounter = 0;
			mechanitesBuilding = false;

			float shockSeverity = InDanger ? PastDangerPercent : 1;
			
			foreach (Thing item in innerContainer)
			{
				if (!(item is Pawn pawn)) continue;
				PawnComponentsUtility.AddComponentsForSpawn(pawn);

				
				
				LordMaker.MakeNewLord(Faction.OfMechanoids,
					new LordJob_AssaultColony(Faction.OfMechanoids, false, true, false, false, false), pawn.Map,
					new[] { pawn });
				
				
				
				if (mechanoidCrippled)
				{
					pawn.health.AddHediff(NMF_DefsOf.THNMF_MechanoidCrippled);
				}
				else
				{
					Hediff shock = HediffMaker.MakeHediff(NMF_DefsOf.THNMF_MechanoidShock, pawn);
					shock.Severity = shockSeverity;
					pawn.health.AddHediff(shock);
				}
			}
			innerContainer.TryDropAll(parent.InteractionCell, map, ThingPlaceMode.Near);
		}

		public void ClearQueuedInformation()
		{
			SetQueuedInformation(null, null);
		}

		public void SetQueuedInformation(Job job, Pawn pawn)
		{
			queuedEnterJob = job;
			queuedPawn = pawn;
		}
    }
}