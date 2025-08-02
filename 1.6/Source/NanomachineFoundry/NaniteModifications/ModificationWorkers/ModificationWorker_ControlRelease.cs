using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace NanomachineFoundry.NaniteModifications.ModificationWorkers
{
    public class ModificationWorker_ControlRelease: ModificationWorker
    {
        private int _controlGroupsConsumed;
        
        
        public ModificationWorker_ControlRelease(NaniteModificationDef def, Pawn pawn) : base(def, pawn)
        {
        }
        public override void ApplyStatOffsets(ref Dictionary<StatDef, float> offsets)
        {
            base.ApplyStatOffsets(ref offsets);
            offsets[StatDefOf.MechControlGroups] = offsets.GetWithFallback(StatDefOf.MechControlGroups) - _controlGroupsConsumed;
            offsets[StatDefOf.MechBandwidth] = offsets.GetWithFallback(StatDefOf.MechBandwidth) + _controlGroupsConsumed * 3;
        }

        public override void RecalculateStats(float naniteLevel, int intendedNaniteLevel = 0, NaniteDef type = null)
        {
            base.RecalculateStats(naniteLevel, intendedNaniteLevel, type);
            int controlGroupBase = (int)(pawn.GetStatValue(StatDefOf.MechControlGroups) + _controlGroupsConsumed);
            _controlGroupsConsumed = Math.Min(intendedNaniteLevel, controlGroupBase - 1);
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