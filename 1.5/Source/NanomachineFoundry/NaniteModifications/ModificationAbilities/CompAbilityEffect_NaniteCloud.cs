using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace NanomachineFoundry.NaniteModifications.ModificationAbilities
{
    
    public abstract class CompProperties_NaniteCloud : CompProperties_AbilityEffect
    {
        public ThingDef projectileDef;
        public EffecterDef sprayEffecter;
        public ThingDef naniteCloudDef;
        public HediffDef inflictionHediff;
        public NaniteModificationDef modification;
        public bool isIntelligent = true;
        public bool spendingCausesShock = false;

        public override IEnumerable<string> ConfigErrors(AbilityDef parentDef)
        {
            foreach (string error in base.ConfigErrors(parentDef))
            {
                yield return error;
            }
            if (projectileDef == null)
            {
                yield return "ERR: no projectile def.";
            }
            if (naniteCloudDef == null)
            {
                yield return "ERR: no nanite cloud def.";
            }
            if (inflictionHediff == null)
            {
                yield return "ERR: no infliction def.";
            }
            if (modification == null)
            {
                yield return "ERR: no modification specified.";
            }
        }
    }
    
    
    
    
    
    public abstract class CompAbilityEffect_NaniteCloud : NaniteCompAbilityEffect
    {
        protected float _radius;
        protected int _durationTicks;
        private readonly Verb _verb;
        private Verb Verb => _verb ?? GenerateVerb();
        
        private CompProperties_NaniteCloud Props => (CompProperties_NaniteCloud) props;
        private Pawn Pawn => parent?.pawn;

        private Verb GenerateVerb()
        {
            Log.Message("H");
            Verb_CastAbility castAbility = new Verb_CastAbility();

            Log.Message("1");
            castAbility.ability = parent;
            Log.Message("1");
            castAbility.caster = Pawn;
            Log.Message("1");
            VerbProperties verbProps = new VerbProperties()
            {
                range = 50,
                targetParams = new TargetingParameters()
                {
                    canTargetLocations = true
                }
            };
            Log.Message("1");

            castAbility.verbProps = verbProps;
            Log.Message("1");

            castAbility.verbTracker = new VerbTracker(Pawn);
            Log.Message("1");

            return castAbility;
            /*return new Verb_CastAbility
            {
                Ability = parent,
                caster = Pawn,
                verbProps = new VerbProperties()
                {
                    range = 50,
                    targetParams = new TargetingParameters()
                    {
                        canTargetLocations = true
                    }
                },
                verbTracker =
                {
                    directOwner = Pawn
                }
            };*/
        }
        
        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            if (parent.pawn.Faction == Faction.OfPlayer)
            {
                return false;
            }
            if (AllocatedNaniteLevel < 1) return false;
            return target is { HasThing: true, Thing: Pawn _ };
        }

        
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Projectile_NaniteCloudLaunch projectile = (Projectile_NaniteCloudLaunch) GenSpawn.Spawn(Props.projectileDef, Pawn.Position, Pawn.Map);
            
            projectile.ConfigureCloudPayload(AffectedCells(target), Props.naniteCloudDef, Props.inflictionHediff, Pawn, _durationTicks, Pawn.GetNaniteTracker().GetNaniteAllocatedToModification(Props.modification).altColor, Props.isIntelligent);
            projectile.Launch(Pawn, Pawn.DrawPos, target, target, ProjectileHitFlags.IntendedTarget);
            Props.sprayEffecter?.Spawn(parent.pawn.Position, target.Cell, parent.pawn.Map).Cleanup();
         
            SpendNanites();
            base.Apply(target, dest);
        }
        
        
        public override void DrawEffectPreview(LocalTargetInfo target)
        {
            GenDraw.DrawFieldEdges(AffectedCells(target).ToList());
        }

        private IEnumerable<IntVec3> AffectedCells(LocalTargetInfo target)
        {
            if (Pawn == null || Pawn.Dead)
            {
                return new List<IntVec3>();
            }
            
            return GenRadial.RadialCellsAround(target.Cell, _radius, true).Where(vec3 => Verb.TryFindShootLineFromTo(target.Cell, vec3, out ShootLine _, true));
        }
        
        private void SpendNanites()
        {
            if (AllocatedNaniteType != null)
            {
                Pawn.GetNaniteTracker().LoseNanites(AllocatedNaniteType, AllocatedNaniteLevel, !Props.spendingCausesShock);
            }
        }
    }
}