using RimWorld;
using UnityEngine;
using Verse;

namespace NanomachineFoundry.NaniteModifications.ModificationAbilities
{
    public class CompProperties_Rotcloud : CompProperties_NaniteCloud
    {
        
        public CompProperties_Rotcloud()
        {
            compClass = typeof (CompAbilityEffect_Rotcloud);
        }
    }
    
    public class CompAbilityEffect_Rotcloud : CompAbilityEffect_NaniteCloud
    {
        public override void RecalculateStats(float naniteLevel, NaniteDef allocatedType = null)
        {
            base.RecalculateStats(naniteLevel, allocatedType);
            //7 cells per level
            _radius = Mathf.Sqrt(4 * naniteLevel / Mathf.PI);
            //1 second per level
            _durationTicks = (int)(60 * 1.5 * naniteLevel);
        }

        //Rot cloud does friendly fire, so they do not shoot it near themselves
        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            if (parent.pawn.Faction == Faction.OfPlayer)
            {
                return false;
            }
            if (target is { HasThing: true, Thing: Pawn _ })
            {
                return parent.pawn.Position.DistanceTo(target.Cell) > _radius;
            }
            return false;
        }
    }
}