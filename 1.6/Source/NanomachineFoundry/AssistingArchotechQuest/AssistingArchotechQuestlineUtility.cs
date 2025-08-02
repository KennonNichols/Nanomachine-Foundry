using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace NanomachineFoundry.AssistingArchotechQuest
{
    public static class AssistingArchotechQuestlineUtility
    {
        public static bool IsNodeQuestActive()
        {
            return IsQuestActive(NMF_DefsOf.THNMF_AssistingArchotechNode, out _);
        }
        
        public static bool IsNodeQuestActive(out List<Quest> nodeQuests)
        {
            return IsQuestActive(NMF_DefsOf.THNMF_AssistingArchotechNode, out nodeQuests);
        }
        
        public static bool IsNexusQuestActive()
        {
            return IsNexusQuestActive(out _);
        }
        
        public static bool IsNexusQuestActive(out List<Quest> nodeQuests)
        {
            return IsQuestActive(NMF_DefsOf.THNMF_AssistingArchotechStart, out nodeQuests);
        }

        public static void CompleteAllNodeQuests(QuestEndOutcome outcome)
        {
            if (IsNodeQuestActive(out List<Quest> quests))
            {
                quests.Do(quest => quest.End(outcome));
            }
        }

        public static void CompleteAllNexusQuests(QuestEndOutcome outcome)
        {
            if (IsNexusQuestActive(out List<Quest> quests))
            {
                quests.Do(quest => quest.End(outcome));
            }
        }
        
        private static bool IsQuestActive(QuestScriptDef rootScript, out List<Quest> nodeQuests)
        {
            nodeQuests = Find.QuestManager.QuestsListForReading.Where(quest =>
                quest.root == rootScript && quest.State < (QuestState)2).ToList();
            return !nodeQuests.NullOrEmpty();
        }

        private static bool IsNexusSiteActive()
        {
            return Find.WorldObjects.Sites.Any(site => site.MainSitePartDef == NMF_DefsOf.THNMF_CorruptNexus);
        }

        public static void CreateNexusSite(int centerTile)
        {
            if (TryFindSiteTile(out PlanetTile tile))
            {
                Site site = SiteMaker.TryMakeSite(new[] { NMF_DefsOf.THNMF_CorruptNexus }, tile);
                Find.WorldObjects.Add(site);
                
                Find.LetterStack.ReceiveLetter("THNMF.EpicenterFound".Translate(), "THNMF.EpicenterFoundDescription".Translate(), LetterDefOf.PositiveEvent, site);
                return;
            }
            Log.Error("Failed to generate psychic scream site.");
        }
        
        private static bool TryFindSiteTile(out PlanetTile tile, bool exitOnFirstTileFound = false)
        {
	        return TileFinder.TryFindNewSiteTile(out tile, 5, 10, 15, true, allowedLandmarks: null, selectLandmarkChance: 0.5f, canSelectComboLandmarks: true, TileFinderMode.Random, exitOnFirstTileFound, false, null);
        }

        public static void NotifyAntennaActive()
        {
            GameComponent_NanomachineFoundry comp = Current.Game.GetComponent<GameComponent_NanomachineFoundry>();
            if (IsNexusSiteActive() || comp.HailActive) return;
            
            comp.SetHail();
        }
    }
}