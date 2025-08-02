using System.Collections.Generic;
using System.Linq;
using NanomachineFoundry.NaniteProduction;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace NanomachineFoundry
{
    public class Alert_ExcessNanites: Alert_Critical
    {
        private readonly List<GlobalTargetInfo> _targets = new List<GlobalTargetInfo>();


        public override AlertPriority Priority => AlertPriority.High;

        private List<GlobalTargetInfo> Culprits { get
            {
                _targets.Clear();
                List<Map> maps = Find.Maps;
                foreach (var breedingPlatform in maps.SelectMany(t => t.listerBuildings.AllBuildingsColonistOfDef(NMF_DefsOf.THNMF_MechaniteBreeder)))
                {
                    if (breedingPlatform.TryGetComp(out CompMechaniteBreeder breeder) && breeder.mechanitesBuilding)
                        _targets.Add((GlobalTargetInfo) (Thing) breedingPlatform);
                }
                return _targets;
            }
        }

        public override string GetLabel() => "THNMF.ExcessMechanitesBuilding".Translate();

        public override TaggedString GetExplanation() => "THNMF.ExcessMechanitesBuildingDescription".Translate();

        public override AlertReport GetReport() => AlertReport.CulpritsAre(Culprits);
    }
}