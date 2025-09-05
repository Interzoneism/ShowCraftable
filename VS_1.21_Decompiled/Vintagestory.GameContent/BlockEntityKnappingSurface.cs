using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityKnappingSurface : BlockEntity
{
	private int selectedRecipeId = -1;

	public bool[,] Voxels = new bool[16, 16];

	public ItemStack BaseMaterial;

	private Cuboidf[] selectionBoxes = Array.Empty<Cuboidf>();

	private KnappingRenderer workitemRenderer;

	private Vec3d lastRemovedLocalPos = new Vec3d();

	private GuiDialog dlg;

	public KnappingRecipe SelectedRecipe => Api.GetKnappingRecipes().FirstOrDefault((KnappingRecipe r) => r.RecipeId == selectedRecipeId);

	public override void OnBlockPlaced(ItemStack byItemStack = null)
	{
		CreateInitialWorkItem();
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		if (BaseMaterial != null)
		{
			BaseMaterial.ResolveBlockOrItem(api.World);
		}
		if (api is ICoreClientAPI coreClientAPI)
		{
			workitemRenderer = new KnappingRenderer(Pos, coreClientAPI);
			RegenMeshAndSelectionBoxes();
			coreClientAPI.Event.ColorsPresetChanged += RegenMeshAndSelectionBoxes;
		}
	}

	internal Cuboidf[] GetSelectionBoxes(IBlockAccessor world, BlockPos pos)
	{
		return selectionBoxes;
	}

	internal void OnBeginUse(IPlayer byPlayer, BlockSelection blockSel)
	{
		if (SelectedRecipe == null && Api.Side == EnumAppSide.Client)
		{
			OpenDialog(Api.World as IClientWorldAccessor, Pos, byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack);
		}
	}

	internal void OnUseOver(IPlayer byPlayer, int selectionBoxIndex, BlockFacing facing, bool mouseMode)
	{
		if (selectionBoxIndex >= 0 && selectionBoxIndex < selectionBoxes.Length)
		{
			Cuboidf cuboidf = selectionBoxes[selectionBoxIndex];
			Vec3i voxelPos = new Vec3i((int)(16f * cuboidf.X1), (int)(16f * cuboidf.Y1), (int)(16f * cuboidf.Z1));
			OnUseOver(byPlayer, voxelPos, facing, mouseMode);
		}
	}

	internal void OnUseOver(IPlayer byPlayer, Vec3i voxelPos, BlockFacing facing, bool mouseMode)
	{
		if (voxelPos == null)
		{
			return;
		}
		if (Api.Side == EnumAppSide.Client)
		{
			SendUseOverPacket(byPlayer, voxelPos, facing, mouseMode);
		}
		if (byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack == null)
		{
			return;
		}
		bool flag = mouseMode && OnRemove(voxelPos, 0);
		if (flag)
		{
			for (int i = 0; i < BlockFacing.HORIZONTALS.Length; i++)
			{
				BlockFacing facing2 = BlockFacing.HORIZONTALS[i];
				Vec3i vec3i = voxelPos.AddCopy(facing2);
				if (Voxels[vec3i.X, vec3i.Z] && !SelectedRecipe.Voxels[vec3i.X, 0, vec3i.Z])
				{
					tryBfsRemove(vec3i.X, vec3i.Z);
				}
			}
		}
		if (mouseMode && (flag || Voxels[voxelPos.X, voxelPos.Z]))
		{
			Api.World.PlaySoundAt(new AssetLocation("sounds/player/knap" + ((Api.World.Rand.Next(2) > 0) ? 1 : 2)), lastRemovedLocalPos.X, lastRemovedLocalPos.Y, lastRemovedLocalPos.Z, byPlayer, randomizePitch: true, 12f);
		}
		if (flag && Api.Side == EnumAppSide.Client)
		{
			spawnParticles(lastRemovedLocalPos);
		}
		RegenMeshAndSelectionBoxes();
		Api.World.BlockAccessor.MarkBlockDirty(Pos);
		Api.World.BlockAccessor.MarkBlockEntityDirty(Pos);
		if (!HasAnyVoxel())
		{
			Api.World.BlockAccessor.SetBlock(0, Pos);
			return;
		}
		CheckIfFinished(byPlayer);
		MarkDirty();
	}

	public void CheckIfFinished(IPlayer byPlayer)
	{
		if (!MatchesRecipe() || !(Api.World is IServerWorldAccessor))
		{
			return;
		}
		Voxels = new bool[16, 16];
		ItemStack itemStack = SelectedRecipe.Output.ResolvedItemstack.Clone();
		selectedRecipeId = -1;
		if (itemStack.StackSize == 1 && itemStack.Class == EnumItemClass.Block)
		{
			Api.World.BlockAccessor.SetBlock(itemStack.Block.BlockId, Pos);
			return;
		}
		int num = 0;
		while (itemStack.StackSize > 0)
		{
			ItemStack itemStack2 = itemStack.Clone();
			itemStack2.StackSize = Math.Min(itemStack.StackSize, itemStack.Collectible.MaxStackSize);
			itemStack.StackSize -= itemStack2.StackSize;
			TreeAttribute treeAttribute = new TreeAttribute();
			treeAttribute["itemstack"] = new ItemstackAttribute(itemStack2);
			treeAttribute["byentityid"] = new LongAttribute(byPlayer.Entity.EntityId);
			Api.Event.PushEvent("onitemknapped", treeAttribute);
			if (byPlayer.InventoryManager.TryGiveItemstack(itemStack2))
			{
				Api.World.PlaySoundAt(new AssetLocation("sounds/player/collect"), byPlayer);
			}
			else
			{
				Api.World.SpawnItemEntity(itemStack2, Pos);
			}
			if (num++ > 1000)
			{
				throw new Exception("Endless loop prevention triggered. Something seems broken with a matching knapping recipe with number " + selectedRecipeId + ". Tried 1000 times to drop the resulting stack " + itemStack.ToString());
			}
		}
		Api.World.BlockAccessor.SetBlock(0, Pos);
	}

	private void spawnParticles(Vec3d pos)
	{
		Random rand = Api.World.Rand;
		for (int i = 0; i < 3; i++)
		{
			Api.World.SpawnParticles(new SimpleParticleProperties
			{
				MinQuantity = 1f,
				AddQuantity = 2f,
				Color = BaseMaterial.Collectible.GetRandomColor(Api as ICoreClientAPI, BaseMaterial),
				MinPos = new Vec3d(pos.X, pos.Y + 0.0625 + 0.009999999776482582, pos.Z),
				AddPos = new Vec3d(0.0625, 0.009999999776482582, 0.0625),
				MinVelocity = new Vec3f(0f, 1f, 0f),
				AddVelocity = new Vec3f(4f * ((float)rand.NextDouble() - 0.5f), 1f * ((float)rand.NextDouble() - 0.5f), 4f * ((float)rand.NextDouble() - 0.5f)),
				LifeLength = 0.2f,
				GravityEffect = 1f,
				MinSize = 0.1f,
				MaxSize = 0.4f,
				ParticleModel = EnumParticleModel.Cube,
				SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.15f)
			});
		}
	}

	private bool MatchesRecipe()
	{
		if (SelectedRecipe == null)
		{
			return false;
		}
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				if (Voxels[i, j] != SelectedRecipe.Voxels[i, 0, j])
				{
					return false;
				}
			}
		}
		return true;
	}

	private Cuboidi LayerBounds()
	{
		Cuboidi cuboidi = new Cuboidi(8, 8, 8, 8, 8, 8);
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				if (SelectedRecipe.Voxels[i, 0, j])
				{
					cuboidi.X1 = Math.Min(cuboidi.X1, i);
					cuboidi.X2 = Math.Max(cuboidi.X2, i);
					cuboidi.Z1 = Math.Min(cuboidi.Z1, j);
					cuboidi.Z2 = Math.Max(cuboidi.Z2, j);
				}
			}
		}
		return cuboidi;
	}

	private bool HasAnyVoxel()
	{
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				if (Voxels[i, j])
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool InBounds(Vec3i voxelPos)
	{
		Cuboidi cuboidi = LayerBounds();
		if (voxelPos.X >= cuboidi.X1 && voxelPos.X <= cuboidi.X2 && voxelPos.Y >= 0 && voxelPos.Y < 16 && voxelPos.Z >= cuboidi.Z1)
		{
			return voxelPos.Z <= cuboidi.Z2;
		}
		return false;
	}

	private bool OnRemove(Vec3i voxelPos, int radius)
	{
		if (SelectedRecipe == null || SelectedRecipe.Voxels[voxelPos.X, 0, voxelPos.Z])
		{
			return false;
		}
		for (int i = -(int)Math.Ceiling((float)radius / 2f); i <= radius / 2; i++)
		{
			for (int j = -(int)Math.Ceiling((float)radius / 2f); j <= radius / 2; j++)
			{
				Vec3i vec3i = voxelPos.AddCopy(i, 0, j);
				if (vec3i.X >= 0 && vec3i.X < 16 && vec3i.Z >= 0 && vec3i.Z < 16 && Voxels[vec3i.X, vec3i.Z])
				{
					Voxels[vec3i.X, vec3i.Z] = false;
					lastRemovedLocalPos.Set((float)Pos.X + (float)voxelPos.X / 16f, (float)Pos.Y + (float)voxelPos.Y / 16f, (float)Pos.Z + (float)voxelPos.Z / 16f);
					return true;
				}
			}
		}
		return false;
	}

	private void tryBfsRemove(int x, int z)
	{
		Queue<Vec2i> queue = new Queue<Vec2i>();
		HashSet<Vec2i> hashSet = new HashSet<Vec2i>();
		queue.Enqueue(new Vec2i(x, z));
		List<Vec2i> list = new List<Vec2i>();
		while (queue.Count > 0)
		{
			Vec2i vec2i = queue.Dequeue();
			for (int i = 0; i < BlockFacing.HORIZONTALS.Length; i++)
			{
				BlockFacing blockFacing = BlockFacing.HORIZONTALS[i];
				Vec2i vec2i2 = vec2i.Copy().Add(blockFacing.Normali.X, blockFacing.Normali.Z);
				if (vec2i2.X >= 0 && vec2i2.X < 16 && vec2i2.Y >= 0 && vec2i2.Y < 16 && Voxels[vec2i2.X, vec2i2.Y] && !hashSet.Contains(vec2i2))
				{
					hashSet.Add(vec2i2);
					list.Add(vec2i2);
					if (SelectedRecipe.Voxels[vec2i2.X, 0, vec2i2.Y])
					{
						return;
					}
					queue.Enqueue(vec2i2);
				}
			}
		}
		if (hashSet.Count == 0 && list.Count == 0)
		{
			list.Add(new Vec2i(x, z));
		}
		Vec3d vec3d = new Vec3d();
		foreach (Vec2i item in list)
		{
			Voxels[item.X, item.Y] = false;
			if (Api.Side == EnumAppSide.Client)
			{
				vec3d.Set((float)Pos.X + (float)item.X / 16f, Pos.Y, (float)Pos.Z + (float)item.Y / 16f);
				spawnParticles(vec3d);
			}
		}
	}

	public void RegenMeshAndSelectionBoxes()
	{
		if (workitemRenderer != null && BaseMaterial != null)
		{
			BaseMaterial.ResolveBlockOrItem(Api.World);
			workitemRenderer.Material = BaseMaterial.Collectible.FirstCodePart(1);
			if (workitemRenderer.Material == null)
			{
				workitemRenderer.Material = BaseMaterial.Collectible.FirstCodePart();
			}
			workitemRenderer.RegenMesh(Voxels, SelectedRecipe);
		}
		List<Cuboidf> list = new List<Cuboidf>();
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				list.Add(new Cuboidf((float)i / 16f, 0f, (float)j / 16f, (float)i / 16f + 0.0625f, 0.0625f, (float)j / 16f + 0.0625f));
			}
		}
		selectionBoxes = list.ToArray();
	}

	public void CreateInitialWorkItem()
	{
		Voxels = new bool[16, 16];
		for (int i = 3; i <= 12; i++)
		{
			for (int j = 3; j <= 12; j++)
			{
				Voxels[i, j] = true;
			}
		}
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		workitemRenderer?.Dispose();
		workitemRenderer = null;
		dlg?.TryClose();
		dlg?.Dispose();
		if (Api is ICoreClientAPI coreClientAPI)
		{
			coreClientAPI.Event.ColorsPresetChanged -= RegenMeshAndSelectionBoxes;
		}
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		deserializeVoxels(tree.GetBytes("voxels"));
		selectedRecipeId = tree.GetInt("selectedRecipeId", -1);
		BaseMaterial = tree.GetItemstack("baseMaterial");
		if (Api?.World != null)
		{
			BaseMaterial?.ResolveBlockOrItem(Api.World);
		}
		RegenMeshAndSelectionBoxes();
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetBytes("voxels", serializeVoxels());
		tree.SetInt("selectedRecipeId", selectedRecipeId);
		tree.SetItemstack("baseMaterial", BaseMaterial);
	}

	private byte[] serializeVoxels()
	{
		byte[] array = new byte[32];
		int num = 0;
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				int num2 = num % 8;
				array[num / 8] |= (byte)((Voxels[i, j] ? 1u : 0u) << num2);
				num++;
			}
		}
		return array;
	}

	private void deserializeVoxels(byte[] data)
	{
		Voxels = new bool[16, 16];
		if (data == null || data.Length < 32)
		{
			return;
		}
		int num = 0;
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				int num2 = num % 8;
				Voxels[i, j] = (data[num / 8] & (1 << num2)) > 0;
				num++;
			}
		}
	}

	public void SendUseOverPacket(IPlayer byPlayer, Vec3i voxelPos, BlockFacing facing, bool mouseMode)
	{
		byte[] data;
		using (MemoryStream memoryStream = new MemoryStream())
		{
			BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
			binaryWriter.Write(voxelPos.X);
			binaryWriter.Write(voxelPos.Y);
			binaryWriter.Write(voxelPos.Z);
			binaryWriter.Write(mouseMode);
			binaryWriter.Write((ushort)facing.Index);
			data = memoryStream.ToArray();
		}
		((ICoreClientAPI)Api).Network.SendBlockEntityPacket(Pos, 1002, data);
	}

	public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
	{
		if (packetid == 1003)
		{
			if (BaseMaterial != null)
			{
				Api.World.SpawnItemEntity(BaseMaterial, Pos);
			}
			Api.World.BlockAccessor.SetBlock(0, Pos);
			Api.World.BlockAccessor.TriggerNeighbourBlockUpdate(Pos);
		}
		if (packetid == 1001)
		{
			int recipeid = SerializerUtil.Deserialize<int>(data);
			KnappingRecipe knappingRecipe = Api.GetKnappingRecipes().FirstOrDefault((KnappingRecipe r) => r.RecipeId == recipeid);
			if (knappingRecipe == null)
			{
				Api.World.Logger.Error("Client tried to selected knapping recipe with id {0}, but no such recipe exists!");
				return;
			}
			selectedRecipeId = knappingRecipe.RecipeId;
			MarkDirty();
			Api.World.BlockAccessor.GetChunkAtBlockPos(Pos).MarkModified();
		}
		if (packetid == 1002)
		{
			Vec3i voxelPos;
			bool mouseMode;
			BlockFacing facing;
			using (MemoryStream input = new MemoryStream(data))
			{
				BinaryReader binaryReader = new BinaryReader(input);
				voxelPos = new Vec3i(binaryReader.ReadInt32(), binaryReader.ReadInt32(), binaryReader.ReadInt32());
				mouseMode = binaryReader.ReadBoolean();
				facing = BlockFacing.ALLFACES[binaryReader.ReadInt16()];
			}
			OnUseOver(player, voxelPos, facing, mouseMode);
		}
	}

	public void OpenDialog(IClientWorldAccessor world, BlockPos pos, ItemStack baseMaterial)
	{
		List<KnappingRecipe> recipes = (from r in Api.GetKnappingRecipes()
			where r.Ingredient.SatisfiesAsIngredient(baseMaterial)
			orderby r.Output.ResolvedItemstack.Collectible.Code
			select r).ToList();
		List<ItemStack> list = recipes.Select((KnappingRecipe r) => r.Output.ResolvedItemstack).ToList();
		ICoreClientAPI capi = Api as ICoreClientAPI;
		dlg?.Dispose();
		dlg = new GuiDialogBlockEntityRecipeSelector(Lang.Get("Select recipe"), list.ToArray(), delegate(int selectedIndex)
		{
			selectedRecipeId = recipes[selectedIndex].RecipeId;
			capi.Network.SendBlockEntityPacket(pos, 1001, SerializerUtil.Serialize(recipes[selectedIndex].RecipeId));
		}, delegate
		{
			capi.Network.SendBlockEntityPacket(pos, 1003);
		}, pos, Api as ICoreClientAPI);
		dlg.TryOpen();
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		if (BaseMaterial != null && SelectedRecipe != null)
		{
			dsc.AppendLine(Lang.Get("Output: {0}", SelectedRecipe.Output?.ResolvedItemstack?.GetName()));
		}
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		workitemRenderer?.Dispose();
		if (Api is ICoreClientAPI coreClientAPI)
		{
			coreClientAPI.Event.ColorsPresetChanged -= RegenMeshAndSelectionBoxes;
		}
	}
}
