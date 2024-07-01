using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace NanomachineFoundry.NaniteModifications.ModificationWorkers
{
    public class ModificationWorker_PsychicCache: ModificationWorker
    {
        private Dictionary<AbilityDef, int> Cache => pawn.GetNaniteTracker().PsychicAbilityCache;
        
        private float _cacheChance;
        public float CachedHeatFactor { get; private set; }
        
        public ModificationWorker_PsychicCache(NaniteModificationDef def, Pawn pawn) : base(def, pawn)
        {
        }

        public override void RecalculateStats(float naniteLevel, int intendedNaniteLevel, NaniteDef type = null)
        {
            base.RecalculateStats(naniteLevel, intendedNaniteLevel, type);
            _cacheChance = naniteLevel * .01f;
            CachedHeatFactor = 1 - naniteLevel * .02f;
        }

        public void TryCache(Psycast psycast)
        {
            if (psycast.def.IsPsycast)
            {
                if (Rand.Chance(_cacheChance))
                {
                    //30 seconds of cachedness
                    Cache[psycast.def] = GenTicks.TicksGame + _cacheDuration;
                    //Alert
                    MoteMaker.ThrowText(psycast.pawn.DrawPos, psycast.pawn.Map, "THNMF.CachedAlert".Translate(), Color.magenta);
                    //Reset CD
                    psycast.ResetCooldown();
                }
            }
        }
        public void DeCache(AbilityDef psycast)
        {
            Cache.Remove(psycast);
        }

        public bool IsCached(AbilityDef psycast)
        {
            if (!Cache.TryGetValue(psycast, out var expirationTick)) return false;
            if (GenTicks.TicksGame > expirationTick)
            {
                //It's past it's time, so we remove it
                Cache.Remove(psycast);
            }
            else
            {
                return true;
            }
            return false;
        }

        public string GetCacheReport(AbilityDef psycast)
        {
            return "THNMF.PsychicCacheReport".Translate((Cache[psycast] - GenTicks.TicksGame).TicksToSeconds());
        }
        
        
        private static int _cacheDuration = 1800;
        private static int CacheSeconds => _cacheDuration / 60;
        public override void ScribeConfigs()
        {
            base.ScribeConfigs();
            Scribe_Values.Look(ref _cacheDuration, ConfigID("cacheDuration"), 1800);
        }
        public override void DoConfigMenu(Listing_Standard listingStandard, ref bool anyChange)
        {
            base.DoConfigMenu(listingStandard, ref anyChange);
            int newTimeSeconds = Mathf.RoundToInt(listingStandard.SliderLabeled("THNMF.Config_CacheDuration".Translate(CacheSeconds), CacheSeconds, 1, 120));
            //Return true if it was changed
            if (CacheSeconds == newTimeSeconds) return;
            _cacheDuration = newTimeSeconds * 60;
            anyChange = true;
        }

        protected override void ResetConfig()
        {
            base.ResetConfig();
            _cacheDuration = 1800;
        }

        public override bool HasConfig => true;
    }
}