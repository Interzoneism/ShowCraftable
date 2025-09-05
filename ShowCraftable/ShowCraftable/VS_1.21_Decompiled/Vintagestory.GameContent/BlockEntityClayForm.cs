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

public class BlockEntityClayForm : BlockEntity
{
	private ItemStack workItemStack;

	private int selectedRecipeId = -1;

	private ClayFormingRecipe selectedRecipe;

	public int AvailableVoxels;

	public bool[,,] Voxels = new bool[16, 16, 16];

	private ItemStack baseMaterial;

	private Cuboidf[] selectionBoxes = Array.Empty<Cuboidf>();

	private ClayFormRenderer workitemRenderer;

	private GuiDialog dlg;

	public ClayFormingRecipe SelectedRecipe => selectedRecipe;

	public bool CanWorkCurrent
	{
		get
		{
			if (workItemStack != null)
			{
				return CanWork(workItemStack);
			}
			return false;
		}
	}

	public ItemStack BaseMaterial => baseMaterial;

	static BlockEntityClayForm()
	{
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		setSelectedRecipe(selectedRecipeId);
		if (workItemStack != null)
		{
			workItemStack.ResolveBlockOrItem(api.World);
			if (baseMaterial == null)
			{
				baseMaterial = new ItemStack(api.World.GetItem(AssetLocation.Create("clay-" + workItemStack.Collectible.Variant["type"], workItemStack.Collectible.Code.Domain)));
			}
			else
			{
				baseMaterial.ResolveBlockOrItem(api.World);
			}
		}
		if (api is ICoreClientAPI coreClientAPI)
		{
			coreClientAPI.Event.RegisterRenderer(workitemRenderer = new ClayFormRenderer(Pos, coreClientAPI), EnumRenderStage.Opaque);
			coreClientAPI.Event.RegisterRenderer(workitemRenderer, EnumRenderStage.AfterFinalComposition);
			RegenMeshForNextLayer();
			coreClientAPI.Event.ColorsPresetChanged += RegenMeshForNextLayer;
		}
	}

	public bool CanWork(ItemStack stack)
	{
		return true;
	}

	internal Cuboidf[] GetSelectionBoxes(IBlockAccessor world, BlockPos pos)
	{
		return selectionBoxes;
	}

	public void PutClay(ItemSlot slot)
	{
		if (workItemStack == null)
		{
			if (Api.World is IClientWorldAccessor)
			{
				OpenDialog(Api.World as IClientWorldAccessor, Pos, slot.Itemstack);
			}
			CreateInitialWorkItem();
			workItemStack = new ItemStack(Api.World.GetItem(AssetLocation.Create("clayworkitem-" + slot.Itemstack.Collectible.Variant["type"], slot.Itemstack.Collectible.Code.Domain)));
			baseMaterial = slot.Itemstack.Clone();
			baseMaterial.StackSize = 1;
		}
		AvailableVoxels += 25;
		slot.TakeOut(1);
		slot.MarkDirty();
		RegenMeshForNextLayer();
		MarkDirty();
	}

	public void OnBeginUse(IPlayer byPlayer, BlockSelection blockSel)
	{
		if (SelectedRecipe == null && Api.Side == EnumAppSide.Client)
		{
			OpenDialog(Api.World as IClientWorldAccessor, Pos, byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack);
		}
	}

	public void OnUseOver(IPlayer byPlayer, int selectionBoxIndex, BlockFacing facing, bool mouseBreakMode)
	{
		if (selectionBoxIndex >= 0 && selectionBoxIndex < selectionBoxes.Length)
		{
			Cuboidf cuboidf = selectionBoxes[selectionBoxIndex];
			Vec3i voxelPos = new Vec3i((int)(16f * cuboidf.X1), (int)(16f * cuboidf.Y1), (int)(16f * cuboidf.Z1));
			Api.World.FrameProfiler.Enter("clayforming");
			OnUseOver(byPlayer, voxelPos, facing, mouseBreakMode);
			Api.World.FrameProfiler.Leave();
		}
	}

	public void OnUseOver(IPlayer byPlayer, Vec3i voxelPos, BlockFacing facing, bool mouseBreakMode)
	{
		if (SelectedRecipe == null || voxelPos == null)
		{
			return;
		}
		if (Api.Side == EnumAppSide.Client)
		{
			SendUseOverPacket(byPlayer, voxelPos, facing, mouseBreakMode);
		}
		ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
		if (activeHotbarSlot.Itemstack == null || !CanWorkCurrent)
		{
			return;
		}
		int num = activeHotbarSlot.Itemstack.Collectible.GetToolMode(activeHotbarSlot, byPlayer, new BlockSelection
		{
			Position = Pos
		});
		bool flag = false;
		Api.World.FrameProfiler.Mark("clayform-modified1");
		int num2 = NextNotMatchingRecipeLayer();
		Api.World.FrameProfiler.Mark("clayform-modified2");
		if (num == 3)
		{
			if (!mouseBreakMode)
			{
				flag = OnCopyLayer(num2);
			}
			else
			{
				num = 1;
			}
		}
		if (num != 3)
		{
			flag = (mouseBreakMode ? OnRemove(num2, voxelPos, facing, num) : OnAdd(num2, voxelPos, facing, num));
		}
		Api.World.FrameProfiler.Mark("clayform-modified3");
		if (flag)
		{
			Api.World.PlaySoundAt(new AssetLocation("sounds/player/clayform.ogg"), byPlayer, byPlayer, randomizePitch: true, 8f);
			Api.World.FrameProfiler.Mark("clayform-playsound");
		}
		num2 = NextNotMatchingRecipeLayer(num2);
		RegenMeshAndSelectionBoxes(num2);
		Api.World.FrameProfiler.Mark("clayform-regenmesh");
		Api.World.BlockAccessor.MarkBlockDirty(Pos);
		Api.World.BlockAccessor.MarkBlockEntityDirty(Pos);
		if (!HasAnyVoxel())
		{
			AvailableVoxels = 0;
			workItemStack = null;
			Api.World.BlockAccessor.SetBlock(0, Pos);
		}
		else
		{
			CheckIfFinished(byPlayer, num2);
			Api.World.FrameProfiler.Mark("clayform-checkfinished");
			MarkDirty();
		}
	}

	public void CheckIfFinished(IPlayer byPlayer, int layer)
	{
		if (!MatchesRecipe(layer) || !(Api.World is IServerWorldAccessor))
		{
			return;
		}
		workItemStack = null;
		Voxels = new bool[16, 16, 16];
		AvailableVoxels = 0;
		ItemStack itemStack = SelectedRecipe.Output.ResolvedItemstack.Clone();
		selectedRecipeId = -1;
		selectedRecipe = null;
		GroundStorageProperties groundStorageProperties = itemStack.Collectible.GetBehavior<CollectibleBehaviorGroundStorable>()?.StorageProps;
		if (groundStorageProperties != null)
		{
			Api.World.BlockAccessor.SetBlock(Api.World.GetBlock("groundstorage").BlockId, Pos);
			int stackSize = itemStack.StackSize;
			bool flag = stackSize == 1;
			if (flag)
			{
				EnumGroundStorageLayout layout = groundStorageProperties.Layout;
				bool flag2 = (((uint)layout <= 1u || layout == EnumGroundStorageLayout.Quadrants) ? true : false);
				flag = flag2;
			}
			if (flag)
			{
				BlockEntityGroundStorage blockEntity = Api.World.BlockAccessor.GetBlockEntity<BlockEntityGroundStorage>(Pos);
				DummySlot sourceSlot = new DummySlot(itemStack);
				blockEntity.DetermineStorageProperties(sourceSlot);
				GroundStorageProperties groundStorageProperties2 = blockEntity.StorageProps.Clone();
				groundStorageProperties2.Layout = EnumGroundStorageLayout.SingleCenter;
				blockEntity.ForceStorageProps(groundStorageProperties2);
				blockEntity.DetermineStorageProperties(sourceSlot);
				blockEntity.Inventory[0].Itemstack = itemStack.Clone();
				blockEntity.Inventory[0].MarkDirty();
				return;
			}
			if ((stackSize == 2 && groundStorageProperties.Layout == EnumGroundStorageLayout.Halves) || (stackSize == 4 && groundStorageProperties.Layout == EnumGroundStorageLayout.Quadrants))
			{
				BlockEntityGroundStorage blockEntity2 = Api.World.BlockAccessor.GetBlockEntity<BlockEntityGroundStorage>(Pos);
				blockEntity2.DetermineStorageProperties(new DummySlot(itemStack));
				itemStack.StackSize = 1;
				for (int i = 0; i < stackSize; i++)
				{
					blockEntity2.Inventory[i].Itemstack = itemStack.Clone();
					blockEntity2.Inventory[i].MarkDirty();
				}
				return;
			}
			if (stackSize <= groundStorageProperties.StackingCapacity && groundStorageProperties.Layout == EnumGroundStorageLayout.Stacking)
			{
				BlockEntityGroundStorage blockEntity3 = Api.World.BlockAccessor.GetBlockEntity<BlockEntityGroundStorage>(Pos);
				blockEntity3.DetermineStorageProperties(new DummySlot(itemStack));
				blockEntity3.Inventory[0].Itemstack = itemStack.Clone();
				blockEntity3.Inventory[0].MarkDirty();
				return;
			}
		}
		if (itemStack.StackSize == 1 && itemStack.Class == EnumItemClass.Block)
		{
			Api.World.BlockAccessor.SetBlock(itemStack.Block.BlockId, Pos);
			return;
		}
		int num = 500;
		while (itemStack.StackSize > 0 && num-- > 0)
		{
			ItemStack itemStack2 = itemStack.Clone();
			itemStack2.StackSize = Math.Min(itemStack.StackSize, itemStack.Collectible.MaxStackSize);
			itemStack.StackSize -= itemStack2.StackSize;
			TreeAttribute treeAttribute = new TreeAttribute();
			treeAttribute["itemstack"] = new ItemstackAttribute(itemStack2);
			treeAttribute["byentityid"] = new LongAttribute(byPlayer.Entity.EntityId);
			Api.Event.PushEvent("onitemclayformed", treeAttribute);
			if (byPlayer.InventoryManager.TryGiveItemstack(itemStack2))
			{
				Api.World.PlaySoundAt(new AssetLocation("sounds/player/collect"), byPlayer);
			}
			else
			{
				Api.World.SpawnItemEntity(itemStack2, Pos);
			}
		}
		if (num <= 1)
		{
			Api.World.Logger.Error("Tried to drop finished clay forming item but failed after 500 times?! Gave up doing so. Out stack was " + itemStack);
		}
		Api.World.BlockAccessor.SetBlock(0, Pos);
	}

	private bool MatchesRecipe(int layer)
	{
		if (SelectedRecipe == null)
		{
			return false;
		}
		return NextNotMatchingRecipeLayer(layer) >= SelectedRecipe.Pattern.Length;
	}

	private int NextNotMatchingRecipeLayer(int layerStart = 0)
	{
		if (SelectedRecipe == null)
		{
			return 0;
		}
		if (layerStart < 0)
		{
			return 0;
		}
		bool[,,] voxels = SelectedRecipe.Voxels;
		for (int i = layerStart; i < 16; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				for (int k = 0; k < 16; k++)
				{
					if (Voxels[j, i, k] != voxels[j, i, k])
					{
						return i;
					}
				}
			}
		}
		return 16;
	}

	private Cuboidi LayerBounds(int layer)
	{
		Cuboidi cuboidi = new Cuboidi(8, 8, 8, 8, 8, 8);
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				if (SelectedRecipe.Voxels[i, layer, j])
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
				for (int k = 0; k < 16; k++)
				{
					if (Voxels[i, j, k])
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	[Obsolete("retained only for mod compatibility, for performance please cache the bounds and use the other overload")]
	public bool InBounds(Vec3i voxelPos, int layer)
	{
		if (layer < 0 || layer >= 16)
		{
			return false;
		}
		Cuboidi bounds = LayerBounds(layer);
		return InBounds(voxelPos, bounds);
	}

	public bool InBounds(Vec3i voxelPos, Cuboidi bounds)
	{
		if (voxelPos.X >= bounds.X1 && voxelPos.X <= bounds.X2 && voxelPos.Y >= 0 && voxelPos.Y < 16 && voxelPos.Z >= bounds.Z1)
		{
			return voxelPos.Z <= bounds.Z2;
		}
		return false;
	}

	private bool OnRemove(int layer, Vec3i voxelPos, BlockFacing facing, int radius)
	{
		bool result = false;
		if (voxelPos.Y != layer)
		{
			return result;
		}
		if (layer < 0 || layer >= 16)
		{
			return result;
		}
		Vec3i vec3i = voxelPos.Clone();
		for (int i = -(int)Math.Ceiling((float)radius / 2f); i <= radius / 2; i++)
		{
			vec3i.X = voxelPos.X + i;
			if (vec3i.X < 0 || vec3i.X >= 16)
			{
				continue;
			}
			for (int j = -(int)Math.Ceiling((float)radius / 2f); j <= radius / 2; j++)
			{
				vec3i.Z = voxelPos.Z + j;
				if (vec3i.Z >= 0 && vec3i.Z < 16 && Voxels[vec3i.X, vec3i.Y, vec3i.Z])
				{
					result = true;
					Voxels[vec3i.X, vec3i.Y, vec3i.Z] = false;
					AvailableVoxels++;
				}
			}
		}
		return result;
	}

	private bool OnCopyLayer(int layer)
	{
		if (layer <= 0 || layer > 15)
		{
			return false;
		}
		bool result = false;
		int num = 4;
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				if (Voxels[i, layer - 1, j] && !Voxels[i, layer, j])
				{
					num--;
					Voxels[i, layer, j] = true;
					AvailableVoxels--;
					result = true;
				}
				if (num == 0)
				{
					return result;
				}
			}
		}
		return result;
	}

	private bool OnAdd(int layer, Vec3i voxelPos, BlockFacing facing, int radius)
	{
		if (voxelPos.Y == layer && facing.IsVertical)
		{
			return OnAdd(layer, voxelPos, radius);
		}
		if (Voxels[voxelPos.X, voxelPos.Y, voxelPos.Z])
		{
			Vec3i voxelPos2 = voxelPos.AddCopy(facing);
			if (layer >= 0 && layer < 16 && InBounds(voxelPos2, LayerBounds(layer)))
			{
				return OnAdd(layer, voxelPos2, radius);
			}
			return false;
		}
		return OnAdd(layer, voxelPos, radius);
	}

	private bool OnAdd(int layer, Vec3i voxelPos, int radius)
	{
		bool result = false;
		if (voxelPos.Y != layer)
		{
			return result;
		}
		if (layer < 0 || layer >= 16)
		{
			return result;
		}
		Cuboidi bounds = LayerBounds(layer);
		Vec3i vec3i = voxelPos.Clone();
		for (int i = -(int)Math.Ceiling((float)radius / 2f); i <= radius / 2; i++)
		{
			vec3i.X = voxelPos.X + i;
			for (int j = -(int)Math.Ceiling((float)radius / 2f); j <= radius / 2; j++)
			{
				vec3i.Z = voxelPos.Z + j;
				if (InBounds(vec3i, bounds) && !Voxels[vec3i.X, vec3i.Y, vec3i.Z])
				{
					AvailableVoxels--;
					result = true;
					Voxels[vec3i.X, vec3i.Y, vec3i.Z] = true;
				}
			}
		}
		return result;
	}

	private void RegenMeshAndSelectionBoxes(int layer)
	{
		if (workitemRenderer != null && layer != 16)
		{
			workitemRenderer.RegenMesh(workItemStack, Voxels, SelectedRecipe, layer);
		}
		List<Cuboidf> list = new List<Cuboidf>();
		bool[,,] array = SelectedRecipe?.Voxels;
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				for (int k = 0; k < 16; k++)
				{
					if (j == 0 || Voxels[i, j, k] || (array != null && j == layer && array[i, j, k]))
					{
						list.Add(new Cuboidf((float)i / 16f, (float)j / 16f, (float)k / 16f, (float)i / 16f + 0.0625f, (float)j / 16f + 0.0625f, (float)k / 16f + 0.0625f));
					}
				}
			}
		}
		selectionBoxes = list.ToArray();
	}

	public void CreateInitialWorkItem()
	{
		Voxels = new bool[16, 16, 16];
		for (int i = 4; i < 12; i++)
		{
			for (int j = 4; j < 12; j++)
			{
				Voxels[i, 0, j] = true;
			}
		}
	}

	private void RegenMeshForNextLayer()
	{
		int layer = NextNotMatchingRecipeLayer();
		RegenMeshAndSelectionBoxes(layer);
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		dlg?.TryClose();
		if (workitemRenderer != null)
		{
			workitemRenderer.Dispose();
			workitemRenderer = null;
		}
		if (Api is ICoreClientAPI coreClientAPI)
		{
			coreClientAPI.Event.ColorsPresetChanged -= RegenMeshForNextLayer;
		}
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		bool flag = deserializeVoxels(tree.GetBytes("voxels"));
		workItemStack = tree.GetItemstack("workItemStack");
		baseMaterial = tree.GetItemstack("baseMaterial");
		AvailableVoxels = tree.GetInt("availableVoxels");
		setSelectedRecipe(tree.GetInt("selectedRecipeId", -1));
		if (Api != null && workItemStack != null)
		{
			workItemStack.ResolveBlockOrItem(Api.World);
			Item item = Api.World.GetItem(AssetLocation.Create("clay-" + workItemStack.Collectible.Variant["type"], workItemStack.Collectible.Code.Domain));
			if (item == null)
			{
				Api.World.Logger.Notification("Clay form base mat is null! Clay form @ {0}/{1}/{2} corrupt. Will reset to blue clay", Pos.X, Pos.Y, Pos.Z);
				item = Api.World.GetItem(new AssetLocation("clay-blue"));
			}
			baseMaterial = new ItemStack(item);
		}
		if (flag)
		{
			RegenMeshForNextLayer();
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetBytes("voxels", serializeVoxels());
		tree.SetItemstack("workItemStack", workItemStack);
		tree.SetItemstack("baseMaterial", baseMaterial);
		tree.SetInt("availableVoxels", AvailableVoxels);
		tree.SetInt("selectedRecipeId", selectedRecipeId);
	}

	private byte[] serializeVoxels()
	{
		byte[] array = new byte[512];
		int num = 0;
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				for (int k = 0; k < 16; k++)
				{
					int num2 = num % 8;
					array[num / 8] |= (byte)((Voxels[i, j, k] ? 1u : 0u) << num2);
					num++;
				}
			}
		}
		return array;
	}

	private bool deserializeVoxels(byte[] data)
	{
		if (data == null || data.Length < 512)
		{
			Voxels = new bool[16, 16, 16];
			return true;
		}
		if (Voxels == null)
		{
			Voxels = new bool[16, 16, 16];
		}
		int num = 0;
		bool flag = false;
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				for (int k = 0; k < 16; k++)
				{
					int num2 = num % 8;
					bool flag2 = (data[num / 8] & (1 << num2)) > 0;
					flag |= Voxels[i, j, k] != flag2;
					Voxels[i, j, k] = flag2;
					num++;
				}
			}
		}
		return flag;
	}

	protected void setSelectedRecipe(int newId)
	{
		if (selectedRecipeId == newId && (selectedRecipe != null || newId < 0))
		{
			return;
		}
		if (newId == -1)
		{
			selectedRecipe = null;
		}
		else
		{
			selectedRecipe = ((Api != null) ? Api.GetClayformingRecipes().FirstOrDefault((ClayFormingRecipe r) => r.RecipeId == newId) : null);
		}
		selectedRecipeId = newId;
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
			if (baseMaterial != null)
			{
				Api.World.SpawnItemEntity(baseMaterial, Pos);
			}
			Api.World.BlockAccessor.SetBlock(0, Pos);
		}
		if (packetid == 1001)
		{
			int recipeid = SerializerUtil.Deserialize<int>(data);
			ClayFormingRecipe clayFormingRecipe = Api.GetClayformingRecipes().FirstOrDefault((ClayFormingRecipe r) => r.RecipeId == recipeid);
			if (clayFormingRecipe == null)
			{
				Api.World.Logger.Error("Client tried to selected clayforming recipe with id {0}, but no such recipe exists!");
				selectedRecipe = null;
				selectedRecipeId = -1;
				return;
			}
			selectedRecipe = clayFormingRecipe;
			selectedRecipeId = clayFormingRecipe.RecipeId;
			MarkDirty();
			Api.World.BlockAccessor.GetChunkAtBlockPos(Pos).MarkModified();
		}
		if (packetid == 1002)
		{
			Vec3i voxelPos;
			bool mouseBreakMode;
			BlockFacing facing;
			using (MemoryStream input = new MemoryStream(data))
			{
				BinaryReader binaryReader = new BinaryReader(input);
				voxelPos = new Vec3i(binaryReader.ReadInt32(), binaryReader.ReadInt32(), binaryReader.ReadInt32());
				mouseBreakMode = binaryReader.ReadBoolean();
				facing = BlockFacing.ALLFACES[binaryReader.ReadInt16()];
			}
			Api.World.FrameProfiler.Enter("clayforming");
			OnUseOver(player, voxelPos, facing, mouseBreakMode);
			Api.World.FrameProfiler.Leave();
		}
	}

	public void OpenDialog(IClientWorldAccessor world, BlockPos pos, ItemStack ingredient)
	{
		if (dlg != null && dlg.IsOpened())
		{
			return;
		}
		if (ingredient.Collectible is ItemWorkItem)
		{
			ingredient = new ItemStack(world.GetItem(AssetLocation.Create("clay-" + ingredient.Collectible.Variant["type"], ingredient.Collectible.Code.Domain)));
		}
		List<ClayFormingRecipe> recipes = (from r in Api.GetClayformingRecipes()
			where r.Ingredient.SatisfiesAsIngredient(ingredient)
			orderby r.Output.ResolvedItemstack.Collectible.Code
			select r).ToList();
		List<ItemStack> list = recipes.Select((ClayFormingRecipe r) => r.Output.ResolvedItemstack).ToList();
		ICoreClientAPI capi = Api as ICoreClientAPI;
		dlg = new GuiDialogBlockEntityRecipeSelector(Lang.Get("Select recipe"), list.ToArray(), delegate(int selectedIndex)
		{
			capi.Logger.VerboseDebug("Select clay from recipe {0}, have {1} recipes.", selectedIndex, recipes.Count);
			selectedRecipe = recipes[selectedIndex];
			selectedRecipeId = selectedRecipe.RecipeId;
			capi.Network.SendBlockEntityPacket(pos, 1001, SerializerUtil.Serialize(recipes[selectedIndex].RecipeId));
			RegenMeshForNextLayer();
		}, delegate
		{
			capi.Network.SendBlockEntityPacket(pos, 1003);
		}, pos, Api as ICoreClientAPI);
		for (int num = 0; num < recipes.Count; num++)
		{
			ItemStack[] array = new ItemStack[1] { ingredient.GetEmptyClone() };
			array[0].StackSize = (int)Math.Ceiling(GameMath.Max(1f, (float)(recipes[num].Voxels.Cast<bool>().Count((bool voxel) => voxel) - 64) / 25f));
			(dlg as GuiDialogBlockEntityRecipeSelector).SetIngredientCounts(num, array);
		}
		dlg.OnClosed += dlg.Dispose;
		dlg.TryOpen();
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		if (workItemStack != null && SelectedRecipe != null)
		{
			dsc.AppendLine(Lang.Get("Output: {0}", SelectedRecipe?.Output?.ResolvedItemstack?.GetName()));
			dsc.AppendLine(Lang.Get("Available Voxels: {0}", AvailableVoxels));
		}
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		workitemRenderer?.Dispose();
		if (Api is ICoreClientAPI coreClientAPI)
		{
			coreClientAPI.Event.ColorsPresetChanged -= RegenMeshForNextLayer;
		}
	}

	public override void OnStoreCollectibleMappings(Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
	{
		workItemStack?.Collectible.OnStoreCollectibleMappings(Api.World, new DummySlot(workItemStack), blockIdMapping, itemIdMapping);
		baseMaterial?.Collectible.OnStoreCollectibleMappings(Api.World, new DummySlot(baseMaterial), blockIdMapping, itemIdMapping);
	}

	public override void OnLoadCollectibleMappings(IWorldAccessor worldForResolve, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed, bool resolveImports)
	{
		ItemStack itemStack = workItemStack;
		if (itemStack != null && !itemStack.FixMapping(oldBlockIdMapping, oldItemIdMapping, worldForResolve))
		{
			workItemStack = null;
		}
		ItemStack itemStack2 = baseMaterial;
		if (itemStack2 != null && !itemStack2.FixMapping(oldBlockIdMapping, oldItemIdMapping, worldForResolve))
		{
			baseMaterial = null;
		}
		workItemStack?.Collectible.OnLoadCollectibleMappings(worldForResolve, new DummySlot(workItemStack), oldBlockIdMapping, oldItemIdMapping, resolveImports);
		baseMaterial?.Collectible.OnLoadCollectibleMappings(worldForResolve, new DummySlot(baseMaterial), oldBlockIdMapping, oldItemIdMapping, resolveImports);
	}
}
