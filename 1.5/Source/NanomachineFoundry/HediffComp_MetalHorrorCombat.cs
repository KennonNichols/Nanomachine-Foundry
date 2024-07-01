using System.Security.Cryptography;
using RimWorld;
using Verse;

namespace NanomachineFoundry
{
    public class HediffCompProperties_MetalHorrorCombat: HediffCompProperties
    {
        public const int MinTicksBetweenInjury = 1500;
        public const int MaxTicksBetweenInjury = 3000;
        public const float EmergeChance = 0.005f;
        public const float MinCutDamage = 1f;
        public const float MaxCutDamage = 3f;

        public HediffCompProperties_MetalHorrorCombat()
        {
            compClass = typeof(HediffComp_MetalHorrorCombat);
        }
    }
    
    public class HediffComp_MetalHorrorCombat: HediffComp
    {
        private HediffCompProperties_MetalHorrorCombat Props => (HediffCompProperties_MetalHorrorCombat)props;
        private int _ticksUntilNextInjury = 7500;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref _ticksUntilNextInjury, "thnmf_ticksUntilNextInjury");
        }


        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (!MetalhorrorUtility.IsInfected(Pawn))
            {
                RemoveSelf();
                return;
            }

            _ticksUntilNextInjury--;

            if (_ticksUntilNextInjury <= 0)
            {
                Attack();
            }
        }


        private void Attack()
        {
            _ticksUntilNextInjury = Rand.Range(HediffCompProperties_MetalHorrorCombat.MinTicksBetweenInjury, HediffCompProperties_MetalHorrorCombat.MaxTicksBetweenInjury);
            if (Rand.Chance(HediffCompProperties_MetalHorrorCombat.EmergeChance))
            {
                Emerge();
            }
            else
            {
                //Cut them
                DamageInfo damageInfo = new DamageInfo(DamageDefOf.Cut, Rand.Range(HediffCompProperties_MetalHorrorCombat.MinCutDamage, HediffCompProperties_MetalHorrorCombat.MaxCutDamage), hitPart: Pawn.health.hediffSet.GetRandomNotMissingPart(DamageDefOf.Cut));
                damageInfo.SetAllowDamagePropagation(val: true);
                damageInfo.SetIgnoreArmor(ignoreArmor: true);
                Pawn.TakeDamage(damageInfo);
            }
            
        }

        private void Emerge()
        {
            MetalhorrorUtility.TryEmerge(Pawn, string.Format("THNMF.MetalhorrorProvoked".Translate(), Pawn.Name.ToStringShort));
            RemoveSelf();
        }

        private void RemoveSelf()
        {
            Pawn.health.hediffSet.hediffs.Remove(parent);
        }
    }
}