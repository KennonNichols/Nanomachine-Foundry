using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace NanomachineFoundry.AssistingArchotechQuest
{
    public class JobDriver_AccessArchotechNode: JobDriver
    {
        private Thing HackTarget => TargetThingA;

        private CompArchotechNodeHackable CompHacking => HackTarget.TryGetComp<CompArchotechNodeHackable>();

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(HackTarget, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            Toil toil = ToilMaker.MakeToil();
            toil.handlingFacing = true;
            toil.tickAction = delegate
            {
                float statValue = pawn.GetStatValue(StatDefOf.HackingSpeed);
                CompHacking.Hack(statValue, pawn);
                pawn.skills.Learn(SkillDefOf.Intellectual, 0.1f);
                pawn.rotationTracker.FaceTarget(HackTarget);
            };
            toil.WithEffect(EffecterDefOf.Hacking, TargetIndex.A);
            if (CompHacking.Props.effectHacking != null)
            {
                toil.WithEffect(() => CompHacking.Props.effectHacking, () => HackTarget.OccupiedRect().ClosestCellTo(pawn.Position));
            }
            toil.WithProgressBar(TargetIndex.A, () => CompHacking.ProgressPercent, interpolateBetweenActorAndTarget: false, alwaysShow: true);
            toil.PlaySoundAtStart(SoundDefOf.Hacking_Started);
            toil.PlaySustainerOrSound(SoundDefOf.Hacking_InProgress);
            toil.AddFinishAction(delegate
            {
                if (CompHacking.IsHacked)
                {
                    SoundDefOf.Hacking_Completed.PlayOneShot(HackTarget);
                    CompHacking.Props.hackingCompletedSound?.PlayOneShot(HackTarget);
                }
                else
                {
                    SoundDefOf.Hacking_Suspended.PlayOneShot(HackTarget);
                }
            });
            toil.FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
            toil.FailOn(() => CompHacking.IsHacked);
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            toil.activeSkill = () => SkillDefOf.Intellectual;
            yield return toil;
        }
    }
}