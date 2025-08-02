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
    public class CompProperties_DismantlePulse : CompProperties_AbilityEffect
    {

        public Dictionary<ThingDef, float> Yields => _yields ??= new Dictionary<ThingDef, float>
        {
            { ThingDefOf.Steel, 15 },
            { ThingDefOf.Plasteel, 3 },
            { ThingDefOf.ComponentIndustrial, 1 },
            { ThingDefOf.ComponentSpacer, 0.25f },
        };
        private Dictionary<ThingDef, float> _yields;
        public float CoreChance = 0.2f;
        
        public CompProperties_DismantlePulse() => compClass = typeof (CompAbilityEffect_DismantlePulse);

    }
    
    
    
  public class CompAbilityEffect_DismantlePulse : NaniteCompAbilityEffect
    {
        public CompProperties_DismantlePulse Props => (CompProperties_DismantlePulse) props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            if (IsThingMechanoid(target.Thing, out Pawn mechanoid))
            {
                if (CanAffordCast(mechanoid))
                {
                    BreakDownMechanoid(mechanoid, target.ToTargetInfo(mechanoid.Map));
                }
                else
                {
                    Messages.Message("THNMF.CannotAffordMechanites".Translate(), target.Thing, MessageTypeDefOf.RejectInput);
                    parent.ResetCooldown();
                }
            }
            else
            {
                Messages.Message("THNMF.MustTargetMechanoid".Translate(), target.Thing, MessageTypeDefOf.RejectInput);
                parent.ResetCooldown();
            }
        }

        public override void Apply(GlobalTargetInfo target)
        {
            base.Apply(target);
            if (IsThingMechanoid(target.Thing, out Pawn mechanoid))
            {
                if (CanAffordCast(mechanoid))
                {
                    BreakDownMechanoid(mechanoid, (TargetInfo)target);
                }
                else
                {
                    Messages.Message("THNMF.CannotAffordMechanites".Translate(), target.Thing, MessageTypeDefOf.RejectInput);
                    parent.ResetCooldown();
                }
            }
            else
            {
                Messages.Message("THNMF.MustTargetMechanoid".Translate(), target.Thing, MessageTypeDefOf.RejectInput);
                parent.ResetCooldown();
            }
        }

        private void BreakDownMechanoid(Pawn mechanoid, TargetInfo target)
        {
            EffecterDefOf.ApocrionAoeWarmup.Spawn().Trigger(target, target);
            SoundDefOf.ControlMech_Complete.PlayOneShot(target);
            SpendNanites(mechanoid);
            float size = mechanoid.BodySize;
            List<ThingDef> thingsToSpawn = new List<ThingDef>();
            foreach (KeyValuePair<ThingDef, float> yield in Props.Yields)
            {
                for (int i = 0; i < yield.Value * size; i++)
                {
                    thingsToSpawn.Add(yield.Key);
                }
            }
            if (Rand.Chance(Props.CoreChance))
            {
                ThingDef core = GetCoreOfMechanoid(mechanoid);
                if (core != null) thingsToSpawn.Add(core);
            }
            thingsToSpawn.Do(def => GenSpawn.Spawn(def, mechanoid.Position, mechanoid.Map, WipeMode.VanishOrMoveAside));
            mechanoid.Destroy();
        }

        private ThingDef[] Cores => _cores ??= new ThingDef[]
        {
            DefDatabase<ThingDef>.GetNamed("SubcoreBasic"),
            DefDatabase<ThingDef>.GetNamed("SubcoreRegular"),
            DefDatabase<ThingDef>.GetNamed("SubcoreHigh"),
        };
        private ThingDef[] _cores;
            
        private ThingDef GetCoreOfMechanoid(Pawn mechanoid)
        {
            return (from recipeDef in DefDatabase<RecipeDef>.AllDefs where recipeDef.products.Any(thingDefCount => thingDefCount.thingDef == mechanoid.def) from variableIngredient1 in recipeDef.ingredients.Where(variableIngredient => variableIngredient.filter?.AllowedThingDefs?.Any(def => Cores.Contains(def)) ?? false) select variableIngredient1.filter.AllowedThingDefs.First(def => Cores.Contains(def))).FirstOrDefault();
        }

        public override bool CanApplyOn(GlobalTargetInfo target)
        {
            if (IsThingMechanoid(target.Thing, out Pawn mechanoid))
            {
                if (CanAffordCast(mechanoid))
                {
                    return true;
                }
                Messages.Message("THNMF.CannotAffordMechanites".Translate(), target.Thing, MessageTypeDefOf.RejectInput);
            }
            else
            {
                Messages.Message("THNMF.MustTargetMechanoid".Translate(), target.Thing, MessageTypeDefOf.RejectInput);
            }
            return false;
            //return target.HasThing && IsThingMechanoid(target.Thing, out Pawn mechanoid) && CanAffordCast(mechanoid);
        }

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (IsThingMechanoid(target.Thing, out Pawn mechanoid))
            {
                if (CanAffordCast(mechanoid))
                {
                    return true;
                }
                Messages.Message("THNMF.CannotAffordMechanites".Translate(), target.Thing, MessageTypeDefOf.RejectInput);
                parent.ResetCooldown();
            }
            else
            {
                Messages.Message("THNMF.MustTargetMechanoid".Translate(), target.Thing, MessageTypeDefOf.RejectInput);
                parent.ResetCooldown();
            }
            return false;
            //return target.HasThing && IsThingMechanoid(target.Thing, out Pawn mechanoid) && CanAffordCast(mechanoid);
        }

        private bool CanAffordCast(Pawn mechanoid)
        {
            return AllocatedNaniteLevel >= mechanoid.BodySize * 5;
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