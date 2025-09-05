using System;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class EntityBehaviorDespawn : EntityBehavior, ITimedDespawn
{
	private float minPlayerDistance = -1f;

	private float belowLightLevel = -1f;

	private float minSeconds = 30f;

	private float accumSeconds;

	private float accumOffset = 2.5f;

	private EnumDespawnMode despawnMode;

	private float deathTimeLocal;

	public float DeathTime
	{
		get
		{
			float? num = entity.Attributes.TryGetFloat("deathTime");
			return deathTimeLocal = ((!num.HasValue) ? 0f : num.Value);
		}
		set
		{
			if (value != deathTimeLocal)
			{
				entity.Attributes.SetFloat("deathTime", value);
				deathTimeLocal = value;
			}
		}
	}

	public float DespawnSeconds
	{
		get
		{
			return minSeconds;
		}
		set
		{
			minSeconds = value;
		}
	}

	public EntityBehaviorDespawn(Entity entity)
		: base(entity)
	{
	}

	public override void Initialize(EntityProperties properties, JsonObject typeAttributes)
	{
		JsonObject jsonObject = typeAttributes["minPlayerDistance"];
		minPlayerDistance = (jsonObject.Exists ? jsonObject.AsFloat() : (-1f));
		JsonObject jsonObject2 = typeAttributes["belowLightLevel"];
		belowLightLevel = (jsonObject2.Exists ? jsonObject2.AsFloat() : (-1f));
		int num = entity.Attributes.GetInt("minsecondsToDespawn");
		if (num > 0)
		{
			minSeconds = num;
		}
		else
		{
			minSeconds = typeAttributes["minSeconds"].AsFloat(30f);
			minSeconds += (float)((double)entity.EntityId / 5.0 % (double)(minSeconds / 20f));
		}
		JsonObject jsonObject3 = typeAttributes["afterDays"];
		if (entity.WatchedAttributes.HasAttribute("despawnTotalDays"))
		{
			despawnMode = (jsonObject3.Exists ? EnumDespawnMode.AfterSecondsOrAfterDays : EnumDespawnMode.AfterSecondsOrAfterDaysIgnorePlayer);
		}
		else if (jsonObject3.Exists)
		{
			despawnMode = EnumDespawnMode.AfterSecondsOrAfterDays;
			entity.WatchedAttributes.SetDouble("despawnTotalDays", entity.World.Calendar.TotalDays + (double)jsonObject3.AsFloat(14f));
		}
		accumOffset += (float)((double)entity.EntityId / 200.0 % 1.0);
		deathTimeLocal = DeathTime;
	}

	public override void OnGameTick(float deltaTime)
	{
		if (!entity.Alive || entity.World.Side == EnumAppSide.Client)
		{
			return;
		}
		deltaTime = (float)Math.Min(deltaTime, 0.2);
		if (!((accumSeconds += deltaTime) > accumOffset))
		{
			return;
		}
		if (despawnMode == EnumDespawnMode.AfterSecondsOrAfterDaysIgnorePlayer && entity.World.Calendar.TotalDays > entity.WatchedAttributes.GetDouble("despawnTotalDays"))
		{
			entity.Die(EnumDespawnReason.Expire);
			accumSeconds = 0f;
			return;
		}
		bool flag = PlayerInRange();
		if (flag || LightLevelOk())
		{
			accumSeconds = 0f;
			DeathTime = 0f;
		}
		else if (despawnMode == EnumDespawnMode.AfterSecondsOrAfterDays && !flag && entity.World.Calendar.TotalDays > entity.WatchedAttributes.GetDouble("despawnTotalDays"))
		{
			entity.Die(EnumDespawnReason.Expire);
			accumSeconds = 0f;
		}
		else if ((DeathTime += accumSeconds) > minSeconds)
		{
			entity.Die(EnumDespawnReason.Expire);
			accumSeconds = 0f;
		}
		else
		{
			accumSeconds = 0f;
		}
	}

	public bool PlayerInRange()
	{
		if (minPlayerDistance < 0f)
		{
			return false;
		}
		return entity.NearestPlayerDistance < minPlayerDistance;
	}

	public bool LightLevelOk()
	{
		if (belowLightLevel < 0f)
		{
			return false;
		}
		EntityPos serverPos = entity.ServerPos;
		return (float)entity.World.BlockAccessor.GetLightLevel((int)serverPos.X, (int)serverPos.Y, (int)serverPos.Z, EnumLightLevelType.MaxLight) >= belowLightLevel;
	}

	public override string PropertyName()
	{
		return "timeddespawn";
	}

	public override void GetInfoText(StringBuilder infotext)
	{
		if (belowLightLevel >= 0f && !LightLevelOk() && entity.Alive)
		{
			infotext.AppendLine(Lang.Get("Deprived of light, might die soon"));
		}
		base.GetInfoText(infotext);
	}

	public void SetDespawnByCalendarDate(double totaldays)
	{
		entity.WatchedAttributes.SetDouble("despawnTotalDays", totaldays);
		despawnMode = EnumDespawnMode.AfterSecondsOrAfterDaysIgnorePlayer;
	}
}
