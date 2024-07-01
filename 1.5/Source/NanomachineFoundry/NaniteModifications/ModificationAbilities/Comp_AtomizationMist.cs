using RimWorld;
using UnityEngine;
using Verse;

namespace NanomachineFoundry.NaniteModifications.ModificationAbilities
{
    public class CompProperties_AtomizationMist : CompProperties_NaniteCloud
    {
        
        public CompProperties_AtomizationMist()
        {
            compClass = typeof (CompAbilityEffect_AtomizationMist);
        }
    }
    
    public class CompAbilityEffect_AtomizationMist : CompAbilityEffect_NaniteCloud
    {
        public override void RecalculateStats(float naniteLevel, NaniteDef allocatedType = null)
        {
            base.RecalculateStats(naniteLevel, allocatedType);
            //7 cells per level
            _radius = Mathf.Sqrt(10 * naniteLevel / Mathf.PI);
            //5 seconds + 2 second per level
            _durationTicks = (int)(60 * (5 + 2 * naniteLevel));
        }
    }
}