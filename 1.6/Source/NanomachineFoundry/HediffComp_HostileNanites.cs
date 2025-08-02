using RimWorld;
using Verse;

namespace NanomachineFoundry
{
    public class HediffCompProperties_HostileNanites: HediffCompProperties
    {
        public float severityLossPerTick = 0.001f;
        public float possessionChance = 0.2f;
        public HediffDef possessionHediff;
        
        public HediffCompProperties_HostileNanites()
        {
            compClass = typeof(HediffComp_HostileNanites);
        }
    }
    
    public class HediffComp_HostileNanites: HediffComp
    {
        private HediffCompProperties_HostileNanites Props => (HediffCompProperties_HostileNanites)props;


        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (!Pawn.Awake())
            {
                parent.Severity -= Props.severityLossPerTick;
            }
        }

        public override void CompPostMerged(Hediff other)
        {
            base.CompPostMerged(other);
            if (Rand.Chance(Props.possessionChance * parent.Severity))
            {
                BecomePossessed();
            }
        }

        private void BecomePossessed()
        {
            Pawn.health.hediffSet.hediffs.Remove(parent);
            Pawn.health.AddHediff(Props.possessionHediff);
        }
    }
}