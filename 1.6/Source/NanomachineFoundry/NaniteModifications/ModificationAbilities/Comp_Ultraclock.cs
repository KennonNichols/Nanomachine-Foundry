using System;
using System.Collections.Generic;
using HarmonyLib;
using NanomachineFoundry.Utils;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace NanomachineFoundry.NaniteModifications.ModificationAbilities
{
    public class CompProperties_Ultraclock : CompProperties_AbilityEffect
    {
        public CompProperties_Ultraclock()
        {
            compClass = typeof (CompAbilityEffect_Ultraclock);
        }
    }
    
    
    public class CompAbilityEffect_Ultraclock : NaniteCompAbilityEffect
    {
        public CompProperties_Ultraclock Props => (CompProperties_Ultraclock)props;

        private float _severity;
        
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            
            Pawn pawn = target.Pawn;
            if (pawn == null)
                return;
            if (pawn.Dead)
                return;

            Hediff ultraclock = HediffMaker.MakeHediff(NMF_DefsOf.THNMF_Ultraclock, target.Pawn);
            ultraclock.Severity = _severity;
            pawn.health.AddHediff(ultraclock);
            Find.BattleLog.Add(new BattleLogEntry_AbilityUsed(parent.pawn, pawn, parent.def, RulePackDefOf.Event_AbilityUsed));
            SpendNanites();
        }

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (target is { HasThing: true, Thing: Pawn { IsColonyMech: true } })
            {
                return true;
            }
            Messages.Message("THNMF.MustTargetMechanoid".Translate(), MessageTypeDefOf.RejectInput);
            return false;
        }

        public override bool CanApplyOn(GlobalTargetInfo target)
        {
            if (target is { HasThing: true, Thing: Pawn { IsColonyMech: true } })
            {
                return true;
            }
            Messages.Message("THNMF.MustTargetMechanoid".Translate(), MessageTypeDefOf.RejectInput);
            return false;
        }

        private void SpendNanites()
        {
            parent.pawn.GetNaniteTracker().LoseNanites(AllocatedNaniteType, AllocatedNaniteLevel, true);
        }
        
        public override void RecalculateStats(float naniteLevel, NaniteDef allocatedType = null)
        {
            base.RecalculateStats(naniteLevel, allocatedType);
            _severity = naniteLevel / 40;
        }


        public override bool AICanTargetNow(LocalTargetInfo target) => false;
    }
}