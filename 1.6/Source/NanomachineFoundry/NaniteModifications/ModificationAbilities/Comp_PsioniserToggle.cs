using System;
using System.Collections.Generic;
using HarmonyLib;
using NanomachineFoundry.NaniteModifications.ModificationWorkers;
using NanomachineFoundry.Utils;
using RimWorld;
using UnityEngine;
using Verse;
using Object = UnityEngine.Object;

namespace NanomachineFoundry.NaniteModifications.ModificationAbilities
{
    public class CompProperties_PsioniserToggle : CompProperties_AbilityEffect
    {
        public CompProperties_PsioniserToggle()
        {
            compClass = typeof (CompAbilityEffect_PsioniserToggle);
        }
    }
    
    public class CompAbilityEffect_PsioniserToggle : NaniteCompAbilityEffect
    {
        public CompProperties_PsioniserToggle Props => (CompProperties_PsioniserToggle)props;
        
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            if (parent.pawn.IsModificationWorkerEnabled(out ModificationWorker_Psioniser psioniser))
            {
                psioniser.ToggleTrance();
            }
        }

        public override bool CanCast => parent.pawn.HasPsylink;
    }
}