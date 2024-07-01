using System;
using System.Collections.Generic;
using NanomachineFoundry.NaniteModifications.ModificationWorkers;
using RimWorld;
using Verse;
using Verse.Grammar;

namespace NanomachineFoundry.NaniteModifications
{
    public abstract class ModificationWorkerAbility: ModificationWorker
    {
        protected abstract int TicksCooldown();
        protected abstract Type GetAbilityType();
        private ModificationAbility _ability;
        private ModificationAbility Ability => _ability ??= (ModificationAbility)Activator.CreateInstance(GetAbilityType(), this);
        private int currentCooldown;

        public abstract Ability GetAbility();
        
        //private static readonly Texture2D GrayoutTex = SolidColorMaterials.NewSolidColorTexture(new Color32(9, 203, 4, 64));
        protected ModificationWorkerAbility(NaniteModificationDef def, Pawn pawn) : base(def, pawn)
        {
            
        }

        public bool AbilityReady => currentCooldown <= 0;

        protected abstract string GetReport();
        
        public override void Tick()
        {
            base.Tick();
            if (currentCooldown > 0)
            {
                currentCooldown--;
            }
        }

        public void AddGizmo(ref IEnumerable<Gizmo> gizmos)
        {
            Command_Action gizmo = new Command_Action
            {
                defaultLabel = def.label.CapitalizeFirst(),
                action = Ability.Use,
                tutorTag = GetReport(),
                icon = def.getIcon()
            };
        }

        public void ResetCooldown()
        {
            currentCooldown = TicksCooldown();
        }
    }
}