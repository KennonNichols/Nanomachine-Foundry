using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace NanomachineFoundry.Patches
{
    [StaticConstructorOnStartup]
    public static class ApplyHarmonyPatches
    {
        static ApplyHarmonyPatches()
        {
            Harmony harmony = new Harmony("ThatHitmann.NanomachineFoundry");
            
            PawnPatches.ApplyPatches(harmony);
            OperationPatches.ApplyPatches(harmony);
            ModificationPatches.ApplyPatches(harmony);
            MiscPatches.ApplyPatches(harmony);
        }
    }
}
