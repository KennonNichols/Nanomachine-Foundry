using System.Collections.Generic;
using System.Linq;
using NanomachineFoundry.Utils;
using RimWorld;
using Verse;

namespace NanomachineFoundry.AdministrationWorkers
{
    public abstract class OperationWorker
    {
        public virtual bool CanApplyToPawn(Pawn pawn, out string issuesTag)
        {
            issuesTag = null;
            return true;
        }
        public abstract void ApplyToPawn(Pawn pawn, CompNaniteOperator naniteOperator);

        public virtual bool IsVisible(Pawn pawn) => true;
    }

    public class PurgePathogensWorker: OperationWorker
    {
        public override bool CanApplyToPawn(Pawn pawn, out string issuesTag)
        {
            if (!pawn.GetAllDiseases().Any())
            {
                issuesTag = "THNMF.PawnMustHaveInfection";
                return false;
            }
            issuesTag = null;
            return true;
        }
        public override void ApplyToPawn(Pawn pawn, CompNaniteOperator naniteOperator)
        {
            Hediff[] hediffs = pawn.GetAllDiseases().ToArray();
            foreach (Hediff disease in hediffs)
            {
                pawn.health.hediffSet.hediffs.Remove(disease);
            }

            Hediff dysbiosis = HediffMaker.MakeHediff(NMF_DefsOf.THNMF_Dysbiosis, pawn);
            dysbiosis.Severity = Rand.Range(.8f, 1);
            pawn.health.hediffSet.hediffs.Add(dysbiosis);
        }
    }

    public class NeurosanitizeDestroyDevianceWorker: OperationWorker
    {
        public override bool CanApplyToPawn(Pawn pawn, out string issuesTag)
        {
            //Check that they have a trait
            if (pawn.story.traits.allTraits.Count <= 0)
            {
                issuesTag = "THNMF.NeurosanitiseNoTraits";
                return false;
            }
            
            issuesTag = null;
            return true;
        }
        public override void ApplyToPawn(Pawn pawn, CompNaniteOperator naniteOperator)
        {
            Find.WindowStack.Add(new WindowNeurosanitizePrompt(pawn));
            
            //Cut brain slightly
            DamageInfo damageInfo = new DamageInfo(DamageDefOf.SurgicalCut, 0.75f, hitPart: pawn.health.hediffSet.GetBrain());
            damageInfo.SetIgnoreArmor(ignoreArmor: true);
            pawn.TakeDamage(damageInfo);
        }
    }

    public class MetalhorrorInoculationWorker: OperationWorker
    {
        public override void ApplyToPawn(Pawn pawn, CompNaniteOperator naniteOperator)
        {
            Log.Message(pawn.ProObj());
            if (MetalhorrorUtility.IsInfected(pawn))
            {
                pawn.health.hediffSet.AddDirect(HediffMaker.MakeHediff(NMF_DefsOf.THNMF_MetalhorrorCombat, pawn));
            }
            else
            {
                MetalhorrorUtility.Infect(pawn, descKey: "THNMF.AccidentalMetalhorrorImplantation");
            }
        }
    }

    public class SeverMechlinkWorker: OperationWorker
    {
        public override bool CanApplyToPawn(Pawn pawn, out string issuesTag)
        {
            if (pawn.mechanitor == null)
            {
                issuesTag = "THNMF.PawnMustBeMechanitor";
                return false;
            }
            issuesTag = null;
            return true;
        }
        public override void ApplyToPawn(Pawn pawn, CompNaniteOperator naniteOperator)
        {
            pawn.health.hediffSet.AddDirect(HediffMaker.MakeHediff(NMF_DefsOf.THNMF_SeveringMechlink, pawn));
        }
    }

    public abstract class NeurorewireWorker: OperationWorker
    {
        protected abstract SkillDef[] GetSkillsToBoost { get; }

        public override bool CanApplyToPawn(Pawn pawn, out string issuesTag)
        {
            if (pawn.GetNaniteTracker().Neurorewired)
            {
                issuesTag = "THNMF.NeurorewireAlreadyCompleted";
                return false;
            }
            return base.CanApplyToPawn(pawn, out issuesTag);
        }

        public override void ApplyToPawn(Pawn pawn, CompNaniteOperator naniteOperator)
        {
            pawn.GetNaniteTracker().Neurorewire(GetSkillsToBoost);
        }
    }

    public class NeurorewirePowerWorker: NeurorewireWorker
    {
        protected override SkillDef[] GetSkillsToBoost => new[] {SkillDefOf.Melee, SkillDefOf.Shooting};
    }

    public class NeurorewireCreativityWorker: NeurorewireWorker
    {
        protected override SkillDef[] GetSkillsToBoost => new[] {SkillDefOf.Construction, SkillDefOf.Mining, SkillDefOf.Crafting, SkillDefOf.Cooking, SkillDefOf.Artistic};
    }
    
    public class NeurorewireEleganceWorker: NeurorewireWorker
    {
        protected override SkillDef[] GetSkillsToBoost => new[] {SkillDefOf.Intellectual, SkillDefOf.Medicine, SkillDefOf.Social, SkillDefOf.Plants, SkillDefOf.Animals};
    }
    
    //Enable the different nanite types
    public abstract class NaniteEnablingWorker: OperationWorker
    {
        protected abstract NaniteDef TypeToEnable { get; }

        public override bool IsVisible(Pawn pawn)
        {
            return !pawn.GetNaniteTracker().IsNaniteTypeAllowed(TypeToEnable);
        }

        public override void ApplyToPawn(Pawn pawn, CompNaniteOperator naniteOperator)
        {
            pawn.GetNaniteTracker().AllowNaniteType(TypeToEnable);
        }
    }

    public class EnableLuciferitesWorker : NaniteEnablingWorker
    {
        protected override NaniteDef TypeToEnable => NMF_DefsOf.THNMF_Luciferite;
    }
    public class EnableMechanitesWorker : NaniteEnablingWorker
    {
        protected override NaniteDef TypeToEnable => NMF_DefsOf.THNMF_Mechanite;
    }
    public class EnableBionanitesWorker : NaniteEnablingWorker
    {
        protected override NaniteDef TypeToEnable => NMF_DefsOf.THNMF_Bionanite;
    }
    
    
    //Reconfigure
    public class ReconfigureWorker: OperationWorker
    {
        public override void ApplyToPawn(Pawn pawn, CompNaniteOperator naniteOperator)
        {
            pawn.GetNaniteTracker().SetConfiguration(naniteOperator.TempConfigs);
        }
    }
    
}