using System;
using System.Collections.Generic;
using HarmonyLib;
using NanomachineFoundry.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace NanomachineFoundry.NaniteModifications.ModificationAbilities
{
    public class CompProperties_InsanityPulse : CompProperties_AbilityEffect
    {
        public MentalStateDef[] Defs => _cachedDefs ??= new[] {
            DefDatabase<MentalStateDef>.GetNamedSilentFail("TerrifyingHallucinations"),
            DefDatabase<MentalStateDef>.GetNamed("GiveUpExit"),
            //DefDatabase<MentalStateDef>.GetNamed("Wander_Psychotic_Short")
        };
        private MentalStateDef[] _cachedDefs;
        
        public CompProperties_InsanityPulse()
        {
            compClass = typeof (CompAbilityEffect_InsanityPulse);
        }
    }
    
    public class CompAbilityEffect_InsanityPulse : NaniteCompAbilityEffect
    {
        public CompProperties_InsanityPulse Props => (CompProperties_InsanityPulse)props;
        private float _berserkChance;
        
        private Traverse cooldown => cachedTraverse ??= Traverse.Create(parent).Field("cooldownDuration");
        private Traverse cachedTraverse; 
        
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn pawn = target.Pawn;
            if (pawn == null)
                return;
            if (pawn.Dead)
                return;
            
            MentalStateDef breakDef;
            if (Rand.Chance(_berserkChance))
            {
                breakDef = MentalStateDefOf.Berserk;
            }
            else
            {
                breakDef = Props.Defs.RandomElement();
            }
            
            pawn.mindState.mentalStateHandler.TryStartMentalState(breakDef, forced: true, forceWake: true);
            Find.BattleLog.Add(new BattleLogEntry_AbilityUsed(parent.pawn, pawn, parent.def, RulePackDefOf.Event_AbilityUsed));
        }
        
        public override void RecalculateStats(float naniteLevel, NaniteDef allocatedType = null)
        {
            base.RecalculateStats(naniteLevel, allocatedType);
            //Set cooldown
            cooldown.SetValue((int)(60 * (60 - 2.5 * naniteLevel)));
            _berserkChance = .1f + .045f * naniteLevel;
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