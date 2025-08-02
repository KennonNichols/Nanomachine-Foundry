using NanomachineFoundry.Utils;
using Verse;

namespace NanomachineFoundry.NaniteModifications.ModificationWorkers
{
    public class ModificationWorker_Biomanager: ModificationWorker
    {
        public float HungerRate { get; private set; }
        
        public ModificationWorker_Biomanager(NaniteModificationDef def, Pawn pawn) : base(def, pawn)
        {
        }

        public override void RecalculateStats(float naniteLevel, int intendedNaniteLevel, NaniteDef type = null)
        {
            base.RecalculateStats(naniteLevel, intendedNaniteLevel, type);
            HungerRate = 1 - .02f * NmfUtils.DiminishingReturnsCalculator.CalculateSumDiminishingReturnsFloatSlow(naniteLevel, 20);
        }
    }
}