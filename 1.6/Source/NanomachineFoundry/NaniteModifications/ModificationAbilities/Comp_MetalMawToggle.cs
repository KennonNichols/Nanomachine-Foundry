using System;
using System.Collections.Generic;
using HarmonyLib;
using NanomachineFoundry.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace NanomachineFoundry.NaniteModifications.ModificationAbilities
{
    public class CompProperties_MetalMawToggle : CompProperties_AbilityEffect
    {
        public CompProperties_MetalMawToggle()
        {
            compClass = typeof (CompAbilityEffect_MetalMawToggle);
        }
    }
    
    public class CompAbilityEffect_MetalMawToggle : NaniteCompAbilityEffect
    {
        public CompProperties_MetalMawToggle Props => (CompProperties_MetalMawToggle)props;
        private float _metalMawSeverity;
        
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            //Remove it
            if (HasMawOut(out Hediff maw))
            {
                parent.pawn.health.RemoveHediff(maw);
            }
            //Add it
            else
            {
                Hediff mawToAdd = HediffMaker.MakeHediff(NMF_DefsOf.THNMF_MetalMaw, parent.pawn);
                mawToAdd.Severity = _metalMawSeverity;
                //mawToAdd.TryGetComp<HediffComp_GetsPermanent>().IsPermanent = true;
                parent.pawn.health.hediffSet.AddDirect(mawToAdd);
            }
        }
        
        public override void RecalculateStats(float naniteLevel, NaniteDef allocatedType = null)
        {
            base.RecalculateStats(naniteLevel, allocatedType);
            //Severity is 1/40th of level
            _metalMawSeverity = naniteLevel / 40;
        }

        private bool HasMawOut(out Hediff maw)
        {
            return parent.pawn.health.hediffSet.TryGetHediff(NMF_DefsOf.THNMF_MetalMaw, out maw);
        }
        
        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            if (parent.pawn.Faction == Faction.OfPlayer)
            {
                return false;
            }
            return !HasMawOut(out _);
        }
    }
}