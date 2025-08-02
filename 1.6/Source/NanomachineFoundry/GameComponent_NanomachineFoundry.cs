using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using NanomachineFoundry.AssistingArchotechQuest;
using NanomachineFoundry.Utils;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace NanomachineFoundry
{
    public class GameComponent_NanomachineFoundry: GameComponent
    {
        public GameComponent_NanomachineFoundry(Game game)
        {
        }
        
        private HashSet<Pawn> _statAffectingPawns = new HashSet<Pawn>();
        private HashSet<Pawn> _capAffectingPawns = new HashSet<Pawn>();
        
        private HashSet<NaniteTracker_Pawn> ActiveTickers => _cachedTickers ??= _tickingPawns.Select(pawn => pawn.GetNaniteTracker()).Where(pawn => pawn != null).ToHashSet();
        private HashSet<NaniteTracker_Pawn> _cachedTickers;
        private HashSet<Pawn> _tickingPawns = new HashSet<Pawn>();
        
        
        public Pawn ActivatorPawn;
        public int ReturnMessageTick = -1;
        public int ReturnTick = -1;
        public bool ReturningPawn => ActivatorPawn != null && ReturnTick >= 0;
        public bool ReturnMessageIncoming => ReturnMessageTick >= 0;
        
        public void StartReturnTimer(Pawn pawn, float seconds)
        {
            ReturnMessageTick = (int)(GenTicks.TicksGame + 60f * (seconds + 2));
            ScreenFader.StartFade(new Color(56, 130, 28), seconds);
            ActivatorPawn = pawn;
        }

        public bool HailActive;

        public void SetHail()
        {
            HailActive = true;
            Find.LetterStack.ReceiveLetter("THNMF.AntennaHail".Translate(), "THNMF.AntennaHailDescription".Translate(), LetterDefOf.PositiveEvent);
        }

        public ICommunicable HailCommunicator => _hailCommunicator ??= new HailCommunicator();
        private ICommunicable _hailCommunicator;

        public void DoReturnMessage()
        {
            //10 second delay
            Find.WindowStack.Add(new Dialog_Message("THNMF.StabilizedArchotechMessage".Translate(ActivatorPawn.Named("PAWN"), ActivatorPawn.Possessive()),
                delegate
                {
                    ReturnTick = GenTicks.TicksGame + 600;
                }));
            ReturnMessageTick = -1;
            ActivatorPawn.DeSpawn();
            Site site = Find.WorldObjects.Sites.FirstOrFallback(site => site.MainSitePartDef == NMF_DefsOf.THNMF_CorruptNexus);
            site?.Destroy();
        }

        public void ReturnPawn()
        {
            Map respawnMap = Current.Game.AnyPlayerHomeMap;
            if (respawnMap == null) return;
            TaleRecorder.RecordTale(NMF_DefsOf.THNMF_SavedAnArchotech, ActivatorPawn);
            ActivatorPawn.GetNaniteTracker().GrantArchites(20);
            ActivatorPawn.health.AddHediff(NMF_DefsOf.THNMF_Archotouched);
            GenSpawn.Spawn(ActivatorPawn, CellFinder.RandomSpawnCellForPawnNear(respawnMap.Center, respawnMap, 50), respawnMap);
            Find.LetterStack.ReceiveLetter("THNMF.StabilizeReturned".Translate(), "THNMF.StabilizedArchotechReturnMessage".Translate(ActivatorPawn.Named("PAWN"), ActivatorPawn.Possessive()), LetterDefOf.PositiveEvent, ActivatorPawn);
            ActivatorPawn = null;
            ReturnTick = -1;
            AssistingArchotechQuestlineUtility.CompleteAllNexusQuests(QuestEndOutcome.Success);
            FreeAllSplinterPuppets();
        }
        
        public bool TryGetStatEffectingTracker(Pawn pawn, out NaniteTracker_Pawn tracker)
        {
            if (_statAffectingPawns.Contains(pawn))
            {
                tracker = pawn.GetNaniteTracker();
                return true;
            }
            tracker = null;
            return false;
        }

        public bool TryGetCapEffectingTracker(Pawn pawn, out NaniteTracker_Pawn tracker)
        {
            if (_capAffectingPawns.Contains(pawn))
            {
                tracker = pawn.GetNaniteTracker();
                return true;
            }
            tracker = null;
            return false;
        }
        
        public void NoteNewTicker(Pawn pawn)
        {
            _cachedTickers = null;
            _tickingPawns.Add(pawn);
        }
        public void RemoveTicker(Pawn pawn)
        {
            _cachedTickers = null;
            _tickingPawns.Remove(pawn);
        }
        
        public void NoteNewStatAffector(Pawn pawn)
        {
            _statAffectingPawns.Add(pawn);
        }
        public void RemoveStatAffector(Pawn pawn)
        {
            _statAffectingPawns.Remove(pawn);
        }
        
        public void NoteNewCapAffector(Pawn pawn)
        {
            _capAffectingPawns.Add(pawn);
        }
        public void RemoveCapAffector(Pawn pawn)
        {
            _capAffectingPawns.Remove(pawn);
        }

        public void RemovePawnFromAllLists(Pawn pawn)
        {
            _cachedTickers = null;
            _tickingPawns.Remove(pawn);
            _statAffectingPawns.Remove(pawn);
            _capAffectingPawns.Remove(pawn);
        }
        
        
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref _tickingPawns, true, "thnmf_activePawns", LookMode.Reference);
            Scribe_Collections.Look(ref _statAffectingPawns, true, "thnmf_statAffectingPawns", LookMode.Reference);
            Scribe_Collections.Look(ref _capAffectingPawns, true, "thnmf_capAffectingPawns", LookMode.Reference);
            Scribe_Deep.Look(ref ActivatorPawn, true, "thnmf_activatorPawn");
            Scribe_Values.Look(ref ReturnMessageTick, "thnmf_returnMessageTick", -1);
            Scribe_Values.Look(ref ReturnTick, "thnmf_returnTick", -1);
            Scribe_Values.Look(ref HailActive, "thnmf_hailActive");
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();
            int ticksGame = Find.TickManager.TicksGame;


            //Cleanup
            if (ticksGame % 15000 == 0)
            {
                NaniteTracker_Pawn.PurgePermadeadPawns();
                NmfUtils.DiminishingReturnsCalculator.CleanCache();
            }
            //Long tick
            if (ticksGame % 2000 == 0)
            {
                if (ReturningPawn)
                {
                    if (ticksGame > ReturnTick)
                    {
                        ReturnPawn();
                    }
                }
                ActiveTickers.Do(tracker => tracker.TickWorkersLong());
            }
            //Rare tick
            if (ticksGame % 250 == 0)
            {
                ActiveTickers.Do(tracker => tracker.TickWorkersRare());
            }
            //Tick
            ActiveTickers.Do(tracker => tracker.TickWorkers());


            if (ReturnMessageIncoming)
            {
                if (ticksGame > ReturnMessageTick - 5)
                {
                    ScreenFader.SetColor(Color.clear);
                }
                if (ticksGame > ReturnMessageTick)
                {
                    DoReturnMessage();
                }
            }


            
        }

        public void FreeAllSplinterPuppets()
        {
            List<Pawn> freedPrisoners = new List<Pawn>();

            foreach (Pawn pawn in PawnsFinder.All_AliveOrDead)
            {
                if (pawn.health.hediffSet.TryGetHediff(NMF_DefsOf.THNMF_ArchosplinterLink, out Hediff link))
                {
                    pawn.health.RemoveHediff(link);
                }
                else
                {
                    continue;
                }
                if (pawn.Dead) continue;
                pawn.guest.Recruitable = true;
                Faction holders = null;
                if (pawn.guest.IsPrisoner)
                {
                    holders = pawn.guest.HostFaction;
                    freedPrisoners.Add(pawn);
                }
                if (pawn.Faction == NMF_DefsOf.SplinterFaction) pawn.SetFaction(NMF_DefsOf.FormerSplinterFaction);
                if (freedPrisoners.Contains(pawn))
                {
                    pawn.guest?.SetGuestStatus(holders, GuestStatus.Prisoner);
                    pawn.guest.resistance = Rand.Range(0.5f, 3f);
                }
            }

            if (freedPrisoners.Any())
            {
                Find.LetterStack.ReceiveLetter("THNMF.SplinterPuppetsFreed".Translate(), "THNMF.SplinterPuppetsFreedDescription".Translate(), LetterDefOf.PositiveEvent, freedPrisoners);
            }
        }
        
    }

    public class HailCommunicator : ICommunicable
    {
        private readonly Pawn _communicator;
        private readonly Faction _communicatingFaction;
        
        public HailCommunicator()
        {
            Faction faction = Find.FactionManager.AllFactionsVisibleInViewOrder.Where(faction1 =>
                !faction1.HostileTo(Faction.OfPlayer) && faction1.def.techLevel >= TechLevel.Industrial && !faction1.IsPlayer).RandomElementWithFallback();
            _communicatingFaction = faction;
            if (faction != null)
            {
                _communicator = faction.leader;
                if (_communicator != null) return;
                PawnGenerationRequest request = new PawnGenerationRequest(PawnKindDefOf.AncientSoldier, Faction.OfAncients);
                _communicator = PawnGenerator.GeneratePawn(request);
            }
            else
            {
                PawnGenerationRequest request = new PawnGenerationRequest(PawnKindDefOf.AncientSoldier, Faction.OfAncients);
                _communicator = PawnGenerator.GeneratePawn(request);
            }
        }
        
        public string GetCallLabel() => "THNMF.FriendlyHail".Translate();
        
        public string GetInfoText() => "THNMF.FriendlyHail".Translate();
        
        public void TryOpenComms(Pawn negotiator)
        {
            string hailMessage = _communicatingFaction == null
                ? "THNMF.AntennaFoundHailMessageNoFaction".Translate(_communicator.Name.ToStringShort,
                    negotiator.Name.ToStringShort)
                : "THNMF.AntennaFoundHailMessage".Translate(_communicator.Name.ToStringShort,
                    _communicatingFaction.Name,
                    negotiator.Name.ToStringShort);
            Find.WindowStack.Add(new Dialog_Message(hailMessage, delegate
            {
                Current.Game.GetComponent<GameComponent_NanomachineFoundry>().HailActive = false;
                AssistingArchotechQuestlineUtility.CreateNexusSite(Find.AnyPlayerHomeMap?.Tile ?? -1);
            }));
        }
        
        

        public Faction GetFaction() => _communicatingFaction;

        public FloatMenuOption CommFloatMenuOption(Building_CommsConsole console, Pawn negotiator)
        {
            string label = "THNMF.AcceptHail".Translate(_communicator.Name.ToStringShort);
            
            return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label, delegate
            {
                console.GiveUseCommsJob(negotiator, this);
            }, MenuOptionPriority.InitiateSocial), negotiator, (LocalTargetInfo) (Thing) console);
        }
    }
}