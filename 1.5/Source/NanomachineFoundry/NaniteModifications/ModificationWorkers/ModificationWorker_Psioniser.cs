using NanomachineFoundry.Utils;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace NanomachineFoundry.NaniteModifications.ModificationWorkers
{
    public class ModificationWorker_Psioniser: ModificationWorker
    {
        public bool Entranced => pawn.GetNaniteTracker()?.Entranced ?? false;
        private Mote _psyfocusMote;
        private Sustainer _sustainer;

        private float _baseAffectorPerTick;
        private float _heatMultiplier = -1;
        private float _psyfocusMultiplier = 0.001f;
        
        public ModificationWorker_Psioniser(NaniteModificationDef def, Pawn pawn) : base(def, pawn)
        {
            
        }

        public override void RecalculateStats(float naniteLevel, int intendedNaniteLevel, NaniteDef type = null)
        {
            base.RecalculateStats(naniteLevel, 0, type);
            //naniteLevel * 0.1f / 60
            _baseAffectorPerTick = naniteLevel * 0.001666f;
        }

        public void ToggleTrance()
        {
            pawn.GetNaniteTracker().Entranced = !Entranced;
        }

        private float GetCurrentAffector()
        {
            MeditationSpotAndFocus spotAndFocus = MeditationUtility.FindMeditationSpot(pawn);
            if (!(spotAndFocus.focus == LocalTargetInfo.Invalid))
            {
                if (spotAndFocus.spot.Thing.InteractionCell.Equals(pawn.Position))
                {
                    float meditationStrength = 0;
                    if (spotAndFocus.focus.Thing is { Destroyed: false })
                        meditationStrength += spotAndFocus.focus.Thing.GetStatValueForPawn(StatDefOf.MeditationFocusStrength, pawn);
                    return _baseAffectorPerTick * (1 + meditationStrength);
                }
            }
            return _baseAffectorPerTick;
        }

        public override void Tick()
        {
            base.Tick();
            if (!Entranced) return;
            if (!pawn.HasPsylink) return;
            if (pawn.Downed || (pawn.MentalState?.causedByMood ?? false))
            {
                if (_psychicComaEnabled)
                {
                    pawn.health.AddHediff(DefDatabase<HediffDef>.GetNamed("PsychicComa"));
                    Find.LetterStack.ReceiveLetter("THNMF.DownedInTrance".Translate(), "THNMF.DownedInTranceDescription".Translate(pawn.Name.ToStringShort), LetterDefOf.NegativeEvent, pawn);
                }
                ToggleTrance();
            }

            
            if (pawn.IsHashIntervalTick(100))
            {
                FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, FleckDefOf.Meditating);
            }
            pawn.psychicEntropy.Notify_Meditated();
            if (pawn.psychicEntropy.PsychicSensitivity > float.Epsilon)
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
                MedidateTick();
            }
        }

        private void MedidateTick()
        {
            //Cool off neurals
            float currentAffector = GetCurrentAffector();
            pawn.psychicEntropy.OffsetPsyfocusDirectly(currentAffector * _psyfocusMultiplier);
            pawn.psychicEntropy.TryAddEntropy(currentAffector * _heatMultiplier, scale: false);
        }
        
        
        private static bool _psychicComaEnabled = true;
        public override void ScribeConfigs()
        {
            base.ScribeConfigs();
            Scribe_Values.Look(ref _psychicComaEnabled, ConfigID("psychicComaEnabled"), true);
        }
        public override void DoConfigMenu(Listing_Standard listingStandard, ref bool anyChange)
        {
            base.DoConfigMenu(listingStandard, ref anyChange);
            bool newEnabled = _psychicComaEnabled;
            listingStandard.CheckboxLabeled("THNMF.Config_PsychicComaEnabled".Translate(), ref newEnabled, "THNMF.Config_PsychicComaEnabledDescription".Translate());
            //Return true if it was changed
            if (newEnabled == _psychicComaEnabled) return;
            _psychicComaEnabled = newEnabled;
            anyChange = true;
        }

        protected override void ResetConfig()
        {
            base.ResetConfig();
            _psychicComaEnabled = true;
        }

        public override bool HasConfig => true;
    }
}