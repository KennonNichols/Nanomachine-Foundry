using RimWorld;
using Verse;

namespace NanomachineFoundry
{
    abstract class HediffCompProperties_NaniteDecayParent: HediffCompProperties
    {
        //Determines the frequency of rot being inflicted
        //Shut the actual fuck up, Microsoft. This is just not an issue, and for some reason you don't have a fucking button to just suppress the warning >:(
        #pragma warning disable CS0649
        public float rotChancePerTick;
        public float maxDamagePerRot;
        public float minDamagePerRot;
        #pragma warning restore CS0649
    }

    class HediffCompProperties_NaniteDecay: HediffCompProperties_NaniteDecayParent
    {

        public HediffCompProperties_NaniteDecay()
        {
            compClass = typeof(HediffComp_NaniteDecay);
        }
    }
    class HediffCompProperties_NaniteBrainDecay : HediffCompProperties_NaniteDecayParent
    {

        public HediffCompProperties_NaniteBrainDecay()
        {
            compClass = typeof(HediffComp_NaniteBrainDecay);
        }
    }

    abstract class HediffComp_NaniteDecayParent: HediffComp
    {
        public HediffCompProperties_NaniteDecayParent Props => (HediffCompProperties_NaniteDecayParent)props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (Rand.Chance(Props.rotChancePerTick * parent.Severity))
            {

                DamageInfo damageInfo = new DamageInfo(NMF_DefsOf.THNMF_Decay, Rand.Range(Props.minDamagePerRot, Props.maxDamagePerRot), hitPart: getBodyPartTarget());
                //dinfo.SetAllowDamagePropagation(val: false);
                damageInfo.SetIgnoreArmor(ignoreArmor: true);
                Pawn.TakeDamage(damageInfo);
                if (Rand.Chance(0.1f))
                {
                    FilthMaker.TryMakeFilth(Pawn.Position, Pawn.MapHeld, ThingDefOf.Filth_CorpseBile, Pawn.LabelShort);
                }
            }
        }

        protected abstract BodyPartRecord getBodyPartTarget();
    }

    class HediffComp_NaniteDecay: HediffComp_NaniteDecayParent
    {
        new public HediffCompProperties_NaniteDecay Props => (HediffCompProperties_NaniteDecay)props;

        protected override BodyPartRecord getBodyPartTarget()
        {
            return Pawn.health.hediffSet.GetRandomNotMissingPart(NMF_DefsOf.THNMF_Decay);
        }
    }

    class HediffComp_NaniteBrainDecay : HediffComp_NaniteDecayParent
    {
        new public HediffCompProperties_NaniteBrainDecay Props => (HediffCompProperties_NaniteBrainDecay)props;

        protected override BodyPartRecord getBodyPartTarget()
        {
            /*BodyPartRecord head = Pawn.health.hediffSet.GetBodyPartRecord(BodyPartDefOf.Head);
            if (head != null)
            {
                IEnumerable<BodyPartRecord> brain = head.GetChildParts(BodyPartTagDefOf.ConsciousnessSource);
                if (brain.Any())
                {
                    return brain.First();
                }
            }*/
            return Pawn.health.hediffSet.GetBrain();
        }
    }
}
