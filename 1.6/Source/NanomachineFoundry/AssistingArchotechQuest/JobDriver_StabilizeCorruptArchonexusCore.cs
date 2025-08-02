using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace NanomachineFoundry.AssistingArchotechQuest
{
    public class JobDriver_StabilizeCorruptArchonexusCore : JobDriver_Goto
    {
        private const int DefaultDuration = 6000;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.InteractionCell);
            Toil toil = ToilMaker.MakeToil();
            toil.handlingFacing = true;
            toil.tickAction = delegate
            {
                pawn.rotationTracker.FaceTarget(TargetA);
            };
            toil.AddFinishAction(delegate
            {
                ((Building_CorruptNexusCore)job.targetA.Thing).Activate(pawn);
            });
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.defaultDuration = DefaultDuration;
            toil.FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
            yield return toil;
        }
    }
}