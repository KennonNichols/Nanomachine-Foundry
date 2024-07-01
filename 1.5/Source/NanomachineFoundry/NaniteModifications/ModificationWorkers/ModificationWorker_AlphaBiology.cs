using NanomachineFoundry.Utils;
using Verse;

namespace NanomachineFoundry.NaniteModifications.ModificationWorkers
{
    public class ModificationWorker_AlphaBiology: ModificationWorker
    {
        public float HungerRate { get; private set; }
        
        public ModificationWorker_AlphaBiology(NaniteModificationDef def, Pawn pawn) : base(def, pawn)
        {
        }

        public override void RecalculateStats(float naniteLevel, int intendedNaniteLevel = 0, NaniteDef type = null)
        {
            base.RecalculateStats(naniteLevel, intendedNaniteLevel, type);
            HungerRate = 1 - .01f;
        }
    }
}