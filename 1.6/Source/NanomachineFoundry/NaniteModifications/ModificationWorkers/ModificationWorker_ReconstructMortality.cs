using Verse;

namespace NanomachineFoundry.NaniteModifications.ModificationWorkers
{
    public class ModificationWorker_ReconstructMortality: ModificationWorker_ResurrectionParent
    {
        public ModificationWorker_ReconstructMortality(NaniteModificationDef def, Pawn pawn) : base(def, pawn)
        {
        }

        public override HediffDef ResurrectionHediffDef => NMF_DefsOf.THNMF_ReconstructMortalityResurrection;

        public override bool CanResurrect(out bool isPermanentlyDead)
        {
            isPermanentlyDead = false;
            if (pawn.health.hediffSet.TryGetHediff(NMF_DefsOf.THNMF_ResurrectionFog, out Hediff fog))
            {
                if (fog.Severity > 1)
                {
                    isPermanentlyDead = true;
                }
            }
            return !isPermanentlyDead;
        }

        private float _consciousnessLoss;
        private const int Inhumanization = 20;

        public override void RecalculateStats(float naniteLevel, int intendedNaniteLevel, NaniteDef type = null)
        {
            base.RecalculateStats(naniteLevel, 0, type);
            _consciousnessLoss = 0.2f - 0.005f * naniteLevel;
        }

        public override void OnResurrect()
        {
            base.OnResurrect();
            InflictDebuffs();
        }

        private void InflictDebuffs()
        {
            if (pawn.health.hediffSet.TryGetHediff(NMF_DefsOf.THNMF_BionanitePower, out Hediff bionaniteHediff))
            {
                if (bionaniteHediff.TryGetComp(out HediffComp_BionanitePower power))
                {
                    power.Progress(Inhumanization);
                }
            }
            Hediff fog = HediffMaker.MakeHediff(NMF_DefsOf.THNMF_ResurrectionFog, pawn);
            fog.Severity = _consciousnessLoss;
            pawn.health.AddHediff(fog);
        }
    }
}