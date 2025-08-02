using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using NanomachineFoundry.NaniteModifications;
using NanomachineFoundry.NaniteModifications.ModificationWorkers;
using RimWorld;
using UnityEngine;
using Verse;
using StatDef = RimWorld.StatDef;

namespace NanomachineFoundry.Utils
{
    public class NaniteTracker_Pawn : IExposable
	{
		private static readonly Dictionary<int, NaniteTracker_Pawn> TRACKERS = new Dictionary<int, NaniteTracker_Pawn>();
		public const int MaxCapacity = 40;


		private HashSet<NaniteModificationDef> _activeModifications = new HashSet<NaniteModificationDef>();
		public HashSet<ModificationWorker> ActiveModWorkers => _cachedModWorkers ??= _activeModifications.Select(def => def.GetNewWorkerInstance(Pawn)).ToHashSet();
		private HashSet<ModificationWorker> _cachedModWorkers;
		private HashSet<ModificationWorker> ActiveStatAffectors => _cachedStatAffectors ??= ActiveModWorkers.Where(worker => worker.def.statAffector).ToHashSet();
		private HashSet<ModificationWorker> _cachedStatAffectors;
		private HashSet<ModificationWorker> ActiveCapacityAffectors => _cachedCapacityAffectors ??= ActiveModWorkers.Where(worker => worker.def.capAffector).ToHashSet();
		private HashSet<ModificationWorker> _cachedCapacityAffectors;
		
		private Dictionary<StatDef, float> ActiveStatOffsets => _cachedStatOffets ??= CalculateStatOffsets();
		private Dictionary<StatDef, float> _cachedStatOffets;
		private Dictionary<StatDef, float> ActiveStatFactors => _cachedStatFactors ??= CalculateStatFactors();
		private Dictionary<StatDef, float> _cachedStatFactors;
		private Dictionary<PawnCapacityDef, float> ActiveCapacityOffsets => _cachedCapacityOffsets ??= CalculateCapacityOffsets();
		private Dictionary<PawnCapacityDef, float> _cachedCapacityOffsets;
		
		
		private HashSet<NaniteModificationDef> _allowedModifications = new HashSet<NaniteModificationDef>();
		private Dictionary<NaniteModificationDef, ModAllocation> _modAllocations;
		private Dictionary<NaniteModificationDef, ModAllocation> ModAllocations => _modAllocations ??= new Dictionary<NaniteModificationDef, ModAllocation>();
		public HashSet<NaniteModificationDef> AllowedModifications => _allowedModifications ??= new HashSet<NaniteModificationDef>();

		private HashSet<ModificationWorker> _queuedWorkersToRecalculate = new HashSet<ModificationWorker>();
		
		///This is NOT a real cache, and is instead psychic abilities that the psychic cache can access.
		public Dictionary<AbilityDef, int> PsychicAbilityCache => _cachedPsychicAbilityCache ??= new Dictionary<AbilityDef, int>();

		private Dictionary<AbilityDef, int> _cachedPsychicAbilityCache;
		
		private HashSet<NaniteDef> _allowedNaniteTypes = new HashSet<NaniteDef>();
		private int _naniteCapacity;
		private Dictionary<NaniteDef, int> _naniteConfigRatios = new Dictionary<NaniteDef, int>();
		private Dictionary<NaniteDef, float> _naniteLevels = new Dictionary<NaniteDef, float>();
		public PawnNaniteFillMode FillMode = PawnNaniteFillMode.ForceRenewable;
		private bool _neurorewired;
		public List<SkillDef> EnabledSkills = new List<SkillDef>();
		private Dictionary<SkillDef, Passion> _savedPassions;

		public bool Entranced;
		public bool LatentArchites;

		public Dictionary<NaniteDef, float> RelativeNaniteLevels => _cachedRelativeNaniteLevels ??= _naniteConfigRatios.ToDictionary(pair => pair.Key,
			pair => _naniteLevels.GetWithFallback(pair.Key) / pair.Value);
		private Dictionary<NaniteDef, float> _cachedRelativeNaniteLevels;

		public bool Neurorewired => _neurorewired;
		public bool HasModifications => _activeModifications.Any();

		
		public void ExposeData()
		{
			Scribe_Values.Look(ref Entranced, "thnmf_entranced");
			Scribe_Values.Look(ref LatentArchites, "thnmf_latentArchites");
			Scribe_Collections.Look(ref _activeModifications, "thnmf_modifications", LookMode.Def);
			Scribe_Collections.Look(ref _allowedNaniteTypes, "thnmf_allowedNanites", LookMode.Def);
			Scribe_Collections.Look(ref _allowedModifications, "thnmf_allowedModification", LookMode.Def);
			Scribe_Values.Look(ref _naniteCapacity, "thnmf_naniteCapacity");
			Scribe_Collections.Look(ref _naniteConfigRatios, "thnmf_naniteRatios", LookMode.Def);
			Scribe_Collections.Look(ref _naniteLevels, "thnmf_naniteLevels", LookMode.Def);
			Scribe_Collections.Look(ref _modAllocations, "thnmf_modAllocations", LookMode.Def, LookMode.Deep);
			Scribe_Values.Look(ref FillMode, "thnmf_fillMode");
			Scribe_Values.Look(ref _neurorewired, "thnmf_neurorewired");
			Scribe_Collections.Look(ref EnabledSkills, "thnmf_enabledSkills", LookMode.Def);
			Scribe_Collections.Look(ref _savedPassions, "thnmf_savedPassions", LookMode.Def);
			Scribe_Collections.Look(ref _cachedPsychicAbilityCache, "thnmf_psychicAbilityCache", LookMode.Def);
			
			UpdateTrackerNaniteLevels();
		}
		
		
		/// <summary>
		/// Returns true if the pawn can continue being mechanized.
		/// </summary>
		/// <param name="amount"></param>
		/// <returns></returns>
        internal bool IncreaseCapacity(int amount)
        {
	        bool currentlyZero = _naniteCapacity == 0;
	        _naniteCapacity += amount;
	        
			if (currentlyZero)
			{
				Find.LetterStack.ReceiveLetter(string.Format("THNMF.PawnMechanizedAlert".Translate(), Pawn.Name.ToStringShort), string.Format("THNMF.PawnMechanizedAlertDescription".Translate(), Pawn.NameShortColored), LetterDefOf.PositiveEvent);
			}
			else if (_naniteCapacity >= MaxCapacity)
            {
				Find.LetterStack.ReceiveLetter(string.Format("THNMF.PawnFullyMechanizedAlert".Translate(), Pawn.Name.ToStringShort), string.Format("THNMF.PawnFullyMechanizedAlertDescription".Translate(), Pawn.NameShortColored), LetterDefOf.PositiveEvent);
			}
			else
			{
				Messages.Message(string.Format("THNMF.PawnMechanizationIncrease".Translate(), Pawn.PossessiveCap(), _naniteCapacity, _naniteCapacity + amount), MessageTypeDefOf.PositiveEvent);
			}

			return _naniteCapacity < MaxCapacity;
        }

		private Pawn Pawn { get; }
		
        private IEnumerable<NaniteEffectCategoryDef> GetAllValidEffectCategories()
        {
	        
	        return _allowedNaniteTypes.SelectMany(naniteType => naniteType.categories).ToHashSet();
        }
        
        public bool DoesPawnHaveNaniteOfCategory(NaniteEffectCategoryDef categoryDef)
        {
	        return GetAllValidEffectCategories().Contains(categoryDef);
        }

		public IEnumerable<NaniteModificationDef> AllActiveModifications => _activeModifications;
		public HashSet<NaniteDef> AllowedNaniteTypes => _allowedNaniteTypes;
		public int NaniteCapacity => _naniteCapacity;
		public ref int CapacityReference()
        {
			return ref _naniteCapacity;
        }
		public Dictionary<NaniteDef, int> NaniteConfigRatios => _naniteConfigRatios;
		public Dictionary<NaniteDef, float> NaniteLevels => _naniteLevels;

		private NaniteTracker_Pawn(Pawn pawn)
		{
			Pawn = pawn;
		}

		public static void Save(Pawn __instance)
        {
            NaniteTracker_Pawn target = Get(__instance);
            Scribe_Deep.Look(ref target, true, "thnmf_naniteTracker", __instance);
            TRACKERS[__instance.thingIDNumber] = target;
        }

		
		public static NaniteTracker_Pawn Get(Pawn pawn)
		{
			if (pawn == null)
			{
				return null;
			}
			if (TRACKERS.TryGetValue(pawn.thingIDNumber, out var value) && value != null)
			{
				return value;
			}
			if (PawnCanMechanize(pawn))
			{
				value = new NaniteTracker_Pawn(pawn);
				TRACKERS.SetOrAdd(pawn.thingIDNumber, value);
				return value;
			}
			return null;
		}

		public static bool PawnCanMechanize(Pawn pawn)
		{
			return (int)(pawn.RaceProps?.intelligence ?? 0) >= 2;
        }

		public bool NaniteForceMode(NaniteDef naniteType)
		{
			return FillMode switch
			{
				PawnNaniteFillMode.ForceAlways => true,
				PawnNaniteFillMode.ForceRenewable => naniteType.renewable,
				PawnNaniteFillMode.ForceNever => false,
				_ => throw new ArgumentOutOfRangeException()
			};
		}

		public void SetNaniteAllocation(NaniteModificationDef mod, ModAllocation allocation)
		{
			if (allocation.Amount == 0 && IsModActive(mod))
			{
				DisableModification(mod);
			}
			else if (allocation.Amount > 0 && !IsModActive(mod))
			{
				EnableModification(mod);
			}
			ModAllocations.SetOrAdd(mod, allocation);
			UpdateTrackerNaniteLevels();
		}
		public bool IsModActive(NaniteModificationDef mod)
		{
			return _activeModifications.Contains(mod);
		}

		public int AllocationToMod(NaniteModificationDef mod)
		{
			return ModAllocations.GetWithFallback(mod).Amount;
		}

		public int FreeAllocatedSpaceOfType(NaniteDef naniteType, NaniteModificationDef modToIgnore = null)
		{
			return _naniteConfigRatios.GetWithFallback(naniteType) - ModAllocations.Where(pair => pair.Value.Def == naniteType && pair.Key != modToIgnore)
				.Select(pair => pair.Value.Amount).Sum();
		}

		public ModAllocation GetAllocation(NaniteModificationDef modificationDef)
		{
			return ModAllocations.GetWithFallback(modificationDef);
		}
		
        
		/// <summary>
		/// Attempt to change pawn nanite level by the given amount. 
		/// </summary>
		/// <param name="typeDef"></param>
		/// <param name="change"></param>
		/// <param name="force">Whether to force the change. If true, the pawn nanite level will change, even if it they don't have enough capacity/nanites to change the full amount.</param>
		/// <returns>Whether the pawn was capable of gaining/losing the full amount specified.</returns>
		public bool TryChangeNanitesLevel(NaniteDef typeDef, float change, bool force = false, bool update = true)
		{
			float currentValue = _naniteLevels.GetWithFallback(typeDef);
			int typeCapacity = _naniteConfigRatios.GetWithFallback(typeDef);
			float newSum = currentValue + change;
			if (newSum < 0 || newSum > typeCapacity)
			{
				if (!force) return false;
				newSum = Mathf.Clamp(newSum, 0, typeCapacity);
			}
			if (Mathf.Abs(newSum - currentValue) < 0.0001) return false;
			_naniteLevels.SetOrAdd(typeDef, newSum);
			if (update)
			{
				UpdateTrackerNaniteLevels();
			}
			return true;
		}

		private int ConfigSum => _naniteConfigRatios?.Values.Sum() ?? 0;

		public int ConfigRemaining => _naniteCapacity - ConfigSum;

		public int HighestConfig => _naniteConfigRatios?.Values.Max() ?? 0;

		/// <summary>
		/// Lose nanites of a specific type
		/// </summary>
		/// <param name="naniteType"></param>
		/// <param name="amount">The amount to lose. This should be positive.</param>
		/// <param name="healthy">Whether this loss was expected. If set to true, pawn will not experience demechanization shock.</param>
		/// <param name="update">Whether to recalculate stats after this loss. This should only be set to false on frequently-called small changes.</param>
		public void LoseNanites(NaniteDef naniteType, float amount, bool healthy = false, bool update = true)
        {
	        if (!healthy)
	        {
		        Hediff hediff = HediffMaker.MakeHediff(naniteType.shockHediff, Pawn);
		        //Shock should scale based on population
		        hediff.Severity = (amount / _naniteLevels.GetWithFallback(naniteType, 1));
		        Pawn.health.AddHediff(hediff);
	        }
	        TryChangeNanitesLevel(naniteType, -amount, true, update);
        }


		private int dropsUntilUpdate = 60;
		public void OnBled(float amount)
		{
			//Only update trackers once every 60 times blood is lost
			bool update = false;
			dropsUntilUpdate++;
			if (dropsUntilUpdate < 0)
			{
				update = true;
				dropsUntilUpdate = 60;
			}
			
			List<NaniteDef> naniteDefs = _naniteLevels.Keys.Where(naniteDef => _naniteLevels.GetWithFallback(naniteDef) > 0).ToList();
			foreach (NaniteDef naniteDef in naniteDefs)
			{
				//Lose that nanite
				LoseNanites(naniteDef, NaniteLossAdjusted(amount, naniteDef), update);
			}
		}
		private float NaniteLossAdjusted(float baseLossAmount, NaniteDef naniteType)
		{
			if (naniteType.absoluteLoss)
			{
				return baseLossAmount * _naniteCapacity;
			}
			return baseLossAmount * _naniteLevels.GetWithFallback(naniteType);
		}
		public void OnHediffAdded(Hediff hediff)
		{
			Log.Message("hediff inflicted on mechanized pawn:");
			Log.Message(hediff);

			if (hediff.def == HediffDefOf.BloodLoss)
			{
				Log.Message("Lost " + hediff.Severity + " blood.");
			}
		}
		
		public IEnumerable<NaniteDef> GetConfigurableNaniteTypes(bool devMode)
		{
			return devMode ? DefDatabase<NaniteDef>.AllDefs : _allowedNaniteTypes.Where(def => def.modifiable);
		}

		public bool IsNaniteTypeAllowed(NaniteDef naniteType)
        {
			return _allowedNaniteTypes.Contains(naniteType);
        }

		public void DisallowNaniteType(NaniteDef naniteType)
        {
			_allowedNaniteTypes.Remove(naniteType);
			RemoveNanitePassiveBuff(naniteType);
		}

		private void GetNanitePassiveBuff(NaniteDef naniteType)
		{
			if (naniteType.passiveHediff != null)
			{
				Pawn.health.AddHediff(naniteType.passiveHediff);
			}
		}

		private void RemoveNanitePassiveBuff(NaniteDef naniteType)
		{
			if (naniteType.passiveHediff == null) return;
			Pawn_HealthTracker health = Pawn.health;
			object obj;
			if (health == null)
			{
				obj = null;
			}
			else
			{
				HediffSet hediffSet = health.hediffSet;
				obj = hediffSet?.GetFirstHediffOfDef(naniteType.passiveHediff);
			}
			Hediff val2 = (Hediff)obj;
			if (val2 != null)
			{
				Pawn.health.RemoveHediff(val2);
			}
		}
		
		public IEnumerable<NaniteDef> GetRefillableTypes()
		{
			return _allowedNaniteTypes.Where(def => def.modifiable && _naniteLevels.GetWithFallback(def, 0) < (_naniteConfigRatios.GetWithFallback(def, 0) + (NaniteForceMode(def) ? 0 : 1)));
		}

		public float GetNaniteLevelPercent(NaniteDef type)
		{
			return _naniteLevels.GetWithFallback(type) / _naniteConfigRatios.GetWithFallback(type, 1);
		}

		public void AllowNaniteType(NaniteDef naniteType)
		{
			GetNanitePassiveBuff(naniteType);
			_allowedNaniteTypes.Add(naniteType);
		}

		internal void SetConfiguration(Dictionary<NaniteDef, int> tempConfigs)
        {
			foreach (NaniteDef naniteType in tempConfigs.Keys)
            {
				int newValue = tempConfigs.GetWithFallback(naniteType);
				_naniteConfigRatios.SetOrAdd(naniteType, newValue);
				if (_naniteLevels.GetWithFallback(naniteType, 0) > newValue)
				{
					_naniteLevels.SetOrAdd(naniteType, newValue);
				}

				//This is for if a type was manually set in dev mode
				if (newValue > 0)
                {
					AllowNaniteType(naniteType);
                }
			}
        }

		public void Neurorewire(SkillDef[] skillsToBoost)
		{
			_savedPassions = new Dictionary<SkillDef, Passion>();
			foreach (SkillDef skill in DefDatabase<SkillDef>.AllDefs.Where(skillsToBoost.Contains))
			{
				//Store passion for if it gets undone
				_savedPassions[skill] = Pawn.skills.GetSkill(skill).passion;
				//Give burning passion in that skill
				Pawn.skills.GetSkill(skill).passion = Passion.Major;
			}
			_neurorewired = true;
			EnabledSkills = skillsToBoost.ToList();
			Pawn.Notify_DisabledWorkTypesChanged();
		}

		public void DebugUndoNeurorewire()
		{
			//Load saved passions
			foreach (KeyValuePair<SkillDef, Passion> passions in _savedPassions)
			{
				Pawn.skills.GetSkill(passions.Key).passion = passions.Value;
			}
			
			_savedPassions = null;
			_neurorewired = false;
			EnabledSkills = null;
			
			Pawn.Notify_DisabledWorkTypesChanged();
		}

		public void DisallowModification(NaniteModificationDef mod)
		{
			SetNaniteAllocation(mod, new ModAllocation(null, 0));
			_allowedModifications.Remove(mod);
			OnModificationsChanged();
		}

		public void SetStatModifiersToRecalculate()
		{
			_cachedStatOffets = null;
			_cachedStatFactors = null;
		}

		private void OnModificationsChanged()
		{
			_cachedCapacityOffsets = null;
			_cachedStatOffets = null;
			_cachedStatFactors = null;
			_cachedStatAffectors = null;
			_cachedModWorkers = null;
		}
		
		private void UpdateTrackerNaniteLevels()
		{
			_cachedRelativeNaniteLevels = null;
			
			//Update the nanite levels of each tracker
			ActiveModWorkers.Do(worker =>
			{
				if (!ModAllocations.ContainsKey(worker.def))
				{
					worker.RecalculateStats(0);
					return;
				}
				ModAllocation alloc = ModAllocations[worker.def];
				worker.RecalculateStats(alloc.Amount * RelativeNaniteLevels.GetWithFallback(alloc.Def), alloc.Amount, alloc.Def);
			});

			_cachedCapacityOffsets = null;
			_cachedStatOffets = null;
			_cachedStatFactors = null;
			
			
			ActiveModWorkers.Where(worker => !_queuedWorkersToRecalculate.Contains(worker)).Do(worker => worker.AfterRecalculated(true));
			_queuedWorkersToRecalculate.Do(worker => worker.AfterRecalculated(false));
			_queuedWorkersToRecalculate.Clear();
		}

		public void AllowModification(NaniteModificationDef modificationDef)
		{
			_allowedModifications.Add(modificationDef);
		}

		public void EnableModification(NaniteModificationDef modificationDef)
		{
			OnModificationsChanged();
			_activeModifications.Add(modificationDef);

			GameComponent_NanomachineFoundry gameComponentNanomachineFoundry =
				Current.Game.GetComponent<GameComponent_NanomachineFoundry>();
			//Add to central ticker if that modification has a ticker
			if (modificationDef.ticker)
			{
				gameComponentNanomachineFoundry.NoteNewTicker(Pawn);
			}
			if (modificationDef.statAffector)
			{
				gameComponentNanomachineFoundry.NoteNewStatAffector(Pawn);
			}
			if (modificationDef.capAffector)
			{
				gameComponentNanomachineFoundry.NoteNewCapAffector(Pawn);
			}
			if (modificationDef.abilityGiver)
			{
				Pawn.abilities.GainAbility(modificationDef.ability);
			}
			if (modificationDef.hediffGiver)
			{
				Pawn.health.AddHediff(modificationDef.hediff);
			}
		}

		public void DisableModification(NaniteModificationDef modificationDef)
		{
			_queuedWorkersToRecalculate.AddRange(ActiveModWorkers.Where(worker => worker.def == modificationDef));
			OnModificationsChanged();
			_activeModifications.Remove(modificationDef);
			
			GameComponent_NanomachineFoundry gameComponentNanomachineFoundry =
				Current.Game.GetComponent<GameComponent_NanomachineFoundry>();
			//Remove if there are no more tickers on this tracker
			if (!_activeModifications.Any(def => def.ticker))
			{
				gameComponentNanomachineFoundry.RemoveTicker(Pawn);
			}
			if (!_activeModifications.Any(def => def.statAffector))
			{
				gameComponentNanomachineFoundry.RemoveStatAffector(Pawn);
			}
			if (!_activeModifications.Any(def => def.capAffector))
			{
				gameComponentNanomachineFoundry.RemoveCapAffector(Pawn);
			}
			if (modificationDef.abilityGiver)
			{
				Pawn.abilities.RemoveAbility(modificationDef.ability);
			}
			if (modificationDef.hediffGiver)
			{
				if (Pawn.health.hediffSet.TryGetHediff(modificationDef.hediff, out Hediff given))
				{
					Pawn.health.RemoveHediff(given);
				}
			}
		}

		public bool IsModificationAllowed(NaniteModificationDef modificationDef)
		{
			return _allowedModifications.Contains(modificationDef);
		}
		
		//Modifications
		public void TickWorkers()
		{
			ActiveModWorkers.Do(worker => worker.Tick());
		}

		public void TickWorkersRare()
		{
			ActiveModWorkers.Do(worker => worker.TickRare());
		}

		public void TickWorkersLong()
		{
			ActiveModWorkers.Do(worker => worker.TickLong());
		}

		public NaniteDef FirstNaniteTypeOfCategory(NaniteEffectCategoryDef effectCategoryDef)
		{
			NaniteDef[] types = _allowedNaniteTypes.Where(def => def.categories.Contains(effectCategoryDef)).ToArray();
			if (types.Any())
			{
				return types.First();
			}
			Log.Error("Pawn has no valid nanites to allocate");
			return null;
		}

		/// <summary>
		/// Sets archites levels to this amount exactly. Will randomly remove other nanites if this brings it over max capacity.
		/// </summary>
		/// <param name="amount"></param>
		public void GrantArchites(int amount)
		{
			if (amount > MaxCapacity)
			{
				Log.Error("Tried granting more archites than pawn max capacity");
				return;
			}
			if (amount < _naniteConfigRatios.GetWithFallback(NMF_DefsOf.THNMF_Archite)) return;
			AllowNaniteType(NMF_DefsOf.THNMF_Archite);
			
			_naniteCapacity += amount - _naniteConfigRatios.GetWithFallback(NMF_DefsOf.THNMF_Archite);
			_naniteConfigRatios[NMF_DefsOf.THNMF_Archite] = amount;
			_naniteLevels[NMF_DefsOf.THNMF_Archite] = amount;

			while (_naniteCapacity > MaxCapacity)
			{
				RemoveRandomNonArchite();
			}
			return;

			void RemoveRandomNonArchite()
			{
				_naniteCapacity -= 1;
				NaniteDef naniteToRemove = _allowedNaniteTypes
					.Where(def => def != NMF_DefsOf.THNMF_Archite && NaniteConfigRatios[def] > 0).RandomElement();
				NaniteConfigRatios[naniteToRemove] -= 1;
				NaniteLevels[naniteToRemove] =
					Math.Min(NaniteLevels[naniteToRemove], NaniteConfigRatios[naniteToRemove]);

				KeyValuePair<NaniteModificationDef, ModAllocation>[] allocationsOfNaniteType = _modAllocations
					.Where(allocation => allocation.Value.Amount > 0 && allocation.Value.Def == naniteToRemove)
					.ToArray();
				if (allocationsOfNaniteType.Sum(allocation => allocation.Value.Amount) > NaniteLevels[naniteToRemove])
				{
					KeyValuePair<NaniteModificationDef, ModAllocation> allocation =
						allocationsOfNaniteType.RandomElement();
					SetNaniteAllocation(allocation.Key, new ModAllocation(allocation.Value.Def, allocation.Value.Amount - 1));
				}
			}
		}


		private Dictionary<StatDef, float> CalculateStatOffsets()
		{
			Dictionary<StatDef, float> runningOffsets = new Dictionary<StatDef, float>();
			foreach (ModificationWorker affector in ActiveStatAffectors)
			{
				affector.ApplyStatOffsets(ref runningOffsets);
			}
			return runningOffsets;
		}
		private Dictionary<StatDef, float> CalculateStatFactors()
		{
			Dictionary<StatDef, float> runningFactors = new Dictionary<StatDef, float>();
			foreach (ModificationWorker affector in ActiveStatAffectors)
			{
				affector.ApplyStatFactors(ref runningFactors);
			}

			/*foreach (StatDef stat in runningFactors.Keys.ToArray())
			{
				if (stat == StatDefOf.PsychicSensitivity)
				{
					Log.Message($"Psychic modifier calculated as: {runningFactors[stat]}");
				}
				//Factors cannot go below 0
				runningFactors[stat] = Math.Max(float.Epsilon, runningFactors[stat]);
			}*/
			
			return runningFactors;
		}
		private Dictionary<PawnCapacityDef, float> CalculateCapacityOffsets()
		{
			Dictionary<PawnCapacityDef, float> runningFactors = new Dictionary<PawnCapacityDef, float>();
			foreach (ModificationWorker affector in ActiveCapacityAffectors)
			{
				affector.ApplyCapacityOffsets(ref runningFactors);
			}
			return runningFactors;
		}

		public bool TryGetStatOffset(StatDef stat, out float offset)
		{
			offset = ActiveStatOffsets.GetWithFallback(stat);
			return offset != 0;
		}

		public bool TryGetStatFactor(StatDef stat, out float factor)
		{
			factor = ActiveStatFactors.GetWithFallback(stat);
			return factor != 0;
		}

		public bool TryGetCapOffset(PawnCapacityDef capacity, out float offset)
		{
			offset = ActiveCapacityOffsets.GetWithFallback(capacity);
			return offset != 0;
		}
		
		public void AffectStat(ref float val, StatDef stat)
		{
			val = val * ActiveStatFactors.GetWithFallback(stat, 1) + ActiveStatOffsets.GetWithFallback(stat);
		}

		public NaniteDef GetNaniteAllocatedToModification(NaniteModificationDef mod)
		{
			if (!ModAllocations.ContainsKey(mod)) return null;
			return ModAllocations[mod].Def;
		}

		public void RefillAllNanites()
		{
			foreach (NaniteDef type in AllowedNaniteTypes)
			{
				NaniteLevels[type] = NaniteConfigRatios[type];
			}
			UpdateTrackerNaniteLevels();
		}
		
		
		public void DebugEmptyNanites()
		{
			foreach (NaniteDef type in AllowedNaniteTypes)
			{
				NaniteLevels[type] = 0;
			}
			UpdateTrackerNaniteLevels();
		}

		public void SetCapacity(int capacity)
		{
			_naniteCapacity = capacity;
		}

		private static List<Pawn> tmpPawnsToRemove = new List<Pawn>();
		private static List<int> tmpPawnIDsToRemove = new List<int>();

		public static void PurgePermadeadPawns()
		{
			tmpPawnsToRemove.Clear();
			tmpPawnIDsToRemove.Clear();
			GameComponent_NanomachineFoundry comp = Current.Game.GetComponent<GameComponent_NanomachineFoundry>();
			
			foreach (int pawnID in TRACKERS.Keys)
			{
				NaniteTracker_Pawn tracker = TRACKERS[pawnID];
				if (tracker == null)
				{
					tmpPawnIDsToRemove.Add(pawnID);
					continue;
				}
				if (!tracker.Pawn.Dead || tracker.Pawn.Corpse != null)
				{
					continue;
				}
				//If the pawn has any resurrection worker, and none of the resurrection workers are permadead, return
				if (tracker.Pawn.GetNaniteTracker().ActiveModWorkers
				    .Any(worker => worker is ModificationWorker_ResurrectionParent))
				{
					if (!tracker.Pawn.GetNaniteTracker().ActiveModWorkers
				     .Any(worker => worker is ModificationWorker_ResurrectionParent {IsPermadead: true}))
					{
						continue;
					}
				}
				
				//Eliminate pawn
				//Queue their removal from tracker dictionary
				tmpPawnsToRemove.Add(tracker.Pawn);
				//Remove them from active tickers
				comp.RemovePawnFromAllLists(tracker.Pawn);
			}
			tmpPawnsToRemove.Do(tracker => TRACKERS.Remove(tracker.thingIDNumber));
			TRACKERS.RemoveRange(tmpPawnIDsToRemove);
		}

		public static void RecalculateAllNaniteWorkers()
		{
			TRACKERS.Values.Do(tracker => tracker?.UpdateTrackerNaniteLevels());
		}
	}

	public class ModAllocation : IExposable
	{
		private NaniteDef _def;
		private int _amount;

		public ModAllocation()
		{
			_def = null;
			_amount = 0;
		}
		
		public ModAllocation(NaniteDef def, int amount)
		{
			_def = def;
			_amount = amount;
		}

		public NaniteDef Def => _def;

		public int Amount => _amount;
		
		public void ExposeData()
		{
			Scribe_Values.Look(ref _amount, "thnmf_amount");
			Scribe_Defs.Look(ref _def, "thnmf_def");
		}
	};
}
