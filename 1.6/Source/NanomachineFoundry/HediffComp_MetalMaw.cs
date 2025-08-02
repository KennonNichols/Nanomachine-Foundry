using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Server;
using NanomachineFoundry.Utils;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace NanomachineFoundry
{
    public class HediffCompProperties_MetalMaw: HediffCompProperties
    {
        public ManeuverDef Maneuver => _maneuver ??=  NMF_DefsOf.Bite;
        private ManeuverDef _maneuver;
        
        public HediffCompProperties_MetalMaw()
        {
            compClass = typeof(HediffComp_MetalMaw);
        }
    }
    
    public class HediffComp_MetalMaw: HediffComp
    {
        private HediffCompProperties_MetalMaw Props => (HediffCompProperties_MetalMaw)props;

        private int _ticksUntilNextBiteAttempt;

        private float AverageCooldown => _avgCache ??= (60 * (6 - 0.1f * LevelFromSeverity));
        private float? _avgCache;
        private float Damage => _dmgCache ??= 10 + 2 * LevelFromSeverity;
        private float? _dmgCache; 
        private float ArmorPenetration => _apCache ??= 0.1f * LevelFromSeverity;
        private float? _apCache;
        //.1 seconds (6 ticks) per level
        private float RegenTickScale => _regenCache ??= 6 * LevelFromSeverity;
        private float? _regenCache;
        private float LevelFromSeverity => parent.Severity * 40;
        private int GetNewTicksUntilNextBiteAttempt => (int)(AverageCooldown * Rand.Range(0.2f, 1.8f));
        
        
        public override void CompPostTick(ref float severityAdjustment)
        {
            _ticksUntilNextBiteAttempt--;
            if (_ticksUntilNextBiteAttempt < 0)
            {
                AttemptBite();
                _ticksUntilNextBiteAttempt = GetNewTicksUntilNextBiteAttempt;
            }
        }

        public override string CompDescriptionExtra => $"Bite strength: {Damage:0.0}";

        private void AttemptBite()
        {
            Pawn target = IdealTarget();
            if (!(target is { Spawned: true })) return;

            IntVec3 position = target.Position;
            Vector3 drawPosition = target.DrawPos;
            RaceProperties raceProps = target.RaceProps;
            
            DamageWorker.DamageResult result = target.TakeDamage(new DamageInfo(DamageDefOf.Bite, Damage, ArmorPenetration));

            BodyPartRecord hitPart = result.LastHitPart;


            if (raceProps.hasMeat)
            {
                if (!target.Dead)
                {
                    if (target.health.hediffSet.GetPartHealth(hitPart) == 0)
                    {
                        OnConsumePart(hitPart, target);
                    }
                    target.stances.stagger.StaggerFor(200);
                }
                else
                {
                    OnConsumeKill(raceProps, drawPosition, parent.pawn.Map);
                }
            }
            
            //Do animation
            parent.pawn.Drawer.Notify_MeleeAttackOn(target);
            parent.pawn.rotationTracker.FaceCell(position);
            //Play sound
            SoundDefOf.MetalHitImportant.PlayOneShot(new TargetInfo(position, target.Map));
            //Notify melee done
            parent.pawn.caller?.Notify_DidMeleeAttack();
            //Add to log
            //new BattleLogEntry_MeleeCombat(rulePackGetter(this.maneuver), alwaysShow, this.CasterPawn, this.currentTarget.Thing, this.ImplementOwnerType, this.tool.labelUsedInLogging ? this.tool.label : "", this.EquipmentSource == null ? (ThingDef) null : this.EquipmentSource.def, this.HediffCompSource == null ? (HediffDef) null : this.HediffCompSource.Def, this.maneuver.logEntryDef);
            BattleLogEntry_MeleeCombat entry = new BattleLogEntry_MeleeCombat(Props.Maneuver.combatLogRulesHit, true, parent.pawn, target, ImplementOwnerTypeDefOf.Hediff, "THNMF.Maw".Translate(), null, Def, Props.Maneuver.logEntryDef);
            Find.BattleLog.Add(entry);
        }
        
        private void OnConsumeKill(RaceProperties raceProps, Vector3 location, Map map)
        {
            //Leave if they have no meat
            if (!raceProps.hasMeat) return;
            
            MoteMaker.ThrowText(location, map, "THNMF.TextMote_Devoured".Translate(), 1.9f);
            
            Hediff regen = HediffMaker.MakeHediff(NMF_DefsOf.THNMF_MawRegeneration, Pawn);
            regen.Severity = RegenTickScale * raceProps.baseHealthScale;
            Pawn.health.AddHediff(regen);
        }

        private void OnConsumePart(BodyPartRecord eatenPart, Pawn pawn)
        {
            //Leave if they have no meat
            if (!pawn.RaceProps.hasMeat) return;
            
            MoteMaker.ThrowText(pawn.Drawer.DrawPos, pawn.Map, "THNMF.TextMote_Eaten".Translate(), 1.9f);
            
            Need_Food foodNeed = Pawn.needs.food;
            //Heal
            if (foodNeed.NutritionWanted < 0.2f)
            {
                //Regeneration
                if (!Pawn.Inhumanized())
                {
                    ThingMaker.MakeThing(Pawn.RaceProps.meatDef).Ingested(Pawn, 0);
                }

                Hediff regen = HediffMaker.MakeHediff(NMF_DefsOf.THNMF_MawRegeneration, Pawn);
                regen.Severity = RegenTickScale * eatenPart.def.GetMaxHealth(pawn);
                Pawn.health.AddHediff(regen);
            }
            //Eat
            else
            {
                //Restore .1 per hp of thing
                if (!Pawn.Inhumanized())
                {
                    foodNeed.CurLevel += ThingMaker.MakeThing(Pawn.RaceProps.meatDef).Ingested(Pawn, Mathf.Min(Pawn.needs.food.NutritionWanted, eatenPart.def.GetMaxHealth(pawn) * 0.1f));
                }
                else
                {
                    foodNeed.CurLevel += Mathf.Min(Pawn.needs.food.NutritionWanted,
                        eatenPart.def.GetMaxHealth(pawn) * 0.1f);
                }
            }
        }
        
        private Pawn IdealTarget()
        {
            List<Pawn> targets = PotentialTargets();
            if (!targets.Any()) return null;
            Pawn[] nonDownedTargets = targets.Where(pawn => !pawn.Downed).ToArray();
            return nonDownedTargets.Any() ? nonDownedTargets.RandomElement() : targets.RandomElement();
        }

        private List<Pawn> PotentialTargets()
        {
            List<Pawn> adjacentEnemies = new List<Pawn>();
            foreach (IntVec3 cell in GenAdj.CellsAdjacent8Way(Pawn))
            {
                adjacentEnemies.AddRange(cell.GetThingList(Pawn.Map).OfType<Pawn>().Where(pawn => pawn.HostileTo(Pawn)));
            }
            return adjacentEnemies;
        }
    }
}