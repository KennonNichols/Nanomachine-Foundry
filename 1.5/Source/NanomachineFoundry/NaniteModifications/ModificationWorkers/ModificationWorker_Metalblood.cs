using NanomachineFoundry.Utils;
using Verse;

namespace NanomachineFoundry.NaniteModifications.ModificationWorkers
{
    public class ModificationWorker_Metalblood: ModificationWorker
    {
        public float DamageMultiplier { get; private set; }
        public ModificationWorker_Metalblood(NaniteModificationDef def, Pawn pawn) : base(def, pawn) { }
        public override void RecalculateStats(float naniteLevel, int intendedNaniteLevel, NaniteDef type = null)
        {
            base.RecalculateStats(naniteLevel, 0, type);
            DamageMultiplier = 1.30f - 0.005f * naniteLevel;
        }
    }
}