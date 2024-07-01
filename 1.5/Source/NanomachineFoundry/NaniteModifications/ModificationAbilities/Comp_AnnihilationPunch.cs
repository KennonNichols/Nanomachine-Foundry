using System;
using System.Collections.Generic;
using HarmonyLib;
using NanomachineFoundry.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace NanomachineFoundry.NaniteModifications.ModificationAbilities
{
    public class CompProperties_AnnihilationPunch : CompProperties_AbilityEffect
    {
        
        public CompProperties_AnnihilationPunch()
        {
            compClass = typeof (CompAbilityEffect_AnnihilationPunch);
        }
    }
    
    public class CompAbilityEffect_AnnihilationPunch : NaniteCompAbilityEffect
    {
        private Traverse cooldown => cachedTraverse ??= Traverse.Create(parent).Field("cooldownDuration");
        private Traverse cachedTraverse; 
        
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            DoPunch(target);
        }
        
        public override void RecalculateStats(float naniteLevel, NaniteDef allocatedType = null)
        {
            cooldown.SetValue((int)(60 * (30 - NmfUtils.DiminishingReturnsCalculator.CalculateSumDiminishingReturnsFloatSlow(naniteLevel, 5))));
        }

        private void DoPunch(LocalTargetInfo target)
        {
            BodyPartRecord hand = null;
            
            foreach (BodyPartRecord notMissingPart in parent.pawn.health.hediffSet.GetNotMissingParts())
            {
                if (notMissingPart.def == BodyPartDefOf.Hand)
                {
                    if (parent.pawn.health.hediffSet.GetPartHealth(notMissingPart) > 0f)
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

         
            //DESTROY IT
            float damage = target.Thing.MaxHitPoints * 10;

            if (target.Pawn != null)
            {
                //Hit pawn
                target.Pawn.TakeDamage(new DamageInfo(DamageDefOf.Vaporize, damage, 1f, hitPart: target.Pawn.health.hediffSet.GetBrain()));
            }
            else
            {
                //Hit thing
                target.Thing.TakeDamage(new DamageInfo(DamageDefOf.Vaporize, damage, 1f));
            }
            //Do animation
            parent.pawn.Drawer.Notify_MeleeAttackOn(target.Thing);
            parent.pawn.rotationTracker.FaceCell(target.Thing.Position);


            Vector3 targetPos = target.Thing.Position.ToVector3();
            Vector3 pawnPos = parent.pawn.Position.ToVector3();
            
            
            
            //Explode
            GenExplosion.DoExplosion(target.Thing.Position, parent.pawn.Map, 2, DamageDefOf.Vaporize, parent.pawn, 20, ignoredThings: new List<Thing>() {parent.pawn});

            
            
            
            //Splatter filth
            ThingDef filth = null;
            if (target.Pawn != null)
            {
                //Smear?
                filth = target.Pawn.RaceProps.BloodSmearDef;
            }
            else
            {
                if (target.Thing.TryGetComp(out CompSpawnerFilthOnTakeDamage filthComp))
                {
                    filth = filthComp.Props.filthDef;
                }
            }

            

            Vector3 direction = (targetPos - pawnPos).normalized;
            float baseAngle = direction.ToAngleFlat();

            if (filth != null)
            {
                for (int i = 0; i < 20; i++)
                {
                    float angle = baseAngle + Rand.Range(-30, 30);
                    float distance = Mathf.Sqrt(Rand.Range(1, 25));
                    
                    Vector3 rotDir = Vector3Utility.FromAngleFlat(angle);
                    
                    IntVec3 cell = (targetPos + rotDir * distance).ToIntVec3();


                    if (FilthMaker.TryMakeFilth(cell, parent.pawn.Map, filth, outFilth: out Filth filthObj))
                    {
                        filthObj.Rotation = Rot4.FromAngleFlat(angle);
                    }
                }
            }
        }

        
        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            if (parent.pawn.Faction == Faction.OfPlayer)
            {
                return false;
            }
            return target is { HasThing: true, Thing: Pawn pawn };
        }
    }
}