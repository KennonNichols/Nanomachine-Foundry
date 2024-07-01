using NanomachineFoundry.Utils;
using RimWorld;
using Verse;

namespace NanomachineFoundry.NaniteModifications.ModificationWorkers
{
    public class ModificationWorker_Clairvoyance: ModificationWorker
    {
        public float CastRangeFactor { get; private set; }
        
        
        public ModificationWorker_Clairvoyance(NaniteModificationDef def, Pawn pawn) : base(def, pawn)
        {
        }

        public override void RecalculateStats(float naniteLevel, int intendedNaniteLevel, NaniteDef type = null)
        {
            base.RecalculateStats(naniteLevel, 0, type);
            CastRangeFactor = 0.25f + naniteLevel * 0.005f;
            if (type != null)
            {
                if (type.tier >= 3) CastRangeFactor = 1;
            }
        }
    }
}