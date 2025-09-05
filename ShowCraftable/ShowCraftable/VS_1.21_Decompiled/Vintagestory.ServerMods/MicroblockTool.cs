using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.ServerMods.WorldEdit;

namespace Vintagestory.ServerMods;

internal class MicroblockTool : PaintBrushTool
{
	private Dictionary<BlockPos, ChiselBlockInEdit> blocksInEdit;

	public MicroblockTool()
	{
	}

	public MicroblockTool(WorldEditWorkspace workspace, IBlockAccessorRevertable blockAccess)
		: base(workspace, blockAccess)
	{
		blocksInEdit = new Dictionary<BlockPos, ChiselBlockInEdit>();
	}

	public override void Load(ICoreAPI api)
	{
		api.ModLoader.GetModSystem<ModSystemDetailModeSync>().Toggle(((ToolBase)this).workspace.PlayerUID, on: true);
	}

	public override void Unload(ICoreAPI api)
	{
		api.ModLoader.GetModSystem<ModSystemDetailModeSync>().Toggle(((ToolBase)this).workspace.PlayerUID, on: false);
	}

	public override void HighlightBlocks(IPlayer player, ICoreServerAPI sapi, EnumHighlightBlocksMode mode)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		sapi.World.HighlightBlocks(player, 1, ((ToolBase)this).GetBlockHighlights(), ((ToolBase)this).GetBlockHighlightColors(), ((int)((ToolBase)this).workspace.ToolOffsetMode == 0) ? EnumHighlightBlocksMode.CenteredToBlockSelectionIndex : EnumHighlightBlocksMode.AttachedToBlockSelectionIndex, ((ToolBase)this).GetBlockHighlightShape(), 0.0625f);
	}

	public override void OnBreak(WorldEdit worldEdit, BlockSelection blockSel, ref EnumHandling handling)
	{
		((ToolBase)this).OnBuild(worldEdit, ((ToolBase)this).ba.GetBlock(blockSel.Position).Id, blockSel, (ItemStack)null);
	}

	public override void OnBuild(WorldEdit worldEdit, int oldBlockId, BlockSelection blockSel, ItemStack withItemStack)
	{
		((ToolBase)this).OnBuild(worldEdit, oldBlockId, blockSel, withItemStack);
		foreach (KeyValuePair<BlockPos, ChiselBlockInEdit> item in blocksInEdit)
		{
			if (item.Value.isNew)
			{
				BlockEntityChisel obj = ((ToolBase)this).ba.GetBlockEntity(item.Value.be.Pos) as BlockEntityChisel;
				TreeAttribute tree = new TreeAttribute();
				item.Value.be.ToTreeAttributes(tree);
				obj.FromTreeAttributes(tree, worldEdit.sapi.World);
				obj.RebuildCuboidList();
				obj.MarkDirty(redrawOnClient: true);
			}
			else
			{
				item.Value.be.MarkDirty(redrawOnClient: true);
			}
		}
	}

	public override void PerformBrushAction(WorldEdit worldEdit, Block placedBlock, int oldBlockId, BlockSelection blockSel, BlockPos targetPos, ItemStack withItemStack)
	{
		//IL_0247: Unknown result type (might be due to invalid IL or missing references)
		//IL_024d: Invalid comparison between Unknown and I4
		//IL_0572: Unknown result type (might be due to invalid IL or missing references)
		//IL_0577: Unknown result type (might be due to invalid IL or missing references)
		//IL_02df: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e5: Invalid comparison between Unknown and I4
		//IL_0440: Unknown result type (might be due to invalid IL or missing references)
		//IL_0446: Invalid comparison between Unknown and I4
		//IL_0882: Unknown result type (might be due to invalid IL or missing references)
		//IL_0885: Unknown result type (might be due to invalid IL or missing references)
		//IL_0897: Expected I4, but got Unknown
		//IL_0708: Unknown result type (might be due to invalid IL or missing references)
		//IL_070b: Invalid comparison between Unknown and I4
		//IL_0716: Unknown result type (might be due to invalid IL or missing references)
		if (((PaintBrushTool)this).BrushDim1 <= 0f)
		{
			return;
		}
		BlockFacing face = blockSel.Face;
		targetPos = (blockSel.DidOffset ? blockSel.Position.AddCopy(face.Opposite) : blockSel.Position);
		Block block = ((ToolBase)this).ba.GetBlock(targetPos);
		BlockChisel blockChisel = block as BlockChisel;
		if (blockChisel == null)
		{
			blockChisel = ((ToolBase)this).ba.GetBlock(new AssetLocation("chiseledblock")) as BlockChisel;
		}
		if (withItemStack != null && withItemStack.Block != null && !ItemChisel.IsValidChiselingMaterial(worldEdit.sapi, targetPos, withItemStack.Block, worldEdit.sapi.World.PlayerByUid(((ToolBase)this).workspace.PlayerUID)))
		{
			(worldEdit.sapi.World.PlayerByUid(((ToolBase)this).workspace.PlayerUID) as IServerPlayer).SendIngameError("notmicroblock", Lang.Get("Must have a chisel material in hands"));
			return;
		}
		BlockEntityChisel blockEntityChisel = ((ToolBase)this).ba.GetBlockEntity(targetPos) as BlockEntityChisel;
		Vec3i vec3i = new Vec3i(Math.Min(16, (int)(blockSel.HitPosition.X * 16.0)), Math.Min(16, (int)(blockSel.HitPosition.Y * 16.0)), Math.Min(16, (int)(blockSel.HitPosition.Z * 16.0)));
		Vec3i voxelPos = new Vec3i(Math.Min(15, (int)(blockSel.HitPosition.X * 16.0)), Math.Min(15, (int)(blockSel.HitPosition.Y * 16.0)), Math.Min(15, (int)(blockSel.HitPosition.Z * 16.0)));
		BlockPos[] array = new BlockPos[6];
		for (int i = 0; i < 6; i++)
		{
			Vec3i vec3i2 = BlockFacing.ALLNORMALI[i];
			array[i] = new BlockPos((int)((float)base.size.X / 2f * (float)vec3i2.X), (int)((float)base.size.Y / 2f * (float)vec3i2.Y), (int)((float)base.size.Z / 2f * (float)vec3i2.Z));
		}
		if ((int)((ToolBase)this).workspace.ToolOffsetMode == 1)
		{
			vec3i.X += array[blockSel.Face.Index].X;
			vec3i.Y += array[blockSel.Face.Index].Y;
			vec3i.Z += array[blockSel.Face.Index].Z;
			if (array[blockSel.Face.Index].X < 0)
			{
				vec3i.X--;
			}
			if ((int)((PaintBrushTool)this).BrushShape == 1)
			{
				if (array[blockSel.Face.Index].X < 0 && ((PaintBrushTool)this).BrushDim1 % 2f == 0f)
				{
					vec3i.X++;
				}
				if (array[blockSel.Face.Index].Y < 0 && ((PaintBrushTool)this).BrushDim2 % 2f != 0f)
				{
					vec3i.Y--;
				}
				if (array[blockSel.Face.Index].Z < 0 && ((PaintBrushTool)this).BrushDim3 % 2f != 0f)
				{
					vec3i.Z--;
				}
				if (base.size.Y == 1 && blockSel.Face.Index == BlockFacing.DOWN.Index)
				{
					vec3i.Y--;
				}
				if (base.size.X == 1 && blockSel.Face.Index == BlockFacing.WEST.Index)
				{
					vec3i.X--;
				}
				if (base.size.Z == 1 && blockSel.Face.Index == BlockFacing.NORTH.Index)
				{
					vec3i.Z--;
				}
			}
			else if ((int)((PaintBrushTool)this).BrushShape == 2)
			{
				if (array[blockSel.Face.Index].Y < 0 && base.size.Y > 2)
				{
					vec3i.Y--;
				}
				if (array[blockSel.Face.Index].Z < 0)
				{
					vec3i.Z--;
				}
				if (base.size.Y == 1 && blockSel.Face.Index == BlockFacing.DOWN.Index)
				{
					vec3i.Y--;
				}
			}
			else
			{
				if (array[blockSel.Face.Index].Y < 0)
				{
					vec3i.Y--;
				}
				if (array[blockSel.Face.Index].Z < 0)
				{
					vec3i.Z--;
				}
			}
		}
		if (oldBlockId >= 0)
		{
			if (placedBlock.ForFluidsLayer)
			{
				worldEdit.sapi.World.BlockAccessor.SetBlock(oldBlockId, blockSel.Position, 2);
			}
			else
			{
				worldEdit.sapi.World.BlockAccessor.SetBlock(oldBlockId, blockSel.Position);
			}
		}
		EnumBrushMode brushMode = ((PaintBrushTool)this).BrushMode;
		int valueOrDefault = (withItemStack?.Block?.BlockId).GetValueOrDefault();
		if (!((ToolBase)this).workspace.MayPlace(((ToolBase)this).ba.GetBlock(valueOrDefault), base.brushPositions.Length))
		{
			return;
		}
		BlockPos blockPos = new BlockPos();
		Vec3i vec3i3 = new Vec3i();
		blocksInEdit.Clear();
		int num = blockEntityChisel?.GetVoxelMaterialAt(voxelPos) ?? block.Id;
		for (int j = 0; j < base.brushPositions.Length; j++)
		{
			BlockPos blockPos2 = base.brushPositions[j];
			long num2 = targetPos.X * 16 + blockPos2.X + vec3i.X;
			long num3 = targetPos.Y * 16 + blockPos2.Y + vec3i.Y;
			long num4 = targetPos.Z * 16 + blockPos2.Z + vec3i.Z;
			BlockPos blockPos3 = blockPos.Set((int)(num2 / 16), (int)(num3 / 16), (int)(num4 / 16));
			vec3i3.Set((int)GameMath.Mod(num2, 16f), (int)GameMath.Mod(num3, 16f), (int)GameMath.Mod(num4, 16f));
			if (!blocksInEdit.TryGetValue(blockPos3, out var value))
			{
				bool flag = false;
				Block block2 = ((ToolBase)this).ba.GetBlock(blockPos3);
				BlockEntityChisel blockEntityChisel2 = ((ToolBase)this).ba.GetBlockEntity(blockPos3) as BlockEntityChisel;
				if (blockEntityChisel2 == null)
				{
					if (withItemStack == null || (((int)brushMode != 2 || block2.Id != 0) && (int)brushMode != 0))
					{
						continue;
					}
					((ToolBase)this).ba.SetBlock(blockChisel.Id, blockPos3);
					string name = withItemStack.GetName();
					blockEntityChisel2 = new BlockEntityChisel();
					blockEntityChisel2.Pos = blockPos3.Copy();
					blockEntityChisel2.CreateBehaviors(blockChisel, worldEdit.sapi.World);
					blockEntityChisel2.Initialize(worldEdit.sapi);
					blockEntityChisel2.WasPlaced(withItemStack.Block, name);
					blockEntityChisel2.VoxelCuboids = new List<uint>();
					flag = true;
				}
				int id = block2.Id;
				if (!flag)
				{
					((ToolBase)this).ba.SetHistoryStateBlock(blockPos3.X, blockPos3.Y, blockPos3.Z, id, id);
				}
				else
				{
					((ToolBase)this).ba.SetHistoryStateBlock(blockPos3.X, blockPos3.Y, blockPos3.Z, 0, blockChisel.Id);
				}
				blockEntityChisel2.BeginEdit(out var voxels, out var voxelMaterial);
				value = (blocksInEdit[blockPos3.Copy()] = new ChiselBlockInEdit
				{
					voxels = voxels,
					voxelMaterial = voxelMaterial,
					be = blockEntityChisel2,
					isNew = flag
				});
			}
			int num5 = 0;
			if (value.voxels[vec3i3.X, vec3i3.Y, vec3i3.Z])
			{
				num5 = value.be.BlockIds[value.voxelMaterial[vec3i3.X, vec3i3.Y, vec3i3.Z]];
			}
			if ((brushMode - 1) switch
			{
				1 => (num5 == 0) ? 1 : 0, 
				0 => (num5 != 0) ? 1 : 0, 
				2 => (num5 == num) ? 1 : 0, 
				_ => 1, 
			} == 0)
			{
				continue;
			}
			if (valueOrDefault == 0)
			{
				value.voxels[vec3i3.X, vec3i3.Y, vec3i3.Z] = false;
				continue;
			}
			int num6 = value.be.BlockIds.IndexOf(valueOrDefault);
			if (num6 < 0)
			{
				num6 = value.be.AddMaterial(((ToolBase)this).ba.GetBlock(valueOrDefault));
			}
			value.voxels[vec3i3.X, vec3i3.Y, vec3i3.Z] = true;
			value.voxelMaterial[vec3i3.X, vec3i3.Y, vec3i3.Z] = (byte)num6;
		}
		foreach (KeyValuePair<BlockPos, ChiselBlockInEdit> item in blocksInEdit)
		{
			item.Value.be.EndEdit(item.Value.voxels, item.Value.voxelMaterial);
			if (item.Value.be.VoxelCuboids.Count == 0)
			{
				((ToolBase)this).ba.SetBlock(0, item.Key);
				((ToolBase)this).ba.RemoveBlockLight(item.Value.be.GetLightHsv(((ToolBase)this).ba), item.Key);
			}
		}
	}
}
