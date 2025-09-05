using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityBehaviorGrow : EntityBehavior
{
	private ITreeAttribute growTree;

	private long callbackId;

	public AssetLocation[] AdultEntityCodes;

	public AssetLocation[] FedAdultEntityCodes;

	public float HoursToGrow { get; set; }

	public float OrPortionsEatenForGrowing { get; set; }

	protected double SpawnedTotalHours
	{
		get
		{
			return growTree.GetDouble("timeSpawned");
		}
		set
		{
			growTree.SetDouble("timeSpawned", value);
		}
	}

	public EntityBehaviorGrow(Entity entity)
		: base(entity)
	{
	}

	public override void Initialize(EntityProperties properties, JsonObject typeAttributes)
	{
		base.Initialize(properties, typeAttributes);
		AdultEntityCodes = AssetLocation.toLocations(typeAttributes["adultEntityCodes"].AsArray(Array.Empty<string>()));
		FedAdultEntityCodes = AssetLocation.toLocations(typeAttributes["fedAdultEntityCodes"].AsArray(Array.Empty<string>()));
		HoursToGrow = typeAttributes["hoursToGrow"].AsFloat(96f);
		OrPortionsEatenForGrowing = typeAttributes["orPortionsEatenForGrowing"].AsFloat(12f);
		growTree = entity.WatchedAttributes.GetTreeAttribute("grow");
		if (growTree == null && entity.Api.Side == EnumAppSide.Server)
		{
			entity.WatchedAttributes.SetAttribute("grow", growTree = new TreeAttribute());
			SpawnedTotalHours = entity.World.Calendar.TotalHours;
		}
		if (FedAdultEntityCodes.Length != 0)
		{
			double? num2;
			double? num = (num2 = entity.Attributes.TryGetDouble("totalDaysReleased"));
			if (num.HasValue)
			{
				double num3 = 216f * (OrPortionsEatenForGrowing - (entity.WatchedAttributes.GetTreeAttribute("hunger")?.TryGetFloat("saturation")).GetValueOrDefault());
				double num4 = num2.Value * (double)entity.World.Calendar.HoursPerDay + num3 - (double)HoursToGrow;
				if (SpawnedTotalHours < num4)
				{
					SpawnedTotalHours = num4;
				}
			}
		}
		callbackId = entity.World.RegisterCallback(CheckGrowth, 3000);
	}

	private void CheckGrowth(float dt)
	{
		callbackId = 0L;
		if (!base.entity.Alive)
		{
			return;
		}
		ITreeAttribute treeAttribute = base.entity.WatchedAttributes.GetTreeAttribute("hunger");
		bool flag = FedAdultEntityCodes.Length != 0 && treeAttribute != null && treeAttribute.GetFloat("saturation") >= OrPortionsEatenForGrowing;
		if (base.entity.World.Calendar.TotalHours >= SpawnedTotalHours + (double)HoursToGrow || flag)
		{
			AssetLocation[] array = (flag ? FedAdultEntityCodes : AdultEntityCodes);
			if (array.Length == 0)
			{
				return;
			}
			AssetLocation assetLocation = array[base.entity.World.Rand.Next(array.Length)];
			EntityProperties entityType = base.entity.World.GetEntityType(assetLocation);
			if (entityType == null)
			{
				base.entity.World.Logger.Error("Misconfigured entity. Entity with code '{0}' is configured (via Grow behavior) to grow into '{1}', but no such entity type was registered.", base.entity.Code, assetLocation);
				return;
			}
			Cuboidf spawnCollisionBox = entityType.SpawnCollisionBox;
			if (base.entity.World.CollisionTester.IsColliding(base.entity.World.BlockAccessor, spawnCollisionBox, base.entity.ServerPos.XYZ, alsoCheckTouch: false))
			{
				callbackId = base.entity.World.RegisterCallback(CheckGrowth, 3000);
				return;
			}
			Entity entity = base.entity.World.ClassRegistry.CreateEntity(entityType);
			entity.ServerPos.SetFrom(base.entity.ServerPos);
			entity.Pos.SetFrom(entity.ServerPos);
			bool keepTextureIndex = base.entity.Properties.Client != null && base.entity.Properties.Client.TexturesAlternatesCount > 0 && entityType.Client != null && base.entity.Properties.Client.TexturesAlternatesCount == entityType.Client.TexturesAlternatesCount;
			entity.Attributes.SetBool("wasFedToAdulthood", flag);
			BecomeAdult(entity, keepTextureIndex);
		}
		else
		{
			callbackId = base.entity.World.RegisterCallback(CheckGrowth, 3000);
			double num = base.entity.World.Calendar.TotalHours - SpawnedTotalHours;
			if (num >= 0.1 * (double)HoursToGrow)
			{
				float num2 = (float)(num / (double)HoursToGrow - 0.1);
				if (num2 >= 1.01f * growTree.GetFloat("age"))
				{
					growTree.SetFloat("age", num2);
					base.entity.WatchedAttributes.MarkPathDirty("grow");
				}
			}
		}
		base.entity.World.FrameProfiler.Mark("checkgrowth");
	}

	protected virtual void BecomeAdult(Entity adult, bool keepTextureIndex)
	{
		adult.WatchedAttributes.SetInt("generation", entity.WatchedAttributes.GetInt("generation"));
		adult.WatchedAttributes.SetDouble("birthTotalDays", entity.World.Calendar.TotalDays);
		if (keepTextureIndex && entity.WatchedAttributes.HasAttribute("textureIndex"))
		{
			adult.WatchedAttributes.SetAttribute("textureIndex", entity.WatchedAttributes.GetAttribute("textureIndex"));
		}
		entity.Die(EnumDespawnReason.Expire);
		entity.World.SpawnEntity(adult);
	}

	public override void OnEntityDespawn(EntityDespawnData despawn)
	{
		entity.World.UnregisterCallback(callbackId);
	}

	public override string PropertyName()
	{
		return "grow";
	}
}
