using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace NanomachineFoundry.NaniteModifications.ModificationWorkers
{
    public class ModificationWorker_PsychicRecycler: ModificationWorker
    {
        private float _sensitivityPerTick;
        private Mote _psyfocusMote;
        private Sustainer _sustainer;
        
        
        
        
        public ModificationWorker_PsychicRecycler(NaniteModificationDef def, Pawn pawn) : base(def, pawn)
        {
        }

        public override void RecalculateStats(float naniteLevel, int intendedNaniteLevel, NaniteDef type = null)
        {
            base.RecalculateStats(naniteLevel, 0, type);
            //naniteLevel * 0.5f * .01f / 2500
            _sensitivityPerTick = naniteLevel * 0.000002f;
        }

        public override void Tick()
        {
            base.Tick();
            if (pawn.Awake()) return;
            if (!ModsConfig.RoyaltyActive || pawn.psychicEntropy == null) return;
            if (pawn.psychicEntropy.CurrentPsyfocus > 0.99f) return;
            
            
            if (pawn.IsHashIntervalTick(100))
            {
                FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, FleckDefOf.Meditating);
            }
            pawn.psychicEntropy.Notify_Meditated();
            if (pawn.HasPsylink && pawn.psychicEntropy.PsychicSensitivity > float.Epsilon)
            {
                float yOffset = (pawn.Position.x % 2 + pawn.Position.z % 2) / 10f;
                if (_psyfocusMote == null || _psyfocusMote.Destroyed)
                {
                    _psyfocusMote = MoteMaker.MakeAttachedOverlay(pawn, ThingDefOf.Mote_PsyfocusPulse, Vector3.zero);
                    _psyfocusMote.yOffset = yOffset;
                }
                _psyfocusMote.Maintain();
                if (_sustainer == null || _sustainer.Ended)
                {
                    _sustainer = SoundDefOf.MeditationGainPsyfocus.TrySpawnSustainer(SoundInfo.InMap(pawn, MaintenanceType.PerTick));
                }
                _sustainer.Maintain();
                pawn.psychicEntropy.OffsetPsyfocusDirectly(_sensitivityPerTick);
            }
            
        }
    }
}