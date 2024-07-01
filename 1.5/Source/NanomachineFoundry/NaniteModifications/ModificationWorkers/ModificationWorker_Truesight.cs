using NanomachineFoundry.Utils;
using RimWorld;
using Verse;

namespace NanomachineFoundry.NaniteModifications.ModificationWorkers
{
    public class ModificationWorker_Truesight: ModificationWorker
    {
        public bool PsychicallyImmune { get; private set; }
        
        
        public ModificationWorker_Truesight(NaniteModificationDef def, Pawn pawn) : base(def, pawn)
        {
        }

        public override void RecalculateStats(float naniteLevel, int intendedNaniteLevel, NaniteDef type = null)
        {
            base.RecalculateStats(naniteLevel, 0, type);
            PsychicallyImmune = naniteLevel >= 5;
        }
    }
}