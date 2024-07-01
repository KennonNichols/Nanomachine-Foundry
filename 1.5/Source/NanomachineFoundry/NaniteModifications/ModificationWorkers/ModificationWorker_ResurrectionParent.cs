using NanomachineFoundry.Utils;
using RimWorld;
using Verse;

namespace NanomachineFoundry.NaniteModifications.ModificationWorkers
{
    public abstract class ModificationWorker_ResurrectionParent: ModificationWorker
    {
        public bool IsPermadead = false;
        protected ModificationWorker_ResurrectionParent(NaniteModificationDef def, Pawn pawn) : base(def, pawn)
        {
            
        }

        public abstract HediffDef ResurrectionHediffDef { get; }

        public void AlertPermadead()
        {
            IsPermadead = true;
            NaniteTracker_Pawn.PurgePermadeadPawns();
            if (pawn.Faction == Faction.OfPlayer) Messages.Message("THNMF.ResurrectingPawnPermadead".Translate(pawn.Name.ToStringShort), pawn, MessageTypeDefOf.PawnDeath);
        }

        public virtual int? OverrideResurrectionTicks => null;

        private HediffComp_NaniteResurrection Resurrector => _cachedResurrector ??= GetResurrector();
        private HediffComp_NaniteResurrection _cachedResurrector;
        
        public virtual void OnResurrect()
        {
            Messages.Message("THNMF.Resurrected".Translate(pawn.Name.ToStringShort), pawn, MessageTypeDefOf.PositiveEvent);
        }
        
        public virtual bool CanResurrect(out bool isPermanentlyDead)
        {
            isPermanentlyDead = false;
            return true;
        }

        private HediffComp_NaniteResurrection GetResurrector()
        {
            if (!pawn.health.hediffSet.TryGetHediff(ResurrectionHediffDef, out Hediff resurrector)) return null;
            return resurrector.TryGetComp(out HediffComp_NaniteResurrection naniteResurrector) ? naniteResurrector : null;
        }
        
        public override void Tick()
        {
            if (pawn.Dead)
            {
                if (Resurrector != null)
                {
                    if (pawn.health.hediffSet.HasHediff(Resurrector.parent.def))
                    {
                        Resurrector.CountDownToResurrection();
                    }
                }
            }
            else
            {
                _cachedResurrector = null;
            }
        }

        public void TryGivePawnResurrectionHediff()
        {
            CanResurrect(out bool permadead);
            if (permadead)
            {
                AlertPermadead();
                return;
            }
            Hediff resurrection = HediffMaker.MakeHediff(ResurrectionHediffDef, pawn);
            resurrection.TryGetComp<HediffComp_NaniteResurrection>().Activate(this);
            pawn.health.AddHediff(resurrection);
        }
    }
}