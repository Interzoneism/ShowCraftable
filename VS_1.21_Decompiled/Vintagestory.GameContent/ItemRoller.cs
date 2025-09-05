using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemRoller : Item
{
	public static List<BlockPos> emptyList = new List<BlockPos>();

	public static List<List<BlockPos>> siteListByFacing = new List<List<BlockPos>>();

	public static List<List<BlockPos>> waterEdgeByFacing = new List<List<BlockPos>>();

	public static List<BlockPos> siteListN = new List<BlockPos>
	{
		new BlockPos(-5, -1, -2),
		new BlockPos(3, 2, 2)
	};

	public static List<BlockPos> waterEdgeListN = new List<BlockPos>
	{
		new BlockPos(3, -1, -2),
		new BlockPos(6, 0, 2)
	};

	public SkillItem[] skillItems;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		siteListByFacing.Add(siteListN);
		waterEdgeByFacing.Add(waterEdgeListN);
		for (int i = 1; i < 4; i++)
		{
			siteListByFacing.Add(rotateList(siteListN, i));
			waterEdgeByFacing.Add(rotateList(waterEdgeListN, i));
		}
		skillItems = new SkillItem[4]
		{
			new SkillItem
			{
				Code = new AssetLocation("east"),
				Name = Lang.Get("facing-east")
			},
			new SkillItem
			{
				Code = new AssetLocation("north"),
				Name = Lang.Get("facing-north")
			},
			new SkillItem
			{
				Code = new AssetLocation("west"),
				Name = Lang.Get("facing-west")
			},
			new SkillItem
			{
				Code = new AssetLocation("south"),
				Name = Lang.Get("facing-south")
			}
		};
		if (api is ICoreClientAPI coreClientAPI)
		{
			skillItems[0].WithIcon(coreClientAPI, coreClientAPI.Gui.LoadSvgWithPadding(new AssetLocation("textures/icons/pointeast.svg"), 48, 48, 5, -1));
			skillItems[1].WithIcon(coreClientAPI, coreClientAPI.Gui.LoadSvgWithPadding(new AssetLocation("textures/icons/pointnorth.svg"), 48, 48, 5, -1));
			skillItems[2].WithIcon(coreClientAPI, coreClientAPI.Gui.LoadSvgWithPadding(new AssetLocation("textures/icons/pointwest.svg"), 48, 48, 5, -1));
			skillItems[3].WithIcon(coreClientAPI, coreClientAPI.Gui.LoadSvgWithPadding(new AssetLocation("textures/icons/pointsouth.svg"), 48, 48, 5, -1));
		}
	}

	public override void OnUnloaded(ICoreAPI api)
	{
		if (skillItems != null)
		{
			SkillItem[] array = skillItems;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Dispose();
			}
		}
	}

	private static List<BlockPos> rotateList(List<BlockPos> startlist, int i)
	{
		Matrixf matrixf = new Matrixf();
		matrixf.RotateY((float)i * ((float)Math.PI / 2f));
		if (i == 2)
		{
			matrixf.Translate(0f, 0f, -1f);
		}
		if (i == 3)
		{
			matrixf.Translate(1f, 0f, -1f);
		}
		List<BlockPos> list = new List<BlockPos>();
		Vec4f vec4f = matrixf.TransformVector(new Vec4f(startlist[0].X, startlist[0].Y, startlist[0].Z, 1f));
		Vec4f vec4f2 = matrixf.TransformVector(new Vec4f(startlist[1].X, startlist[1].Y, startlist[1].Z, 1f));
		BlockPos item = new BlockPos((int)Math.Round(Math.Min(vec4f.X, vec4f2.X)), (int)Math.Round(Math.Min(vec4f.Y, vec4f2.Y)), (int)Math.Round(Math.Min(vec4f.Z, vec4f2.Z)));
		BlockPos item2 = new BlockPos((int)Math.Round(Math.Max(vec4f.X, vec4f2.X)), (int)Math.Round(Math.Max(vec4f.Y, vec4f2.Y)), (int)Math.Round(Math.Max(vec4f.Z, vec4f2.Z)));
		list.Add(item);
		list.Add(item2);
		return list;
	}

	public override int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection)
	{
		return GetOrient(byPlayer);
	}

	public static int GetOrient(IPlayer byPlayer)
	{
		siteListN = new List<BlockPos>
		{
			new BlockPos(-5, -1, -1),
			new BlockPos(3, 2, 2)
		};
		waterEdgeListN = new List<BlockPos>
		{
			new BlockPos(3, -1, -1),
			new BlockPos(6, 0, 2)
		};
		siteListByFacing.Clear();
		waterEdgeByFacing.Clear();
		siteListByFacing.Add(siteListN);
		waterEdgeByFacing.Add(waterEdgeListN);
		for (int i = 1; i < 4; i++)
		{
			siteListByFacing.Add(rotateList(siteListN, i));
			waterEdgeByFacing.Add(rotateList(waterEdgeListN, i));
		}
		return ObjectCacheUtil.GetOrCreate(byPlayer.Entity.Api, "rollerOrient-" + byPlayer.PlayerUID, () => 0);
	}

	public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel)
	{
		return skillItems;
	}

	public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int toolMode)
	{
		api.ObjectCache["rollerOrient-" + byPlayer.PlayerUID] = toolMode;
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
	{
		if (blockSel == null)
		{
			return;
		}
		IPlayer player = (byEntity as EntityPlayer)?.Player;
		if (slot.StackSize < 5)
		{
			(api as ICoreClientAPI)?.TriggerIngameError(this, "need5", Lang.Get("Need 5 rolles to place a boat construction site"));
			return;
		}
		if (!suitableLocation(player, blockSel))
		{
			(api as ICoreClientAPI)?.TriggerIngameError(this, "unsuitableLocation", Lang.Get("Requires a suitable location near water to place a boat construction site. Boat will roll towards the blue highlighted area. Use tool mode to rotate"));
			return;
		}
		slot.TakeOut(5);
		slot.MarkDirty();
		string text = "oak";
		int orient = GetOrient(player);
		EntityProperties entityType = byEntity.World.GetEntityType(new AssetLocation("boatconstruction-sailed-" + text));
		Entity entity = byEntity.World.ClassRegistry.CreateEntity(entityType);
		entity.ServerPos.SetPos(blockSel.Position.ToVec3d().AddCopy(0.5, 1.0, 0.5));
		entity.ServerPos.Yaw = -(float)Math.PI / 2f + (float)orient * ((float)Math.PI / 2f);
		if (orient == 1)
		{
			entity.ServerPos.Z -= 1.0;
		}
		if (orient == 2)
		{
			entity.ServerPos.X -= 1.0;
		}
		if (orient == 3)
		{
			entity.ServerPos.Z += 1.0;
		}
		entity.Pos.SetFrom(entity.ServerPos);
		byEntity.World.SpawnEntity(entity);
		api.World.PlaySoundAt(new AssetLocation("sounds/block/planks"), byEntity, player);
		handling = EnumHandHandling.PreventDefault;
	}

	private bool suitableLocation(IPlayer forPlayer, BlockSelection blockSel)
	{
		int orient = GetOrient(forPlayer);
		List<BlockPos> list = siteListByFacing[orient];
		List<BlockPos> list2 = waterEdgeByFacing[orient];
		IBlockAccessor ba = api.World.BlockAccessor;
		bool placeable = true;
		BlockPos position = blockSel.Position;
		BlockPos blockPos = list[0].AddCopy(0, 1, 0).Add(position);
		BlockPos blockPos2 = list[1].AddCopy(-1, 0, -1).Add(position);
		blockPos2.Y = blockPos.Y;
		api.World.BlockAccessor.WalkBlocks(blockPos, blockPos2, delegate(Block block, int x, int y, int z)
		{
			if (!block.SideIsSolid(new BlockPos(x, y, z), BlockFacing.UP.Index))
			{
				placeable = false;
			}
		});
		if (!placeable)
		{
			return false;
		}
		BlockPos minPos = list[0].AddCopy(0, 2, 0).Add(position);
		BlockPos maxPos = list[1].AddCopy(-1, 1, -1).Add(position);
		api.World.BlockAccessor.WalkBlocks(minPos, maxPos, delegate(Block block, int x, int y, int z)
		{
			Cuboidf[] collisionBoxes = block.GetCollisionBoxes(ba, new BlockPos(x, y, z));
			if (collisionBoxes != null && collisionBoxes.Length != 0)
			{
				placeable = false;
			}
		});
		BlockPos minPos2 = list2[0].AddCopy(0, 1, 0).Add(position);
		BlockPos maxPos2 = list2[1].AddCopy(-1, 0, -1).Add(position);
		WalkBlocks(minPos2, maxPos2, delegate(Block block, int x, int y, int z)
		{
			if (!block.IsLiquid())
			{
				placeable = false;
			}
		}, 2);
		return placeable;
	}

	public void WalkBlocks(BlockPos minPos, BlockPos maxPos, Action<Block, int, int, int> onBlock, int layer)
	{
		IBlockAccessor blockAccessor = api.World.BlockAccessor;
		int x = minPos.X;
		int internalY = minPos.InternalY;
		int z = minPos.Z;
		int x2 = maxPos.X;
		int internalY2 = maxPos.InternalY;
		int z2 = maxPos.Z;
		for (int i = x; i <= x2; i++)
		{
			for (int j = internalY; j <= internalY2; j++)
			{
				for (int k = z; k <= z2; k++)
				{
					Block block = blockAccessor.GetBlock(i, j, k);
					onBlock(block, i, j, k);
				}
			}
		}
	}
}
