using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using HarmonyLib;
using NanomachineFoundry.NaniteModifications;
using NanomachineFoundry.Utils;
using UnityEngine;
using Verse;

namespace NanomachineFoundry
{
    public class NMFSettings : ModSettings
    {
        public static float MechanizationAge = 1.5f;
        public static int MechanizationAgingMultiplier = 30;
        
        private const float ModWindowHeight = 150f;
        private const float GapSize = 10f;
        private Vector2 _scrollPosition;
        public void DoSettingsWindowContents(Rect inRect)
        {
            bool anyChange = false;
            
            Rect basicArea = inRect.TopPart(0.2f);
            Listing_Standard centralThings = new Listing_Standard();
            centralThings.Begin(basicArea);
            DoAgeConfig(ref anyChange, centralThings);
            centralThings.End();
            
            
            List<NaniteModificationDef> defs =
                DefDatabase<NaniteModificationDef>.AllDefsListForReading.Where(def =>
                    def.ConfigWorkerInstance().HasConfig).ToList();
            Rect displayArea = inRect.TopPart(0.75f) with {y = inRect.y + inRect.height * 0.20f};
            Rect sectionRect = new Rect(displayArea.x, displayArea.y, displayArea.width - 50f, Math.Max(displayArea.height, (ModWindowHeight + GapSize) * (defs.Count + 1)));
            Listing_Standard listingStandard = new Listing_Standard();
            Widgets.BeginScrollView(displayArea, ref _scrollPosition, sectionRect);
            listingStandard.Begin(sectionRect);
            defs.Do(def =>
            {
                Listing_Standard modSection = listingStandard.BeginSection(ModWindowHeight);
                def.ConfigWorkerInstance().DoConfigMenu(modSection, ref anyChange);
                listingStandard.EndSection(modSection);
                listingStandard.Gap(GapSize);
            });
            listingStandard.EndSection(listingStandard.BeginSection(ModWindowHeight));
            if (anyChange)
            {
                NaniteTracker_Pawn.RecalculateAllNaniteWorkers();
                Write();
            }
            listingStandard.End();
            Widgets.EndScrollView();
        }

        private void DoAgeConfig(ref bool anyChange, Listing_Standard listingStandard)
        {
            float newMechAge = (float)Math.Round(listingStandard.SliderLabeled("THNMF.Config_MechanizationAge".Translate(MechanizationAge.ToString("0.0")), MechanizationAge, 0.1f, 10, 0.5f, "THNMF.Config_MechanizationAgeDescription".Translate()), 1);
            int agingMultiplier = Mathf.RoundToInt(listingStandard.SliderLabeled(
                "THNMF.Config_MechanizationAgingMultiplier".Translate(MechanizationAgingMultiplier), MechanizationAgingMultiplier, 1,
                100, 0.5f, "THNMF.Config_MechanizationAgingMultiplierDescription".Translate()));
            
            
            //Reset button
            if (listingStandard.ButtonText("ResetBinding".Translate()))
            {
                newMechAge = 1.5f;
                agingMultiplier = 30;
            }
            if (Mathf.Approximately(newMechAge, MechanizationAge) && agingMultiplier == MechanizationAgingMultiplier) return;
            MechanizationAge = newMechAge;
            MechanizationAgingMultiplier = agingMultiplier;
            anyChange = true;
        }

        public override void ExposeData()
        {
            //Log.Message("Loading shit!");
            base.ExposeData();
            Scribe_Values.Look(ref MechanizationAgingMultiplier, "thnmf_mechanizationAgingMultiplier", 30);
            Scribe_Values.Look(ref MechanizationAge, "thnmf_mechanizationAge", 1.5f);
            DefDatabase<NaniteModificationDef>.AllDefs.Do(def => def.ConfigWorkerInstance().ScribeConfigs());
        }

        public void NotifyTouchLoad()
        {
            //Log.Message("Touch loaded:");
            //Log.Message(DefDatabase<NaniteModificationDef>.AllDefsListForReading.Count);
        }
    }


    [StaticConstructorOnStartup]
    public class SettingsLoader
    {
        static SettingsLoader()
        {
            NanomachineFoundry_Mod.LoadSettingsIfNotYet();
        }
    }
    
    public class NanomachineFoundry_Mod: Mod
    {
        public static void LoadSettingsIfNotYet()
        {
            if (_hasLoaded) return;
            NanomachineFoundry_Mod mod = LoadedModManager.GetMod<NanomachineFoundry_Mod>();
            mod.Settings.NotifyTouchLoad();
        }

        private static bool _hasLoaded;

        private NMFSettings LoadSettings()
        {
            _hasLoaded = true;
            return GetSettings<NMFSettings>();
        }

        private NMFSettings Settings => _settings ??= LoadSettings();
        private NMFSettings _settings;
        
        public NanomachineFoundry_Mod(ModContentPack content) : base(content)
        {
            //Log.Message("Starting shit!");
        }
        
        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            Settings.DoSettingsWindowContents(inRect);
        }
        
        public override string SettingsCategory()
        {
            return Content.Name;
        }
        
        
    }
}