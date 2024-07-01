using System.Collections.Generic;
using System.Linq;
using NanomachineFoundry.NaniteModifications;
using NanomachineFoundry.Utils;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;

namespace NanomachineFoundry.AssistingArchotechQuest
{
    public class SitePartWorker_CorruptNexus: SitePartWorker
    {
        public override void PostMapGenerate(Map map)
        {
            base.PostMapGenerate(map);
            GameCondition cond = GameConditionMaker.MakeCondition(NMF_DefsOf.THNMF_ArchiteFog);
            cond.Permanent = true;
            map.GameConditionManager.RegisterCondition(cond);
        }

        public override void SitePartWorkerTick(SitePart sitePart)
        {
            base.SitePartWorkerTick(sitePart);
            
            if (AssistingArchotechQuestlineUtility.IsNodeQuestActive()) return;

            if (!Find.WorldObjects.Caravans.Any()) return;
            
            if (DoesPlayerHaveAnyArchitePawn()) return;
            
            foreach (var caravan in Find.WorldObjects.Caravans.Where(caravan => caravan.pather.Destination == sitePart.site.Tile && caravan.pather.curPath.NodesLeftCount <= 2))
            {
                string caravanName = caravan.pawns.InnerListForReading.Count > 1
                    ? "THNMF.CaravanPlural".Translate().ToString()
                    : caravan.pawns.InnerListForReading.First().Name.ToStringShort;
                Find.WindowStack.Add(new Dialog_Message("THNMF.PsychicWarning".Translate(caravanName), delegate {}));
                
                GiveNodeQuest();
                break;
            }
        }

        private static bool DoesPlayerHaveAnyArchitePawn()
        {
            foreach (Pawn pawn in PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_OfPlayerFaction)
            {
                if (pawn.GetNaniteTracker()?.IsNaniteTypeAllowed(NMF_DefsOf.THNMF_Archite) ?? false) return true;
            }

            foreach (var pawn in PawnsFinder.All_AliveOrDead.Where(pawn => pawn.Faction == Faction.OfPlayer))
            {
                if (pawn.Dead)
                {
                    if (pawn.Corpse == null)
                    {
                        continue;
                    }
                }
                if (pawn.GetNaniteTracker()?.IsNaniteTypeAllowed(NMF_DefsOf.THNMF_Archite) ?? false) return true;
            }
            return false;
        }
        
        //!DoesCaravanHaveArchitePawn(caravan)

        private static bool DoesCaravanHaveArchitePawn(Caravan caravan)
        {
            foreach (Pawn caravanPawn in caravan.pawns)
            {
                if (caravanPawn.GetNaniteTracker()?.IsNaniteTypeAllowed(NMF_DefsOf.THNMF_Archite) ?? false) return true;
            }

            return false;
        }

        private static void GiveNodeQuest()
        {
            Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(NMF_DefsOf.THNMF_AssistingArchotechNode, 0);
            if (!quest.hidden && NMF_DefsOf.THNMF_AssistingArchotechNode.sendAvailableLetter)
            {
                QuestUtility.SendLetterQuestAvailable(quest);
            }
        }
    }
}