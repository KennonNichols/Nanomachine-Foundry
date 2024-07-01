using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace NanomachineFoundry
{
    public class HediffCompProperties_ConsciousnessLossFromSeverity: HediffCompProperties
    {
        public HediffCompProperties_ConsciousnessLossFromSeverity()
        {
            compClass = typeof(HediffComp_ConsciousnessLossFromSeverity);
        }
    }

    public class HediffComp_ConsciousnessLossFromSeverity: HediffComp
    {
        public HediffCompProperties_ConsciousnessLossFromSeverity Props => (HediffCompProperties_ConsciousnessLossFromSeverity)props;

        private bool _tickedOnce = false;
        
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            UpdateConsciousnessLoss();
        }

        public override void CompPostMerged(Hediff other)
        {
            base.CompPostMerged(other);
            UpdateConsciousnessLoss();
        }

        private void UpdateConsciousnessLoss()
        {
            List<PawnCapacityModifier> offsets = parent.CurStage.capMods;
            PawnCapacityModifier consciousnessOffset = offsets.First(modifier => modifier.capacity == PawnCapacityDefOf.Consciousness);
            consciousnessOffset.offset = -parent.Severity;
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (_tickedOnce) return;
            UpdateConsciousnessLoss();
            _tickedOnce = true;
        }
    }
}