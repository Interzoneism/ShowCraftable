using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class ModSystemExplosionAffectedStability : ModSystem
{
	private ICoreServerAPI sapi;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return forSide == EnumAppSide.Server;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		sapi = api;
		api.Event.RegisterEventBusListener(onExplosion, 0.5, "onexplosion");
		api.Event.DidPlaceBlock += OnBlockPlacedEvent;
	}

	private void onExplosion(string eventName, ref EnumHandling handling, IAttribute data)
	{
		ITreeAttribute obj = data as ITreeAttribute;
		BlockPos blockPos = obj.GetBlockPos("pos");
		double num = obj.GetDouble("destructionRadius");
		double num2 = num * num * num;
		int num3 = (int)Math.Round(num) + 1;
		Random rand = sapi.World.Rand;
		BlockPos blockPos2 = new BlockPos();
		while (num2-- > 0.0)
		{
			int num4 = rand.Next(2 * num3) - num3;
			int num5 = rand.Next(2 * num3) - num3;
			int num6 = rand.Next(2 * num3) - num3;
			blockPos2.Set(blockPos.X + num4, blockPos.Y + num5, blockPos.Z + num6);
			sapi.World.BlockAccessor.GetBlock(blockPos2, 1).GetBehavior<BlockBehaviorUnstableRock>()?.CheckCollapsible(sapi.World, blockPos2);
		}
	}

	private void OnBlockPlacedEvent(IServerPlayer byPlayer, int oldblockId, BlockSelection blockSel, ItemStack withItemStack)
	{
		(withItemStack?.Block?.GetBehavior<BlockBehaviorUnstableRock>())?.CheckCollapsible(sapi.World, blockSel.Position);
	}
}
