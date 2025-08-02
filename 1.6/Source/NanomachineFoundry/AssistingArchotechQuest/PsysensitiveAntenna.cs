using System.Collections.Generic;
using System.Text;
using PipeSystem;
using RimWorld;
using Verse;

namespace NanomachineFoundry.AssistingArchotechQuest
{
    public class PsysensitiveAntenna: Building
    {
        private int _rareTicksUntilNotify = 360;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref _rareTicksUntilNotify, "thnmf_rareTicksUntilNotify");
        }

        public override void TickRare()
        {
            base.TickRare();
            if (!AwaitingResponse) return;
            if (!IsActiveCommsConsole) return;
            if (!AssistingArchotechQuestlineUtility.IsNexusQuestActive()) return;
            _rareTicksUntilNotify--;
            if (_rareTicksUntilNotify > 0) return;
            _rareTicksUntilNotify = -1;
            AssistingArchotechQuestlineUtility.NotifyAntennaActive();
        }

        private bool AwaitingResponse => _rareTicksUntilNotify > 0;

        private bool IsActiveCommsConsole
        {
            get
            {
                Thing console = Map.listerThings.ThingsOfDef(ThingDefOf.CommsConsole).FirstOrFallback();
                if (console != null)
                {
                    if (console.TryGetComp(out CompPowerTrader trader))
                    {
                        return trader.PowerOn;
                    }
                }

                return false;
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            if (!DebugSettings.ShowDevGizmos)
            {
                yield break;
            }
            Command_Action commandAction = new Command_Action
            {
                defaultLabel = "DEV: Activate quest",
                action = AssistingArchotechQuestlineUtility.NotifyAntennaActive
            };
            yield return commandAction;
        }

        public override string GetInspectString()
        {
            if (!AssistingArchotechQuestlineUtility.IsNexusQuestActive()) return base.GetInspectString();
            StringBuilder builder = new StringBuilder(base.GetInspectString());
            if (_rareTicksUntilNotify > 0)
            {
                if (!IsActiveCommsConsole)
                {
                    builder.AppendLineIfNotEmpty().Append("THNMF.CannotUseNoCommsConsole".Translate());
                }
                else
                {
                    builder.AppendLineIfNotEmpty().Append("THNMF.UsingCommsConsole".Translate());
                    
                }
            }
            return builder.ToString();
        }
    }
}