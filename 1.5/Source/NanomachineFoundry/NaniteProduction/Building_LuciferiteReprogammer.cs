using System.Collections.Generic;
using System.Linq;
using PipeSystem;
using RimWorld;
using Verse;

namespace NanomachineFoundry.NaniteProduction
{
    public class Building_LuciferiteReprogammer : Building
    {
        private float ProductionPerDay = 3;
        private int ConversionInterval = 250;
        private float LuciferitesProducedPerInterval => ProductionPerDay / 60000 * ConversionInterval;
        private float ScarletToLuciferiteRatio;
        private float ScarletsConsumedPerInterval => LuciferitesProducedPerInterval * ScarletToLuciferiteRatio;
        
        public IEnumerable<CompResource> Resources => _compResources ??= (_compResources = GetComps<CompResource>());
        
        private CompPowerTrader PowerTrader => _compPowerTrader ??= (_compPowerTrader = GetComp<CompPowerTrader>());

        public CompResource LuciferiteResource => _luciferiteResource ??=
            Resources.First(resource => resource.PipeNet.def == NMF_DefsOf.THNMF_LuciferiteNet);
        private CompResource _luciferiteResource;
        
        public CompResource ScarletMechanitesResource => _scarletMechanitesResource ??=
            Resources.First(resource => resource.PipeNet.def != NMF_DefsOf.THNMF_LuciferiteNet);
        private CompResource _scarletMechanitesResource;

        private IEnumerable<CompResource> _compResources;
        
        private CompPowerTrader _compPowerTrader;
        
        public override void Tick()
        {
            base.Tick();
            if (!PowerTrader.PowerOn) return;
            if (!this.IsHashIntervalTick(ConversionInterval)) return;
            //If we have enough free space to convert, and enough origin to convert 
            if (!(LuciferiteResource.PipeNet.AvailableCapacity > 0) ||
                !(ScarletMechanitesResource.PipeNet.CurrentStored() >= ScarletsConsumedPerInterval)) return;
            
            LuciferiteResource.PipeNet.DistributeAmongStorage(LuciferitesProducedPerInterval, out float luciferitesProduced);
            ScarletMechanitesResource.PipeNet.DrawAmongStorage(luciferitesProduced * ScarletToLuciferiteRatio, ScarletMechanitesResource.PipeNet.storages);
        }
    }
}