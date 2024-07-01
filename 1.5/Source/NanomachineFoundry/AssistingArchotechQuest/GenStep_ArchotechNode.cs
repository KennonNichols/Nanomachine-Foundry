using System.Collections.Generic;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
using Verse;

namespace NanomachineFoundry.AssistingArchotechQuest
{
    public class GenStep_ArchotechNode: GenStep_Scatterer
    {
        public ThingSetMakerDef thingSetMakerDef;

        private const int Size = 7;

        public override int SeedPart => 913432591;

        protected override bool CanScatterAt(IntVec3 c, Map map)
        {
            if (!base.CanScatterAt(c, map))
            {
                return false;
            }
            if (!c.SupportsStructureType(map, TerrainAffordanceDefOf.Heavy))
            {
                return false;
            }
            if (!map.reachability.CanReachMapEdge(c, TraverseParms.For(TraverseMode.PassDoors)))
            {
                return false;
            }
            CellRect rect = CellRect.CenteredOn(c, 7, 7);
            if (MapGenerator.TryGetVar<List<CellRect>>("UsedRects", out var var) && var.Any((CellRect x) => x.Overlaps(rect)))
            {
                return false;
            }
            foreach (IntVec3 item in rect)
            {
                if (!item.InBounds(map) || item.GetEdifice(map) != null)
                {
                    return false;
                }
            }
            return true;
        }

        protected override void ScatterAt(IntVec3 loc, Map map, GenStepParams parms, int count = 1)
        {
            CellRect cellRect = CellRect.CenteredOn(loc, 7, 7).ClipInsideMap(map);
            if (!MapGenerator.TryGetVar<List<CellRect>>("UsedRects", out var var))
            {
                var = new List<CellRect>();
                MapGenerator.SetVar("UsedRects", var);
            }
            ResolveParams resolveParams = default(ResolveParams);
            resolveParams.rect = cellRect;
            resolveParams.faction = map.ParentFaction;
            if (parms.sitePart is { things.Any: true })
            {
                resolveParams.stockpileConcreteContents = parms.sitePart.things;
            }
            else
            {
                ItemStashContentsComp component = map.Parent.GetComponent<ItemStashContentsComp>();
                if (component != null && component.contents.Any)
                {
                    resolveParams.stockpileConcreteContents = component.contents;
                }
                else
                {
                    resolveParams.thingSetMakerDef = thingSetMakerDef ?? ThingSetMakerDefOf.MapGen_DefaultStockpile;
                }
            }
            BaseGen.globalSettings.map = map;
            BaseGen.symbolStack.Push("storage", resolveParams);
            BaseGen.Generate();
            MapGenerator.SetVar("RectOfInterest", cellRect);
            var.Add(cellRect);
            
            Thing node = ThingMaker.MakeThing(NMF_DefsOf.THNMF_ArchotechNode);
            GenSpawn.Spawn(node, cellRect.CenterCell, map, Rot4.Random);
            
        }
    }
}