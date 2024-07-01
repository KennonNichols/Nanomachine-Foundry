using RimWorld;
using UnityEngine;
using Verse;

namespace NanomachineFoundry.NaniteModifications.ModificationWorkers
{
    public class ModificationWorker_RefuteMortality: ModificationWorker_ResurrectionParent
    {
        public ModificationWorker_RefuteMortality(NaniteModificationDef def, Pawn pawn) : base(def, pawn)
        {
        }

        private float _reviveCost;
        
        public override HediffDef ResurrectionHediffDef => NMF_DefsOf.THNMF_RefuteMortalityResurrection;

        public override bool CanResurrect(out bool isPermanentlyDead)
        {
            float architeLevel = pawn.GetNaniteTracker().NaniteLevels.GetWithFallback(NMF_DefsOf.THNMF_Archite);
            isPermanentlyDead = architeLevel < _reviveCost;
            return !isPermanentlyDead;
        }

        
        
        public override void RecalculateStats(float naniteLevel, int intendedNaniteLevel, NaniteDef type = null)
        {
            base.RecalculateStats(naniteLevel, 0);
            _reviveCost = Mathf.Max(4, pawn.GetNaniteTracker().NaniteConfigRatios.GetWithFallback(NMF_DefsOf.THNMF_Archite) - (pawn.GetNaniteTracker().GetAllocation(def)?.Amount ?? 0) * 2);
        }

        public override void OnResurrect()
        {
            pawn.GetNaniteTracker().TryChangeNanitesLevel(NMF_DefsOf.THNMF_Archite, -_reviveCost, true);
            base.OnResurrect();
        }
    }
}