using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace NanomachineFoundry.NaniteModifications.ModificationAbilities
{
    public class CompProperties_MechanoidLeech : CompProperties_AbilityEffect
    {
        public float apocritonMultiplier = 3;
        public HediffDef shockHediff;
        public HediffDef foreignNaniteHediff;
        public CompProperties_MechanoidLeech() => compClass = typeof (CompAbilityEffect_MechanoidLeech);

    }
    
    
    
  public class CompAbilityEffect_MechanoidLeech : NaniteCompAbilityEffect
    {
        public CompProperties_MechanoidLeech Props => (CompProperties_MechanoidLeech) props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            if (CanApplyToThing(target.Thing, out Pawn mechanoid)) LeechFromMechanoid(mechanoid, target.ToTargetInfo(parent.pawn.Map));
        }

        public override void Apply(GlobalTargetInfo target)
        {
            base.Apply(target);
            if (CanApplyToThing(target.Thing, out Pawn mechanoid)) LeechFromMechanoid(mechanoid, (TargetInfo)target);
        }

        public override string ExtraLabelMouseAttachment(LocalTargetInfo target)
        {
            if (!target.HasThing) return null;
            if (!CanApplyToThingSilent(target.Thing, out Pawn mechanoid)) return null;
            StringBuilder builder = new StringBuilder();
            GetAmountsInflicted(mechanoid, out float nanitesSpent, out float severityInflicted);
            builder.AppendLine("THNMF.MechanoidLeechResult".Translate(
                mechanoid.BodySize,
                severityInflicted.ToStringPercent("0"),
                nanitesSpent.ToString("0.0"),
                (2 * mechanoid.BodySize * severityInflicted).ToString("0.0"),
                (nanitesSpent - 2 * mechanoid.BodySize * severityInflicted).ToString("0.0")));
            if (severityInflicted >= 1)
            {
                builder.AppendLine("THNMF.MechanoidLeechWillKill".Translate().Colorize(Color.red));
            }
            if (parent.pawn.health.hediffSet.HasHediff(Props.foreignNaniteHediff))
            {
                builder.AppendLine("THNMF.MechanoidLeechWarning".Translate(parent.pawn.Name.ToStringShort).Colorize(Color.yellow));
            }
            return builder.ToString();
        }

        private void LeechFromMechanoid(Pawn mechanoid, TargetInfo target)
        {
            EffecterDefOf.ApocrionAoeWarmup.Spawn().Trigger(target, target);
            SoundDefOf.ControlMech_Complete.PlayOneShot(target);
            GetAmountsInflicted(mechanoid, out float nanitesSpent, out float severityInflicted);
            //Inflict mechanoid shock
            Hediff shock = HediffMaker.MakeHediff(Props.shockHediff, mechanoid);
            shock.Severity = severityInflicted;
            mechanoid.health.AddHediff(shock);
            //Absorb the nanites, and spend them
            parent.pawn.GetNaniteTracker()
                .TryChangeNanitesLevel(NMF_DefsOf.THNMF_Mechanite, 2 * mechanoid.BodySize * severityInflicted - nanitesSpent, true);
            //Give the pawns hostile nanites
            Hediff foreignNanites = HediffMaker.MakeHediff(Props.foreignNaniteHediff, parent.pawn);
            foreignNanites.Severity = mechanoid.BodySize * severityInflicted * 0.25f;
            parent.pawn.health.AddHediff(foreignNanites);
            //Humanlike pawn Diogenes was added to non-humanlike faction mechanoid hive
        }

        public override bool HideTargetPawnTooltip => true;

        private void GetAmountsInflicted(Pawn mechanoid, out float nanitesSpent, out float severityInflicted)
        {
            //Choose the max we can spend, or the min we need to kill, whichever is lower
            nanitesSpent = AllocatedNaniteLevel;
            float minToKill = MinNanitesToKill(mechanoid);
            if (minToKill < nanitesSpent)
            {
                nanitesSpent = minToKill; 
                severityInflicted = 1;
            }
            else
            {
                severityInflicted = SeverityInflicted(mechanoid, nanitesSpent);
            }
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
                if (!mechanoid.IsColonyMech)
                {
                    return true;
                }
                Messages.Message( "THNMF.CannotLeechFriendly".Translate(), thing, MessageTypeDefOf.RejectInput);
            }
            else
            {
                Messages.Message("THNMF.MustTargetMechanoid".Translate(), thing, MessageTypeDefOf.RejectInput);
            }
            parent.ResetCooldown();
            return false;
        }
        
        private bool CanApplyToThingSilent(Thing thing, out Pawn mechanoid)
        {
            if (!IsThingMechanoid(thing, out mechanoid)) return false; 
            return !mechanoid.IsColonyMech;
        }
        
        private bool IsMechanoidApocriton(Pawn mechanoid)
        {
            return mechanoid.RaceProps.AnyPawnKind == DefDatabase<PawnKindDef>.GetNamed("Mech_Apocriton");
        }

        private float MinNanitesToKill(Pawn mechanoid)
        {
            return mechanoid.BodySize / 0.12f * (IsMechanoidApocriton(mechanoid) ? Props.apocritonMultiplier : 1);
        }
        
        private float SeverityInflicted(Pawn mechanoid, float nanitesSpent)
        {
            return nanitesSpent * 0.12f / mechanoid.BodySize / (IsMechanoidApocriton(mechanoid) ? Props.apocritonMultiplier : 1);
        }

        private void SpendNanites(float amount)
        {
            parent.pawn.GetNaniteTracker().LoseNanites(AllocatedNaniteType, amount, true);
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