using System;
using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using NanomachineFoundry.Utils;
using RimWorld;
using Verse;

namespace NanomachineFoundry
{
    public class DebugActions
    {
        /*[DebugAction("Nanomachine Foundry", "Undo neurorewire", false, false, false, false, 0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void DebugAction()
        {
            Log.Message("Hello, World!");
        }*/
        
        [DebugAction("Nanomachine Foundry", "Undo neurorewire", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void UndoNeurowire()
        {
            DebugTools.curTool = new DebugTool("Select pawn to undo neurorewire", delegate
            {
                UI.MouseCell().GetFirstPawn(Find.CurrentMap)?.GetNaniteTracker()?.DebugUndoNeurorewire();
            });
        }
        
        [DebugAction("Nanomachine Foundry", "Empty nanites levels", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void EmptyNanites()
        {
            DebugTools.curTool = new DebugTool("Select pawn to empty nanites", delegate
            {
                UI.MouseCell().GetFirstPawn(Find.CurrentMap)?.GetNaniteTracker()?.DebugEmptyNanites();
            });
        }
        
        [DebugAction("Nanomachine Foundry", "Apply nanite operation", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ApplyNaniteOperation()
        {
            List<DebugMenuOption> list = DefDatabase<NaniteOperationDef>.AllDefs.Select(operation => new DebugMenuOption(operation.label, DebugMenuOptionMode.Tool, delegate
                {
                    Pawn pawn = UI.MouseCell().GetFirstPawn(Find.CurrentMap);
                    if (pawn != null)
                    {
                        operation.AdministerToPawn(pawn, null);
                    }
                }))
                .ToList();
            Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
        }
        
        [DebugAction("Nanomachine Foundry", "Grant archites", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void GrantArchites()
        {
            DebugTools.curTool = new DebugTool("Select pawn to grant archites to", delegate
            {
                Pawn pawn = UI.MouseCell().GetFirstPawn(Find.CurrentMap);
                if (pawn?.GetNaniteTracker() != null)
                {
                    List<DebugMenuOption> list = new List<DebugMenuOption>();
                    for (int index = 1; index <= 40; ++index)
                    {
                        var level = index;
                        list.Add(new DebugMenuOption(level.ToString(), DebugMenuOptionMode.Action, delegate
                        {
                            pawn.GetNaniteTracker().GrantArchites(level);
                        }));
                    }
                    Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
                }
            });
        }
        
        [DebugAction("Nanomachine Foundry", "Remove nanite modification", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void RemoveNaniteModification()
        {
            DebugTools.curTool = new DebugTool("Select pawn to remove a modification from", delegate
            {
                Pawn pawn = UI.MouseCell().GetFirstPawn(Find.CurrentMap);
                if (pawn != null)
                {
                    if (pawn.IsMechanized())
                    {
                    
                        List<DebugMenuOption> list = pawn.GetNaniteTracker().AllowedModifications.Select(mod => new DebugMenuOption(mod.label, DebugMenuOptionMode.Action, delegate
                            {
                                pawn.GetNaniteTracker().DisallowModification(mod);
                            }))
                            .ToList();
                        Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
                    }
                }
            });
        }
    }
}