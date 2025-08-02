using HarmonyLib;
using NanomachineFoundry.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using NanomachineFoundry.NaniteModifications;
using NanomachineFoundry.NaniteModifications.ModificationWorkers;
using NanomachineFoundry.NaniteProduction;
using RimWorld;
using UnityEngine;
using Verse;
using NaniteTracker_Pawn = NanomachineFoundry.Utils.NaniteTracker_Pawn;

namespace NanomachineFoundry.Patches
{
	[StaticConstructorOnStartup]
    class PawnPatches
    {
		public static void ApplyPatches(Harmony harmony)
		{
			//Save the pawn's nanite tracker when they are saved
			harmony.Patch(AccessTools.Method(typeof(Pawn), "ExposeData"), postfix: new HarmonyMethod(typeof(NaniteTracker_Pawn), "Save"));
			
			//Notify the tracker when a hediff is added to a mechanized pawn
			harmony.Patch(AccessTools.Method(typeof(HealthUtility), nameof(HealthUtility.AdjustSeverity), new[] { typeof(Pawn), typeof(HediffDef), typeof(float) }), prefix: new HarmonyMethod(typeof(PawnPatches), nameof(OnHediffSeverityAdjusted)));
			harmony.Patch(AccessTools.Method(typeof(StatWorker), nameof(StatWorker.GetValueUnfinalized)), transpiler: new HarmonyMethod(typeof(PawnPatches), nameof(StatGetValueTranspile)));
			harmony.Patch(AccessTools.Method(typeof(StatWorker), nameof(StatWorker.GetOffsetsAndFactorsExplanation)), transpiler: new HarmonyMethod(typeof(PawnPatches), nameof(StatGetExplanationTranspile)));
			harmony.Patch(AccessTools.Method(typeof(PawnCapacityUtility), "CalculateCapacityLevel"), postfix: new HarmonyMethod(typeof(PawnPatches), nameof(AddNaniteBoostRead)));
			harmony.Patch(AccessTools.Method(typeof(GeneUtility), nameof(GeneUtility.AddedAndImplantedPartsWithXenogenesCount)),
				postfix: new HarmonyMethod(typeof(PawnPatches), nameof(AddMechanizationToTranshumanistThought)));
			harmony.Patch(AccessTools.Method(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), new []{ typeof(PawnGenerationRequest) }),
				postfix: new HarmonyMethod(typeof(PawnPatches), nameof(ModifyGeneratedPawn)));
			harmony.Patch(AccessTools.Method(typeof(LifeStageWorker), nameof(LifeStageWorker.Notify_LifeStageStarted)),
				postfix: new HarmonyMethod(typeof(PawnPatches), nameof(OnPawnAgeUp)));
			harmony.Patch(AccessTools.Method(typeof(PregnancyUtility), nameof(PregnancyUtility.ApplyBirthOutcome)),
				postfix: new HarmonyMethod(typeof(PawnPatches), nameof(PostBirth)));
			// harmony.Patch(AccessTools.Method(typeof(FloatMenuMakerMap), "AddHumanlikeOrders"),
			// 	postfix: new HarmonyMethod(typeof(PawnPatches), nameof(AddOptionToCarryToBreeder)));
			harmony.Patch(AccessTools.Method(typeof(FloatMenuMakerMap), "GetOptions"),
				postfix: new HarmonyMethod(typeof(PawnPatches), nameof(AddOptionToCarryToBreeder)));
		}
		
		
		
		// public static void AddOptionToCarryToBreeder(List<FloatMenuOption> opts, Vector3 clickPos, Pawn pawn)
		// {
		// 	foreach (LocalTargetInfo item in GenUI.TargetsAt(clickPos, CompMechaniteBreeder.GetTargetingParameters()))
		// 	{
		// 		Pawn pawn2 = item.Pawn;
		// 		if (pawn2 != pawn)
		// 		{
		// 			CompMechaniteBreeder.AddCarryToJobs(opts, pawn, pawn2);
		// 		}
		// 	}
		// }
		
		public static void AddOptionToCarryToBreeder(ref List<FloatMenuOption> __result, List<Pawn> selectedPawns, Vector3 clickPos, ref FloatMenuContext context)
		{
			if (selectedPawns.Empty())
			{
				return;
			}
			foreach (LocalTargetInfo item in GenUI.TargetsAt(clickPos, CompMechaniteBreeder.GetTargetingParameters()))
			{
				Pawn pawn2 = item.Pawn;
				if (!selectedPawns.Contains(pawn2))
				{
					CompMechaniteBreeder.AddCarryToJobs(__result, selectedPawns[0], pawn2);
				}
				
				
			}
		}
		

		public static void PostBirth(ref Thing __result, RitualOutcomePossibility outcome, float quality, Precept_Ritual ritual, List<GeneDef> genes, Pawn geneticMother, Thing birtherThing, Pawn father = null, Pawn doctor = null, LordJob_Ritual lordJobRitual = null, RitualRoleAssignments assignments = null, bool preventLetter = false)
		{
			if (__result is Pawn { Dead: false } baby)
			{
				baby.GetNaniteTracker().LatentArchites = ((geneticMother?.GetNaniteTracker()?.AllowedNaniteTypes?.Contains(NMF_DefsOf.THNMF_Archite) ?? false) && Rand.Bool) || ((father?.GetNaniteTracker()?.AllowedNaniteTypes?.Contains(NMF_DefsOf.THNMF_Archite) ?? false) && Rand.Bool);
			}
		}
		public static void OnPawnAgeUp(Pawn pawn, LifeStageDef previousLifeStage)
		{
			if (previousLifeStage == LifeStageDefOf.HumanlikeChild)
			{
				if (pawn.GetNaniteTracker()?.LatentArchites ?? false)
				{
					pawn.GetNaniteTracker().GrantArchites(Rand.Range(5, 10));
					pawn.health.AddHediff(NMF_DefsOf.THNMF_ArchiteBorn);
					Find.LetterStack.ReceiveLetter("THNMF.ArchitesInherited".Translate(), "THNMF.ArchitesInheritedDescription".Translate(pawn.Name.ToStringShort), LetterDefOf.PositiveEvent, new LookTargets(pawn));
				}
			}
		}
		public static void ModifyGeneratedPawn(ref Pawn __result, PawnGenerationRequest request)
		{
			if (__result == null) return;
			if (__result.RaceProps.IsMechanoid)
			{
				__result.health.overrideDeathOnDownedChance = 0.8f;
			}
			PawnSpawnNaniteUtility.RunPawnMechanizeChance(__result, request);
		}
		
        
		
		
		
		
		public static void AddMechanizationToTranshumanistThought(ref int __result, Pawn pawn)
		{
			if (pawn.IsMechanized()) __result++;
		}
		
	    public static void AddNaniteBoostRead(ref float __result, HediffSet diffSet, PawnCapacityDef capacity,
			List<PawnCapacityUtility.CapacityImpactor> impactors = null, bool forTradePrice = false)
	    {
		    GameComponent_NanomachineFoundry comp = Current.Game.GetComponent<GameComponent_NanomachineFoundry>();
			
			if (comp.TryGetCapEffectingTracker(diffSet.pawn, out NaniteTracker_Pawn tracker))
			{
				if (tracker.TryGetCapOffset(capacity, out float offset))
				{
					impactors?.Add(new CapacityImpactorNanite
					{
						capacity = capacity
					});
					__result += offset;
					__result = GenMath.RoundedHundredth(Mathf.Max(__result, capacity.minValue));
				}
			}
		}
		/*public static void AddNaniteBoost(ref float __result, PawnCapacityWorker __instance, HediffSet diffSet,
				PawnCapacityDef capacity, List<PawnCapacityUtility.CapacityImpactor> impactors = null)
		//public static void AddNaniteBoost(ref float __result, HediffSet diffSet, PawnCapacityDef capacity,
		//	List<PawnCapacityUtility.CapacityImpactor> impactors = null, bool forTradePrice = false)
		{
			GameComponent_NanomachineFoundry comp = Current.Game.GetComponent<GameComponent_NanomachineFoundry>();
			if (comp.GetCapEffectingTracker(diffSet.pawn, out NaniteTracker_Pawn tracker))
			{
				if (tracker.TryGetCapOffset(capacity, out float offset))
				{
					impactors?.Add(new CapacityImpactorNanite());
					__result += offset;
					__result = GenMath.RoundedHundredth(Mathf.Max(__result, capacity.minValue));
				}
			}
		}*/
		
		public static IEnumerable<CodeInstruction> StatGetValueTranspile(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			List<CodeInstruction> codeInstructionsList = instructions.ToList();
			FieldInfo ageTrackerField = AccessTools.Field(typeof(Pawn), nameof(Pawn.ageTracker));
			int startIndex = codeInstructionsList.FindIndex(
				codeInstructionsList.FindIndex(codeInstruction => codeInstruction.LoadsField(ageTrackerField)),
				codeInstruction => codeInstruction.opcode == OpCodes.Stloc_0) + 1;
			
			// //Language generator :) (I don't know what that means)
			foreach (CodeInstruction command in new[]
			         {
				         new CodeInstruction(OpCodes.Ldloc_1),
				         new CodeInstruction(OpCodes.Ldloca, 0),
				         new CodeInstruction(OpCodes.Ldarg_0),
				         //Stat is the protected statDef in the StatWorker
				         new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(StatWorker), "stat")),
				         new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PawnPatches), nameof(ApplyStatModification)))
			         })
			{
				codeInstructionsList.Insert(startIndex++, command);
			}
			
			
			return codeInstructionsList;


		}
		
		//Referenced near end of StatGetValueTranspile
		public static void ApplyStatModification(Pawn pawn, ref float val, StatDef stat)
		{
			GameComponent_NanomachineFoundry comp = Current.Game.GetComponent<GameComponent_NanomachineFoundry>();
			if (comp.TryGetStatEffectingTracker(pawn, out NaniteTracker_Pawn tracker))
			{
				tracker.AffectStat(ref val, stat);
			}
		}
		public static IEnumerable<CodeInstruction> StatGetExplanationTranspile(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			List<CodeInstruction> codeInstructionsList = instructions.ToList();
			FieldInfo ageTrackerField = AccessTools.Field(typeof(Pawn), nameof(Pawn.ageTracker));
			int startIndex = codeInstructionsList.FindIndex(codeInstruction => codeInstruction.LoadsField(ageTrackerField)) - 1;
			// Log.Message(startIndex);
			
			// Log.Message(codeInstructionsList[startIndex]);
			// Label label = (Label)codeInstructionsList[codeInstructionsList.FindIndex(startIndex, codeInstruction => codeInstruction.opcode == OpCodes.Beq_S)].operand;
			// int index = codeInstructionsList.FindIndex(startIndex, ins => ins.opcode == OpCodes.Pop) + 1;
			// codeInstructionsList[index].labels.Remove(label);
			foreach (CodeInstruction command in new[]
			         {
				         // new CodeInstruction(OpCodes.Ldloc_2).WithLabels(label),
				         // new CodeInstruction(OpCodes.Ldloc_0),
				         // new CodeInstruction(OpCodes.Ldarg_0),
				         // new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(StatWorker), "stat")),
				         // new CodeInstruction(OpCodes.Ldarg_0),
				         // new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PawnPatches), nameof(GetStatModifyExplanation)))
				         new CodeInstruction(OpCodes.Ldloc_0),
				         new CodeInstruction(OpCodes.Ldarg_2),
				         new CodeInstruction(OpCodes.Ldarg_0),
				         new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(StatWorker), "stat")),
				         new CodeInstruction(OpCodes.Ldarg_0),
				         new CodeInstruction(OpCodes.Call,
					         AccessTools.Method(typeof(PawnPatches), nameof(GetStatModifyExplanation)))
			         })
			{
				codeInstructionsList.Insert(startIndex, command);
				// Log.Message("Inserts: " + command.opcode + " between " + codeInstructionsList[startIndex - 1].opcode + " and " + codeInstructionsList[startIndex + 1].opcode);
				startIndex++;
			}

			// Pawn thing1 = new Pawn();
			// StringBuilder sb = new StringBuilder();
			// StatDef stat = new StatDef();
			// StatWorker worker = new StatWorker();
			//
			// GetStatModifyExplanation(thing1, sb, stat, worker);

			
			return codeInstructionsList;
		}

		public static void GetStatModifyExplanation(Pawn pawn, StringBuilder builder, StatDef stat, StatWorker worker)
		{
			GameComponent_NanomachineFoundry comp = Current.Game.GetComponent<GameComponent_NanomachineFoundry>();
			if (comp.TryGetStatEffectingTracker(pawn, out NaniteTracker_Pawn tracker))
			{
				if (tracker.TryGetStatFactor(stat, out float factor))
				{
					builder.AppendLine(string.Format("THNMF.NaniteFactor".Translate(), worker.ValueToString(factor, finalized: false, ToStringNumberSense.Factor)));
				}
				if (tracker.TryGetStatOffset(stat, out float offset))
				{
					builder.AppendLine(string.Format("THNMF.NaniteOffset".Translate(), worker.ValueToString(offset, finalized: false, ToStringNumberSense.Offset)));
				}
			}
		}
		
		

		
		//Used for blood loss
		public static bool OnHediffSeverityAdjusted(Pawn pawn, HediffDef hdDef, float sevOffset)
		{
			if (hdDef != HediffDefOf.BloodLoss) return true;
			if (!pawn.IsMechanized()) return true;
			NaniteTracker_Pawn tracker = pawn.GetNaniteTracker();
			if (sevOffset > 0)
			{
				tracker.OnBled(sevOffset);
			}
			return true;
		}

		
		
	}
}
