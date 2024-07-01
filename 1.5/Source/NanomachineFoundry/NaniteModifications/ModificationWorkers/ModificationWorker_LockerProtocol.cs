using NanomachineFoundry.NaniteModifications.ModificationAbilities;
using NanomachineFoundry.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace NanomachineFoundry.NaniteModifications.ModificationWorkers
{
    public class ModificationWorker_LockerProtocol: ModificationWorker_ResurrectionParent
    {
        public ModificationWorker_LockerProtocol(NaniteModificationDef def, Pawn pawn) : base(def, pawn)
        {
        }

        private float _resurrectionTimeHours;
        private float _brainHealth;
        private bool _canHealBrainDamage;
        private NaniteDef _allocatedNanites;
        private float _shockSeverity;

        public override HediffDef ResurrectionHediffDef => NMF_DefsOf.THNMF_LockerProtocolResurrection;
        //2500
        public override int? OverrideResurrectionTicks => Mathf.Max(300, (int)(2500 * _resurrectionTimeHours));

        public override void RecalculateStats(float naniteLevel, int intendedNaniteLevel, NaniteDef type = null)
        {
            base.RecalculateStats(naniteLevel, 0, type);
            _resurrectionTimeHours = def.TryGetScaledValueSlow(naniteLevel, "ResurrectionTimeHours");
            _allocatedNanites = type;
            _canHealBrainDamage = (_allocatedNanites?.tier ?? 0) >= 2;
            _shockSeverity = 1 - 0.02f * naniteLevel;
        }

        public override void OnResurrect()
        {
            base.OnResurrect();
            NaniteTracker_Pawn tracker = pawn.GetNaniteTracker();
            //Spend nanites
            tracker.TryChangeNanitesLevel(_allocatedNanites, -tracker.GetAllocation(def).Amount, true);
            //Heal brain damage
            if (_canHealBrainDamage)
            {
                BodyPartRecord brain = pawn.health.hediffSet.GetBrain();
                pawn.health.RestorePart(brain);
            }
            
            
            //Inflict shock
            if (_allocatedNanites.shockHediff != null)
            {
                Hediff shock = HediffMaker.MakeHediff(_allocatedNanites.shockHediff, pawn);
                shock.Severity = _shockSeverity;
                pawn.health.AddHediff(shock);
            }
        }

        public override bool CanResurrect(out bool isPermanentlyDead)
        {
            NaniteTracker_Pawn tracker = pawn.GetNaniteTracker();
            //Spend nanites
            if (tracker.GetAllocation(def).Amount > tracker.NaniteLevels[_allocatedNanites])
            {
                isPermanentlyDead = true;
                return false;
            }
            
            _brainHealth = pawn.health.hediffSet.GetPartHealth(pawn.health.hediffSet.GetBrain());
            if (_brainHealth > 0 || _canHealBrainDamage)
            {
                isPermanentlyDead = false;
            }
            else
            {
                isPermanentlyDead = true;
            }
            return !isPermanentlyDead;
        }
    }
}