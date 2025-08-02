using System.Collections.Generic;
using System.Text;
using PipeSystem;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace NanomachineFoundry.NaniteProduction
{
    public class Building_BionaniteExtractor : Building
    {
		private bool initalized;

		private CompFacility facilityComp;

		private CompResource compResource;
		
		private CompPowerTrader powerComp;

		private CompCableConnection cableConnection;

		private Sustainer workingSustainer;

		public CompFacility FacilityComp => facilityComp ??= GetComp<CompFacility>();
		public CompPowerTrader Power => powerComp ??= GetComp<CompPowerTrader>();
		public CompResource Resource => compResource ??= GetComp<CompResource>();
		
		public List<Thing> Platforms => FacilityComp.LinkedBuildings;

		private float BionanitesPerDay
		{
			get
			{
				if (!Power.PowerOn)
				{
					return 0f;
				}
				float num = 0f;
				foreach (Thing platform in Platforms)
				{
					if (platform is Building_HoldingPlatform { Occupied: true } building_HoldingPlatform)
					{
						num += CompProducesBioferrite.BioferritePerDay(building_HoldingPlatform.HeldPawn);
					}
				}
				return num;
			}
		}

		public CompCableConnection CableConnection => cableConnection ??= GetComp<CompCableConnection>();

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			if (!respawningAfterLoad)
			{
				Initialize();
			}
		}

		public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
		{
			base.DeSpawn(mode);
			FacilityComp.OnLinkAdded -= OnLinkAdded;
			FacilityComp.OnLinkRemoved -= OnLinkRemoved;
			initalized = false;
			workingSustainer?.End();
			workingSustainer = null;
		}

		private void Initialize()
		{
			if (initalized)
			{
				return;
			}
			initalized = true;
			FacilityComp.OnLinkAdded += OnLinkAdded;
			FacilityComp.OnLinkRemoved += OnLinkRemoved;
			foreach (Thing platform in Platforms)
			{
				if (platform is Building_HoldingPlatform building_HoldingPlatform)
				{
					building_HoldingPlatform.innerContainer.OnContentsChanged += RebuildCables;
				}
			}
			RebuildCables();
		}

		protected override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			base.DrawAt(drawLoc, flip);
			if (!initalized)
			{
				Initialize();
			}
		}

		private void OnLinkRemoved(CompFacility facility, Thing thing)
		{
			if (thing is Building_HoldingPlatform building_HoldingPlatform)
			{
				building_HoldingPlatform.innerContainer.OnContentsChanged -= RebuildCables;
				RebuildCables();
			}
		}

		private void OnLinkAdded(CompFacility facility, Thing thing)
		{
			if (thing is Building_HoldingPlatform building_HoldingPlatform)
			{
				building_HoldingPlatform.innerContainer.OnContentsChanged += RebuildCables;
				RebuildCables();
			}
		}

		protected override void Tick()
		{
			base.Tick();
			if (this.IsHashIntervalTick(250))
			{
				Resource.PipeNet.DistributeAmongStorage(BionanitesPerDay / 60000f * 250f, out _);
			}
			if (IsWorking())
			{
				if (workingSustainer == null)
				{
					workingSustainer = SoundDefOf.BioferriteHarvester_Ambient.TrySpawnSustainer(SoundInfo.InMap(this));
				}
				workingSustainer.Maintain();
			}
			else
			{
				workingSustainer?.End();
				workingSustainer = null;
			}
		}

		public override bool IsWorking()
		{
			return BionanitesPerDay != 0f;
		}

		public override string GetInspectString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(base.GetInspectString());
			stringBuilder.Append("THNMF.BionanitesPerDay".Translate(BionanitesPerDay));
			return stringBuilder.ToString();
		}

		public override void Notify_DefsHotReloaded()
		{
			base.Notify_DefsHotReloaded();
			RebuildCables();
		}

		private void RebuildCables()
		{
			CableConnection.RebuildCables(Platforms, thing => thing is Building_HoldingPlatform building_HoldingPlatform && building_HoldingPlatform.Occupied);
		}
    }
}