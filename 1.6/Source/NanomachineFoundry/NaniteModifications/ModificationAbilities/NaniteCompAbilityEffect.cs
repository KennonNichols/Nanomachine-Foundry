using RimWorld;

namespace NanomachineFoundry.NaniteModifications.ModificationAbilities
{
    public abstract class NaniteCompAbilityEffect: CompAbilityEffect
    {
        protected NaniteDef AllocatedNaniteType { get; private set; }
        protected float AllocatedNaniteLevel { get; private set; }
        
        public virtual void RecalculateStats(float naniteLevel, NaniteDef allocatedType = null)
        {
            AllocatedNaniteType = allocatedType;
            AllocatedNaniteLevel = naniteLevel;
        }
    }
}   