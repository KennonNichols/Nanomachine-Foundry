using System;
using System.Collections;
using System.Collections.Generic;
using NanomachineFoundry.NaniteModifications;
using NanomachineFoundry.NaniteModifications.ModificationWorkers;
using RimWorld;
using Verse;
using Verse.AI;

namespace NanomachineFoundry
{    
    public class HediffCompProperties_NaniteResurrection: HediffCompProperties
    {
        public int resurrectionTicks;
        public string extraDescription;

        public override IEnumerable<string> ConfigErrors(HediffDef parentDef)
        {
            foreach (string error in base.ConfigErrors(parentDef))
            {
                yield return error;
            }

            if (extraDescription == "")
            {
                yield return "ExtraDescription cannot be empty";
            }
        }

        public HediffCompProperties_NaniteResurrection()
        {
            compClass = typeof(HediffComp_NaniteResurrection);
        }
    }
    public class HediffComp_NaniteResurrection: HediffComp
    {
        private HediffCompProperties_NaniteResurrection Props => (HediffCompProperties_NaniteResurrection)props;

        private Type _modificationWorkerType;


        private ModificationWorker_ResurrectionParent WorkerInflicted => _knownWorkerInflicted ??= GetWorker();
        private ModificationWorker_ResurrectionParent _knownWorkerInflicted;
        private int _lastCheckTick;
        private int LastCheckTick => _lastCheckTick;
        private int ResurrectionTimeTicks => WorkerInflicted?.OverrideResurrectionTicks ?? Props.resurrectionTicks;

        private int TicksUntilResurrect => LastCheckTick + ResurrectionTimeTicks - Find.TickManager.TicksGame;
        
        public void Activate(ModificationWorker_ResurrectionParent worker)
        {
            _modificationWorkerType = worker.GetType();
            _knownWorkerInflicted = worker;
            ResetTimer();
        }

        private ModificationWorker_ResurrectionParent GetWorker()
        {
            ModificationWorker worker = parent.pawn.GetModificationWorkerOfType(_modificationWorkerType);
            if (!(worker is ModificationWorker_ResurrectionParent))
            {
                Log.Error("Pawn has no valid worker associated with resurrection hediff. Removing hediff.");
                parent.pawn.health.RemoveHediff(parent);
            }
            return (ModificationWorker_ResurrectionParent)worker;
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref _lastCheckTick, "thnmf_lastCheckTick");
            Scribe_Values.Look(ref _modificationWorkerType, "thnmf_workerType");
        }
        
        private void TryResurrect()
        {
            bool shouldGiveUp = false;
            if (WorkerInflicted?.CanResurrect(out shouldGiveUp) ?? true)
            {
                if (ResurrectionUtility.TryResurrect(parent.pawn))
                {
                    shouldGiveUp = true;
                    
                    if (parent.pawn.Faction != Faction.OfPlayer && !parent.pawn.Downed)
                    {
                        Thing thing = GenClosest.ClosestThingReachable(parent.pawn.Position, parent.pawn.Map, ThingRequest.ForGroup(ThingRequestGroup.Weapon), PathEndMode.OnCell, TraverseParms.For(parent.pawn), 5f);
                        if (thing != null)
                        {
                            Job job = JobGiver_PickupDroppedWeapon.PickupWeaponJob(parent.pawn, thing, ignoreForbidden: true);
                            if (job != null)
                            {
                                parent.pawn.jobs.StartJob(job, JobCondition.InterruptForced);
                            }
                        }
                    }
            
                    WorkerInflicted?.OnResurrect();
                }
            }
            else
            {
                if (shouldGiveUp)
                {
                    WorkerInflicted.AlertPermadead();
                }
            }

            if (shouldGiveUp)
            {
                parent.pawn.health.RemoveHediff(parent);
            }
            else
            {
                ResetTimer();
            }
        }
        
        private void ResetTimer()
        {
            _lastCheckTick = GenTicks.TicksGame;
        }

        public override string CompDescriptionExtra => "\n" + Props.extraDescription + "\n" + string.Format("THNMF.ResurrectionTimeRemaining".Translate(), TicksUntilResurrect.ToStringTicksToPeriod());

        public void CountDownToResurrection()
        {
            if (TicksUntilResurrect <= 0)
            {
                TryResurrect();
            }
        }
    }
}