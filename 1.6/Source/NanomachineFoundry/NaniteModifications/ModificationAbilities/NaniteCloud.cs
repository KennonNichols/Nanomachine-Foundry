using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using NanomachineFoundry.Utils;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Object = System.Object;

namespace NanomachineFoundry.NaniteModifications.ModificationAbilities
{
    public class NaniteCloud: ThingWithComps
    {
        public NaniteCloud center;
        public bool isCenter;
        public HashSet<NaniteCloud> peers;
        
        public Dictionary<Pawn, int> infectionProgress = new Dictionary<Pawn, int>();
        
        public Pawn Originator;
        public HediffDef InflictionHediff;
        public Color Color;
        public int DurationTicks;
        protected float _cloudDensity = 1;
        private Effecter gasMoteEffecter;
        protected bool dissipating;
        protected bool _intelligent;
        
        
        protected virtual int InfectionSeverityMinAmount() => 60;
        protected virtual float DissipationRate() => 0.1f;
        private const float MinDensity = 0.05f;
        private const float SeverityPerTick = 0.00001f;
        private const float MoteCommonality = 0.03f;

        public void ConfigureNaniteCloud(int durationTicks, Color color, HediffDef inflictionHediff, Pawn originator, bool intelligent = true)
        {
            DurationTicks = durationTicks;
            Color = color;
            InflictionHediff = inflictionHediff;
            Originator = originator;
            gasMoteEffecter.children.Do(effecter => effecter.colorOverride = color);
            _intelligent = intelligent;
        }
        
        public NaniteCloud()
        {
            ResetEffecter();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref _cloudDensity, "thnmf_cloudDensity");
            Scribe_Values.Look(ref DurationTicks, "thnmf_durationTicks");
            Scribe_Values.Look(ref _intelligent, "thnmf_intelligent");
            Scribe_Defs.Look(ref InflictionHediff, "thnmf_inflictionHediff");
            Scribe_Values.Look(ref Color, "thnmf_color");
            Scribe_References.Look(ref Originator, "thnmf_originator");
            
            Scribe_Values.Look(ref isCenter, "thnmf_isCenter");
            if (!isCenter)
            {
                Scribe_References.Look(ref center, "thnmf_center");
            }
            
            Scribe_Collections.Look(ref peers, "thnmf_peers", LookMode.Reference);
        }

        protected override void Tick()
        {
            DurationTicks--;
            InflictOnPeopleInCell();
            base.Tick();
            if (Find.TickManager.TicksGame % 15 == 0)
            {
                if (Find.TickManager.TicksGame % 600 == 0)
                {
                    ResetEffecter();
                }
                if (DurationTicks < 0)
                {
                    dissipating = true;
                    Dissipate();       
                }
                
                
                
                if (Rand.Chance(MoteCommonality * _cloudDensity)) gasMoteEffecter?.EffectTick(this, this); 
            }
        }
        
        
        private void ResetEffecter()
        {
            gasMoteEffecter?.Cleanup();
            gasMoteEffecter = null;
            gasMoteEffecter = NMF_DefsOf.THNMF_Nanite_Cloud.Spawn();
            gasMoteEffecter.children.Do(effecter => effecter.colorOverride = Color);
            gasMoteEffecter.Trigger(this, this);
        }

        private void InflictOnPeopleInCell()
        {
            if (Destroyed) return;
            //Increment infections
            foreach (Pawn pawn in PawnsInCell())
            {
                if (Originator != null)
                {
                    if ((!Originator?.Faction?.HostileTo(pawn.Faction) ?? true) && _intelligent) continue;
                }
                if (pawn.RaceProps.IsMechanoid) continue;
                
                infectionProgress[pawn] = infectionProgress.GetWithFallback(pawn) + 1;
                if (infectionProgress[pawn] > InfectionSeverityMinAmount())
                {
                    if (pawn.health.hediffSet.TryGetHediff(InflictionHediff, out Hediff hediff))
                    {
                        hediff.Severity += SeverityPerTick * _cloudDensity;
                    }
                    else
                    {
                        //Piss off faction
                        if (!pawn.Faction.HostileTo(Originator?.Faction))
                        {
                            Faction.OfPlayer.TryAffectGoodwillWith(pawn.Faction, -20, reason: HistoryEventDefOf.UsedHarmfulAbility);
                        }

                        pawn.health.hediffSet.AddDirect(HediffMaker.MakeHediff(InflictionHediff, pawn));
                    }
                }
            }
        }

        protected virtual void Dissipate()
        {
            _cloudDensity = Mathf.Lerp(_cloudDensity, AverageOfNearbyClouds(), DissipationRate());
            
            if (_cloudDensity < MinDensity)
            {
                Disappear();
            }
        }

        private float AverageOfNearbyClouds()
        {
            float sum = 0;
            List<IntVec3> adjacentCells = new List<IntVec3>();
            for (int index = 1; index < 9; ++index)
            {
                adjacentCells.Add(Position + GenAdj.AdjacentCellsAndInside[index]);
            }
            adjacentCells.Do(vec3 =>
            {
                IEnumerable<NaniteCloud> clouds = vec3.GetThingList(Map).OfType<NaniteCloud>();
                IEnumerable<NaniteCloud> naniteClouds = clouds as NaniteCloud[] ?? clouds.ToArray();
                if (naniteClouds.Any()) sum += naniteClouds.First()._cloudDensity;
            });
            return sum / 8;
        }

        private IEnumerable<Pawn> PawnsInCell()
        {
            return Position.GetThingList(Map).OfType<Pawn>();
        }
        
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            //GenDraw.DrawCircleOutline(drawLoc,1);
            //if ((Object) this.materialResolved == (Object) null)
            //    this.materialResolved = this.def.DrawMatSingle;
        }

        public virtual void Disappear()
        {
            gasMoteEffecter?.Cleanup();
            Destroy();
        }

        public override string LabelMouseover => $"{Label.CapitalizeFirst()} ({_cloudDensity.ToStringPercent()})";
    }

    public class LivingNaniteCloud : NaniteCloud
    {
        protected override int InfectionSeverityMinAmount() => 1;
        protected override float DissipationRate() => 0.05f;
        private const int sightRange = 15;

        protected override void Tick()
        {
            if (!dissipating) TrackEnemies();
            base.Tick();
        }

        private void TrackEnemies()
        {
            if (Originator == null || Originator.Dead) return;
            HashSet<IAttackTarget> naniteTargets = Originator.Map.attackTargetsCache.GetPotentialTargetsFor(Originator)
                .Where(target => GenSight.LineOfSightToThing(Position, target.Thing, Map))
                .OrderBy(target => target.Thing.Position.DistanceTo(Position)).ToHashSet();
            if (naniteTargets.Any())
            {
                if (naniteTargets.First().Thing.Position.DistanceTo(Position) < sightRange)
                {
                    SeekTarget(naniteTargets.First().Thing, 0.005f);
                }
            }
        }

        private void SeekTarget(Thing thing, float aggression)
        {
            //Small chance to move tile
            if (Rand.Chance(aggression))
            {
                IntVec3 moveOrder = thing.Position - Position;
                int x = moveOrder.x;
                int z = moveOrder.z;
                int y = moveOrder.y;

                if (x > 0.5) x = 1;
                else if (x < -0.5) x = -1;
                else x = 0;
                if (z > 0.5) z = 1;
                else if (z < -0.5) z = -1;
                else z = 0;
                
                Position += new IntVec3(x, y, z);
            }
        }

        protected override void Dissipate()
        {
            base.Dissipate();
            if (Originator != null)
            {
                SeekTarget(Originator, 0.01f);
                if (Position == Originator.Position)
                {
                    Originator.GetNaniteTracker()
                        .TryChangeNanitesLevel(NMF_DefsOf.THNMF_Archite, 0.2f * _cloudDensity, true);
                    Disappear();
                }
            }
        }
    }

    public class FlammableNaniteCloud : NaniteCloud
    {
	    protected override void Tick()
        {
            if (!Destroyed)
            {
                if (AnyFireInTile()) ExplodeCloud();
                base.Tick();
            }
        }

        private bool AnyFireInTile()
        {
            return Position.ContainsStaticFire(Map) || Position.GetThingList(Map).Any(thing => thing.IsBurning());
        }

        private void ExplodeCloud()
        {
            int numClouds = 0;
            foreach (var naniteCloud in peers.ToArray().Where(naniteCloud => naniteCloud != center))
            {
                numClouds++;
                naniteCloud.Disappear();
            }

            if (isCenter)
            {
                Detonate(numClouds);
            }
            else
            {
                ((FlammableNaniteCloud) center).Detonate(numClouds);
            }
        }

        private void Detonate(int amount)
        {
            GenExplosion.DoExplosion(Position, Map,  Mathf.Sqrt(amount), DamageDefOf.Bomb, this, (int)(Mathf.Sqrt(amount) * 5), chanceToStartFire: 0.8f);
            Disappear();
        }

        public override void Disappear()
        {
            
            peers.ToArray().Do(cloud => cloud.peers.Remove(this));
            base.Disappear();
        }
    }
}