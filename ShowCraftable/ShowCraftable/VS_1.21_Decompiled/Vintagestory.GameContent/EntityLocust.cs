using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityLocust : EntityGlowingAgent
{
	private double mul1;

	private double mul2;

	private bool lightEmitting;

	private int cnt;

	public override byte[] LightHsv
	{
		get
		{
			if (!lightEmitting)
			{
				return null;
			}
			return base.LightHsv;
		}
	}

	public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
	{
		base.Initialize(properties, api, InChunkIndex3d);
		lightEmitting = !Code.Path.Contains("sawblade");
	}

	public override double GetWalkSpeedMultiplier(double groundDragFactor = 0.3)
	{
		double num = (servercontrols.Sneak ? ((double)GlobalConstants.SneakSpeedMultiplier) : 1.0) * (servercontrols.Sprint ? GlobalConstants.SprintSpeedMultiplier : 1.0);
		if (FeetInLiquid)
		{
			num /= 2.5;
		}
		num *= mul1 * mul2;
		return num * (double)GameMath.Clamp(Stats.GetBlended("walkspeed"), 0f, 999f);
	}

	public override void OnGameTick(float dt)
	{
		base.OnGameTick(dt);
		if (cnt++ > 2)
		{
			cnt = 0;
			EntityPos sidedPos = base.SidedPos;
			Block blockRaw = World.BlockAccessor.GetBlockRaw((int)sidedPos.X, (int)(sidedPos.InternalY - 0.05000000074505806), (int)sidedPos.Z);
			Block blockRaw2 = World.BlockAccessor.GetBlockRaw((int)sidedPos.X, (int)(sidedPos.InternalY + 0.009999999776482582), (int)sidedPos.Z);
			mul1 = ((blockRaw.Code == null || blockRaw.Code.Path.Contains("metalspike")) ? 1f : blockRaw.WalkSpeedMultiplier);
			mul2 = ((blockRaw2.Code == null || blockRaw2.Code.Path.Contains("metalspike")) ? 1f : blockRaw2.WalkSpeedMultiplier);
		}
	}

	public override bool ReceiveDamage(DamageSource damageSource, float damage)
	{
		if (damageSource.GetCauseEntity() is EntityEidolon)
		{
			return false;
		}
		return base.ReceiveDamage(damageSource, damage);
	}
}
