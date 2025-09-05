using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VSSurvivalMod.Systems.ChiselModes;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityChisel : BlockEntityMicroBlock
{
	public static bool ForceDetailingMode = false;

	public static ChiselMode defaultMode = new OneByChiselMode();

	public ushort[] AvailMaterialQuantities;

	protected byte nowmaterialIndex;

	public static bool ConstrainToAvailableMaterialQuantity = true;

	public bool DetailingMode
	{
		get
		{
			ItemStack itemStack = ((Api.World as IClientWorldAccessor).Player?.InventoryManager?.ActiveHotbarSlot)?.Itemstack;
			if (Api.Side == EnumAppSide.Client)
			{
				if (itemStack == null || itemStack.Collectible?.Tool != EnumTool.Chisel)
				{
					return ForceDetailingMode;
				}
				return true;
			}
			return false;
		}
	}

	public override void WasPlaced(Block block, string blockName)
	{
		base.WasPlaced(block, blockName);
		AvailMaterialQuantities = new ushort[1];
		CuboidWithMaterial cuboidWithMaterial = new CuboidWithMaterial();
		for (int i = 0; i < VoxelCuboids.Count; i++)
		{
			BlockEntityMicroBlock.FromUint(VoxelCuboids[i], cuboidWithMaterial);
			AvailMaterialQuantities[0] = (ushort)(AvailMaterialQuantities[0] + cuboidWithMaterial.SizeXYZ);
		}
	}

	public SkillItem GetChiselMode(IPlayer player)
	{
		if (Api.Side != EnumAppSide.Client)
		{
			return null;
		}
		ICoreClientAPI coreClientAPI = (ICoreClientAPI)Api;
		ItemSlot itemSlot = player?.InventoryManager?.ActiveHotbarSlot;
		ItemChisel itemChisel = (ItemChisel)(itemSlot?.Itemstack.Collectible);
		int? num = itemChisel.GetToolMode(itemSlot, player, new BlockSelection
		{
			Position = Pos
		});
		if (!num.HasValue)
		{
			return null;
		}
		return itemChisel.GetToolModes(itemSlot, coreClientAPI.World.Player, new BlockSelection
		{
			Position = Pos
		})[num.Value];
	}

	public ChiselMode GetChiselModeData(IPlayer player)
	{
		ItemSlot itemSlot = player?.InventoryManager?.ActiveHotbarSlot;
		if (!(itemSlot?.Itemstack?.Collectible is ItemChisel itemChisel))
		{
			return defaultMode;
		}
		int? num = itemChisel.GetToolMode(itemSlot, player, new BlockSelection
		{
			Position = Pos
		});
		if (!num.HasValue)
		{
			return null;
		}
		return (ChiselMode)itemChisel.ToolModes[num.Value].Data;
	}

	public int GetChiselSize(IPlayer player)
	{
		return GetChiselModeData(player)?.ChiselSize ?? 0;
	}

	public Vec3i GetVoxelPos(BlockSelection blockSel, int chiselSize)
	{
		RegenSelectionVoxelBoxes(mustLoad: true, chiselSize);
		Cuboidf[] array = selectionBoxesVoxels;
		if (blockSel.SelectionBoxIndex >= array.Length)
		{
			return null;
		}
		Cuboidf cuboidf = array[blockSel.SelectionBoxIndex];
		return new Vec3i((int)(16f * cuboidf.X1), (int)(16f * cuboidf.Y1), (int)(16f * cuboidf.Z1));
	}

	internal void OnBlockInteract(IPlayer byPlayer, BlockSelection blockSel, bool isBreak)
	{
		if (Api.World.Side == EnumAppSide.Client && DetailingMode)
		{
			Cuboidf[] orCreateVoxelSelectionBoxes = GetOrCreateVoxelSelectionBoxes(byPlayer);
			if (blockSel.SelectionBoxIndex < orCreateVoxelSelectionBoxes.Length)
			{
				Cuboidf cuboidf = orCreateVoxelSelectionBoxes[blockSel.SelectionBoxIndex];
				Vec3i voxelPos = new Vec3i((int)(16f * cuboidf.X1), (int)(16f * cuboidf.Y1), (int)(16f * cuboidf.Z1));
				UpdateVoxel(byPlayer, byPlayer.InventoryManager.ActiveHotbarSlot, voxelPos, blockSel.Face, isBreak);
			}
		}
	}

	public bool Interact(IPlayer byPlayer, BlockSelection blockSel)
	{
		if (byPlayer != null)
		{
			ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
			if (activeHotbarSlot != null && activeHotbarSlot.Itemstack?.Collectible.Tool == EnumTool.Knife)
			{
				BlockFacing face = blockSel.Face;
				int num = (face.IsVertical ? face.Index : BlockFacing.HORIZONTALS_ANGLEORDER[GameMath.Mod(face.HorizontalAngleIndex + rotationY / 90, 4)].Index);
				if (DecorIds != null && DecorIds[num] != 0)
				{
					Block block = Api.World.Blocks[DecorIds[num]];
					Api.World.SpawnItemEntity(block.OnPickBlock(Api.World, Pos), Pos);
					DecorIds[num] = 0;
					MarkDirty(redrawOnClient: true, byPlayer);
				}
				return true;
			}
		}
		return false;
	}

	public void SetNowMaterialId(int materialId)
	{
		nowmaterialIndex = (byte)Math.Max(0, BlockIds.IndexOf(materialId));
	}

	public void PickBlockMaterial(IPlayer byPlayer)
	{
		if (byPlayer != null)
		{
			ItemSlot itemSlot = byPlayer.InventoryManager?.ActiveHotbarSlot;
			if (itemSlot != null && itemSlot.Itemstack?.Collectible is ItemChisel itemChisel)
			{
				Vec3d hitPosition = byPlayer.CurrentBlockSelection.HitPosition;
				Vec3i voxelPos = new Vec3i(Math.Min(15, (int)(hitPosition.X * 16.0)), Math.Min(15, (int)(hitPosition.Y * 16.0)), Math.Min(15, (int)(hitPosition.Z * 16.0)));
				int voxelMaterialAt = GetVoxelMaterialAt(voxelPos);
				int toolMode = (byte)Math.Max(0, BlockIds.IndexOf(voxelMaterialAt)) + itemChisel.ToolModes.Length;
				itemChisel.SetToolMode(itemSlot, byPlayer, byPlayer.CurrentBlockSelection, toolMode);
			}
		}
	}

	internal void UpdateVoxel(IPlayer byPlayer, ItemSlot itemslot, Vec3i voxelPos, BlockFacing facing, bool isBreak)
	{
		if (!Api.World.Claims.TryAccess(byPlayer, Pos, EnumBlockAccessFlags.Use))
		{
			MarkDirty(redrawOnClient: true, byPlayer);
		}
		else if (GetChiselModeData(byPlayer).Apply(this, byPlayer, voxelPos, facing, isBreak, nowmaterialIndex))
		{
			if (Api.Side == EnumAppSide.Client)
			{
				MarkMeshDirty();
				BlockEntityMicroBlock.UpdateNeighbors(this);
			}
			RegenSelectionBoxes(Api.World, byPlayer);
			MarkDirty(redrawOnClient: true, byPlayer);
			if (Api.Side == EnumAppSide.Client)
			{
				SendUseOverPacket(voxelPos, facing, isBreak);
			}
			double posx = (float)Pos.X + (float)voxelPos.X / 16f;
			double posy = (float)Pos.InternalY + (float)voxelPos.Y / 16f;
			double posz = (float)Pos.Z + (float)voxelPos.Z / 16f;
			Api.World.PlaySoundAt(new AssetLocation("sounds/player/knap" + ((Api.World.Rand.Next(2) > 0) ? 1 : 2)), posx, posy, posz, byPlayer, randomizePitch: true, 12f);
			if (byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative && Api.World.Rand.Next(3) == 0)
			{
				itemslot.Itemstack?.Collectible.DamageItem(Api.World, byPlayer.Entity, itemslot);
			}
			if (VoxelCuboids.Count == 0)
			{
				Api.World.BlockAccessor.SetBlock(0, Pos);
				Api.World.BlockAccessor.RemoveBlockLight(GetLightHsv(Api.World.BlockAccessor), Pos);
			}
		}
	}

	public void SendUseOverPacket(Vec3i voxelPos, BlockFacing facing, bool isBreak)
	{
		byte[] data;
		using (MemoryStream memoryStream = new MemoryStream())
		{
			BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
			binaryWriter.Write(voxelPos.X);
			binaryWriter.Write(voxelPos.Y);
			binaryWriter.Write(voxelPos.Z);
			binaryWriter.Write(isBreak);
			binaryWriter.Write((ushort)facing.Index);
			binaryWriter.Write(nowmaterialIndex);
			data = memoryStream.ToArray();
		}
		((ICoreClientAPI)Api).Network.SendBlockEntityPacket(Pos, 1010, data);
	}

	public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
	{
		if (!Api.World.Claims.TryAccess(player, Pos, EnumBlockAccessFlags.BuildOrBreak))
		{
			player.InventoryManager.ActiveHotbarSlot.MarkDirty();
			return;
		}
		if (packetid == 1002)
		{
			EditSignPacket editSignPacket = SerializerUtil.Deserialize<EditSignPacket>(data);
			base.BlockName = editSignPacket.Text;
			MarkDirty(redrawOnClient: true, player);
			Api.World.BlockAccessor.GetChunkAtBlockPos(Pos).MarkModified();
		}
		if (packetid == 1010)
		{
			Vec3i voxelPos;
			bool isBreak;
			BlockFacing facing;
			using (MemoryStream input = new MemoryStream(data))
			{
				BinaryReader binaryReader = new BinaryReader(input);
				voxelPos = new Vec3i(binaryReader.ReadInt32(), binaryReader.ReadInt32(), binaryReader.ReadInt32());
				isBreak = binaryReader.ReadBoolean();
				facing = BlockFacing.ALLFACES[binaryReader.ReadInt16()];
				nowmaterialIndex = (byte)Math.Clamp(binaryReader.ReadByte(), 0, BlockIds.Length - 1);
			}
			UpdateVoxel(player, player.InventoryManager.ActiveHotbarSlot, voxelPos, facing, isBreak);
		}
		if (packetid == 1011)
		{
			PickBlockMaterial(player);
		}
	}

	public override Cuboidf[] GetSelectionBoxes(IBlockAccessor world, BlockPos pos, IPlayer forPlayer = null)
	{
		ICoreAPI api = Api;
		if (api != null && api.Side == EnumAppSide.Client && DetailingMode)
		{
			if (forPlayer == null)
			{
				forPlayer = (Api.World as IClientWorldAccessor).Player;
			}
			int chiselSize = GetChiselSize(forPlayer);
			if (prevSize > 0 && prevSize != chiselSize)
			{
				selectionBoxesVoxels = null;
			}
			prevSize = chiselSize;
			return GetOrCreateVoxelSelectionBoxes(forPlayer);
		}
		return base.GetSelectionBoxes(world, pos, forPlayer);
	}

	private Cuboidf[] GetOrCreateVoxelSelectionBoxes(IPlayer byPlayer)
	{
		if (selectionBoxesVoxels == null)
		{
			GenerateSelectionVoxelBoxes(byPlayer);
		}
		return selectionBoxesVoxels;
	}

	public bool SetVoxel(Vec3i voxelPos, bool add, IPlayer byPlayer, byte materialId)
	{
		int chiselSize = GetChiselSize(byPlayer);
		if (add && ConstrainToAvailableMaterialQuantity && AvailMaterialQuantities != null)
		{
			int num = AvailMaterialQuantities[materialId];
			CuboidWithMaterial cuboidWithMaterial = new CuboidWithMaterial();
			int num2 = 0;
			foreach (uint voxelCuboid in VoxelCuboids)
			{
				BlockEntityMicroBlock.FromUint(voxelCuboid, cuboidWithMaterial);
				if (cuboidWithMaterial.Material == materialId)
				{
					num2 += cuboidWithMaterial.SizeXYZ;
				}
			}
			num2 += chiselSize * chiselSize * chiselSize;
			if (num2 > num)
			{
				(Api as ICoreClientAPI)?.TriggerIngameError(this, "outofmaterial", Lang.Get("Out of material, add more material to continue adding voxels"));
				return false;
			}
		}
		if (!SetVoxel(voxelPos, add, materialId, chiselSize))
		{
			return false;
		}
		if (Api.Side == EnumAppSide.Client && !add)
		{
			Vec3d vec3d = Pos.ToVec3d().Add((double)voxelPos.X / 16.0, (double)voxelPos.Y / 16.0, (double)voxelPos.Z / 16.0).Add((double)((float)chiselSize / 4f) / 16.0, (double)((float)chiselSize / 4f) / 16.0, (double)((float)chiselSize / 4f) / 16.0);
			int num3 = chiselSize * 5 - 2 + Api.World.Rand.Next(5);
			Block block = Api.World.GetBlock(BlockIds[materialId]);
			while (num3-- > 0)
			{
				Api.World.SpawnParticles(1f, block.GetRandomColor(Api as ICoreClientAPI, Pos, BlockFacing.UP) | -16777216, vec3d, vec3d.Clone().Add((double)((float)chiselSize / 4f) / 16.0, (double)((float)chiselSize / 4f) / 16.0, (double)((float)chiselSize / 4f) / 16.0), new Vec3f(-1f, -0.5f, -1f), new Vec3f(1f, 1f + (float)chiselSize / 3f, 1f), 1f, 1f, (float)chiselSize / 30f + 0.1f + (float)Api.World.Rand.NextDouble() * 0.25f, EnumParticleModel.Cube);
			}
		}
		return true;
	}

	public override void RegenSelectionBoxes(IWorldAccessor worldForResolve, IPlayer byPlayer)
	{
		base.RegenSelectionBoxes(worldForResolve, byPlayer);
		if (byPlayer != null)
		{
			int chiselSize = GetChiselSize(byPlayer);
			RegenSelectionVoxelBoxes(mustLoad: false, chiselSize);
		}
		else
		{
			selectionBoxesVoxels = null;
		}
	}

	public void GenerateSelectionVoxelBoxes(IPlayer byPlayer)
	{
		int chiselSize = GetChiselSize(byPlayer);
		RegenSelectionVoxelBoxes(mustLoad: true, chiselSize);
	}

	public void RegenSelectionVoxelBoxes(bool mustLoad, int chiselSize)
	{
		if (selectionBoxesVoxels == null && !mustLoad)
		{
			return;
		}
		HashSet<Cuboidf> hashSet = new HashSet<Cuboidf>();
		if (chiselSize <= 0)
		{
			chiselSize = 16;
		}
		float num = (float)chiselSize / 16f;
		float num2 = (float)chiselSize / 16f;
		float num3 = (float)chiselSize / 16f;
		CuboidWithMaterial cuboidWithMaterial = BlockEntityMicroBlock.tmpCuboids[0];
		for (int i = 0; i < VoxelCuboids.Count; i++)
		{
			BlockEntityMicroBlock.FromUint(VoxelCuboids[i], cuboidWithMaterial);
			for (int j = cuboidWithMaterial.X1; j < cuboidWithMaterial.X2; j += chiselSize)
			{
				for (int k = cuboidWithMaterial.Y1; k < cuboidWithMaterial.Y2; k += chiselSize)
				{
					for (int l = cuboidWithMaterial.Z1; l < cuboidWithMaterial.Z2; l += chiselSize)
					{
						float num4 = (float)Math.Floor((float)j / (float)chiselSize) * num;
						float num5 = (float)Math.Floor((float)k / (float)chiselSize) * num2;
						float num6 = (float)Math.Floor((float)l / (float)chiselSize) * num3;
						if (!(num4 + num > 1f) && !(num5 + num2 > 1f) && !(num6 + num3 > 1f))
						{
							hashSet.Add(new Cuboidf(num4, num5, num6, num4 + num, num5 + num2, num6 + num3));
						}
					}
				}
			}
		}
		selectionBoxesVoxels = hashSet.ToArray();
	}

	public int AddMaterial(Block addblock, out bool isFull, bool compareToPickBlock = true)
	{
		Cuboidf[] array = addblock.GetCollisionBoxes(Api.World.BlockAccessor, Pos);
		int num = 0;
		if (array == null)
		{
			array = new Cuboidf[1] { Cuboidf.Default() };
		}
		foreach (Cuboidf cuboidf in array)
		{
			num += new Cuboidi((int)(16f * cuboidf.X1), (int)(16f * cuboidf.Y1), (int)(16f * cuboidf.Z1), (int)(16f * cuboidf.X2), (int)(16f * cuboidf.Y2), (int)(16f * cuboidf.Z2)).SizeXYZ;
		}
		if (compareToPickBlock && !BlockIds.Contains(addblock.Id))
		{
			int[] blockIds = BlockIds;
			foreach (int index in blockIds)
			{
				Block block = Api.World.Blocks[index];
				if (block.OnPickBlock(Api.World, Pos).Block?.Id == addblock.Id)
				{
					addblock = block;
				}
			}
		}
		if (!BlockIds.Contains(addblock.Id))
		{
			isFull = false;
			BlockIds = BlockIds.Append(addblock.Id);
			if (AvailMaterialQuantities != null)
			{
				AvailMaterialQuantities = AvailMaterialQuantities.Append((ushort)num);
			}
			return BlockIds.Length - 1;
		}
		int num2 = BlockIds.IndexOf(addblock.Id);
		isFull = AvailMaterialQuantities[num2] >= 4096;
		if (AvailMaterialQuantities != null)
		{
			AvailMaterialQuantities[num2] = (ushort)Math.Min(65535, AvailMaterialQuantities[num2] + num);
		}
		return num2;
	}

	public int AddMaterial(Block block)
	{
		bool isFull;
		return AddMaterial(block, out isFull);
	}

	public override bool RemoveMaterial(Block block)
	{
		int num = BlockIds.IndexOf(block.Id);
		if (AvailMaterialQuantities != null && num >= 0)
		{
			AvailMaterialQuantities = AvailMaterialQuantities.RemoveEntry(num);
		}
		return base.RemoveMaterial(block);
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		base.FromTreeAttributes(tree, worldAccessForResolve);
		if (tree["availMaterialQuantities"] is IntArrayAttribute intArrayAttribute)
		{
			AvailMaterialQuantities = new ushort[intArrayAttribute.value.Length];
			for (int i = 0; i < intArrayAttribute.value.Length; i++)
			{
				AvailMaterialQuantities[i] = (ushort)intArrayAttribute.value[i];
			}
			while (BlockIds.Length > AvailMaterialQuantities.Length)
			{
				AvailMaterialQuantities = AvailMaterialQuantities.Append((ushort)4096);
			}
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		if (AvailMaterialQuantities != null)
		{
			IntArrayAttribute intArrayAttribute = new IntArrayAttribute();
			intArrayAttribute.value = new int[AvailMaterialQuantities.Length];
			for (int i = 0; i < AvailMaterialQuantities.Length; i++)
			{
				intArrayAttribute.value[i] = AvailMaterialQuantities[i];
			}
			tree["availMaterialQuantities"] = intArrayAttribute;
		}
	}
}
