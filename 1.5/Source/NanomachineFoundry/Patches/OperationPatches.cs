using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using NanomachineFoundry.Utils;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace NanomachineFoundry.Patches
{
    public class OperationPatches
    {
        public new static Type GetType()
        {
            return typeof(PawnPatches);
        }

        private static HashSet<ResearchProjectDef> _correctedProjects = new HashSet<ResearchProjectDef>();

        public static void ApplyPatches(Harmony harmony)
        {
            harmony.Patch(AccessTools.PropertyGetter(typeof(SkillRecord), "PermanentlyDisabled"), prefix: new HarmonyMethod(typeof(OperationPatches), "CheckDefAbilityByNeurorewire"));
            harmony.Patch(AccessTools.PropertyGetter(typeof(SkillRecord), "TotallyDisabled"), prefix: new HarmonyMethod(typeof(OperationPatches), "CheckDefAbilityByNeurorewire"));
            harmony.Patch(AccessTools.PropertyGetter(typeof(ResearchProjectDef), nameof(ResearchProjectDef.UnlockedDefs)), postfix: new HarmonyMethod(typeof(OperationPatches), "BeforeTryingToGetUnlockedDefs"));
            
            
            
            harmony.Patch(AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.CombinedDisabledWorkTags)),
                prefix: new HarmonyMethod(typeof(OperationPatches), nameof(NeurorewireEnableWorkTags)));
            MethodInfo fillMethod = typeof(Pawn)
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .FirstOrDefault(method => method.Name.Contains("FillList"));
            harmony.Patch(fillMethod, prefix: new HarmonyMethod(typeof(OperationPatches), nameof(AddNeurorewireDisabledWorkTypes)));
        }
        
        
        //NEUROREWIRE: allow all work types and tags if rewired, except ones explicitly cut
        public static bool AddNeurorewireDisabledWorkTypes(Pawn __instance, ref List<WorkTypeDef> list)
        {
            if (!(__instance?.GetNaniteTracker()?.Neurorewired ?? false)) return true;
            list.Clear();
            List<SkillDef> enabledSkills = __instance.GetNaniteTracker().EnabledSkills;

            //Disable all work types that do not correspond to an enabled skill
            list.AddRange(DefDatabase<WorkTypeDef>.AllDefs.Where(def =>
                def.relevantSkills.Any(skillDef => !enabledSkills.Contains(skillDef))));
            return false;
        }
        private static bool NeurorewireEnableWorkTags(Pawn __instance, ref WorkTags __result)
        {
            if (!(__instance?.GetNaniteTracker()?.Neurorewired ?? false)) return true;
            __result = 0;
            return false;
        }
        
        
        
        
        // ReSharper disable twice InconsistentNaming
        private static void BeforeTryingToGetUnlockedDefs(ResearchProjectDef __instance, ref List<Def> __result)
        {
            //Stop if we've done this already
            if (_correctedProjects.Contains(__instance)) return;
            
            List<Def> cachedUnlockedDefs =
                Traverse.Create(__instance).Field("cachedUnlockedDefs").GetValue() as List<Def>;

            NaniteOperationDef[] operations = DefDatabase<NaniteOperationDef>.AllDefs.Where(def => def.researchPrerequisites.Contains(__instance)).ToArray();
            //add operations to the cachedUnlockedDefs
            // ReSharper disable once PossibleNullReferenceException
            cachedUnlockedDefs.AddRange(operations);
            //Prevent this from running twice
            _correctedProjects.Add(__instance);
        }
        

        // ReSharper disable twice InconsistentNaming
        private static bool CheckDefAbilityByNeurorewire(ref bool __result, SkillRecord __instance)
        {
            NaniteTracker_Pawn tracker = __instance.Pawn?.GetNaniteTracker();
            if (tracker == null) return true;
            if (!tracker.Neurorewired) return true;
            __result = !tracker.EnabledSkills.Contains(__instance.def);
            return false;
        }
        

    }
}