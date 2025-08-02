using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace NanomachineFoundry
{
    public class HediffCompProperties_SeveringMechlink: HediffCompProperties
    {
        public HediffCompProperties_SeveringMechlink()
        {
            compClass = typeof(HediffComp_SeveringMechlink);
        }
    }
    
    public class HediffComp_SeveringMechlink: HediffComp
    {
        private HediffCompProperties_SeveringMechlink Props => (HediffCompProperties_SeveringMechlink)props;
        private int _ticksUntilRelease = 75000;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref _ticksUntilRelease, "thnmf_ticksUntilRelease");
        }

        public override string CompDebugString()
        {
            
            return $"Ticks until operation complete: {_ticksUntilRelease}\n{base.CompDebugString()}";
        }


        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            
            _ticksUntilRelease--;
            if (_ticksUntilRelease <= 0)
            {
                RemoveMechlink();
            }
        }

        private void RemoveMechlink()
        {
            //Hediff mechlink = Pawn.health.hediffSet.hediffs.First(hediff => hediff.GetType() == typeof(Hediff_Mechlink));
            
            //Log.Message(mechlink);

            //If the pawn is not removing mechlink, do it.
            if (Pawn.jobs.jobQueue.All(job => job.job.def != NMF_DefsOf.THNMF_RemoveMechlinkFromSelf))
            {
                Pawn.jobs.jobQueue.EnqueueFirst(JobMaker.MakeJob(NMF_DefsOf.THNMF_RemoveMechlinkFromSelf));
            }
            
            
            //RemoveSelf();
        }

        private void RemoveSelf()
        {
            Pawn.health.hediffSet.hediffs.Remove(parent);
        }
    }
}