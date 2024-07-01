using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace NanomachineFoundry.NaniteProduction
{
    public class JobDriver_CarryToMechaniteBreeder: JobDriver
    {
        private const TargetIndex TakeeInd = TargetIndex.A;

        private const TargetIndex CasketInd = TargetIndex.B;

        private Pawn Takee => job.GetTarget(TargetIndex.A).Pawn;

        private CompMechaniteBreeder Breeder => job.GetTarget(TargetIndex.B).Thing.TryGetComp<CompMechaniteBreeder>();

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Takee, job, 1, -1, null, errorOnFailed) && pawn.Reserve(Breeder.parent, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            AddFinishAction(delegate
            {
                if (Breeder != null && Breeder.queuedEnterJob == job)
                {
                    Breeder.ClearQueuedInformation();
                }
            });
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.B);
            this.FailOnAggroMentalState(TargetIndex.A);
            Toil goToTakee = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell).FailOnDestroyedNullOrForbidden(TargetIndex.A).FailOnDespawnedNullOrForbidden(TargetIndex.B)
                .FailOnSomeonePhysicallyInteracting(TargetIndex.A);
            Toil startCarryingTakee = Toils_Haul.StartCarryThing(TargetIndex.A);
            Toil goToThing = Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.InteractionCell);
            yield return Toils_Jump.JumpIf(goToThing, () => pawn.IsCarryingPawn(Takee));
            yield return goToTakee;
            yield return startCarryingTakee;
            yield return goToThing;
            yield return PrepareToEnterToil(TargetIndex.B);
            yield return new Toil
            {
                initAction = delegate
                {
                    if (Breeder.Occupant == null)
                    {
                        Breeder.InsertPawn(Takee);
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }

        public static Toil PrepareToEnterToil(TargetIndex podIndex)
        {
            Toil toil = Toils_General.Wait(50);
            toil.FailOnCannotTouch(podIndex, PathEndMode.InteractionCell);
            toil.WithProgressBarToilDelay(podIndex);
            return toil;
        }
    }
}