using System;
using System.Collections.Generic;
using System.Linq;
using NanomachineFoundry.NaniteModifications;
using NanomachineFoundry.NaniteModifications.ModificationAbilities;
using RimWorld;
using UnityEngine;
using Verse;
using Random = UnityEngine.Random;

namespace NanomachineFoundry.Utils
{
    public static class PawnSpawnNaniteUtility
    {
        public static NaniteDef[] StrangerSpawnNaniteTypes => _cachedStrangerSpawnNaniteTypes ??= DefDatabase<NaniteDef>.AllDefs.Where(def => def.spawnable).ToArray();
        private static NaniteDef[] _cachedStrangerSpawnNaniteTypes;
        
        public static NaniteDef[] ColonistSpawnNaniteTypes => _cachedColonistSpawnNaniteTypes ??= DefDatabase<NaniteDef>.AllDefs.Where(def => def.colonistSpawnable).ToArray();
        private static NaniteDef[] _cachedColonistSpawnNaniteTypes;
        
        public static NaniteEffectCategoryDef[] PawnSpawnEffectCategoryDefs => _cachedPawnSpawnEffectCategoryDefs ??= DefDatabase<NaniteEffectCategoryDef>.AllDefs.Where(def => def.spawnable).ToArray();
        private static NaniteEffectCategoryDef[] _cachedPawnSpawnEffectCategoryDefs;
        
        public static NaniteModificationDef[] PawnSpawnModificationDefs => _cachedPawnSpawnModificationDefs ??= DefDatabase<NaniteModificationDef>.AllDefs.Where(def => PawnSpawnEffectCategoryDefs.Contains(def.categoryDef)).ToArray();
        private static NaniteModificationDef[] _cachedPawnSpawnModificationDefs;

        private static IEnumerable<NaniteModificationDef> ValidSpawnModifications(NaniteDef naniteType) =>
            PawnSpawnModificationDefs.Where(def => naniteType.categories.Contains(def.categoryDef));
        
        public static float StrangerMechanizedSpawnChance = 1f / 15;
        public static float ColonistMechanizedSpawnChance = 1f / 20;
        
        //20 at 250000
        //40 at 500000
        
        public static SimpleCurve NaniteCapacityFromWealthCurve = new SimpleCurve
        {
            new CurvePoint(0f, 1f),
            new CurvePoint(90000f, 10f),
            new CurvePoint(250000f, 20f),
            new CurvePoint(410000f, 30f),
            new CurvePoint(500000f, 40f)
        };

        private static int _saltMin = -150000;
        private static int _saltMax = 15000;
        
        
        
        public static void RunPawnMechanizeChance(Pawn pawn, PawnGenerationRequest request)
        {
            if (pawn == null) return;
            if ((request.Faction?.def?.techLevel ?? 0) < TechLevel.Industrial) return;
            if (!NaniteTracker_Pawn.PawnCanMechanize(pawn)) return;
            Faction playerFaction = Faction.OfPlayerSilentFail;
            if (playerFaction == null) return;
            bool colonist = (request.Faction ?? playerFaction) == playerFaction;
            if (!Rand.Chance(colonist ? ColonistMechanizedSpawnChance : StrangerMechanizedSpawnChance)) return;
            var map = request.Tile >= 0 ? Current.Game.FindMap(request.Tile) : Current.Game.AnyPlayerHomeMap;
            if (map == null)
            {
                return;
            }
            
            float wealth = map.PlayerWealthForStoryteller;
            NaniteDef type = (colonist ? ColonistSpawnNaniteTypes : StrangerSpawnNaniteTypes).RandomElementByWeight(
                def => def.spawnWeight);
            NaniteModificationDef[] validModifications = ValidSpawnModifications(type).InRandomOrder().ToArray();
            
            MechanizePawn(pawn, wealth, type, validModifications);
        }
        
        
        public static void MechanizePawn(Pawn pawn, float wealth, NaniteDef type, IEnumerable<NaniteModificationDef> allowedModification)
        {
            int capacity = Mathf.Clamp((int)NaniteCapacityFromWealthCurve.Evaluate(
                wealth + Rand.Range(_saltMin, _saltMax)), 1, 40);
            NaniteTracker_Pawn tracker = pawn.GetNaniteTracker();
            tracker.SetCapacity(capacity);
            tracker.AllowNaniteType(type);
            tracker.SetConfiguration(new Dictionary<NaniteDef, int> {{type, capacity}});
            NaniteModificationDef[] validModifications = allowedModification.InRandomOrder().ToArray();
            int modificationCount = Math.Min(ModificationCountFromNaniteCapacity(capacity), validModifications.Length);
            int capacityRemaining = capacity;
            Dictionary<NaniteModificationDef, int> allocations = new Dictionary<NaniteModificationDef, int>();
            
            while (capacityRemaining > 0)
            {
                int index = Random.Range(0, modificationCount - 1);
                NaniteModificationDef mod = validModifications[index];
                tracker.AllowModification(mod);
                allocations.Increment(mod);
                capacityRemaining--;
            }
            foreach (var (naniteModificationDef, amount) in allocations)
            {
                tracker.SetNaniteAllocation(naniteModificationDef, new ModAllocation(type, amount));
            }
            tracker.RefillAllNanites();
        }

        public static void ArchosplinterizePawn(Pawn pawn)
        {
            pawn.health.AddHediff(NMF_DefsOf.THNMF_ArchosplinterLink);
            pawn.guest.Recruitable = false;

            if (!pawn.health.hediffSet.TryGetHediff(NMF_DefsOf.THNMF_ArchitePower, out Hediff power)) return;
            if (power.TryGetComp(out HediffComp_ArchitePower architePower))
            {
                architePower.SetColorsToMax();
            }
        }

        private static int ModificationCountFromNaniteCapacity(int capacity)
        {
            return Mathf.Max(1, capacity / 10 + Rand.Range(-1, 1));
        }
    }
}