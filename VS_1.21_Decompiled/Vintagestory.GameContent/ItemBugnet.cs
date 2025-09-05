using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class ItemBugnet : Item
{
	public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
	{
		if (api.Side == EnumAppSide.Client)
		{
			Vec3d fromPos = byEntity.Pos.XYZ.Add(byEntity.LocalEyePos);
			float range = 2.5f;
			BlockSelection blockSelection = new BlockSelection();
			EntitySelection entitySelection = new EntitySelection();
			BlockFilter bfilter = (BlockPos pos, Block block) => block == null || block.RenderPass != EnumChunkRenderPass.Meta;
			EntityFilter efilter = delegate(Entity e)
			{
				if (e.Alive)
				{
					if (!e.IsInteractable)
					{
						JsonObject attributes = e.Properties.Attributes;
						if (attributes == null || !attributes["netCaughtItemCode"].Exists)
						{
							goto IL_004a;
						}
					}
					return e.EntityId != byEntity.EntityId;
				}
				goto IL_004a;
				IL_004a:
				return false;
			};
			api.World.RayTraceForSelection(fromPos, byEntity.SidedPos.Pitch, byEntity.SidedPos.Yaw, range, ref blockSelection, ref entitySelection, bfilter, efilter);
			if (entitySelection != null && entitySelection.Entity?.Properties.Attributes?["netCaughtItemCode"].Exists == true)
			{
				(api as ICoreClientAPI).Network.GetChannel("catchcreature").SendPacket(new CatchCreaturePacket
				{
					entityId = entitySelection.Entity.EntityId
				});
			}
		}
		handling = EnumHandHandling.PreventDefaultAction;
	}
}
