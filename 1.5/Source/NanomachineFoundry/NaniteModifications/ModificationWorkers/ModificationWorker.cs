using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using HarmonyLib;
using NanomachineFoundry.NaniteModifications.ModificationAbilities;
using NanomachineFoundry.Utils;
using RimWorld;
using Verse;

namespace NanomachineFoundry.NaniteModifications.ModificationWorkers
{
	public class ModificationWorker
	{
		public NaniteModificationDef def;
		public Pawn pawn;

		private float _naniteLevel;
		
		
		
		private Dictionary<StatDef, float> ActiveStatOffsets => _cachedStatOffets ??= CalculateStatOffsets();
		private Dictionary<StatDef, float> _cachedStatOffets;
		private Dictionary<StatDef, float> ActiveStatFactors => _cachedStatFactors ??= CalculateStatFactors();
		private Dictionary<StatDef, float> _cachedStatFactors;
		private Dictionary<PawnCapacityDef, float> ActiveCapacityOffsets => _cachedCapacityOffsets ??= CalculateCapOffsets();
		private Dictionary<PawnCapacityDef, float> _cachedCapacityOffsets;
		
		public ModificationWorker(NaniteModificationDef def, Pawn pawn)
		{
			this.def = def;
			this.pawn = pawn;
		}

		public virtual void Tick()
		{
		}

		public virtual void TickRare()
		{
		}

		public virtual void TickLong()
		{
		}
		
		public virtual void AfterRecalculated(bool isActive)
		{
		}

		public virtual void RecalculateStats(float naniteLevel, int intendedNaniteLevel = 0, NaniteDef type = null)
		{
			_naniteLevel = naniteLevel;

			_cachedStatFactors = null;
			_cachedStatOffets = null;
			_cachedCapacityOffsets = null;

			pawn.health.capacities.Notify_CapacityLevelsDirty();	
			
			
			//Recalculate nanite levels in the ability
			if (def.ability != null)
			{
				Ability ability = pawn.abilities.GetAbility(def.ability);
				foreach (AbilityComp abilityComp in ability.comps)
				{
					if (abilityComp is NaniteCompAbilityEffect naniteAbility)
					{
						naniteAbility.RecalculateStats(naniteLevel, type);
					}
				}
			}
			if (def.hediff != null)
			{
				//Set severity of associated hediff
				if (pawn.health.hediffSet.TryGetHediff(def.hediff, out Hediff hediff))
				{
					hediff.Severity = naniteLevel / 40;
				}
			}
		}

		public virtual void ApplyStatOffsets(ref Dictionary<StatDef, float> offsets)
		{
			foreach (KeyValuePair<StatDef, float> offset in ActiveStatOffsets)
			{
				offsets[offset.Key] = offsets.GetWithFallback(offset.Key) + offset.Value;
			}
		}

		public virtual void ApplyStatFactors(ref Dictionary<StatDef, float> factors)
		{
			foreach (KeyValuePair<StatDef, float> factor in ActiveStatFactors)
			{
				factors[factor.Key] = factors.GetWithFallback(factor.Key, 1) + factor.Value;
			}
		}
		
		public virtual void ApplyCapacityOffsets(ref Dictionary<PawnCapacityDef, float> offsets)
		{
			foreach (KeyValuePair<PawnCapacityDef, float> offset in ActiveCapacityOffsets)
			{
				offsets[offset.Key] = offsets.GetWithFallback(offset.Key) + offset.Value;
			}
		}
		
		private Dictionary<StatDef, float> CalculateStatOffsets()
		{
			Dictionary<StatDef, float> runningOffsets = new Dictionary<StatDef, float>();
			if (def.scalingStatOffsets == null) return runningOffsets;
			
			foreach (ModificationScalingStatAffector statAffector in def.scalingStatOffsets)
			{
				runningOffsets[statAffector.affectedStat] = statAffector.getScaledValue(_naniteLevel);
			}
			return runningOffsets;
		}
		
		private Dictionary<PawnCapacityDef, float> CalculateCapOffsets()
		{
			Dictionary<PawnCapacityDef, float> runningOffsets = new Dictionary<PawnCapacityDef, float>();
			if (def.scalingCapacityOffsets == null) return runningOffsets;
			foreach (ModificationScalingCapacityAffector capAffector in def.scalingCapacityOffsets)
			{
				runningOffsets[capAffector.affectedCapacity] = capAffector.getScaledValue(_naniteLevel);
			}
			return runningOffsets;
		}
		
		private Dictionary<StatDef, float> CalculateStatFactors()
		{
			Dictionary<StatDef, float> runningFactors = new Dictionary<StatDef, float>();
			if (def.scalingStatFactors == null) return runningFactors;
			foreach (ModificationScalingStatAffector statAffector in def.scalingStatFactors)
			{
				//Base for factors is 1
				runningFactors[statAffector.affectedStat] = statAffector.getScaledValue(_naniteLevel);
			}	
			return runningFactors;
		}







		public string ConfigID(string valueName) => $"thnmf_workerConfig_{def.defName}_{valueName}";

		protected void DoConfigLabel(Listing_Standard listingStandard)
		{
			listingStandard.Label(def.LabelCap);
			listingStandard.GapLine();
		}

		public virtual void ScribeConfigs()
		{
			def.ScribeAllConfigs(this);
		}
		
		public virtual void DoConfigMenu(Listing_Standard listingStandard, ref bool anyChange)
		{
			DoConfigLabel(listingStandard);
			if (listingStandard.ButtonText("ResetBinding".Translate(), widthPct: 0.2f))
			{
				ResetConfig();
				anyChange = true;
			}

			def.DoAutoConfigMenu(listingStandard, ref anyChange);
		}

		protected virtual void ResetConfig()
		{
			def.ResetAutoConfig();
		}
		
		public virtual bool HasConfig => def.AutoConfigurable;
	}


	public class CapacityImpactorNanite : PawnCapacityUtility.CapacityImpactorCapacity
	{
		public override string Readable(Pawn pawn)
		{
			return "THNMF.NaniteCapBonus".Translate();
		}
	}
}
