using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NanomachineFoundry.AdministrationWorkers;
using NanomachineFoundry.NaniteModifications;
using NanomachineFoundry.Utils;
using PipeSystem;
using RimWorld;
using UnityEngine;
using Verse;

namespace NanomachineFoundry
{
    public class NaniteOperationDef: Def
    {
        public Type tabType;
        public int operationDurationTicks;
        public string iconPath;
        public List<ResearchProjectDef> researchPrerequisites;
        public virtual Texture2D icon { get; private set; }
        protected bool IsUnlocked => !researchPrerequisites.Any(r => !r.IsFinished);
        
        protected int naniteCost;
        protected NaniteEffectCategoryDef categoryDef;
        public OperationWorker worker { get; private set; }

        public Type workerClass;

        private string _costDescription;

        public string CostDescription => _costDescription ?? string.Format("THNMF.OperationCostReport".Translate(),
            naniteCost, categoryDef.label,
            (DefDatabase<NaniteDef>.AllDefs.Where(def => def.pipeNet != null && def.categories.Contains(categoryDef))
                .AsReadableList(def => (def.plural).Colorize(def.color))));

        public virtual string FullDescription => naniteCost > 0 ? $"{description}\n\n{CostDescription}" : description;
        
        public bool IsVisible(Pawn pawn)
        {
            return IsUnlocked && (worker?.IsVisible(pawn) ?? true);
        }

        public bool HasOperationCost(out int cost, out NaniteEffectCategoryDef category)
        {
            cost = naniteCost;
            category = categoryDef;
            return naniteCost > 0;
        }
        
        public override void PostLoad()
        {
            base.PostLoad();
            LongEventHandler.ExecuteWhenFinished(delegate
            {
                try
                {
                    icon = ContentFinder<Texture2D>.Get(iconPath);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    icon = BaseContent.BadTex;
                    throw;
                }
            });
            if (workerClass != null)
            {
                worker = (OperationWorker)Activator.CreateInstance(workerClass);
            }
        }
        /*
         * foreach (ResearchProjectDef project in researchPrerequisites)
            {
                Log.Message("project gotten");
                Log.Message(project);
                //If we are already linked by the thing, don't bother.
                if (project.InfoCardHyperlinks.Any(hyperlink => hyperlink.def == this)) continue;

                List<Dialog_InfoCard.Hyperlink> links =
                    Traverse.Create(project).Field("cachedHyperlinks").GetValue() as List<Dialog_InfoCard.Hyperlink>;
                links.Add(new Dialog_InfoCard.Hyperlink(this));
                Log.Message(links);
            }
         */
        
        public virtual void AdministerToPawn(Pawn pawn, CompNaniteOperator naniteOperator)
        {
            worker.ApplyToPawn(pawn, naniteOperator);
        }


        public virtual bool CanAdministerToPawn(Pawn pawn, out string issuesTag)
        {
            if (!worker.CanApplyToPawn(pawn, out issuesTag)) return false;
            if (!IsUnlocked)
            {
                issuesTag = "THNMF.NotUnlocked";
                return false;
            }

            issuesTag = null;
            return true;
        }

        public override IEnumerable<string> ConfigErrors()
        {
            if (naniteCost > 0 && categoryDef == null)
            {
                yield return "Effect category cannot be null for an operation with a price";
            }
            if (workerClass == null)
            {
                yield return "Worker class cannot be null";
            }
        }

        public bool CanAfford(IEnumerable<CompResource> resources, out NaniteDef lowestAffordableTier)
        {
            if (naniteCost <= 0)
            {
                lowestAffordableTier = NMF_DefsOf.THNMF_Mechanite;
                return true;
            }
            
            List<NaniteDef> validDefs = new List<NaniteDef>();
            //For each network the operator has
            foreach (CompResource resource in resources)
            {
                //If we managed to find the nanite type associated with the pipe net
                if (NaniteDef.TryGetNaniteTypeAssociatedWithPipeNetwork(resource.PipeNet.def, out NaniteDef naniteType))
                {
                    //If that nanite supports the operation
                    if (naniteType.categories.Contains(categoryDef))
                    {
                        //And if the nanite supports it
                        if (resource.PipeNet.Stored >= naniteCost)
                        {
                            validDefs.Add(naniteType);
                        }
                    }
                }
            }

            if (validDefs.Count > 0)
            {
                lowestAffordableTier = validDefs.MinBy(def => def.tier);
                return true;
            }

            lowestAffordableTier = null;
            return false;
        }
    }

    public class NaniteInstallationOperationDef: NaniteOperationDef
    {
        private NaniteModificationDef modification;
        
        public override string FullDescription => modification.FullDescription(0);

        public override Texture2D icon => modification.getIcon();

        public override void AdministerToPawn(Pawn pawn, CompNaniteOperator naniteOperator)
        {
            NaniteTracker_Pawn.Get(pawn).AllowModification(modification);
        }
        
        public override IEnumerable<string> ConfigErrors()
        {
            if (naniteCost > 0 && categoryDef == null)
            {
                yield return "Effect category cannot be null for an operation with a price";
            }
            if (modification == null)
            {
                yield return "Modification cannot be null";
            }
        }

        public override bool CanAdministerToPawn(Pawn pawn, out string issuesTag)
        {
            if (!IsUnlocked)
            {
                issuesTag = "THNMF.NotUnlocked";
                return false;
            }
            
            if (modification.categoryDef == NMF_DefsOf.THNMF_Psychic && !pawn.HasPsylink)
            {
                issuesTag = "THNMF.PawnMustBePsycaster";
                return false;
            }
            
            if (modification.categoryDef == NMF_DefsOf.THNMF_Mechanitor && !MechanitorUtility.IsMechanitor(pawn))
            {
                issuesTag = "THNMF.PawnMustBeMechanitor";
                return false;
            }
            
            issuesTag = "";
            if (!pawn.IsMechanized())
            {
                issuesTag = "THNMF.MustBeMechanized";
                return false;
            }

            NaniteTracker_Pawn tracker = NaniteTracker_Pawn.Get(pawn);

            if (tracker.IsModificationAllowed(modification))
            {
                issuesTag = "THNMF.ModificationAlreadyExists";
                return false;
            }

            if (!tracker.AllowedNaniteTypes.Any(def => def.categories.Contains(modification.categoryDef)))
            {
                issuesTag = "THNMF.NoValidNanitesToSupportOperation";
                return false;
            }

            issuesTag = null;
            return true;
        }
    }
}
