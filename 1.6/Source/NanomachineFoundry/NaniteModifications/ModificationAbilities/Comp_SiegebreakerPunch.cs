using NanomachineFoundry.NaniteModifications.ModificationWorkers;
using RimWorld;
using UnityEngine;
using Verse;

namespace NanomachineFoundry.NaniteModifications.ModificationAbilities
{
    public class CompProperties_SiegebreakerPunch : CompProperties_AbilityEffect
    {
        
        public CompProperties_SiegebreakerPunch()
        {
            compClass = typeof (CompAbilityEffect_SiegebreakerPunch);
        }
    }
    
    public class CompAbilityEffect_SiegebreakerPunch : NaniteCompAbilityEffect
    {
        private SimpleCurve handDamageCurve = new SimpleCurve
        {
            new CurvePoint(0f, 0.05f),
            new CurvePoint(0.3f, 0.2f),
            new CurvePoint(0.65f, 0.6f),
            new CurvePoint(1f, 0.9f),
        };
        
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            DoPunch(target);
        }

        
        private bool _uninhibited;

        public override void RecalculateStats(float naniteLevel, NaniteDef allocatedType = null)
        {
            base.RecalculateStats(naniteLevel, allocatedType);
            
            _uninhibited = parent.pawn.IsModificationWorkerEnabled(out ModificationWorker_InstinctInhibitor _);
        }

        private void DoPunch(LocalTargetInfo target)
        {
            BodyPartRecord hand = null;
            
            foreach (BodyPartRecord notMissingPart in parent.pawn.health.hediffSet.GetNotMissingParts())
            {
                if (notMissingPart.def == BodyPartDefOf.Hand)
                {
                    if (parent.pawn.health.hediffSet.GetPartHealth(notMissingPart) > 2f || _uninhibited)
                    {
                        hand = notMissingPart;
                        break;
                    }
                }
            }
            if (hand == null)
            {
                Messages.Message("THNMF.PawnHasNoHand".Translate(), parent.pawn, MessageTypeDefOf.RejectInput);
                return;
            }

            float handHealth = parent.pawn.health.hediffSet.GetPartHealth(hand);
            float handPercentage = handHealth / BodyPartDefOf.Hand.hitPoints;
            //Overhealthed hands do not give a damage boost, but underhealthed hands do confer a damage loss.
            handPercentage = Mathf.Min(1, handPercentage);
         
            
            float damage = 50 + 2 * AllocatedNaniteLevel * Mathf.Pow(parent.pawn.skills.GetSkill(SkillDefOf.Melee).Level, .333f);
            if (_uninhibited)
            {
                damage *= 1.5f;
            }
            
            
            float intendedDamage = damage * handPercentage;
            
            target.Thing.TakeDamage(new DamageInfo(DamageDefOf.Blunt, intendedDamage, 1f));
            //Do animation
            parent.pawn.Drawer.Notify_MeleeAttackOn(target.Thing);
            parent.pawn.rotationTracker.FaceCell(target.Thing.Position);
            

            if (target.Thing is Pawn pawn)
            {
                if (pawn.Dead)
                {
                    return;
                }
                pawn.stances.stagger.StaggerFor(480);
            }
            
            if (target.Thing.Destroyed)
            {
                return;
            }
            
            
            float targetHpRemainingFraction =  (float)target.Thing.HitPoints / target.Thing.MaxHitPoints;
            
            
            float randomSalt = Rand.Range(-0.5f, 0.5f);

            float handDamageFractionDealt = handDamageCurve.Evaluate(targetHpRemainingFraction + randomSalt);

            float handDamageDealt = handDamageFractionDealt * BodyPartDefOf.Hand.hitPoints * handPercentage;
            
            if (!_uninhibited)
            {
                handDamageDealt = Mathf.Min(handDamageDealt, 0.9f * handHealth);
            }

            parent.pawn.TakeDamage(new DamageInfo(DamageDefOf.Crush,
                handDamageDealt, 1, hitPart: hand));

        }

        
        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            if (parent.pawn.Faction == Faction.OfPlayer)
            {
                return false;
            }
            return target is { HasThing: true, Thing: Pawn _ };
        }
    }
}