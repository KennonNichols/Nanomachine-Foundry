using RimWorld;
using UnityEngine;
using Verse;

namespace NanomachineFoundry.NaniteModifications.ModificationAbilities
{
    public class CompProperties_EjectNanites : CompProperties_NaniteCloud
    {
        
        public CompProperties_EjectNanites()
        {
            compClass = typeof (CompAbilityEffect_EjectNanites);
        }
    }
    
    public class CompAbilityEffect_EjectNanites : CompAbilityEffect_NaniteCloud
    {
        public override void RecalculateStats(float naniteLevel, NaniteDef allocatedType = null)
        {
            base.RecalculateStats(naniteLevel, allocatedType);
            //7 cells per level
            _radius = Mathf.Sqrt(7 * naniteLevel / Mathf.PI);
            //1 second per level
            _durationTicks = (int)(60 * naniteLevel);
        }
    }
}