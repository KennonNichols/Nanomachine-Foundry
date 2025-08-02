using System.Linq;
using RimWorld;

namespace NanomachineFoundry.AssistingArchotechQuest
{
    public class GameCondition_ArchiteFog: GameCondition_ForceWeather
    {
        public override void GameConditionTick()
        {
            base.GameConditionTick();
            foreach (var pawn in SingleMap.mapPawns.AllHumanlike.Where(pawn => !pawn.health.hediffSet.HasHediff(NMF_DefsOf.THNMF_DecayArchiteInfection)).Where(pawn => !(pawn.GetNaniteTracker()?.IsNaniteTypeAllowed(NMF_DefsOf.THNMF_Archite) ?? false)))
            {
                pawn.health.AddHediff(NMF_DefsOf.THNMF_DecayArchiteInfection);
            }
        }
    }
}