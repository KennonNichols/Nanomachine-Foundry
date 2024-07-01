using RimWorld;
using Verse;

namespace NanomachineFoundry
{
    public class HediffCompProperties_ArchosplinterLink: HediffCompProperties
    {
        public HediffCompProperties_ArchosplinterLink()
        {
            compClass = typeof(HediffComp_ArchosplinterLink);
        }
    }
    
    public class HediffComp_ArchosplinterLink: HediffComp
    {
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (parent.pawn.IsHashIntervalTick(2500))
            {
                if (parent.pawn.Faction == NMF_DefsOf.SplinterFaction) return;
                RecruitUtility.Recruit(parent.pawn, NMF_DefsOf.SplinterFaction);
                parent.pawn.guest.Recruitable = false;
                Find.LetterStack.ReceiveLetter("THNMF.SplinterPuppetRetaken".Translate(), "THNMF.SplinterPuppetRetakenDescription".Translate(parent.pawn.Name.ToStringShort), LetterDefOf.ThreatBig, parent.pawn);
            }
        }
    }
}