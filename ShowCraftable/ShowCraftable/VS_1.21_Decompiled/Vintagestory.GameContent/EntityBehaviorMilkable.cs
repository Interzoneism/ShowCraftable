using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class EntityBehaviorMilkable : EntityBehavior
{
	private double lastMilkedTotalHours;

	private float aggroChance;

	private bool aggroTested;

	private bool clientCanContinueMilking;

	private EntityBehaviorMultiply bhmul;

	private float lactatingDaysAfterBirth = 21f;

	private float yieldLitres = 10f;

	private ItemStack liquidStack;

	private long lastIsMilkingStateTotalMs;

	private ILoadedSound milkSound;

	public bool IsBeingMilked => entity.World.ElapsedMilliseconds - lastIsMilkingStateTotalMs < 1000;

	public EntityBehaviorMilkable(Entity entity)
		: base(entity)
	{
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		base.Initialize(properties, attributes);
		liquidStack = new ItemStack(entity.World.GetItem(new AssetLocation("milkportion")));
		lactatingDaysAfterBirth = attributes["lactatingDaysAfterBirth"].AsFloat(21f);
		yieldLitres = attributes["yieldLitres"].AsFloat(10f);
		JsonItemStack jsonItemStack = attributes["liquidStack"].AsObject<JsonItemStack>();
		if (jsonItemStack != null && jsonItemStack.Resolve(entity.World, "milking liquid stack"))
		{
			liquidStack = jsonItemStack.ResolvedItemstack;
		}
	}

	public override string PropertyName()
	{
		return "milkable";
	}

	public override void OnEntityLoaded()
	{
		init();
	}

	public override void OnEntitySpawn()
	{
		init();
	}

	private void init()
	{
		lastMilkedTotalHours = entity.WatchedAttributes.GetFloat("lastMilkedTotalHours");
		if (entity.World.Side != EnumAppSide.Client)
		{
			entity.GetBehavior<EntityBehaviorTaskAI>().TaskManager.OnShouldExecuteTask += (IAiTask task) => !IsBeingMilked;
			bhmul = entity.GetBehavior<EntityBehaviorMultiply>();
			bhmul.TotalDaysLastBirth = Math.Min(bhmul.TotalDaysLastBirth, entity.World.Calendar.TotalDays);
			lastMilkedTotalHours = Math.Min(lastMilkedTotalHours, entity.World.Calendar.TotalHours);
		}
	}

	public bool TryBeginMilking()
	{
		lastIsMilkingStateTotalMs = entity.World.ElapsedMilliseconds;
		bhmul = entity.GetBehavior<EntityBehaviorMultiply>();
		if (!CanMilk())
		{
			return false;
		}
		int num = entity.WatchedAttributes.GetInt("generation");
		aggroChance = Math.Min(1f - (float)num / 3f, 0.95f);
		aggroTested = false;
		clientCanContinueMilking = true;
		if (entity.World.Side == EnumAppSide.Server)
		{
			AiTaskManager taskManager = entity.GetBehavior<EntityBehaviorTaskAI>().TaskManager;
			taskManager.StopTask(typeof(AiTaskWander));
			taskManager.StopTask(typeof(AiTaskSeekEntity));
			taskManager.StopTask(typeof(AiTaskSeekFoodAndEat));
			taskManager.StopTask(typeof(AiTaskStayCloseToEntity));
		}
		else if (entity.World is IClientWorldAccessor clientWorldAccessor)
		{
			milkSound?.Dispose();
			milkSound = clientWorldAccessor.LoadSound(new SoundParams
			{
				DisposeOnFinish = true,
				Location = new AssetLocation("sounds/creature/sheep/milking.ogg"),
				Position = entity.Pos.XYZFloat,
				SoundType = EnumSoundType.Sound
			});
			milkSound.Start();
		}
		return true;
	}

	protected bool CanMilk()
	{
		bhmul = entity.GetBehavior<EntityBehaviorMultiply>();
		if ((double)entity.WatchedAttributes.GetFloat("stressLevel") > 0.1)
		{
			if (entity.World.Api is ICoreClientAPI coreClientAPI)
			{
				coreClientAPI.TriggerIngameError(this, "notready", Lang.Get("Currently too stressed to be milkable"));
			}
			return false;
		}
		double num = Math.Max(0.0, entity.World.Calendar.TotalDays - bhmul.TotalDaysLastBirth);
		if (bhmul != null && num >= (double)lactatingDaysAfterBirth)
		{
			return false;
		}
		if (entity.World.Calendar.TotalHours - lastMilkedTotalHours < (double)entity.World.Calendar.HoursPerDay)
		{
			return false;
		}
		return true;
	}

	public bool CanContinueMilking(IPlayer milkingPlayer, float secondsUsed)
	{
		if (!CanMilk())
		{
			return false;
		}
		lastIsMilkingStateTotalMs = entity.World.ElapsedMilliseconds;
		if (entity.World.Side == EnumAppSide.Client)
		{
			if (!clientCanContinueMilking)
			{
				milkSound?.Stop();
				milkSound?.Dispose();
			}
			else
			{
				milkSound.SetPosition((float)entity.Pos.X, (float)entity.Pos.Y, (float)entity.Pos.Z);
			}
			return clientCanContinueMilking;
		}
		if (secondsUsed > 1f && !aggroTested && entity.World.Side == EnumAppSide.Server)
		{
			aggroTested = true;
			if (entity.World.Rand.NextDouble() < (double)aggroChance)
			{
				entity.GetBehavior<EntityBehaviorEmotionStates>().TryTriggerState("aggressiveondamage", 1L);
				entity.WatchedAttributes.SetFloat("stressLevel", Math.Max(entity.WatchedAttributes.GetFloat("stressLevel"), 0.25f));
				if (entity.Properties.Sounds.ContainsKey("hurt"))
				{
					entity.World.PlaySoundAt(entity.Properties.Sounds["hurt"].Clone().WithPathPrefixOnce("sounds/").WithPathAppendixOnce(".ogg"), entity);
				}
				(entity.Api as ICoreServerAPI).Network.SendEntityPacket(milkingPlayer as IServerPlayer, entity.EntityId, 1337);
				if (entity.World.Api is ICoreClientAPI coreClientAPI)
				{
					coreClientAPI.TriggerIngameError(this, "notready", Lang.Get("Became stressed from the milking attempt. Not milkable while stressed."));
				}
				return false;
			}
		}
		return true;
	}

	public void MilkingComplete(ItemSlot slot, EntityAgent byEntity)
	{
		lastMilkedTotalHours = entity.World.Calendar.TotalHours;
		entity.WatchedAttributes.SetFloat("lastMilkedTotalHours", (float)lastMilkedTotalHours);
		if (!(slot.Itemstack.Collectible is BlockLiquidContainerBase blockLiquidContainerBase))
		{
			return;
		}
		if (entity.World.Side == EnumAppSide.Server)
		{
			ItemStack itemStack = liquidStack.Clone();
			itemStack.StackSize = 999999;
			if (slot.Itemstack.StackSize == 1)
			{
				blockLiquidContainerBase.TryPutLiquid(slot.Itemstack, itemStack, yieldLitres);
			}
			else
			{
				ItemStack itemStack2 = slot.TakeOut(1);
				blockLiquidContainerBase.TryPutLiquid(itemStack2, itemStack, yieldLitres);
				if (!byEntity.TryGiveItemStack(itemStack2))
				{
					byEntity.World.SpawnItemEntity(itemStack2, byEntity.Pos.XYZ.Add(0.0, 0.5, 0.0));
				}
			}
			slot.MarkDirty();
		}
		milkSound?.Stop();
		milkSound?.Dispose();
	}

	public override void OnReceivedServerPacket(int packetid, byte[] data, ref EnumHandling handled)
	{
		if (packetid == 1337)
		{
			clientCanContinueMilking = false;
		}
	}

	public override void GetInfoText(StringBuilder infotext)
	{
		if (!entity.Alive)
		{
			return;
		}
		bhmul = entity.GetBehavior<EntityBehaviorMultiply>();
		double num = (double)lactatingDaysAfterBirth - Math.Max(0.0, entity.World.Calendar.TotalDays - bhmul.TotalDaysLastBirth);
		if (bhmul == null || !(num > 0.0))
		{
			return;
		}
		if (entity.World.Calendar.TotalHours - lastMilkedTotalHours >= (double)entity.World.Calendar.HoursPerDay)
		{
			if ((double)entity.WatchedAttributes.GetFloat("stressLevel") > 0.1)
			{
				infotext.AppendLine(Lang.Get("Lactating for {0} days, currently too stressed to be milkable.", (int)num));
				return;
			}
			int num2 = entity.WatchedAttributes.GetInt("generation");
			if (num2 < 3)
			{
				if (num2 == 0)
				{
					infotext.AppendLine(Lang.Get("Lactating for {0} days, can be milked, but will become aggressive.", (int)num));
				}
				else
				{
					infotext.AppendLine(Lang.Get("Lactating for {0} days, can be milked, but might become aggressive.", (int)num));
				}
			}
			else
			{
				infotext.AppendLine(Lang.Get("Lactating for {0} days, can be milked.", (int)num));
			}
		}
		else
		{
			infotext.AppendLine(Lang.Get("Lactating for {0} days.", (int)num));
		}
	}
}
