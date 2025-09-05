using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityBehaviorBreathe : EntityBehavior
{
	private ITreeAttribute oxygenTree;

	private float oxygenCached = -1f;

	private float maxOxygen;

	private Cuboidd tmp = new Cuboidd();

	private float breathAccum;

	private float padding = 0.1f;

	private Block suffocationSourceBlock;

	private float damageAccum;

	public float Oxygen
	{
		get
		{
			return oxygenCached = oxygenTree.GetFloat("currentoxygen");
		}
		set
		{
			if (value != oxygenCached)
			{
				oxygenCached = value;
				oxygenTree.SetFloat("currentoxygen", value);
				entity.WatchedAttributes.MarkPathDirty("oxygen");
			}
		}
	}

	public float MaxOxygen
	{
		get
		{
			return maxOxygen;
		}
		set
		{
			maxOxygen = value;
			oxygenTree.SetFloat("maxoxygen", value);
			entity.WatchedAttributes.MarkPathDirty("oxygen");
		}
	}

	public bool HasAir
	{
		get
		{
			return oxygenTree.GetBool("hasair");
		}
		set
		{
			if (oxygenTree.GetBool("hasair") != value)
			{
				oxygenTree.SetBool("hasair", value);
				entity.WatchedAttributes.MarkPathDirty("oxygen");
			}
		}
	}

	public EntityBehaviorBreathe(Entity entity)
		: base(entity)
	{
	}

	public override void Initialize(EntityProperties properties, JsonObject typeAttributes)
	{
		base.Initialize(properties, typeAttributes);
		oxygenTree = entity.WatchedAttributes.GetTreeAttribute("oxygen");
		if (oxygenTree == null)
		{
			entity.WatchedAttributes.SetAttribute("oxygen", oxygenTree = new TreeAttribute());
			float defaultValue = 40000f;
			if (entity is EntityPlayer)
			{
				defaultValue = entity.World.Config.GetAsInt("lungCapacity", 40000);
			}
			MaxOxygen = typeAttributes["maxoxygen"].AsFloat(defaultValue);
			Oxygen = typeAttributes["currentoxygen"].AsFloat(MaxOxygen);
			HasAir = true;
		}
		else
		{
			maxOxygen = oxygenTree.GetFloat("maxoxygen");
		}
		breathAccum = (float)entity.World.Rand.NextDouble();
	}

	public override void OnEntityRevive()
	{
		Oxygen = MaxOxygen;
	}

	public void Check()
	{
		maxOxygen = oxygenTree.GetFloat("maxoxygen");
		if (entity.World.Side == EnumAppSide.Client)
		{
			return;
		}
		bool hasAir = true;
		if (entity is EntityPlayer)
		{
			EntityPlayer entityPlayer = (EntityPlayer)entity;
			EnumGameMode currentGameMode = entity.World.PlayerByUid(entityPlayer.PlayerUID).WorldData.CurrentGameMode;
			if (currentGameMode == EnumGameMode.Creative || currentGameMode == EnumGameMode.Spectator)
			{
				HasAir = true;
				return;
			}
		}
		EntityPos sidedPos = entity.SidedPos;
		double num = (entity.Swimming ? entity.Properties.SwimmingEyeHeight : entity.Properties.EyeHeight);
		double num2 = (sidedPos.Y + num) % 1.0;
		BlockPos blockPos = new BlockPos((int)(sidedPos.X + entity.LocalEyePos.X), (int)(sidedPos.Y + num), (int)(sidedPos.Z + entity.LocalEyePos.Z), sidedPos.Dimension);
		Block block = entity.World.BlockAccessor.GetBlock(blockPos, 3);
		JsonObject attributes = block.Attributes;
		if (attributes == null || attributes["asphyxiating"].AsBool(defaultValue: true))
		{
			Cuboidf[] collisionBoxes = block.GetCollisionBoxes(entity.World.BlockAccessor, blockPos);
			Cuboidf cuboidf = new Cuboidf();
			if (collisionBoxes != null)
			{
				for (int i = 0; i < collisionBoxes.Length; i++)
				{
					cuboidf.Set(collisionBoxes[i]);
					cuboidf.OmniGrowBy(0f - padding);
					tmp.Set((float)blockPos.X + cuboidf.X1, (float)blockPos.Y + cuboidf.Y1, (float)blockPos.Z + cuboidf.Z1, (float)blockPos.X + cuboidf.X2, (float)blockPos.Y + cuboidf.Y2, (float)blockPos.Z + cuboidf.Z2);
					cuboidf.OmniGrowBy(padding);
					if (tmp.Contains(sidedPos.X + entity.LocalEyePos.X, sidedPos.Y + entity.LocalEyePos.Y, sidedPos.Z + entity.LocalEyePos.Z))
					{
						Cuboidd other = entity.SelectionBox.ToDouble();
						if (tmp.Intersects(other))
						{
							hasAir = false;
							suffocationSourceBlock = block;
							break;
						}
					}
				}
			}
		}
		if (block.IsLiquid() && (double)((float)block.LiquidLevel / 7f) > num2)
		{
			hasAir = false;
		}
		HasAir = hasAir;
	}

	public override void OnGameTick(float deltaTime)
	{
		if (entity.State == EnumEntityState.Inactive)
		{
			return;
		}
		if (!HasAir)
		{
			float num = (Oxygen = Math.Max(0f, Oxygen - deltaTime * 1000f));
			if (num <= 0f && entity.World.Side == EnumAppSide.Server)
			{
				damageAccum += deltaTime;
				if ((double)damageAccum > 0.75)
				{
					damageAccum = 0f;
					DamageSource damageSource = new DamageSource
					{
						Source = EnumDamageSource.Block,
						SourceBlock = suffocationSourceBlock,
						Type = EnumDamageType.Suffocation
					};
					entity.ReceiveDamage(damageSource, 0.5f);
				}
			}
		}
		else if (Oxygen < maxOxygen)
		{
			Oxygen = Math.Min(maxOxygen, Oxygen + deltaTime * 10000f);
		}
		base.OnGameTick(deltaTime);
		breathAccum += deltaTime;
		if (breathAccum > 1f)
		{
			breathAccum = 0f;
			Check();
		}
	}

	public override string PropertyName()
	{
		return "breathe";
	}
}
