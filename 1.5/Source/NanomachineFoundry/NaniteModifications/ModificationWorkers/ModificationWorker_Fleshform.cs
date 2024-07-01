using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace NanomachineFoundry.NaniteModifications.ModificationWorkers
{
    public class ModificationWorker_Fleshform: ModificationWorker
    {
        private int _recoveryTimeTicks;
        private Dictionary<Hediff, int> _partsToHeal = new Dictionary<Hediff, int>();
        private Dictionary<BodyPartDef, HediffDef> _fleshReplacements = new Dictionary<BodyPartDef, HediffDef>()
        {
            { BodyPartDefOf.Arm, HediffDefOf.Tentacle },
            { BodyPartDefOf.Leg, HediffDefOf.Tentacle },
            { BodyPartDefOf.Lung, HediffDefOf.FleshmassLung },
        };
        
        
        public ModificationWorker_Fleshform(NaniteModificationDef def, Pawn pawn) : base(def, pawn)
        {
        }

        public override void TickRare()
        {
            foreach (Hediff hediffToHeal in pawn.health.hediffSet.hediffs.Where(hediff =>
                         !_partsToHeal.ContainsKey(hediff) && (hediff.def == HediffDefOf.MissingBodyPart || _fleshReplacements.ContainsValue(hediff.def))).ToArray())
            {
                _partsToHeal.Add(hediffToHeal, hediffToHeal.tickAdded + _recoveryTimeTicks);
                if (hediffToHeal.def != HediffDefOf.MissingBodyPart ||
                    !_fleshReplacements.TryGetValue(hediffToHeal.Part.def, out HediffDef replacement)) continue;
                pawn.health.RestorePart(hediffToHeal.Part);
                pawn.health.AddHediff(replacement, hediffToHeal.Part);
                FleshbeastUtility.MeatSplatter(3, pawn.PositionHeld, pawn.MapHeld);
            }
        }

        public override void TickLong()
        {
            foreach (Hediff lostPart in _partsToHeal.Keys.ToArray())
            {
                if (_partsToHeal[lostPart] >= GenTicks.TicksGame) continue;
                    
                //We have fixed it
                _partsToHeal.Remove(lostPart);
                if (pawn.health.hediffSet.PartIsMissing(lostPart.Part))
                {
                    pawn.health.RestorePart(lostPart.Part);
                    continue;
                }
                    
                Hediff lostPartHediff = pawn.health.hediffSet.GetDirectlyAddedPartFor(lostPart.Part);
                if (lostPartHediff == null)
                {
                    continue;
                }
                    
                if (!_fleshReplacements.ContainsValue(lostPartHediff.def)) continue;
                pawn.health.RemoveHediff(lostPartHediff);
                pawn.health.RestorePart(lostPart.Part);
            }
        }


        public override void RecalculateStats(float naniteLevel, int intendedNaniteLevel, NaniteDef type = null)
        {
            base.RecalculateStats(naniteLevel, 0, type);
            _recoveryTimeTicks = (int)(3600 * def.TryGetScaledValueSlow(intendedNaniteLevel, "RecoveryTimeHours"));
        }
        
    }
}