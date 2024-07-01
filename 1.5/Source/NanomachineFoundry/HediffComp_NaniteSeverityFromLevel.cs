using Microsoft.SqlServer.Server;
using NanomachineFoundry.Utils;
using Verse;

namespace NanomachineFoundry
{
    public class HediffCompProperties_NaniteSeverityFromLevel: HediffCompProperties
    {
        public readonly int SyncInterval = 5000;
        public NaniteDef naniteType;
        public HediffCompProperties_NaniteSeverityFromLevel()
        {
            compClass = typeof(HediffComp_NaniteSeverityFromLevel);
        }
    }
    
    public class HediffComp_NaniteSeverityFromLevel: HediffComp
    {
        private HediffCompProperties_NaniteSeverityFromLevel Props => (HediffCompProperties_NaniteSeverityFromLevel)props;


        public override string CompDescriptionExtra => "\n\n" + string.Format("THNMF.NaniteLevelForHediff".Translate(),
            Props.naniteType.label.CapitalizeFirst(), parent.Severity.ToString("0.0"));


        public override void CompPostTick(ref float severityAdjustment)
        {
            if (Pawn.IsHashIntervalTick(Props.SyncInterval))
            {
                AdjustSeverityToNaniteLevel();
            }
        }

        private void AdjustSeverityToNaniteLevel()
        {
            parent.Severity = Pawn.GetNaniteTracker().GetNaniteLevelPercent(Props.naniteType);
        }
    }
}