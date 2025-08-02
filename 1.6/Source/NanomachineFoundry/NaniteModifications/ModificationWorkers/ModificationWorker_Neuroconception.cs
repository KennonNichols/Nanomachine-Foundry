using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace NanomachineFoundry.NaniteModifications.ModificationWorkers
{
    public class ModificationWorker_Neuroconception: ModificationWorker
    {
        private float _draftedFactor;
        private float _undraftedFactor;
        private bool _isHigh;
        
        
        public ModificationWorker_Neuroconception(NaniteModificationDef def, Pawn pawn) : base(def, pawn)
        {
        }

        public override void ApplyStatFactors(ref Dictionary<StatDef, float> factors)
        {
            base.ApplyStatFactors(ref factors);
            factors[StatDefOf.PsychicSensitivity] = factors.GetWithFallback(StatDefOf.PsychicSensitivity, 1) + (_isHigh ? _draftedFactor: _undraftedFactor);
        }


        public override void RecalculateStats(float naniteLevel, int intendedNaniteLevel, NaniteDef type = null)
        {
            base.RecalculateStats(naniteLevel, 0, type);
            _draftedFactor = naniteLevel * .1f;
            _undraftedFactor = Math.Max(-0.999f, -naniteLevel * .1f);
        }

        public override void Tick()
        {
            base.Tick();
            bool shouldBeHigh = ShouldBeHigh();
            if (_isHigh == shouldBeHigh) return;
            _isHigh = shouldBeHigh;
            pawn.GetNaniteTracker().SetStatModifiersToRecalculate();
        }

        private bool ShouldBeHigh()
        {
            if (pawn.Drafted) return true;
            if (pawn.psychicEntropy.IsCurrentlyMeditating) return true;
            return false;
        }
    }
}