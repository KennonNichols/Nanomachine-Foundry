using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using NanomachineFoundry.NaniteModifications;
using NanomachineFoundry.Utils;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;

namespace NanomachineFoundry.AssistingArchotechQuest
{
    public class GenStep_CorruptNexus: GenStep_Scatterer
    {
        private const int Size = 60;

        public override int SeedPart => 473957495;

        public override void Generate(Map map, GenStepParams parms)
        {
            count = 1;
            nearMapCenter = true;
            base.Generate(map, parms);
        }

        protected override bool CanScatterAt(IntVec3 c, Map map)
        {
            if (!base.CanScatterAt(c, map))
            {
                return false;
            }
            if (!c.Standable(map))
            {
                return false;
            }
            if (c.Roofed(map))
            {
                return false;
            }
            ThingDef corruptNexus = NMF_DefsOf.THNMF_CorruptNexusCore;
            IntVec3 c2 = ThingUtility.InteractionCellWhenAt(corruptNexus, c, corruptNexus.defaultPlacingRot, map);
            return map.reachability.CanReachMapEdge(c2, TraverseParms.For(TraverseMode.PassDoors));
        }

        protected override void ScatterAt(IntVec3 c, Map map, GenStepParams parms, int stackCount = 1)
        {
            SitePartParams parms2 = parms.sitePart.parms;
            ResolveParams resolveParams = default(ResolveParams);
            resolveParams.threatPoints = parms2.threatPoints;
            resolveParams.rect = CellRect.CenteredOn(c, 60, 60);
            BaseGen.globalSettings.map = map;
            BaseGen.symbolStack.Push("archonexus", resolveParams);
            BaseGen.Generate();
            
            //Don't look at this, please :)
            Thing archonexusCore = map.listerThings.ThingsOfDef(ThingDefOf.ArchonexusCore).First();
            IntVec3 coreLocation = archonexusCore.Position;
            Rot4 coreRotation = archonexusCore.Rotation;
            Thing.allowDestroyNonDestroyable = true;
            archonexusCore.Destroy();
            Thing.allowDestroyNonDestroyable = false;
            Thing nexus = ThingMaker.MakeThing(NMF_DefsOf.THNMF_CorruptNexusCore);
            GenSpawn.Spawn(nexus, coreLocation, map, coreRotation);
            
            //Or this (adds nexus guardian)
            string name = Rand.Range(1, 13).ToString();
            PawnGenerationRequest request = new PawnGenerationRequest(PawnKindDefOf.AncientSoldier, NMF_DefsOf.SplinterFaction, PawnGenerationContext.NonPlayer, map.Tile, forceGenerateNewPawn: true, allowDead: false, allowDowned: false, canGeneratePawnRelations: false, mustBeCapableOfViolence: true, biologicalAgeRange: new FloatRange(18, 32), fixedBirthName: name, forceNoBackstory: true);
            NaniteEffectCategoryDef architeCat = DefDatabase<NaniteEffectCategoryDef>.GetNamed("THNMF_Archite");
            List<NaniteModificationDef> modificationDefs = DefDatabase<NaniteModificationDef>.AllDefs
                .Where(naniteModificationDef => naniteModificationDef.categoryDef == architeCat).ToList();

            int tries = 0;
            const int maxTries = 30;
            
            while (tries < maxTries)
            {
                tries++;
                Pawn pawn = PawnGenerator.GeneratePawn(request);
                if (pawn.IsMechanized()) continue;
                PawnSpawnNaniteUtility.MechanizePawn(pawn, 5000000, NMF_DefsOf.THNMF_Archite, modificationDefs);
                GenSpawn.Spawn(pawn, map.Center + new IntVec3(0, 0, 8), map);


                PawnSpawnNaniteUtility.ArchosplinterizePawn(pawn);
                
                pawn.story.Adulthood = NMF_DefsOf.THNMF_NexusGuardian;
                pawn.story.birthLastName = "THNMF.GuardianLastName".Translate();
                pawn.Name = new NameTriple(name, name, "THNMF.GuardianLastName".Translate());
                
                LordMaker.MakeNewLord(NMF_DefsOf.SplinterFaction, new LordJob_DefendBase(NMF_DefsOf.SplinterFaction, map.Center, 0), map, [pawn]);
                break;
            }
            
            
        }
    }
}