using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace NanomachineFoundry.NaniteModifications.ModificationWorkers
{
    public class ModificationWorker_ControlDrift: ModificationWorker
    {
        public ModificationWorker_ControlDrift(NaniteModificationDef def, Pawn pawn) : base(def, pawn)
        {
        }
        public override void AfterRecalculated(bool isActive)
        {
            base.AfterRecalculated(isActive);
            if (!ModsConfig.BiotechActive || pawn.mechanitor == null)
                return;
            pawn.mechanitor.Notify_ApparelChanged();
        }
    }
}