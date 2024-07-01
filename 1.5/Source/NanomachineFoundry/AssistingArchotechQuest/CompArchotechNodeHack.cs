using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using NanomachineFoundry.NaniteModifications;
using NanomachineFoundry.Utils;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace NanomachineFoundry.AssistingArchotechQuest
{
	public class CompProperties_ArchotechNodeHackable : CompProperties
	{
		public float defence;

		public EffecterDef effectHacking;

		public bool glowIfHacked;

		public SoundDef hackingCompletedSound;

		public CompProperties_ArchotechNodeHackable()
		{
			compClass = typeof(CompArchotechNodeHackable);
		}
	}

	public class CompArchotechNodeHackable : ThingComp, IThingGlower
	{
		private float progress;

		public float defence;

		private bool zombiesSpawned;

		private float lastUserSpeed = 1f;

		private int lastHackTick = -1;

		private Pawn lastUser;

		public CompProperties_ArchotechNodeHackable Props => (CompProperties_ArchotechNodeHackable)props;

		public float ProgressPercent => progress / defence;

		public bool IsHacked => progress >= defence;

		public bool ShouldBeLitNow()
		{
			if (IsHacked)
			{
				return Props.glowIfHacked;
			}
			return true;
		}

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			if (!respawningAfterLoad)
			{
				defence = Props.defence;
			}
			base.PostSpawnSetup(respawningAfterLoad);
		}

		public void Hack(float amount, Pawn hacker = null)
		{
			if (progress > defence / 2 && !zombiesSpawned)
			{
				SummonArchiteZombies();
			}
			
			
			
			bool isHacked = IsHacked;
			progress += amount;
			progress = Mathf.Clamp(progress, 0f, defence);
			if (!isHacked && IsHacked)
			{
				if (hacker != null) FinishHack(hacker);
			}
			if (lastHackTick < 0)
			{
				if (hacker != null) StartHack(hacker);	
			}
			lastUserSpeed = amount;
			lastHackTick = Find.TickManager.TicksGame;
			lastUser = hacker;
		}
		
		public void SummonArchiteZombies()
		{
			List<Pawn> pawns = new List<Pawn>();
			NaniteEffectCategoryDef architeCat = DefDatabase<NaniteEffectCategoryDef>.GetNamed("THNMF_Archite");
			List<NaniteModificationDef> modificationDefs = DefDatabase<NaniteModificationDef>.AllDefs
				.Where(def => def.categoryDef == architeCat).ToList();

			Map currentMap = parent.Map; 
			Map playerHome = Current.Game.AnyPlayerHomeMap;
			float wealth;
			if (playerHome == null)
			{
				wealth = 25000;
			}
			else
			{
				wealth = playerHome.PlayerWealthForStoryteller;
			}

			int spawned = 0;
			const int maxSpawns = 3;
			
			float generatedValue = 0;
			
			
			Faction faction = NMF_DefsOf.SplinterFaction;
			
			
			PawnGenerationRequest request = new PawnGenerationRequest(NMF_DefsOf.THNMF_PuppetSoldier, faction, PawnGenerationContext.NonPlayer, currentMap.Tile, forceGenerateNewPawn: true, allowDead: false, allowDowned: false, canGeneratePawnRelations: false, mustBeCapableOfViolence: true);
			
			while (generatedValue < wealth && spawned < maxSpawns)
			{
				spawned++;
				Pawn pawn = PawnGenerator.GeneratePawn(request);
				generatedValue += pawn.MarketValue * 50;

				
				CellFinder.TryFindRandomEdgeCellWith(c => currentMap.reachability.CanReachColony(c) && !c.Fogged(currentMap), currentMap, CellFinder.EdgeRoadChance_Hostile, out IntVec3 cell);

				
				GenSpawn.Spawn(pawn, cell, currentMap);
				
				pawns.Add(pawn);

				if (!pawn.IsMechanized())
				{
					PawnSpawnNaniteUtility.MechanizePawn(pawn, wealth, NMF_DefsOf.THNMF_Archite, modificationDefs);
				}
				
				PawnSpawnNaniteUtility.ArchosplinterizePawn(pawn);

				generatedValue += pawn.MarketValue;
			}

			LordMaker.MakeNewLord(faction, new LordJob_AssaultColony(faction, canKidnap: false, canTimeoutOrFlee: false, sappers: false, useAvoidGridSmart: true, canSteal: false, breachers: false, canPickUpOpportunisticWeapons: false), currentMap, pawns);
			
			Find.LetterStack.ReceiveLetter("THNMF.ArchiteZombies".Translate(), "THNMF.ArchiteZombiesDescription".Translate(), LetterDefOf.ThreatBig, pawns);
			
			
			zombiesSpawned = true;
		}

		public void SummonZombieScout()
		{
			NaniteEffectCategoryDef architeCat = DefDatabase<NaniteEffectCategoryDef>.GetNamed("THNMF_Archite");
			List<NaniteModificationDef> modificationDefs = DefDatabase<NaniteModificationDef>.AllDefs
				.Where(def => def.categoryDef == architeCat).ToList();

			Map currentMap = parent.Map; 
			Map playerHome = Current.Game.AnyPlayerHomeMap;
			float wealth;
			if (playerHome == null)
			{
				wealth = 25000;
			}
			else
			{
				wealth = playerHome.PlayerWealthForStoryteller;
			}

			Faction faction = NMF_DefsOf.SplinterFaction;
			
			
			PawnGenerationRequest request = new PawnGenerationRequest(NMF_DefsOf.THNMF_PuppetSoldier, faction, PawnGenerationContext.NonPlayer, currentMap.Tile, forceGenerateNewPawn: true, allowDead: false, allowDowned: false, canGeneratePawnRelations: false, mustBeCapableOfViolence: true);
			
			Pawn pawn = PawnGenerator.GeneratePawn(request);

			CellFinder.TryFindRandomEdgeCellWith(c => currentMap.reachability.CanReachColony(c) && !c.Fogged(currentMap), currentMap, CellFinder.EdgeRoadChance_Hostile, out IntVec3 cell);
			
			
			GenSpawn.Spawn(pawn, cell, currentMap);
			

			if (!pawn.IsMechanized())
			{
				PawnSpawnNaniteUtility.MechanizePawn(pawn, wealth, NMF_DefsOf.THNMF_Archite, modificationDefs);
			}
			
			PawnSpawnNaniteUtility.ArchosplinterizePawn(pawn);


			LordMaker.MakeNewLord(faction, new LordJob_AssaultColony(faction, canKidnap: false, canTimeoutOrFlee: false, sappers: false, useAvoidGridSmart: true, canSteal: false, breachers: false, canPickUpOpportunisticWeapons: false), currentMap, [pawn]);
			
			
			
			Find.LetterStack.ReceiveLetter("THNMF.ArchiteZombie".Translate(), "THNMF.ArchiteZombieDescription".Translate(), LetterDefOf.ThreatSmall, pawn);
		}

		private void StartHack(Pawn hacker)
		{
			SummonZombieScout();
			Find.WindowStack.Add(new Dialog_Message("THNMF.PsychicWarningStartHack".Translate(hacker.Name.ToStringShort)));
		}

		private static void FinishHack(Pawn hacker)
		{
			Find.WindowStack.Add(new Dialog_Message("THNMF.PsychicMessageEndHack".Translate(hacker.Name.ToStringShort, hacker.Possessive())));
			hacker.GetNaniteTracker().GrantArchites(5);
			AssistingArchotechQuestlineUtility.CompleteAllNodeQuests(QuestEndOutcome.Success);
		}


		public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
		{
			if (IsHacked)
			{
				yield return new FloatMenuOption("CannotHack".Translate(parent.Label) + ": " + "AlreadyHacked".Translate().CapitalizeFirst(), null);
				yield break;
			}
			if (!selPawn.CanReach(parent, PathEndMode.InteractionCell, Danger.Deadly))
			{
				yield return new FloatMenuOption("CannotHack".Translate(parent.Label) + ": " + "NoPath".Translate().CapitalizeFirst(), null);
				yield break;
			}
			if (!HackUtility.IsCapableOfHacking(selPawn))
			{
				yield return new FloatMenuOption("CannotHack".Translate(parent.Label) + ": " + "IncapableOfHacking".Translate(), null);
				yield break;
			}
			yield return new FloatMenuOption("Hack".Translate(parent.Label), delegate
			{
				selPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(NMF_DefsOf.THNMF_AccessArchotechNode, parent), JobTag.Misc);
			});
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look(ref progress, "progress");
			Scribe_Values.Look(ref lastUserSpeed, "lastUserSpeed");
			Scribe_Values.Look(ref lastHackTick, "lastHackTick");
			Scribe_Values.Look(ref defence, "defence");
			Scribe_References.Look(ref lastUser, "lasterUser");
			Scribe_Values.Look(ref zombiesSpawned, "zombiesSpawned");
		}

		public override string CompInspectStringExtra()
		{
			TaggedString taggedString = "HackProgress".Translate() + ": " + progress.ToStringWorkAmount() + " / " + defence.ToStringWorkAmount();
			if (IsHacked)
			{
				taggedString += " (" + "Hacked".Translate() + ")";
			}
			if (lastHackTick > Find.TickManager.TicksGame - 30)
			{
				string text = lastUser == null ? (string)"HackingSpeed".Translate() : "HackingLastUser".Translate(lastUser) + " " + "HackingSpeed".Translate();
				taggedString += "\n" + text + ": " + StatDefOf.HackingSpeed.ValueToString(lastUserSpeed);
			}
			return taggedString;
		}

		public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
		{
			yield return new StatDrawEntry(StatCategoryDefOf.Basics, "HackProgress".Translate(), progress.ToStringWorkAmount() + " / " + defence.ToStringWorkAmount(), "Stat_Thing_HackingProgress".Translate(), 3100);
		}

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			if (DebugSettings.ShowDevGizmos && !IsHacked)
			{
				yield return new Command_Action
				{
					defaultLabel = "DEV: Hack +10%",
					action = delegate
					{
						Hack(defence * 0.1f);
					}
				};
				yield return new Command_Action
				{
					defaultLabel = "DEV: Complete hack",
					action = delegate
					{
						Hack(defence);
					}
				};
			}
		}
	}
}


