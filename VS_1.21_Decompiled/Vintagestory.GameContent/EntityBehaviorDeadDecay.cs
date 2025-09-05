using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityBehaviorDeadDecay : EntityBehavior
{
	private ITreeAttribute decayTree;

	private JsonObject typeAttributes;

	public float HoursToDecay { get; set; }

	public double TotalHoursDead
	{
		get
		{
			return decayTree.GetDouble("totalHoursDead");
		}
		set
		{
			decayTree.SetDouble("totalHoursDead", value);
		}
	}

	public EntityBehaviorDeadDecay(Entity entity)
		: base(entity)
	{
	}

	public override void Initialize(EntityProperties properties, JsonObject typeAttributes)
	{
		base.Initialize(properties, typeAttributes);
		(entity as EntityAgent).AllowDespawn = false;
		this.typeAttributes = typeAttributes;
		HoursToDecay = typeAttributes["hoursToDecay"].AsFloat(96f);
		decayTree = entity.WatchedAttributes.GetTreeAttribute("decay");
		if (decayTree == null)
		{
			entity.WatchedAttributes.SetAttribute("decay", decayTree = new TreeAttribute());
			TotalHoursDead = entity.World.Calendar.TotalHours;
		}
	}

	public override void OnGameTick(float deltaTime)
	{
		if (!entity.Alive && TotalHoursDead + (double)HoursToDecay < entity.World.Calendar.TotalHours)
		{
			DecayNow();
		}
		base.OnGameTick(deltaTime);
	}

	public void DecayNow()
	{
		if ((entity as EntityAgent).AllowDespawn)
		{
			return;
		}
		(entity as EntityAgent).AllowDespawn = true;
		if (entity.DespawnReason == null)
		{
			entity.DespawnReason = new EntityDespawnData
			{
				DamageSourceForDeath = null,
				Reason = EnumDespawnReason.Death
			};
		}
		if (typeAttributes["decayedBlock"].Exists)
		{
			AssetLocation blockCode = new AssetLocation(typeAttributes["decayedBlock"].AsString());
			Block block = entity.World.GetBlock(blockCode);
			double num = entity.ServerPos.X + (double)entity.SelectionBox.X1 - (double)entity.OriginSelectionBox.X1;
			double num2 = entity.ServerPos.Y + (double)entity.SelectionBox.Y1 - (double)entity.OriginSelectionBox.Y1;
			double num3 = entity.ServerPos.Z + (double)entity.SelectionBox.Z1 - (double)entity.OriginSelectionBox.Z1;
			BlockPos pos = new BlockPos((int)num, (int)num2, (int)num3);
			IBlockAccessor blockAccessor = entity.World.BlockAccessor;
			if (blockAccessor.GetBlock(pos).IsReplacableBy(block))
			{
				blockAccessor.SetBlock(block.BlockId, pos);
				blockAccessor.MarkBlockDirty(pos);
			}
			else
			{
				BlockFacing[] hORIZONTALS = BlockFacing.HORIZONTALS;
				for (int i = 0; i < hORIZONTALS.Length; i++)
				{
					hORIZONTALS[i].IterateThruFacingOffsets(pos);
					if (entity.World.BlockAccessor.GetBlock(pos).IsReplacableBy(block))
					{
						entity.World.BlockAccessor.SetBlock(block.BlockId, pos);
						break;
					}
				}
			}
		}
		Vec3d vec3d = entity.SidedPos.XYZ + entity.CollisionBox.Center - entity.OriginCollisionBox.Center;
		vec3d.Y += entity.Properties.DeadCollisionBoxSize.Y / 2f;
		entity.World.SpawnParticles(new EntityCubeParticles(entity.World, entity.EntityId, vec3d, 0.15f, (int)(40f + entity.Properties.DeadCollisionBoxSize.X * 60f), 0.4f, 1f));
	}

	public override void OnEntityDeath(DamageSource damageSourceForDeath)
	{
		base.OnEntityDeath(damageSourceForDeath);
		TotalHoursDead = entity.World.Calendar.TotalHours;
		if (damageSourceForDeath != null && damageSourceForDeath.Source == EnumDamageSource.Void)
		{
			(entity as EntityAgent).AllowDespawn = true;
		}
	}

	public override string PropertyName()
	{
		return "deaddecay";
	}
}
