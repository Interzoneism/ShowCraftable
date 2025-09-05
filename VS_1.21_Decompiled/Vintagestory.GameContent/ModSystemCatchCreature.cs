using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class ModSystemCatchCreature : ModSystem
{
	private ICoreServerAPI sapi;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return true;
	}

	public override void Start(ICoreAPI api)
	{
		api.Network.RegisterChannel("catchcreature").RegisterMessageType<CatchCreaturePacket>();
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		sapi = api;
		api.Network.GetChannel("catchcreature").SetMessageHandler<CatchCreaturePacket>(onCatchCreature);
	}

	private void onCatchCreature(IServerPlayer fromPlayer, CatchCreaturePacket packet)
	{
		Entity entityById = sapi.World.GetEntityById(packet.entityId);
		if (entityById == null || entityById.Pos.DistanceTo(fromPlayer.Entity.Pos.XYZ.Add(fromPlayer.Entity.LocalEyePos)) > (double)fromPlayer.WorldData.PickingRange)
		{
			return;
		}
		JsonObject attributes = entityById.Properties.Attributes;
		if (attributes != null && attributes["netCaughtItemCode"].Exists)
		{
			entityById.Die(EnumDespawnReason.Death, new DamageSource
			{
				Source = EnumDamageSource.Entity,
				SourceEntity = fromPlayer.Entity,
				Type = EnumDamageType.BluntAttack
			});
			ItemStack itemstack = new ItemStack(sapi.World.GetItem(new AssetLocation(entityById.Properties.Attributes["netCaughtItemCode"].AsString())));
			if (!fromPlayer.Entity.TryGiveItemStack(itemstack))
			{
				sapi.World.SpawnItemEntity(itemstack, fromPlayer.Entity.Pos.XYZ);
			}
		}
	}
}
