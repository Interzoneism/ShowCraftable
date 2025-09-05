using System;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class EntityBehaviorMultiply : EntityBehaviorMultiplyBase
{
	private JsonObject typeAttributes;

	private long callbackId;

	private AssetLocation[] spawnEntityCodes;

	private bool eatAnyway;

	internal float PregnancyDays => typeAttributes["pregnancyDays"].AsFloat(3f);

	internal string RequiresNearbyEntityCode => typeAttributes["requiresNearbyEntityCode"].AsString("");

	internal float RequiresNearbyEntityRange => typeAttributes["requiresNearbyEntityRange"].AsFloat(5f);

	public float SpawnQuantityMin => typeAttributes["spawnQuantityMin"].AsFloat(1f);

	public float SpawnQuantityMax => typeAttributes["spawnQuantityMax"].AsFloat(2f);

	public double TotalDaysLastBirth
	{
		get
		{
			return multiplyTree.GetDouble("totalDaysLastBirth", -9999.0);
		}
		set
		{
			multiplyTree.SetDouble("totalDaysLastBirth", value);
			entity.WatchedAttributes.MarkPathDirty("multiply");
		}
	}

	public double TotalDaysPregnancyStart
	{
		get
		{
			return multiplyTree.GetDouble("totalDaysPregnancyStart");
		}
		set
		{
			multiplyTree.SetDouble("totalDaysPregnancyStart", value);
			entity.WatchedAttributes.MarkPathDirty("multiply");
		}
	}

	public bool IsPregnant
	{
		get
		{
			return multiplyTree.GetBool("isPregnant");
		}
		set
		{
			multiplyTree.SetBool("isPregnant", value);
			entity.WatchedAttributes.MarkPathDirty("multiply");
		}
	}

	public override bool ShouldEat
	{
		get
		{
			if (!eatAnyway)
			{
				if (!IsPregnant && GetSaturation() < base.PortionsEatenForMultiply)
				{
					return base.TotalDaysCooldownUntil <= entity.World.Calendar.TotalDays;
				}
				return false;
			}
			return true;
		}
	}

	public EntityBehaviorMultiply(Entity entity)
		: base(entity)
	{
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		base.Initialize(properties, attributes);
		typeAttributes = attributes;
		if (entity.World.Side == EnumAppSide.Server)
		{
			if (!multiplyTree.HasAttribute("totalDaysLastBirth"))
			{
				TotalDaysLastBirth = -9999.0;
			}
			callbackId = entity.World.RegisterCallback(CheckMultiply, 3000);
		}
	}

	protected virtual void CheckMultiply(float dt)
	{
		if (!entity.Alive)
		{
			callbackId = 0L;
			return;
		}
		callbackId = entity.World.RegisterCallback(CheckMultiply, 3000);
		if (entity.World.Calendar == null)
		{
			return;
		}
		double totalDays = entity.World.Calendar.TotalDays;
		if (!IsPregnant)
		{
			if (TryGetPregnant())
			{
				IsPregnant = true;
				TotalDaysPregnancyStart = totalDays;
			}
			return;
		}
		if (totalDays - TotalDaysPregnancyStart > (double)PregnancyDays)
		{
			Random rand = entity.World.Rand;
			float q = SpawnQuantityMin + (float)rand.NextDouble() * (SpawnQuantityMax - SpawnQuantityMin);
			TotalDaysLastBirth = totalDays;
			base.TotalDaysCooldownUntil = totalDays + (base.MultiplyCooldownDaysMin + rand.NextDouble() * (base.MultiplyCooldownDaysMax - base.MultiplyCooldownDaysMin));
			IsPregnant = false;
			entity.WatchedAttributes.MarkPathDirty("multiply");
			GiveBirth(q);
		}
		entity.World.FrameProfiler.Mark("multiply");
	}

	protected virtual void GiveBirth(float q)
	{
		Random rand = base.entity.World.Rand;
		int num = base.entity.WatchedAttributes.GetInt("generation");
		if (spawnEntityCodes == null)
		{
			PopulateSpawnEntityCodes();
		}
		if (spawnEntityCodes == null)
		{
			return;
		}
		while (q > 1f || rand.NextDouble() < (double)q)
		{
			q -= 1f;
			AssetLocation entityCode = spawnEntityCodes[rand.Next(spawnEntityCodes.Length)];
			EntityProperties entityType = base.entity.World.GetEntityType(entityCode);
			if (entityType != null)
			{
				Entity entity = base.entity.World.ClassRegistry.CreateEntity(entityType);
				entity.ServerPos.SetFrom(base.entity.ServerPos);
				entity.ServerPos.Motion.X += (rand.NextDouble() - 0.5) / 20.0;
				entity.ServerPos.Motion.Z += (rand.NextDouble() - 0.5) / 20.0;
				entity.Pos.SetFrom(entity.ServerPos);
				entity.Attributes.SetString("origin", "reproduction");
				entity.WatchedAttributes.SetInt("generation", num + 1);
				base.entity.World.SpawnEntity(entity);
			}
		}
	}

	protected virtual void PopulateSpawnEntityCodes()
	{
		JsonObject jsonObject = typeAttributes["spawnEntityCodes"];
		if (!jsonObject.Exists)
		{
			jsonObject = typeAttributes["spawnEntityCode"];
			if (jsonObject.Exists)
			{
				spawnEntityCodes = new AssetLocation[1]
				{
					new AssetLocation(jsonObject.AsString(""))
				};
			}
		}
		else if (jsonObject.IsArray())
		{
			SpawnEntityProperties[] array = jsonObject.AsArray<SpawnEntityProperties>();
			spawnEntityCodes = new AssetLocation[array.Length];
			for (int i = 0; i < array.Length; i++)
			{
				spawnEntityCodes[i] = new AssetLocation(array[i].Code ?? "");
			}
		}
		else
		{
			spawnEntityCodes = new AssetLocation[1]
			{
				new AssetLocation(jsonObject.AsString(""))
			};
		}
	}

	public override void TestCommand(object arg)
	{
		GiveBirth((int)arg);
	}

	protected virtual bool TryGetPregnant()
	{
		if (base.entity.World.Rand.NextDouble() > 0.06)
		{
			return false;
		}
		if (base.TotalDaysCooldownUntil > base.entity.World.Calendar.TotalDays)
		{
			return false;
		}
		ITreeAttribute treeAttribute = base.entity.WatchedAttributes.GetTreeAttribute("hunger");
		if (treeAttribute == null)
		{
			return false;
		}
		float num = treeAttribute.GetFloat("saturation");
		if (num >= base.PortionsEatenForMultiply)
		{
			Entity entity = null;
			if (RequiresNearbyEntityCode != null && (entity = GetRequiredEntityNearby()) == null)
			{
				return false;
			}
			if (base.entity.World.Rand.NextDouble() < 0.2)
			{
				treeAttribute.SetFloat("saturation", num - 1f);
				return false;
			}
			treeAttribute.SetFloat("saturation", num - base.PortionsEatenForMultiply);
			if (entity != null)
			{
				ITreeAttribute treeAttribute2 = entity.WatchedAttributes.GetTreeAttribute("hunger");
				if (treeAttribute2 != null)
				{
					num = treeAttribute2.GetFloat("saturation");
					treeAttribute2.SetFloat("saturation", Math.Max(0f, num - 1f));
				}
			}
			IsPregnant = true;
			TotalDaysPregnancyStart = base.entity.World.Calendar.TotalDays;
			base.entity.WatchedAttributes.MarkPathDirty("multiply");
			return true;
		}
		return false;
	}

	protected virtual Entity GetRequiredEntityNearby()
	{
		if (RequiresNearbyEntityCode == null)
		{
			return null;
		}
		return entity.World.GetNearestEntity(entity.ServerPos.XYZ, RequiresNearbyEntityRange, RequiresNearbyEntityRange, delegate(Entity e)
		{
			if (e.WildCardMatch(new AssetLocation(RequiresNearbyEntityCode)))
			{
				if (e.WatchedAttributes.GetBool("doesEat"))
				{
					ITreeAttribute obj = e.WatchedAttributes["hunger"] as ITreeAttribute;
					if (obj == null || !(obj.GetFloat("saturation") >= 1f))
					{
						goto IL_005c;
					}
				}
				return true;
			}
			goto IL_005c;
			IL_005c:
			return false;
		});
	}

	public override void OnEntityDespawn(EntityDespawnData despawn)
	{
		entity.World.UnregisterCallback(callbackId);
	}

	public override string PropertyName()
	{
		return "multiply";
	}

	public override void GetInfoText(StringBuilder infotext)
	{
		multiplyTree = entity.WatchedAttributes.GetTreeAttribute("multiply");
		if (IsPregnant)
		{
			infotext.AppendLine(Lang.Get("Is pregnant"));
		}
		else
		{
			if (!entity.Alive)
			{
				return;
			}
			ITreeAttribute treeAttribute = entity.WatchedAttributes.GetTreeAttribute("hunger");
			if (treeAttribute != null)
			{
				float num = treeAttribute.GetFloat("saturation");
				infotext.AppendLine(Lang.Get("Portions eaten: {0}", num));
			}
			double num2 = base.TotalDaysCooldownUntil - entity.World.Calendar.TotalDays;
			if (num2 > 0.0)
			{
				if (num2 > 3.0)
				{
					infotext.AppendLine(Lang.Get("Several days left before ready to mate"));
				}
				else
				{
					infotext.AppendLine(Lang.Get("Less than 3 days before ready to mate"));
				}
			}
			else
			{
				infotext.AppendLine(Lang.Get("Ready to mate"));
			}
		}
	}
}
