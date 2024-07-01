using System.Collections.Generic;
using System.Linq;
using NanomachineFoundry.NaniteModifications;
using RimWorld;
using Verse;
using Verse.AI;

namespace NanomachineFoundry
{
    public class JobDriver_RemoveMechlinkFromSelf : JobDriver
    {
        private const TargetIndex CorpseInd = TargetIndex.A;

        private const int RemoveTicks = 75;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        public override string GetReport()
        {
            return "ReportRemovingMechlink".Translate(HediffDefOf.MechlinkImplant.label);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => !pawn.health.hediffSet.HasHediff(HediffDefOf.MechlinkImplant));
            Toil toil = Toils_General.Wait(RemoveTicks).WithEffect(() => EffecterDefOf.Surgery, pawn)
                .PlaySustainerOrSound(SoundDefOf.Recipe_Surgery)
                .PlaySoundAtEnd(SoundDefOf.Mechlink_Removed);
            toil.handlingFacing = true;
            yield return toil;
            Toil toil2 = ToilMaker.MakeToil("MakeNewToils");
            toil2.initAction = delegate
            {
                RemoveHediffOfDef(HediffDefOf.MechlinkImplant);
                RemoveHediffOfDef(NMF_DefsOf.THNMF_SeveringMechlink);
                InflictScar();
                Mechlink obj = (Mechlink)ThingMaker.MakeThing(ThingDefOf.Mechlink);
                obj.sentMechsToPlayer = pawn.IsColonist || pawn.IsPrisoner || pawn.IsSlave || pawn.IsQuestLodger();
                GenPlace.TryPlaceThing(obj, pawn.Position, base.Map, ThingPlaceMode.Near);
                
                
                
                //Remove mechanitor mods
                bool anyModsRemoved = false;
                if (pawn.IsMechanized())
                {
                    foreach (NaniteModificationDef naniteMod in pawn.GetNaniteTracker().AllowedModifications.Where(def => def.categoryDef == NMF_DefsOf.THNMF_Mechanitor).ToArray())
                    {
                        pawn.GetNaniteTracker().DisallowModification(naniteMod);
                        anyModsRemoved = true;
                    }
                }
                
                
                
                Find.LetterStack.ReceiveLetter("THNMF.MechlinkSevered".Translate(), string.Format((anyModsRemoved? "THNMF.MechlinkSeveredDesctiptionLostModifications" : "THNMF.MechlinkSeveredDescription").Translate(), pawn.Name.ToStringShort), LetterDefOf.PositiveEvent);
            };
            toil2.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return toil2;
        }

        private void RemoveHediffOfDef(HediffDef def)
        {
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            for (int num = hediffs.Count - 1; num >= 0; num--)
            {
                if (hediffs[num].def == def)
                {
                    pawn.health.RemoveHediff(hediffs[num]);
                }
            }
        }


        private void InflictScar()
        {
            BodyPartRecord torso = pawn.health.hediffSet.GetBodyPartRecord(BodyPartDefOf.Torso);
            if (torso == null) return;
            foreach (BodyPartRecord part in torso.parts)
            {
                //This sucks
                //Neck is index number 12
                if (part.Index == 12)
                {
                    //Hediff scar = HediffMaker.MakeHediff(HediffDefOf.SurgicalCut, pawn, part);
                    
                    //DamageWorker.DamageResult result = new DamageWorker.DamageResult().
                    
                    
                    DamageInfo damageInfo = new DamageInfo(DamageDefOf.SurgicalCut, 0.5f, hitPart: part);
                    damageInfo.SetAllowDamagePropagation(val: true);
                    damageInfo.SetIgnoreArmor(ignoreArmor: true);
                    pawn.TakeDamage(damageInfo);
                }
                //Log.Message(part.Index); 
                //Log.Message(part.);
                //if (part.def == ) continue;
                //foreach (PawnRenderNode nodeJr in node.children)
                //{
                //    if (nodeJr.Props.tagDef == PawnRenderNodeTagDefOf)
                //    {
                //        propertiesEyeColored.color = _baseEyeColor;
                //    }
                //}
            }
        }
    }
}