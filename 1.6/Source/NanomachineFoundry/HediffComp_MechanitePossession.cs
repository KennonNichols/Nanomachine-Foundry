using System.Linq;
using JetBrains.Annotations;
using NanomachineFoundry.AssistingArchotechQuest;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace NanomachineFoundry
{
    public class HediffCompProperties_MechanitePossession : HediffCompProperties
    {
        public ThoughtDef possessionThought;
        public ThoughtDef possessionThoughtWitness;
        public ThoughtDef possessionThoughtTranshumanist;
        [CanBeNull] public MemeDef transhumanistMeme;
        
        public HediffCompProperties_MechanitePossession()
        {
            compClass = typeof(HediffComp_MechanitePossession);
        }
    }

    public class HediffComp_MechanitePossession : HediffComp
    {
        private HediffCompProperties_MechanitePossession Props => (HediffCompProperties_MechanitePossession)props;


        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            Possess(parent.pawn);
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            Depossess(parent.pawn);
        }

        private void Possess(Pawn pawn)
        {
            Find.LetterStack.ReceiveLetter("THNMF.MechanitePossession".Translate(),
                "THNMF.MechanitePossessionDescription".Translate(pawn.Name.ToStringShort, Faction.OfMechanoids.Name),
                LetterDefOf.ThreatBig, pawn, Faction.OfMechanoids);
            
            
            
            //Upset pawns
            UpsetAllColonistsForPossession(pawn);

            //Some jank to quiet Rimworld's bitching when adding a humanoid to the mechhive
            var oldProp = Faction.OfMechanoids.def.humanlikeFaction;
            Faction.OfMechanoids.def.humanlikeFaction = true;
            RecruitUtility.Recruit(pawn, Faction.OfMechanoids);
            Faction.OfMechanoids.def.humanlikeFaction = oldProp;

            pawn.guest.Recruitable = false;
            LordMaker.MakeNewLord(Faction.OfMechanoids,
                new LordJob_AssaultColony(Faction.OfMechanoids, false, true, false, false, false), pawn.Map,
                new[] { pawn });
        }

        private void Depossess(Pawn pawn)
        {
            Find.LetterStack.ReceiveLetter("THNMF.MechanitePossessionEnded".Translate(),
                "THNMF.MechanitePossessionEndedDescription".Translate(pawn.Name.ToStringShort),
                LetterDefOf.PositiveEvent, pawn, Faction.OfMechanoids);
            RecruitUtility.Recruit(pawn, Faction.OfPlayer);
        }

        private bool IsTranshumanist(Pawn pawn)
        {
            if (!pawn.RaceProps.Humanlike) return false;
            if (pawn.story.traits.HasTrait(TraitDefOf.Transhumanist)) return true;
            if (!ModsConfig.IdeologyActive) return false;
            return pawn.Ideo?.HasMeme(Props.transhumanistMeme) ?? false;
        }

        private void UpsetAllColonistsForPossession(Pawn victim)
        {
            foreach (var witness in PawnsFinder.AllMaps_SpawnedPawnsInFaction(victim.Faction).Where(witness => witness != victim && witness.RaceProps.Humanlike && PawnUtility.ShouldGetThoughtAbout(witness, victim)))
            {
                ThoughtDef memory;
                if (IsTranshumanist(witness))
                {
                    memory = Props.possessionThoughtTranshumanist;
                }
                else if (ThoughtUtility.Witnessed(witness, victim))
                {
                    memory = Props.possessionThoughtWitness;
                }
                else
                {
                    memory = Props.possessionThought;
                }
                witness.needs.mood.thoughts.memories.TryGainMemory(memory);
            }
        }
    }
}