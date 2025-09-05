using System;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class EntityBehaviorBodyTemperature : EntityBehavior
{
	private ITreeAttribute tempTree;

	private ICoreAPI api;

	private EntityAgent eagent;

	private float accum;

	private float slowaccum;

	private float veryslowaccum;

	private BlockPos plrpos = new BlockPos();

	private BlockPos tmpPos = new BlockPos();

	private bool inEnclosedRoom;

	private float tempChange;

	private float clothingBonus;

	private float damagingFreezeHours;

	private int sprinterCounter;

	private double lastWearableHoursTotalUpdate;

	private float bodyTemperatureResistance;

	private ICachingBlockAccessor blockAccess;

	public float NormalBodyTemperature;

	private bool firstTick;

	private long lastMoveMs;

	public float CurBodyTemperature
	{
		get
		{
			return tempTree.GetFloat("bodytemp");
		}
		set
		{
			tempTree.SetFloat("bodytemp", value);
			entity.WatchedAttributes.MarkPathDirty("bodyTemp");
		}
	}

	protected float nearHeatSourceStrength
	{
		get
		{
			return tempTree.GetFloat("nearHeatSourceStrength");
		}
		set
		{
			tempTree.SetFloat("nearHeatSourceStrength", value);
		}
	}

	public float Wetness
	{
		get
		{
			return entity.WatchedAttributes.GetFloat("wetness");
		}
		set
		{
			entity.WatchedAttributes.SetFloat("wetness", value);
		}
	}

	public double LastWetnessUpdateTotalHours
	{
		get
		{
			return entity.WatchedAttributes.GetDouble("lastWetnessUpdateTotalHours");
		}
		set
		{
			entity.WatchedAttributes.SetDouble("lastWetnessUpdateTotalHours", value);
		}
	}

	public double BodyTempUpdateTotalHours
	{
		get
		{
			return tempTree.GetDouble("bodyTempUpdateTotalHours");
		}
		set
		{
			tempTree.SetDouble("bodyTempUpdateTotalHours", value);
			entity.WatchedAttributes.MarkPathDirty("bodyTemp");
		}
	}

	public EntityBehaviorBodyTemperature(Entity entity)
		: base(entity)
	{
		eagent = entity as EntityAgent;
	}

	public override void Initialize(EntityProperties properties, JsonObject typeAttributes)
	{
		api = entity.World.Api;
		blockAccess = api.World.GetCachingBlockAccessor(synchronize: false, relight: false);
		tempTree = entity.WatchedAttributes.GetTreeAttribute("bodyTemp");
		NormalBodyTemperature = typeAttributes["defaultBodyTemperature"].AsFloat(37f);
		if (tempTree == null)
		{
			entity.WatchedAttributes.SetAttribute("bodyTemp", tempTree = new TreeAttribute());
			CurBodyTemperature = NormalBodyTemperature + 4f;
			BodyTempUpdateTotalHours = api.World.Calendar.TotalHours;
			LastWetnessUpdateTotalHours = api.World.Calendar.TotalHours;
		}
		else
		{
			BodyTempUpdateTotalHours = api.World.Calendar.TotalHours;
			LastWetnessUpdateTotalHours = api.World.Calendar.TotalHours;
			bodyTemperatureResistance = entity.World.Config.GetString("bodyTemperatureResistance").ToFloat();
		}
	}

	public override void OnEntityDespawn(EntityDespawnData despawn)
	{
		blockAccess?.Dispose();
		blockAccess = null;
	}

	public override void OnGameTick(float deltaTime)
	{
		if (!firstTick && api.Side == EnumAppSide.Client && entity.Properties.Client.Renderer is EntityShapeRenderer entityShapeRenderer)
		{
			entityShapeRenderer.getFrostAlpha = delegate
			{
				float temperature = api.World.BlockAccessor.GetClimateAt(entity.Pos.AsBlockPos, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, api.World.Calendar.TotalDays).Temperature;
				float num8 = GameMath.Clamp((NormalBodyTemperature - CurBodyTemperature) / 4f - 0.5f, 0f, 1f);
				return GameMath.Clamp((Math.Max(0f, 0f - temperature) - 5f) / 5f, 0f, 1f) * num8;
			};
		}
		firstTick = true;
		updateFreezingAnimState();
		accum += deltaTime;
		slowaccum += deltaTime;
		veryslowaccum += deltaTime;
		plrpos.Set((int)entity.Pos.X, (int)entity.Pos.Y, (int)entity.Pos.Z);
		if (veryslowaccum > 10f && damagingFreezeHours > 3f)
		{
			if (api.World.Config.GetString("harshWinters").ToBool(defaultValue: true))
			{
				entity.ReceiveDamage(new DamageSource
				{
					DamageTier = 0,
					Source = EnumDamageSource.Weather,
					Type = EnumDamageType.Frost
				}, 0.2f);
			}
			veryslowaccum = 0f;
			if (eagent.Controls.Sprint)
			{
				sprinterCounter = GameMath.Clamp(sprinterCounter + 1, 0, 10);
			}
			else
			{
				sprinterCounter = GameMath.Clamp(sprinterCounter - 1, 0, 10);
			}
		}
		if (slowaccum > 3f)
		{
			if (api.Side == EnumAppSide.Server)
			{
				Room roomForPosition = api.ModLoader.GetModSystem<RoomRegistry>().GetRoomForPosition(plrpos);
				inEnclosedRoom = roomForPosition.ExitCount == 0 || roomForPosition.SkylightCount < roomForPosition.NonSkylightCount;
				nearHeatSourceStrength = 0f;
				double px = entity.Pos.X;
				double py = entity.Pos.Y + 0.9;
				double pz = entity.Pos.Z;
				double proximityPower = (inEnclosedRoom ? 0.875 : 1.25);
				BlockPos minPos;
				BlockPos maxPos;
				if (inEnclosedRoom && roomForPosition.Location.SizeX >= 1 && roomForPosition.Location.SizeY >= 1 && roomForPosition.Location.SizeZ >= 1)
				{
					minPos = new BlockPos(roomForPosition.Location.MinX, roomForPosition.Location.MinY, roomForPosition.Location.MinZ);
					maxPos = new BlockPos(roomForPosition.Location.MaxX, roomForPosition.Location.MaxY, roomForPosition.Location.MaxZ);
				}
				else
				{
					minPos = plrpos.AddCopy(-3, -3, -3);
					maxPos = plrpos.AddCopy(3, 3, 3);
				}
				blockAccess.Begin();
				blockAccess.WalkBlocks(minPos, maxPos, delegate(Block block, int x, int y, int z)
				{
					IHeatSource heatSource = block.GetInterface<IHeatSource>(api.World, tmpPos.Set(x, y, z));
					if (heatSource != null)
					{
						float num8 = Math.Min(1f, 9f / (8f + (float)Math.Pow(tmpPos.DistanceSqToNearerEdge(px, py, pz), proximityPower)));
						nearHeatSourceStrength += heatSource.GetHeatStrength(api.World, tmpPos, plrpos) * num8;
					}
				});
			}
			updateWearableConditions();
			entity.WatchedAttributes.MarkPathDirty("bodyTemp");
			slowaccum = 0f;
		}
		if (!(accum > 1f) || api.Side != EnumAppSide.Server)
		{
			return;
		}
		EntityPlayer entityPlayer = entity as EntityPlayer;
		IPlayer player = entityPlayer?.Player;
		if (api.Side == EnumAppSide.Server)
		{
			IServerPlayer obj = player as IServerPlayer;
			if (obj == null || obj.ConnectionState != EnumClientState.Playing)
			{
				return;
			}
		}
		if ((player != null && player.WorldData.CurrentGameMode == EnumGameMode.Creative) || (player != null && player.WorldData.CurrentGameMode == EnumGameMode.Spectator))
		{
			CurBodyTemperature = NormalBodyTemperature;
			entity.WatchedAttributes.SetFloat("freezingEffectStrength", 0f);
			return;
		}
		if (player != null && (entityPlayer.Controls.TriesToMove || entityPlayer.Controls.Jump || entityPlayer.Controls.LeftMouseDown || entityPlayer.Controls.RightMouseDown))
		{
			lastMoveMs = entity.World.ElapsedMilliseconds;
		}
		ClimateCondition climateAt = api.World.BlockAccessor.GetClimateAt(plrpos);
		if (climateAt == null)
		{
			return;
		}
		Vec3d windSpeedAt = api.World.BlockAccessor.GetWindSpeedAt(plrpos);
		bool flag = api.World.BlockAccessor.GetRainMapHeightAt(plrpos) <= plrpos.Y;
		float num = climateAt.Rainfall * (flag ? 0.06f : 0f) * ((climateAt.Temperature < -1f) ? 0.05f : 1f);
		if (num > 0f && entityPlayer != null)
		{
			ItemSlot itemSlot = entityPlayer.Player.InventoryManager.GetOwnInventory("character")?.FirstOrDefault((ItemSlot slot) => (slot as ItemSlotCharacter).Type == EnumCharacterDressType.Head);
			if (itemSlot != null && !itemSlot.Empty)
			{
				num *= GameMath.Clamp(1f - itemSlot.Itemstack.ItemAttributes["rainProtectionPerc"].AsFloat(), 0f, 1f);
			}
		}
		Wetness = GameMath.Clamp(Wetness + num + (float)(entity.Swimming ? 1 : 0) - (float)Math.Max(0.0, (api.World.Calendar.TotalHours - LastWetnessUpdateTotalHours) * (double)GameMath.Clamp(nearHeatSourceStrength, 1f, 2f)), 0f, 1f);
		LastWetnessUpdateTotalHours = api.World.Calendar.TotalHours;
		accum = 0f;
		float num2 = (float)sprinterCounter / 2f;
		float num3 = (float)Math.Max(0.0, (double)Wetness - 0.1) * 15f;
		float num4 = climateAt.Temperature + clothingBonus + num2 - num3;
		float num5 = num4 - GameMath.Clamp(num4, bodyTemperatureResistance, 50f);
		if (num5 == 0f)
		{
			num5 = Math.Max(num4 - bodyTemperatureResistance, 0f);
		}
		float num6 = GameMath.Clamp(num5 / 6f, -6f, 6f);
		tempChange = nearHeatSourceStrength + (inEnclosedRoom ? 1f : (0f - (float)Math.Max((windSpeedAt.Length() - 0.15) * 2.0, 0.0) + num6));
		EntityBehaviorTiredness? behavior = entity.GetBehavior<EntityBehaviorTiredness>();
		if (behavior != null && behavior.IsSleeping)
		{
			if (inEnclosedRoom)
			{
				tempChange = GameMath.Clamp(NormalBodyTemperature - CurBodyTemperature, -0.15f, 0.15f);
			}
			else if (!flag)
			{
				tempChange += GameMath.Clamp(NormalBodyTemperature - CurBodyTemperature, 1f, 1f);
			}
		}
		if (entity.IsOnFire)
		{
			tempChange = Math.Max(25f, tempChange);
		}
		float num7 = (float)(api.World.Calendar.TotalHours - BodyTempUpdateTotalHours);
		if (!((double)num7 > 0.01))
		{
			return;
		}
		if ((double)tempChange < -0.5 || tempChange > 0f)
		{
			if ((double)tempChange > 0.5)
			{
				tempChange *= 2f;
			}
			CurBodyTemperature = GameMath.Clamp(CurBodyTemperature + tempChange * num7, 31f, 45f);
		}
		BodyTempUpdateTotalHours = api.World.Calendar.TotalHours;
		float value = GameMath.Clamp((NormalBodyTemperature - CurBodyTemperature) / 4f - 0.5f, 0f, 1f);
		entity.WatchedAttributes.SetFloat("freezingEffectStrength", value);
		if (NormalBodyTemperature - CurBodyTemperature > 4f)
		{
			damagingFreezeHours += num7;
		}
		else
		{
			damagingFreezeHours = 0f;
		}
	}

	private void updateFreezingAnimState()
	{
		float num = entity.WatchedAttributes.GetFloat("freezingEffectStrength");
		bool flag = (entity as EntityAgent)?.LeftHandItemSlot?.Itemstack != null || (entity as EntityAgent)?.RightHandItemSlot?.Itemstack != null;
		EnumGameMode? enumGameMode = (entity as EntityPlayer)?.Player?.WorldData?.CurrentGameMode;
		if ((damagingFreezeHours > 0f || (double)num > 0.4) && enumGameMode != EnumGameMode.Creative && enumGameMode != EnumGameMode.Spectator && entity.Alive)
		{
			if (flag)
			{
				entity.StartAnimation("coldidleheld");
				entity.StopAnimation("coldidle");
			}
			else
			{
				entity.StartAnimation("coldidle");
				entity.StopAnimation("coldidleheld");
			}
		}
		else if (entity.AnimManager.IsAnimationActive("coldidle") || entity.AnimManager.IsAnimationActive("coldidleheld"))
		{
			entity.StopAnimation("coldidle");
			entity.StopAnimation("coldidleheld");
		}
	}

	public void didConsume(ItemStack stack, float intensity = 1f)
	{
		Math.Abs(stack.Collectible.GetTemperature(api.World, stack) - CurBodyTemperature);
		_ = 10f;
	}

	private void updateWearableConditions()
	{
		double num = api.World.Calendar.TotalHours - lastWearableHoursTotalUpdate;
		if (num < -1.0)
		{
			lastWearableHoursTotalUpdate = api.World.Calendar.TotalHours;
		}
		else
		{
			if (num < 0.5)
			{
				return;
			}
			EntityAgent obj = entity as EntityAgent;
			clothingBonus = 0f;
			float changeVal = 0f;
			if (entity.World.ElapsedMilliseconds - lastMoveMs <= 3000)
			{
				changeVal = (0f - (float)num) / 1296f;
			}
			EntityBehaviorPlayerInventory entityBehaviorPlayerInventory = obj?.GetBehavior<EntityBehaviorPlayerInventory>();
			if (entityBehaviorPlayerInventory?.Inventory != null)
			{
				foreach (ItemSlot item in entityBehaviorPlayerInventory.Inventory)
				{
					if (item.Itemstack?.Collectible is ItemWearable { IsArmor: false } itemWearable)
					{
						clothingBonus += itemWearable.GetWarmth(item);
						itemWearable.ChangeCondition(item, changeVal);
					}
				}
			}
			lastWearableHoursTotalUpdate = api.World.Calendar.TotalHours;
		}
	}

	public override void OnEntityRevive()
	{
		BodyTempUpdateTotalHours = api.World.Calendar.TotalHours;
		LastWetnessUpdateTotalHours = api.World.Calendar.TotalHours;
		Wetness = 0f;
		CurBodyTemperature = NormalBodyTemperature + 4f;
	}

	public override string PropertyName()
	{
		return "bodytemperature";
	}
}
