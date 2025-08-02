using System;
using System.Collections.Generic;
using HarmonyLib;
using NanomachineFoundry.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace NanomachineFoundry.NaniteModifications.ModificationAbilities
{
    public class CompProperties_Conflagration : CompProperties_AbilityEffect
    {
        public readonly float IgniteChance = 0.25f;
        
        public CompProperties_Conflagration()
        {
            compClass = typeof (CompAbilityEffect_Conflagration);
        }
    }
    
    public class CompAbilityEffect_Conflagration : NaniteCompAbilityEffect
    {
        public CompProperties_Conflagration Props => (CompProperties_Conflagration)props;
        
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            GenExplosion.DoExplosion(parent.pawn.Position, parent.pawn.Map, _radius, DamageDefOf.Flame, parent.pawn, (int)_damage, chanceToStartFire: 0.8F, ignoredThings: new List<Thing> {parent.pawn});
            parent.pawn.GetNaniteTracker().LoseNanites(NMF_DefsOf.THNMF_Bionanite, AllocatedNaniteLevel, true);
        }
        
        
        private float _damage;
        private float _radius;
        
        public override void RecalculateStats(float naniteLevel, NaniteDef allocatedType = null)
        {
            base.RecalculateStats(naniteLevel, allocatedType);
            _damage = naniteLevel;
            _radius = (float)Math.Sqrt(2 * naniteLevel / Math.PI);
        }

        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            return false;
        }
    }
}