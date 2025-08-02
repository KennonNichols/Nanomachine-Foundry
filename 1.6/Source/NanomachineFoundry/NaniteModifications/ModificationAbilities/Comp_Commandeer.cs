using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace NanomachineFoundry.NaniteModifications.ModificationAbilities
{
    public class CompProperties_Commandeer : CompProperties_AbilityEffect
    {

        
        public CompProperties_Commandeer() => compClass = typeof (CompAbilityEffect_Commandeer);

    }
    
    
    
  public class CompAbilityEffect_Commandeer : NaniteCompAbilityEffect
    {
        public CompProperties_Commandeer Props => (CompProperties_Commandeer) props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            if (CanApplyToThing(target.Thing, out Pawn mechanoid)) CommandeerMechanoid(mechanoid, target.ToTargetInfo(parent.pawn.Map));
        }

        public override void Apply(GlobalTargetInfo target)
        {
            base.Apply(target);
            if (CanApplyToThing(target.Thing, out Pawn mechanoid)) CommandeerMechanoid(mechanoid, (TargetInfo)target);
        }

        private void CommandeerMechanoid(Pawn mechanoid, TargetInfo target)
        {
            EffecterDefOf.ApocrionAoeWarmup.Spawn().Trigger(target, target);
            SoundDefOf.ControlMech_Complete.PlayOneShot(target);
            SpendNanites(mechanoid);
            mechanoid.SetFaction(parent.pawn.Faction);
            parent.pawn.relations.AddDirectRelation(PawnRelationDefOf.Overseer, mechanoid);
        }


        public override bool CanApplyOn(GlobalTargetInfo target)
        {
            return CanApplyToThing(target.Thing, out _);
        }

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            return CanApplyToThing(target.Thing, out _);
        }

        private bool CanApplyToThing(Thing thing, out Pawn mechanoid)
        {
            if (IsThingMechanoid(thing, out mechanoid))
            {
                if (CanAffordCast(mechanoid))
                {
                    if (parent.pawn.mechanitor.UsedBandwidth + mechanoid.GetStatValue(StatDefOf.BandwidthCost) <=
                        (double)parent.pawn.mechanitor.TotalBandwidth)
                    {
                        if (mechanoid.RaceProps.AnyPawnKind != DefDatabase<PawnKindDef>.GetNamed("Mech_Apocriton"))
                        {
                            if (!mechanoid.IsColonyMech)
                            {
                                return true;
                            }
                            else
                            {
                                Messages.Message( "THNMF.CannotCommandeerFriendly".Translate(), thing, MessageTypeDefOf.RejectInput);
                            }
                        }
                        else
                        {
                            Messages.Message("THNMF.NotControllable".Translate(), thing, MessageTypeDefOf.RejectInput);
                        }
                    }
                    else
                    {
                        Messages.Message("THNMF.NotEnoughBandwidth".Translate(), thing, MessageTypeDefOf.RejectInput);
                    }
                }
                else
                {
                    Messages.Message("THNMF.CannotAffordMechanites".Translate(), thing, MessageTypeDefOf.RejectInput);
                }
            }
            else
            {
                Messages.Message("THNMF.MustTargetMechanoid".Translate(), thing, MessageTypeDefOf.RejectInput);
            }
            parent.ResetCooldown();
            return false;
        }

        private bool CanAffordCast(Pawn mechanoid)
        {
            return AllocatedNaniteLevel >= mechanoid.BodySize * 7;
        }

        private void SpendNanites(Pawn mechanoid)
        {
            parent.pawn.GetNaniteTracker().LoseNanites(AllocatedNaniteType, mechanoid.BodySize * 5, true);
        }
        
        private static bool IsThingMechanoid(Thing suspectedMechanoid, out Pawn mechanoid)
        {
            mechanoid = suspectedMechanoid switch
            {
                Corpse corpse => corpse.InnerPawn,
                Pawn pawn => pawn,
                _ => null
            };
            return mechanoid != null && mechanoid.RaceProps.IsMechanoid;
        }
        
        public override bool AICanTargetNow(LocalTargetInfo target) => false;
    }
}