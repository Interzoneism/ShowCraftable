using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class AiTaskIdle : AiTaskBase
{
	public int minduration;

	public int maxduration;

	public float chance;

	public AssetLocation onBlockBelowCode;

	public long idleUntilMs;

	private bool entityWasInRange;

	private long lastEntityInRangeTestTotalMs;

	private string[] stopOnNearbyEntityCodesExact;

	private string[] stopOnNearbyEntityCodesBeginsWith = Array.Empty<string>();

	private string targetEntityFirstLetters = "";

	private float stopRange;

	private bool stopOnHurt;

	private EntityPartitioning partitionUtil;

	private bool stopNow;

	private float tamingGenerations = 10f;

	public AiTaskIdle(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
		: base(entity, taskConfig, aiConfig)
	{
		partitionUtil = entity.Api.ModLoader.GetModSystem<EntityPartitioning>();
		minduration = taskConfig["minduration"].AsInt(2000);
		maxduration = taskConfig["maxduration"].AsInt(4000);
		chance = taskConfig["chance"].AsFloat(1.1f);
		string text = taskConfig["onBlockBelowCode"].AsString();
		tamingGenerations = taskConfig["tamingGenerations"].AsFloat(10f);
		if (text != null && text.Length > 0)
		{
			onBlockBelowCode = new AssetLocation(text);
		}
		stopRange = taskConfig["stopRange"].AsFloat();
		stopOnHurt = taskConfig["stopOnHurt"].AsBool();
		string[] array = taskConfig["stopOnNearbyEntityCodes"].AsArray(new string[1] { "player" });
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		foreach (string text2 in array)
		{
			if (text2.EndsWith('*'))
			{
				list2.Add(text2.Substring(0, text2.Length - 1));
			}
			else
			{
				list.Add(text2);
			}
		}
		stopOnNearbyEntityCodesExact = list.ToArray();
		stopOnNearbyEntityCodesBeginsWith = list2.ToArray();
		string[] array2 = stopOnNearbyEntityCodesExact;
		foreach (string text3 in array2)
		{
			if (text3.Length != 0)
			{
				char c = text3[0];
				if (targetEntityFirstLetters.IndexOf(c) < 0)
				{
					targetEntityFirstLetters += c;
				}
			}
		}
		array2 = stopOnNearbyEntityCodesBeginsWith;
		foreach (string text4 in array2)
		{
			if (text4.Length != 0)
			{
				char c2 = text4[0];
				if (targetEntityFirstLetters.IndexOf(c2) < 0)
				{
					targetEntityFirstLetters += c2;
				}
			}
		}
		if (maxduration < 0)
		{
			idleUntilMs = -1L;
		}
		else
		{
			idleUntilMs = entity.World.ElapsedMilliseconds + minduration + entity.World.Rand.Next(maxduration - minduration);
		}
		int num = entity.WatchedAttributes.GetInt("generation");
		float num2 = Math.Max(0f, (tamingGenerations - (float)num) / tamingGenerations);
		if (WhenInEmotionState != null)
		{
			num2 = 1f;
		}
		stopRange *= num2;
		lastEntityInRangeTestTotalMs = entity.World.ElapsedMilliseconds - entity.World.Rand.Next(1500);
	}

	public override bool ShouldExecute()
	{
		long elapsedMilliseconds = entity.World.ElapsedMilliseconds;
		if (cooldownUntilMs < elapsedMilliseconds && entity.World.Rand.NextDouble() < (double)chance)
		{
			if (entity.Properties.Habitat == EnumHabitat.Land && entity.FeetInLiquid)
			{
				return false;
			}
			if (!PreconditionsSatisifed())
			{
				return false;
			}
			if (elapsedMilliseconds - lastEntityInRangeTestTotalMs > 2000)
			{
				entityWasInRange = entityInRange();
				lastEntityInRangeTestTotalMs = elapsedMilliseconds;
			}
			if (entityWasInRange)
			{
				return false;
			}
			Block blockRaw = entity.World.BlockAccessor.GetBlockRaw((int)entity.ServerPos.X, (int)entity.ServerPos.InternalY - 1, (int)entity.ServerPos.Z, 1);
			if (!blockRaw.SideSolid[BlockFacing.UP.Index])
			{
				return false;
			}
			if (onBlockBelowCode == null)
			{
				return true;
			}
			Block blockRaw2 = entity.World.BlockAccessor.GetBlockRaw((int)entity.ServerPos.X, (int)entity.ServerPos.InternalY, (int)entity.ServerPos.Z);
			if (!blockRaw2.WildCardMatch(onBlockBelowCode))
			{
				if (blockRaw2.Replaceable >= 6000)
				{
					return blockRaw.WildCardMatch(onBlockBelowCode);
				}
				return false;
			}
			return true;
		}
		return false;
	}

	public override void StartExecute()
	{
		base.StartExecute();
		if (maxduration < 0)
		{
			idleUntilMs = -1L;
		}
		else
		{
			idleUntilMs = entity.World.ElapsedMilliseconds + minduration + entity.World.Rand.Next(maxduration - minduration);
		}
		entity.IdleSoundChanceModifier = 0f;
		stopNow = false;
	}

	public override bool ContinueExecute(float dt)
	{
		if (base.rand.NextDouble() < 0.30000001192092896)
		{
			long elapsedMilliseconds = entity.World.ElapsedMilliseconds;
			if (elapsedMilliseconds - lastEntityInRangeTestTotalMs > 1500 && stopOnNearbyEntityCodesExact != null)
			{
				entityWasInRange = entityInRange();
				lastEntityInRangeTestTotalMs = elapsedMilliseconds;
			}
			if (entityWasInRange)
			{
				return false;
			}
			if (!IsInValidDayTimeHours(initialRandomness: false))
			{
				return false;
			}
		}
		if (!stopNow)
		{
			if (idleUntilMs >= 0)
			{
				return entity.World.ElapsedMilliseconds < idleUntilMs;
			}
			return true;
		}
		return false;
	}

	public override void FinishExecute(bool cancelled)
	{
		base.FinishExecute(cancelled);
		entity.IdleSoundChanceModifier = 1f;
	}

	private bool entityInRange()
	{
		if (stopRange <= 0f)
		{
			return false;
		}
		bool found = false;
		partitionUtil.WalkEntities(entity.ServerPos.XYZ, stopRange, delegate(Entity e)
		{
			if (!e.Alive || e.EntityId == entity.EntityId || !e.IsInteractable)
			{
				return true;
			}
			string path = e.Code.Path;
			if (targetEntityFirstLetters.IndexOf(path[0]) < 0)
			{
				return true;
			}
			for (int i = 0; i < stopOnNearbyEntityCodesExact.Length; i++)
			{
				if (path == stopOnNearbyEntityCodesExact[i])
				{
					if (e is EntityPlayer entityPlayer)
					{
						IPlayer player = entity.World.PlayerByUid(entityPlayer.PlayerUID);
						if (player == null || (player.WorldData.CurrentGameMode != EnumGameMode.Creative && player.WorldData.CurrentGameMode != EnumGameMode.Spectator))
						{
							found = true;
							return false;
						}
						return false;
					}
					found = true;
					return false;
				}
			}
			for (int j = 0; j < stopOnNearbyEntityCodesBeginsWith.Length; j++)
			{
				if (path.StartsWithFast(stopOnNearbyEntityCodesBeginsWith[j]))
				{
					found = true;
					return false;
				}
			}
			return true;
		}, EnumEntitySearchType.Creatures);
		return found;
	}

	public override void OnEntityHurt(DamageSource source, float damage)
	{
		if (stopOnHurt)
		{
			stopNow = true;
		}
	}
}
