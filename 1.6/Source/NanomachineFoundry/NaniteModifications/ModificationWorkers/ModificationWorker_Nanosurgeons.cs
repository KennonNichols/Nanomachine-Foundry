using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace NanomachineFoundry.NaniteModifications.ModificationWorkers
{
    public class ModificationWorker_Nanosurgeons: ModificationWorker
    {
        private float _tendQuality;
        private float _tendChancePerLongTick;
        public float BleedFactor { get; private set; }
        
        public ModificationWorker_Nanosurgeons(NaniteModificationDef def, Pawn pawn) : base(def, pawn)
        {
        }
        
        public override void TickLong()
        {
            if (Rand.Chance(_tendChancePerLongTick))
            {
                TreatRandomInjury();
            }
        }

        private void TreatRandomInjury()
        {
            Hediff[] hediffs = pawn.health.hediffSet.GetHediffsTendable().Where(hediff => hediff.TendableNow()).ToArray();
            if (hediffs.Any())
            {
                Hediff[] bleedingHediffs = hediffs.Where(hediff => hediff.Bleeding).ToArray();
                if (bleedingHediffs.Any())
                {
                    TreatInjury(bleedingHediffs.MaxBy(hediff => hediff.BleedRate));   
                }
                else
                {
                    TreatInjury(hediffs.RandomElement());
                }
            }
        }

        private void TreatInjury(Hediff injury)
        {
            injury.Tended(_tendQuality, 1);
        }

        public override void RecalculateStats(float naniteLevel, int intendedNaniteLevel, NaniteDef type = null)
        {
            base.RecalculateStats(naniteLevel, intendedNaniteLevel, type);
            _tendQuality = def.TryGetScaledValueSlow(naniteLevel, "TreatQuality");
            _tendChancePerLongTick = def.TryGetScaledValueSlow(naniteLevel, "TreatCommonality") / 30;
            BleedFactor = 1 - 0.01f * naniteLevel;
        }
    }
}