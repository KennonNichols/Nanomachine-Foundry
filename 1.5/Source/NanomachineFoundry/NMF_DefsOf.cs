using System.Collections.Generic;
using System.Linq;
using NanomachineFoundry.NaniteModifications;
using PipeSystem;
using RimWorld;
using Verse;
using Verse.AI;

// ReSharper disable InconsistentNaming
namespace NanomachineFoundry
{
    [DefOf]
    public static class NMF_DefsOf
    {
        public static NaniteDef THNMF_Archite;
        public static NaniteDef THNMF_Mechanite;
        public static NaniteDef THNMF_Bionanite;
        public static NaniteDef THNMF_Luciferite;
        
        public static JobDef THNMF_EnterNaniteInjector;
        public static JobDef THNMF_RemoveMechlinkFromSelf;
        public static JobDef THNMF_CarryToMechaniteBreeder;
        public static JobDef THNMF_AccessArchotechNode;

        public static NaniteEffectCategoryDef THNMF_Mechanitor;
        public static NaniteEffectCategoryDef THNMF_Psychic;

        public static BackstoryDef THNMF_NexusGuardian;
        public static FactionDef THNMF_Archosplinter;
        public static FactionDef THNMF_ArchosplinterFormer;
        
        public static PipeNetDef THNMF_LuciferiteNet;

        public static HediffDef THNMF_ArchiteBorn;
        public static HediffDef THNMF_Archotouched;

        public static PawnKindDef THNMF_PuppetSoldier;
        
        //Operations/modification
        public static HediffDef THNMF_DecayNaniteInfection;
        public static HediffDef THNMF_DecayArchiteInfection;
        public static HediffDef THNMF_Dysbiosis;
        public static HediffDef THNMF_MetalhorrorCombat;
        public static HediffDef THNMF_SeveringMechlink;
        public static HediffDef THNMF_MechanizationShock;
        public static HediffDef THNMF_MetalMaw;
        public static HediffDef THNMF_MawRegeneration;
        public static HediffDef THNMF_LockerProtocolResurrection;
        public static HediffDef THNMF_AngelOSResurrection;
        public static HediffDef THNMF_ReconstructMortalityResurrection;
        public static HediffDef THNMF_RefuteMortalityResurrection;
        public static HediffDef THNMF_ResurrectionFog;
        public static HediffDef THNMF_BionanitePower;
        public static HediffDef THNMF_Ultraclock;

        public static HediffDef THNMF_MechanoidCrippled;
        public static HediffDef THNMF_MechanoidShock;
        public static HediffDef THNMF_ArchosplinterLink;
        public static HediffDef THNMF_ArchitePower;
        
        public static DamageDef THNMF_Decay;

        public static NaniteOperationDef THNMF_Reconfigure;
        
        public static EffecterDef THNMF_Nanite_Cloud;

        public static ThingDef THNMF_MechaniteBreeder;
        public static ThingDef THNMF_CorruptNexusCore;

        public static GameConditionDef THNMF_ArchiteFog;
        public static JobDef THNMF_StabilizeCorruptArchonexusCore;
        public static TaleDef THNMF_SavedAnArchotech;
        public static SitePartDef THNMF_CorruptNexus;
        public static SitePartDef THNMF_ArchotechNodeSite;
        public static ThingDef THNMF_ArchotechNode;
        public static QuestScriptDef THNMF_AssistingArchotechNode;
        public static QuestScriptDef THNMF_AssistingArchotechStart;


        public static Faction SplinterFaction => _splinterFaction ??= GetFaction(THNMF_Archosplinter);
        private static Faction _splinterFaction;
        public static Faction FormerSplinterFaction => _formerSplinterFaction ??= GetFaction(THNMF_ArchosplinterFormer);
        private static Faction _formerSplinterFaction;

        private static Faction GetFaction(FactionDef def)
        {
            IEnumerable<Faction> faction = Find.FactionManager.AllFactions.Where(faction => faction.def == def);
            IEnumerable<Faction> enumerable = faction as Faction[] ?? faction.ToArray();
            if (enumerable.Any())
            {
                return enumerable.First();
            }

            if (def == THNMF_Archosplinter)
            {
                return Faction.OfAncientsHostile;
            }
            return Faction.OfAncients;
        }

        static NMF_DefsOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(NMF_DefsOf));
        }
    }
}
