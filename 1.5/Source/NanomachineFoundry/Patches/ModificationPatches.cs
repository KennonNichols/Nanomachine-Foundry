using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using NanomachineFoundry.NaniteModifications.ModificationAbilities;
using NanomachineFoundry.NaniteModifications.ModificationWorkers;
using NanomachineFoundry.Utils;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace NanomachineFoundry.Patches
{
    [HarmonyPatch(typeof(Ability), nameof(Ability.Activate), typeof(GlobalTargetInfo))]
    internal static class AbilityActivate1Patch
    {
        [HarmonyReversePatch]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool CallBaseAbility(Ability instance, GlobalTargetInfo globalTargetInfo)
        {
            return instance.Activate(globalTargetInfo);
        }
    }
        
    [HarmonyPatch(typeof(Ability), nameof(Ability.Activate), typeof(LocalTargetInfo), typeof(LocalTargetInfo))]
    internal static class AbilityActivate2Patch
    {
        [HarmonyReversePatch]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool CallBaseAbility(Ability instance, LocalTargetInfo target, LocalTargetInfo dest)
        {
            return instance.Activate(target, dest);
        }
    }
        
    [HarmonyPatch(typeof(Ability), nameof(Ability.CanCast), MethodType.Getter)]
    internal static class AbilityCanCastPatch
    {
        [HarmonyReversePatch]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool CallBaseAbility(Ability instance)
        {
            return instance.CanCast;
        }
    }
        
    [HarmonyPatch(typeof(Command_Ability), "DisabledCheck")]
    internal static class AbilityCommandDisabledCheckPatch
    {
        [HarmonyReversePatch]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CallBaseAbility(Command_Ability instance)
        {
            new Traverse(instance).Method("DisabledCheck").GetValue();
        }
    }
        
    [HarmonyPatch(typeof(Ability), nameof(Ability.GizmoDisabled))]
    internal static class AbilityGizmoDisabledCheckPatch
    {
        [HarmonyReversePatch]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool CallBaseAbility(Ability instance, out string reason)
        {
            return instance.GizmoDisabled(out reason);
        }
    }
        
    [HarmonyPatch(typeof(Verb_CastAbility), nameof(Verb_CastAbility.ValidateTarget))]
    internal static class VerbPsycastCanCastCheckPatch
    {
        [HarmonyReversePatch]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool CallBaseAbility(Verb_CastAbility instance, LocalTargetInfo target, bool showMessages = true)
        {
            Log.Message("Starting base ability call.");
            Log.Message($"Our instance is: {instance}");
            return instance.ValidateTarget(target, showMessages);
        }
    }




    [StaticConstructorOnStartup]
    public class ModificationPatches
    {
        
        private static readonly Texture2D CacheBackgroundIcon = ContentFinder<Texture2D>.Get("UI/CacheBg");
        
        
        private static readonly Color PawnColor = new Color(208, 155, 97);
        
        
        
        //TEMP for determining what throws a message
        /*private static void PostMessageSent(Message msg, bool historical = true)
        {
            Log.Message($"Sent message: {msg.text}");
        }*/
        public static void ApplyPatches(Harmony harmony)
        {
            //TEMP for determining what throws a message
            //harmony.Patch(AccessTools.Method(typeof(Messages), nameof(Messages.Message), new []{typeof(Message), typeof(bool)}),
            //    postfix: new HarmonyMethod(typeof(ModificationPatches), nameof(PostMessageSent)));
         
            
            
            
            
            
            
            
            harmony.PatchAll();
            
            harmony.Patch(AccessTools.Method(typeof(HediffSet), "CalculateBleedRate"),
                postfix: new HarmonyMethod(typeof(ModificationPatches), nameof(PostGetBleedRate)));
            harmony.Patch(AccessTools.Method(typeof(HediffSet), "CalculatePain"),
                postfix: new HarmonyMethod(typeof(ModificationPatches), nameof(PostGetPainAmount)));
            harmony.Patch(AccessTools.Method(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.AddHediff), new[] { typeof(Hediff), typeof(BodyPartRecord), typeof(DamageInfo), typeof(DamageWorker.DamageResult) }),
                prefix: new HarmonyMethod(typeof(ModificationPatches), nameof(OnHediffAdded)));
            harmony.Patch(AccessTools.PropertyGetter(typeof(Need_Comfort), nameof(Need_Comfort.CurInstantLevel)),
                postfix: new HarmonyMethod(typeof(ModificationPatches), nameof(AfterComfortGotten)));
            harmony.Patch(AccessTools.Method(typeof(FloatMenuMakerMap), "AddDraftedOrders"),
                new HarmonyMethod(typeof(ModificationPatches), nameof(PreDraftedOrdersGiven)));
            harmony.Patch(AccessTools.Method(typeof(FloatMenuMakerMap), "AddHumanlikeOrders"),
                new HarmonyMethod(typeof(ModificationPatches), nameof(PreHumanlikeOrdersGiven)));
            harmony.Patch(AccessTools.PropertyGetter(typeof(Pawn_DraftController), nameof(Pawn_DraftController.ShowDraftGizmo)),
                postfix: new HarmonyMethod(typeof(ModificationPatches), nameof(AfterGetShowDraftGizmo)));
            harmony.Patch(AccessTools.Method(typeof(HediffSet), nameof(HediffSet.GetHungerRateFactor)),
                postfix: new HarmonyMethod(typeof(ModificationPatches), nameof(AfterHungerRateGotten)));
            harmony.Patch(AccessTools.Method(typeof(Pawn), "DoKillSideEffects"),
                postfix: new HarmonyMethod(typeof(ModificationPatches), nameof(PostPawnKillSideEffects)));
            harmony.Patch(AccessTools.Method(typeof(ThoughtWorker_PsychicDrone), "CurrentStateInternal"),
                prefix: new HarmonyMethod(typeof(ModificationPatches), nameof(GetDroneState)));
            harmony.Patch(AccessTools.Method(typeof(GameCondition_PsychicSuppression), nameof(GameCondition_PsychicSuppression.CheckPawn)),
                prefix: new HarmonyMethod(typeof(ModificationPatches), nameof(ApplyPsychicSuppressionToPawn)));
            harmony.Patch(AccessTools.Method(typeof(Psycast), nameof(Psycast.CanApplyPsycastTo)),
                prefix: new HarmonyMethod(typeof(ModificationPatches), nameof(CanApplyPsycast)));
            harmony.Patch(AccessTools.Method(typeof(CompDisruptorFlare), "PsychicStun"),
                prefix: new HarmonyMethod(typeof(ModificationPatches), nameof(DisruptorFlareStun)));
            harmony.Patch(AccessTools.Method(typeof(CompRevenant), nameof(CompRevenant.Hypnotize)),
                prefix: new HarmonyMethod(typeof(ModificationPatches), nameof(Hypnotize)));
            harmony.Patch(AccessTools.Method(typeof(CompRevenant), "CheckIfSeen"),
                prefix: new HarmonyMethod(typeof(ModificationPatches), nameof(CheckIfRevenantSeen)));
            harmony.Patch(AccessTools.Method(typeof(Verb), nameof(Verb.TryFindShootLineFromTo)),
                prefix: new HarmonyMethod(typeof(ModificationPatches), nameof(PreFindShootLine)));
            harmony.Patch(AccessTools.Method(typeof(Verb_CastAbility), nameof(Verb_CastAbility.DrawHighlight)),
                postfix: new HarmonyMethod(typeof(ModificationPatches), nameof(PostVerbDrawHighlight)));
            harmony.Patch(AccessTools.Method(typeof(Psycast), nameof(Psycast.Activate), new []{ typeof(GlobalTargetInfo) }),
                prefix: new HarmonyMethod(typeof(ModificationPatches), nameof(PrePsycastActivate1)));
            harmony.Patch(AccessTools.Method(typeof(Psycast), nameof(Psycast.Activate), new []{ typeof(LocalTargetInfo), typeof(LocalTargetInfo) }),
                prefix: new HarmonyMethod(typeof(ModificationPatches), nameof(PrePsycastActivate2)));
            harmony.Patch(AccessTools.Method(typeof(Command_Ability), nameof(Command_Ability.GizmoOnGUI)),
                prefix: new HarmonyMethod(typeof(ModificationPatches), nameof(PreDrawAbilityCommandGizmo)));
            harmony.Patch(AccessTools.Method(typeof(AbilityDef), nameof(AbilityDef.GetTooltip)),
                postfix: new HarmonyMethod(typeof(ModificationPatches), nameof(PreGetAbilityTooltip)));
            harmony.Patch(AccessTools.PropertyGetter(typeof(Psycast), nameof(Psycast.CanCast)),
                prefix: new HarmonyMethod(typeof(ModificationPatches), nameof(PrePsycastCanCast)));
            harmony.Patch(AccessTools.Method(typeof(Command_Psycast), "DisabledCheck"),
                prefix: new HarmonyMethod(typeof(ModificationPatches), nameof(PreCommandPsycastDisabledCheck)));
            harmony.Patch(AccessTools.PropertyGetter(typeof(Command_Psycast), nameof(Command_Psycast.TopRightLabel)),
                prefix: new HarmonyMethod(typeof(ModificationPatches), nameof(PreCommandPsycastTopRightLabel)));
            harmony.Patch(AccessTools.Method(typeof(Psycast), nameof(Psycast.GizmoDisabled)),
                prefix: new HarmonyMethod(typeof(ModificationPatches), nameof(PrePsycastGizmoDisabled)));
            harmony.Patch(AccessTools.Method(typeof(Verb_CastPsycast), nameof(Verb_CastPsycast.ValidateTarget)),
                prefix: new HarmonyMethod(typeof(ModificationPatches), nameof(PreVerbPsycastTargetValidated)));
            harmony.Patch(AccessTools.Method(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.StartJob)),
                prefix: new HarmonyMethod(typeof(ModificationPatches), nameof(PreJobStarted)));
            
            
            harmony.Patch(AccessTools.Method(typeof(Thing), nameof(Thing.TakeDamage)),
                prefix: new HarmonyMethod(typeof(ModificationPatches), nameof(PreDamageTaken)));
        }

        //METALBLOOD: turn sharp damage into blunt
        private static bool PreDamageTaken(Thing __instance, ref DamageInfo dinfo)
        {
            if (!(__instance is Pawn pawn)) return true;
            if (!pawn.IsModificationWorkerEnabled(out ModificationWorker_Metalblood metalblood)) return true;
            if (dinfo.Def.armorCategory != DamageArmorCategoryDefOf.Sharp) return true;
            dinfo.SetAmount(dinfo.Amount * metalblood.DamageMultiplier);
            dinfo.Def = DamageDefOf.Blunt;
            return true;
        }
        
        //PSIONISER: prevent activities while entranced
        private static bool PreJobStarted(
            Pawn_JobTracker __instance,
            Pawn ___pawn,
            Job newJob,
            JobCondition lastJobEndCondition = JobCondition.None,
            ThinkNode jobGiver = null,
            bool resumeCurJobAfterwards = false,
            bool cancelBusyStances = true,
            ThinkTreeDef thinkTree = null,
            JobTag? tag = null,
            bool fromQueue = false,
            bool canReturnCurJobToPool = false,
            bool? keepCarryingThingOverride = null,
            bool continueSleeping = false,
            bool addToJobsThisTick = true,
            bool preToilReservationsCanFail = false)
        {
            //Cancel if entranced
            if (___pawn.IsModificationWorkerEnabled(out ModificationWorker_Psioniser psioniser))
            {
                if (psioniser.Entranced)
                {
                    //Only allow psycasts and the trance toggle abilities to be used
                    if (newJob.def == JobDefOf.CastAbilityOnThing || newJob.def == JobDefOf.CastAbilityOnWorldTile)
                    {
                        Ability ability = newJob.ability;
                        if (ability is Psycast)
                        {
                            return true;
                        }
                        if (ability.EffectComps.Any(effect => effect is CompAbilityEffect_PsioniserToggle))
                        {
                            return true;
                        }
                        Messages.Message("THNMF.CanOnlyCastPsycasts".Translate(), ___pawn, MessageTypeDefOf.RejectInput, false);
                    }
                    return false;
                }
            }
            return true;
        }
        
        



        //PSYCHIC CACHE: just so much shit. All of it. This was so hard :(
        private static bool PreVerbPsycastTargetValidated(ref bool __result, Verb_CastPsycast __instance,
            LocalTargetInfo target, bool showMessages = true)
        {
            if (!IsPsycastCached(__instance.Psycast, out ModificationWorker_PsychicCache psychicCache)) return true;
            if (!VerbPsycastCanCastCheckPatch.CallBaseAbility(__instance, target, showMessages))
            {
                __result = false;
                return false;
            }
            int num1 = __instance.ability.EffectComps.All(e => e.Props.canTargetBosses) ? 1 : 0;
            Pawn pawn = target.Pawn;
            if (num1 == 0 && pawn != null && pawn.kindDef.isBoss)
            {
                Messages.Message("CommandPsycastInsanityImmune".Translate(), __instance.caster, MessageTypeDefOf.RejectInput, false);

                __result = false;
                return false;
            }
            if (__instance.CasterPawn.psychicEntropy.PsychicSensitivity < 1.401298464324817E-45)
            {
                Messages.Message("CommandPsycastZeroPsychicSensitivity".Translate(), __instance.caster, MessageTypeDefOf.RejectInput, false);

                __result = false;
                return false;
            }
            if (__instance.Psycast.def.EntropyGain > 1.401298464324817E-45 && __instance.CasterPawn.psychicEntropy.WouldOverflowEntropy(__instance.Psycast.def.EntropyGain * psychicCache.CachedHeatFactor + PsycastUtility.TotalEntropyFromQueuedPsycasts(__instance.CasterPawn)))
            {
                Log.Message("A");
                Messages.Message("CommandPsycastWouldExceedEntropy".Translate(), __instance.caster, MessageTypeDefOf.RejectInput, false);

                __result = false;
                return false;
            }
                
            __result = true;
            return false;
        }
        private static bool PrePsycastGizmoDisabled(ref bool __result, Psycast __instance, ref string reason)
        {
            reason = "";
            if (!IsPsycastCached(__instance, out ModificationWorker_PsychicCache psychicCache)) return true;
            if (__instance.pawn.psychicEntropy.PsychicSensitivity < 1.401298464324817E-45)
            {
                reason = "CommandPsycastZeroPsychicSensitivity".Translate();
                __result = true;
                return false;
            }
            float num = PsycastUtility.TotalPsyfocusCostOfQueuedPsycasts(__instance.pawn);
            if (__instance.def.level > 0 && __instance.pawn.GetPsylinkLevel() < __instance.def.level)
            {
                reason = "CommandPsycastHigherLevelPsylinkRequired".Translate(__instance.def.level);
                __result = true;
                return false;
            }
            if (__instance.def.level > __instance.pawn.psychicEntropy.MaxAbilityLevel)
            {
                reason = "CommandPsycastLowPsyfocus".Translate((NamedArgument) Pawn_PsychicEntropyTracker.PsyfocusBandPercentages[__instance.def.RequiredPsyfocusBand].ToStringPercent());
                __result = true;
                return false;
            }
            if (__instance.def.EntropyGain <= 1.401298464324817E-45 || !__instance.pawn.psychicEntropy.WouldOverflowEntropy(__instance.def.EntropyGain * psychicCache.CachedHeatFactor + PsycastUtility.TotalEntropyFromQueuedPsycasts(__instance.pawn)))
                return AbilityGizmoDisabledCheckPatch.CallBaseAbility(__instance, out reason);
            Log.Message("B");
            reason = "CommandPsycastWouldExceedEntropy".Translate((NamedArgument) __instance.def.label);
            __result = true;
            return false;

        }
        private static bool PreCommandPsycastDisabledCheck(Command_Psycast __instance, ref string ___disabledReason, ref bool ___disabled)
        {
            if (!IsPsycastCached((Psycast)__instance.Ability, out ModificationWorker_PsychicCache psychicCache)) return true;
            AbilityDef def = __instance.Ability.def;
            Pawn pawn = __instance.Ability.pawn;
            __instance.Disabled = false;
            if (def.EntropyGain > 1.401298464324817E-45)
            {
                if (pawn.GetPsylinkLevel() < def.level)
                {
                    ___disabledReason = "CommandPsycastHigherLevelPsylinkRequired".Translate(def.level);
                    ___disabled = true;
                }
                else if (pawn.psychicEntropy.WouldOverflowEntropy(def.EntropyGain * psychicCache.CachedHeatFactor +
                                                                  PsycastUtility.TotalEntropyFromQueuedPsycasts(
                                                                      pawn)))
                {
                    ___disabledReason = "CommandPsycastWouldExceedEntropy".Translate((NamedArgument) def.label);
                    ___disabled = true;
                }
            }
            AbilityCommandDisabledCheckPatch.CallBaseAbility(__instance);
            return false;

        }
        private static bool PrePsycastCanCast(ref bool __result, Psycast __instance)
        {
            if (!IsPsycastCached(__instance, out ModificationWorker_PsychicCache psychicCache)) return true;
            //If can't cast normally, return
            if (!AbilityCanCastPatch.CallBaseAbility(__instance))
            {
                __result = false;
                return false;
            }
            __result = (__instance.pawn.GetPsylinkLevel() >= __instance.def.level || __instance.def.level <= 0) && !__instance.pawn.psychicEntropy.WouldOverflowEntropy(__instance.def.EntropyGain * psychicCache.CachedHeatFactor);
            return false;
        }
        private static bool PreCommandPsycastTopRightLabel(ref string __result, Command_Psycast __instance)
        {
            if (IsPsycastCached((Psycast)__instance.Ability, out ModificationWorker_PsychicCache psychicCache))
            {
                AbilityDef def = __instance.Ability.def;
                string s = "";
                if (def.EntropyGain > 1.401298464324817E-45)
                    s += ("NeuralHeatLetter".Translate() + ": " + (def.EntropyGain * psychicCache.CachedHeatFactor).ToString("0") + "\n");
                if (def.PsyfocusCost > 1.401298464324817E-45)
                {
                    s += ("PsyfocusLetter".Translate() + ": 0");
                }
                __result = s.TrimEndNewlines();
                return false;
            }

            return true;
        }
        private static void PreGetAbilityTooltip(ref string __result, AbilityDef __instance, Pawn pawn = null)
        {
            if (IsPsycastCacheable(__instance, pawn, out ModificationWorker_PsychicCache psychicCache))
            {
                if (psychicCache.IsCached(__instance))
                {
                    __result += "\n" + psychicCache.GetCacheReport(__instance);
                }   
            }
        }
        private static bool PrePsycastActivate1(ref bool __result, Psycast __instance, GlobalTargetInfo target)
        {
            if (IsPsycastCacheable(__instance, out ModificationWorker_PsychicCache psychicCache))
            {
                //Cast differently when cached
                if (psychicCache.IsCached(__instance.def))
                {
                    //Gain entropy, but reduced by psychic cache
                    if (__instance.def.EntropyGain > 1.401298464324817E-45 &&
                        !__instance.pawn.psychicEntropy.TryAddEntropy(__instance.def.EntropyGain * psychicCache.CachedHeatFactor))
                    {
                        __result = false;
                        return false;
                    }
                    //Cast, but skip the spending of psyfocus
                    psychicCache.DeCache(__instance.def);
                    __result = AbilityActivate1Patch.CallBaseAbility(__instance, target);
                    psychicCache.TryCache(__instance);
                    return false;
                    //Hopefully this doesn't do infinite recursion
                    //(__instance as Ability).Activate(target);
                }
                psychicCache.TryCache(__instance);
            }
            return true;
        }
        private static bool PrePsycastActivate2(ref bool __result, Psycast __instance, LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (IsPsycastCacheable(__instance, out ModificationWorker_PsychicCache psychicCache))
            {
                //Cast differently when cached
                if (psychicCache.IsCached(__instance.def))
                {
                    //Causes less entropy
                    if (!ModLister.RoyaltyInstalled ||
                        __instance.def.EntropyGain > 1.401298464324817E-45 &&
                        !__instance.pawn.psychicEntropy.TryAddEntropy(__instance.def.EntropyGain *
                                                                      psychicCache.CachedHeatFactor))
                    {
                        __result = false;
                        return false;
                    }
                    if (__instance.def.showPsycastEffects)
                    {
                        if (__instance.EffectComps.Any(c => c.Props.psychic))
                        {
                            if (__instance.def.HasAreaOfEffect)
                            {
                                FleckMaker.Static(target.Cell, __instance.pawn.Map, FleckDefOf.PsycastAreaEffect, __instance.def.EffectRadius);
                                SoundDefOf.PsycastPsychicPulse.PlayOneShot(new TargetInfo(target.Cell, __instance.pawn.Map));
                            }
                            else
                                SoundDefOf.PsycastPsychicEffect.PlayOneShot(new TargetInfo(target.Cell, __instance.pawn.Map));
                        }
                        else if (__instance.def.HasAreaOfEffect && __instance.def.canUseAoeToGetTargets)
                            SoundDefOf.Psycast_Skip_Pulse.PlayOneShot(new TargetInfo(target.Cell, __instance.pawn.Map));
                    }
                    
                    psychicCache.DeCache(__instance.def);
                    __result = AbilityActivate2Patch.CallBaseAbility(__instance, target, dest);
                    psychicCache.TryCache(__instance);
                    return false;
                }
                psychicCache.TryCache(__instance);
            }
            return true;
        }
        public static bool PreDrawAbilityCommandGizmo(Command_Ability __instance, Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            if (__instance is Command_Psycast psycast)
            {
                if (IsPsycastCached((Psycast)psycast.Ability, out ModificationWorker_PsychicCache psychicCache))
                {
                    //Draw outline
                    Rect rect1 = new Rect(topLeft.x, topLeft.y, psycast.GetWidth(maxWidth), 75f).ExpandedBy(4f);
                    GUI.DrawTexture(rect1, CacheBackgroundIcon);
                }
            }

            return true;
        }
        private static bool IsPsycastCached(Psycast psycast, out ModificationWorker_PsychicCache psychicCache)
        {
            if (!psycast.pawn.IsModificationWorkerEnabled(out psychicCache)) return false;
            return psychicCache.IsCached(psycast.def);
        }
        private static bool IsPsycastCacheable(Psycast psycast, out ModificationWorker_PsychicCache psychicCache)
        {
            return psycast.pawn.IsModificationWorkerEnabled(out psychicCache);
        }
        private static bool IsPsycastCacheable(AbilityDef abilityDef, Pawn pawn, out ModificationWorker_PsychicCache psychicCache)
        {
            if (!abilityDef.IsPsycast)
            {
                psychicCache = null;
                return false;
            }
            if (pawn == null)
            {
                psychicCache = null;
                return false;
            }
            return pawn.IsModificationWorkerEnabled(out psychicCache);
        }
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        

        
        //CLAIRVOYANCE: can cast through walls at reduced range
        private static bool PreFindShootLine(Verb __instance, ref bool __result, IntVec3 root, LocalTargetInfo targ,
            ref ShootLine resultingLine, bool ignoreRange = false)
        {
            if (!IsVerbClairvoyantCastable(__instance, out ModificationWorker_Clairvoyance clairvoyance)) return true;
            
            if (targ.HasThing && targ.Thing.Map != __instance.caster.Map)
            {
                return true;
            }
            if (__instance.verbProps.IsMeleeAttack || __instance.EffectiveRange <= 1.4199999570846558)
            {
                return true;
            }
            CellRect occupiedRect = targ.HasThing ? targ.Thing.OccupiedRect() : CellRect.SingleCell(targ.Cell);
            if (!ignoreRange && __instance.OutOfRange(root, targ, occupiedRect))
            {
                return true;
            }
            if (__instance.CasterPawn.Position.DistanceTo(targ.Cell) >
                __instance.EffectiveRange * clairvoyance.CastRangeFactor) return true;
            
            //If we are in range
            resultingLine = new ShootLine(__instance.CasterPawn.Position, targ.Cell);
            __result = true;
            return false;

        }
        private static void PostVerbDrawHighlight(Verb __instance, LocalTargetInfo target)
        {
            if (!IsVerbClairvoyantCastable(__instance, out ModificationWorker_Clairvoyance clairvoyance)) return;
            if (Find.CurrentMap == null || __instance.IsMeleeAttack)
                return;
            float radius = __instance.EffectiveRange * clairvoyance.CastRangeFactor;
            if (radius > 0.0 && (double) radius < GenRadial.MaxRadialPatternRadius)
                GenDraw.DrawRadiusRing(__instance.caster.Position, radius, Color.magenta);
        }
        private static bool IsVerbClairvoyantCastable(Verb verb, out ModificationWorker_Clairvoyance clairvoyanceWorker)
        {
            clairvoyanceWorker = null;
            if (!(verb is Verb_CastPsycast)) return false;
            if (!verb.CasterIsPawn) return false;
            if (!verb.verbProps.requireLineOfSight) return false;
            return verb.CasterPawn.IsModificationWorkerEnabled(
                out clairvoyanceWorker);
        }
        //TRUESIGHT: can always see a revenant
        private static bool CheckIfRevenantSeen(CompRevenant __instance)
        {
            Pawn revenant = (Pawn) __instance.parent;
            
            List<Pawn> colonistsSpawned = revenant.Map.mapPawns.FreeColonistsSpawned.Where(pawn => pawn.IsTruesightDeaf()).ToList();

            if (!colonistsSpawned.Any()) return true;

            Traverse lastSeenLetterTickTraverse = Traverse.Create(__instance).Field("lastSeenLetterTick");

            int lastSeenLetterTick = (int)lastSeenLetterTickTraverse.GetValue();
            
            
            foreach (var pawn in colonistsSpawned.Where(pawn => revenant.PositionHeld.InHorDistOf(pawn.PositionHeld, 8.9f) && GenSight.LineOfSightToThing(pawn.PositionHeld, revenant, revenant.Map)))
            {
                if (revenant.IsPsychologicallyInvisible() && Find.TickManager.TicksGame > lastSeenLetterTick + 1200)
                {
                    Find.LetterStack.ReceiveLetter("LetterRevenantSeenLabel".Translate(), "LetterRevenantSeen".Translate(pawn.Named("PAWN")), LetterDefOf.ThreatBig, (LookTargets) (Thing) pawn);
                    lastSeenLetterTickTraverse.SetValue(Find.TickManager.TicksGame);
                }
                __instance.Invisibility.BecomeVisible();
                __instance.becomeInvisibleTick = Find.TickManager.TicksGame + 140;
                return false;
            }
            return true;
        }
        //TRUESIGHT: cannot be hypnotized by a revenant
        private static bool Hypnotize(Pawn victim)
        {
            if (victim.IsTruesightDeaf())
            {
                Find.LetterStack.ReceiveLetter("THNMF.PawnNotHypnotized".Translate(), "THNMF.PawnNotHypnotizedDescription".Translate(victim.Name.ToStringShort.Colorize(PawnColor)), LetterDefOf.NeutralEvent, new LookTargets(victim));
                return false;
            }
            return true;
        }
        //TRUESIGHT: cannot be stunned by disruptor flare
        private static bool DisruptorFlareStun(Pawn pawn)
        {
            return !pawn.IsTruesightDeaf();
        }
        //TRUESIGHT: cannot be targeted by hostile psycasts
        private static bool CanApplyPsycast(Psycast __instance, ref bool __result, LocalTargetInfo target)
        {
            Pawn pawn = target.Pawn;
            if (pawn == null) return true;
            if (!pawn.IsTruesightDeaf()) return true;
            if (!__instance.EffectComps.Any(e => e.Props.psychic))
            {
                __result = true;
                return false;
            }
            
            if (!__instance.def.hostile) { return true;}
            __result = false;
            return false;
        }
        //TRUESIGHT: immune to psychic drone
        private static bool GetDroneState(ref ThoughtState __result, Pawn p)
        {
            if (p.IsTruesightDeaf())
            {
                __result = false;
                return false;
            }
            return true;
        }
        //TRUESIGHT: immune to psychic suppression
        private static bool ApplyPsychicSuppressionToPawn(Pawn pawn, Gender targetGender)
        {
            return !pawn.IsTruesightDeaf();
        }
        //NANITE RESURRECTORS: trigger resurrection hediff on death
        private static void PostPawnKillSideEffects(Pawn __instance, DamageInfo? dinfo, Hediff exactCulprit,
            bool spawned)
        {
            if (__instance.IsModificationWorkerEnabled(out ModificationWorker_ResurrectionParent resurrectionWorker))
            {
                resurrectionWorker.TryGivePawnResurrectionHediff();
            }
        }
        //BIOMANAGER && ALPHA BIOLOGY: reduce hunger rate
        private static void AfterHungerRateGotten(HediffSet __instance, ref float __result, HediffDef ignore = null)
        {
            if (__instance.pawn.IsModificationWorkerEnabled(out ModificationWorker_Biomanager biomanager))
            {
                __result *= biomanager.HungerRate;
            }
            if (__instance.pawn.IsModificationWorkerEnabled(out ModificationWorker_AlphaBiology alphaBio))
            {
                __result *= alphaBio.HungerRate;
            }
        }
        //NANOSURGEONS: adjusts bleed rate
        private static void PostGetBleedRate(HediffSet __instance, ref float __result)
        {
            if (__instance.pawn.IsModificationWorkerEnabled(out ModificationWorker_Nanosurgeons nanosurgeons))
            {
                __result *= nanosurgeons.BleedFactor;
            }
        }
        //INSTINCT INHIBITOR: dulls pain
        private static void PostGetPainAmount(HediffSet __instance, ref float __result)
        {
            if (__instance.pawn.IsModificationWorkerEnabled(out ModificationWorker_InstinctInhibitor inhibitor))
            {
                __result += inhibitor.PainOffset;
            }
        }
        //INSTINCT INHIBITOR: prevents addiction (except in cases where the body cannot gain tolerance)
        private static bool OnHediffAdded(Pawn_HealthTracker __instance, Hediff hediff, BodyPartRecord part = null, DamageInfo? dinfo = null, DamageWorker.DamageResult result = null)
        {
            if (!hediff.def.IsAddiction) return true;
            if (((Pawn)Traverse.Create(__instance).Field("pawn").GetValue()).IsModificationWorkerEnabled(
                    out ModificationWorker_InstinctInhibitor _))
            {
                return ((Hediff_Addiction)hediff).Chemical?.toleranceHediff == null;
            }
            return true;
        }
        //INSTINCT INHIBITOR: minimum baseline comfort
        private static void AfterComfortGotten(Need_Comfort __instance, ref float __result)
        {
            if (((Pawn)Traverse.Create(__instance).Field("pawn").GetValue()).IsModificationWorkerEnabled(
                    out ModificationWorker_InstinctInhibitor inhibitor))
            {
                __result = Mathf.Max(inhibitor.MinimumComfort, __result);
            }
        }       //INSTINCT INHIBITOR: ignore combat orders while they can attack
        private static bool PreHumanlikeOrdersGiven(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
        {
            if (pawn.Drafted) return IsPawnResponsive(pawn, true);
            return true;
        }
        private static bool PreDraftedOrdersGiven(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts, bool suppressAutoTakeableGoto = false)
        {
            return IsPawnResponsive(pawn, true);
        }
        private static void AfterGetShowDraftGizmo(Pawn_DraftController __instance, ref bool __result)
        {
            if (!__instance.pawn.Drafted) return;
            if (!IsPawnResponsive(__instance.pawn))
            {
                __result = false;
            }
        }
        private static bool IsPawnResponsive(Pawn pawn, bool alert = false)
        {
            if (pawn.IsModificationWorkerEnabled(out ModificationWorker_InstinctInhibitor inhibitor))
            {
                if (inhibitor.CanAttackEnemy(alert))
                {
                    if (alert)
                    {
                        Messages.Message(string.Format("THNMF.PawnUnresponsive".Translate(), pawn.Name.ToStringShort), pawn, MessageTypeDefOf.RejectInput);
                    }
                    return false;
                }
            }

            return true;
        }

        
        
        
        
    }
    
    
}