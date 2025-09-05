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

public class BlockEntityAnvil : BlockEntity, IRotatable, ITemperatureSensitive
{
	public static SimpleParticleProperties bigMetalSparks;

	public static SimpleParticleProperties smallMetalSparks;

	public static SimpleParticleProperties slagPieces;

	private ItemStack workItemStack;

	public int SelectedRecipeId = -1;

	public byte[,,] Voxels = new byte[16, 6, 16];

	private float voxYOff = 0.625f;

	private Cuboidf[] selectionBoxes = new Cuboidf[1];

	public int OwnMetalTier;

	private AnvilWorkItemRenderer workitemRenderer;

	public int rotation;

	public float MeshAngle;

	private MeshData currentMesh;

	private GuiDialog dlg;

	private ItemStack returnOnCancelStack;

	private static int bitsPerByte;

	private static int partsPerByte;

	public bool[,,] recipeVoxels
	{
		get
		{
			if (SelectedRecipe == null)
			{
				return null;
			}
			bool[,,] array = SelectedRecipe.Voxels;
			bool[,,] array2 = new bool[array.GetLength(0), array.GetLength(1), array.GetLength(2)];
			if (rotation == 0)
			{
				return array;
			}
			for (int i = 0; i < rotation / 90; i++)
			{
				for (int j = 0; j < array.GetLength(0); j++)
				{
					for (int k = 0; k < array.GetLength(1); k++)
					{
						for (int l = 0; l < array.GetLength(2); l++)
						{
							array2[l, k, j] = array[16 - j - 1, k, l];
						}
					}
				}
				array = (bool[,,])array2.Clone();
			}
			return array2;
		}
	}

	public SmithingRecipe SelectedRecipe => Api.GetSmithingRecipes().FirstOrDefault((SmithingRecipe r) => r.RecipeId == SelectedRecipeId);

	public bool CanWorkCurrent
	{
		get
		{
			if (workItemStack != null)
			{
				return workItemStack.Collectible.GetCollectibleInterface<IAnvilWorkable>().CanWork(WorkItemStack);
			}
			return false;
		}
	}

	public ItemStack WorkItemStack => workItemStack;

	public bool IsHot => (workItemStack?.Collectible.GetTemperature(Api.World, workItemStack) ?? 0f) > 20f;

	static BlockEntityAnvil()
	{
		bitsPerByte = 2;
		partsPerByte = 8 / bitsPerByte;
		smallMetalSparks = new SimpleParticleProperties(2f, 5f, ColorUtil.ToRgba(255, 255, 233, 83), new Vec3d(), new Vec3d(), new Vec3f(-3f, 8f, -3f), new Vec3f(3f, 12f, 3f), 0.1f, 1f, 0.25f, 0.25f, EnumParticleModel.Quad);
		smallMetalSparks.VertexFlags = 128;
		smallMetalSparks.AddPos.Set(0.0625, 0.0, 0.0625);
		smallMetalSparks.ParticleModel = EnumParticleModel.Quad;
		smallMetalSparks.LifeLength = 0.03f;
		smallMetalSparks.MinVelocity = new Vec3f(-2f, 1f, -2f);
		smallMetalSparks.AddVelocity = new Vec3f(4f, 2f, 4f);
		smallMetalSparks.MinQuantity = 6f;
		smallMetalSparks.AddQuantity = 12f;
		smallMetalSparks.MinSize = 0.1f;
		smallMetalSparks.MaxSize = 0.1f;
		smallMetalSparks.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.1f);
		bigMetalSparks = new SimpleParticleProperties(4f, 8f, ColorUtil.ToRgba(255, 255, 233, 83), new Vec3d(), new Vec3d(), new Vec3f(-1f, 1f, -1f), new Vec3f(2f, 4f, 2f), 0.5f, 1f, 0.25f, 0.25f);
		bigMetalSparks.VertexFlags = 128;
		bigMetalSparks.AddPos.Set(0.0625, 0.0, 0.0625);
		bigMetalSparks.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.25f);
		bigMetalSparks.Bounciness = 1f;
		bigMetalSparks.addLifeLength = 2f;
		bigMetalSparks.GreenEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -233f);
		bigMetalSparks.BlueEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -83f);
		slagPieces = new SimpleParticleProperties(2f, 12f, ColorUtil.ToRgba(255, 255, 233, 83), new Vec3d(), new Vec3d(), new Vec3f(-1f, 0.5f, -1f), new Vec3f(2f, 1.5f, 2f), 0.5f, 1f, 0.25f, 0.5f);
		slagPieces.AddPos.Set(0.0625, 0.0, 0.0625);
		slagPieces.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.25f);
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		workItemStack?.ResolveBlockOrItem(api.World);
		if (api is ICoreClientAPI coreClientAPI)
		{
			coreClientAPI.Event.RegisterRenderer(workitemRenderer = new AnvilWorkItemRenderer(this, Pos, coreClientAPI), EnumRenderStage.Opaque);
			coreClientAPI.Event.RegisterRenderer(workitemRenderer, EnumRenderStage.AfterFinalComposition);
			RegenMeshAndSelectionBoxes();
			coreClientAPI.Tesselator.TesselateBlock(base.Block, out currentMesh);
			coreClientAPI.Event.ColorsPresetChanged += RegenMeshAndSelectionBoxes;
		}
		string key = base.Block.Variant["metal"];
		if (api.ModLoader.GetModSystem<SurvivalCoreSystem>().metalsByCode.TryGetValue(key, out var value))
		{
			OwnMetalTier = value.Tier;
		}
	}

	internal Cuboidf[] GetSelectionBoxes(IBlockAccessor world, BlockPos pos)
	{
		return selectionBoxes;
	}

	internal bool OnPlayerInteract(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		ItemStack itemstack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
		if (itemstack != null && itemstack.Collectible.Tool == EnumTool.Hammer)
		{
			return RotateWorkItem(byPlayer.Entity.Controls.ShiftKey);
		}
		if (byPlayer.Entity.Controls.ShiftKey)
		{
			return TryPut(world, byPlayer, blockSel);
		}
		return TryTake(world, byPlayer, blockSel);
	}

	private bool RotateWorkItem(bool ccw)
	{
		byte[,,] array = new byte[16, 6, 16];
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 6; j++)
			{
				for (int k = 0; k < 16; k++)
				{
					if (ccw)
					{
						array[k, j, i] = Voxels[i, j, 16 - k - 1];
					}
					else
					{
						array[k, j, i] = Voxels[16 - i - 1, j, k];
					}
				}
			}
		}
		rotation = (rotation + (ccw ? 270 : 90)) % 360;
		Voxels = array;
		RegenMeshAndSelectionBoxes();
		MarkDirty();
		return true;
	}

	private bool TryTake(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (workItemStack == null)
		{
			return false;
		}
		ditchWorkItemStack(byPlayer);
		return true;
	}

	private bool TryPut(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
		if (activeHotbarSlot.Itemstack == null)
		{
			return false;
		}
		ItemStack itemstack = activeHotbarSlot.Itemstack;
		IAnvilWorkable collectibleInterface = itemstack.Collectible.GetCollectibleInterface<IAnvilWorkable>();
		if (collectibleInterface == null)
		{
			return false;
		}
		int requiredAnvilTier = collectibleInterface.GetRequiredAnvilTier(itemstack);
		if (requiredAnvilTier > OwnMetalTier)
		{
			if (world.Side == EnumAppSide.Client)
			{
				(Api as ICoreClientAPI).TriggerIngameError(this, "toolowtier", Lang.Get("Working this metal needs a tier {0} anvil", requiredAnvilTier));
			}
			return false;
		}
		ItemStack itemStack = collectibleInterface.TryPlaceOn(itemstack, this);
		if (itemStack != null)
		{
			if (workItemStack == null)
			{
				workItemStack = itemStack;
				rotation = workItemStack.Attributes.GetInt("rotation");
			}
			else if (workItemStack.Collectible is ItemWorkItem { isBlisterSteel: not false })
			{
				return false;
			}
			if (SelectedRecipeId < 0)
			{
				List<SmithingRecipe> matchingRecipes = collectibleInterface.GetMatchingRecipes(itemstack);
				if (matchingRecipes.Count == 1)
				{
					SelectedRecipeId = matchingRecipes[0].RecipeId;
				}
				else if (world.Side == EnumAppSide.Client)
				{
					OpenDialog(itemstack);
				}
			}
			returnOnCancelStack = activeHotbarSlot.TakeOut(1);
			activeHotbarSlot.MarkDirty();
			Api.World.Logger.Audit("{0} Put 1x{1} on to Anvil at {2}.", byPlayer?.PlayerName, itemStack.Collectible.Code, Pos);
			if (Api.Side == EnumAppSide.Server)
			{
				RegenMeshAndSelectionBoxes();
			}
			CheckIfFinished(byPlayer);
			MarkDirty();
			return true;
		}
		return false;
	}

	internal void OnBeginUse(IPlayer byPlayer, BlockSelection blockSel)
	{
	}

	internal void OnUseOver(IPlayer byPlayer, int selectionBoxIndex)
	{
		if (selectionBoxIndex > 0 && selectionBoxIndex < selectionBoxes.Length)
		{
			Cuboidf cuboidf = selectionBoxes[selectionBoxIndex];
			Vec3i voxelPos = new Vec3i((int)(16f * cuboidf.X1), (int)(16f * cuboidf.Y1) - 10, (int)(16f * cuboidf.Z1));
			OnUseOver(byPlayer, voxelPos, new BlockSelection
			{
				Position = Pos,
				SelectionBoxIndex = selectionBoxIndex
			});
		}
	}

	internal void OnUseOver(IPlayer byPlayer, Vec3i voxelPos, BlockSelection blockSel)
	{
		if (voxelPos == null)
		{
			return;
		}
		if (SelectedRecipe == null)
		{
			ditchWorkItemStack();
			return;
		}
		if (Api.Side == EnumAppSide.Client)
		{
			SendUseOverPacket(byPlayer, voxelPos);
		}
		ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
		if (activeHotbarSlot.Itemstack == null || !CanWorkCurrent)
		{
			return;
		}
		int toolMode = activeHotbarSlot.Itemstack.Collectible.GetToolMode(activeHotbarSlot, byPlayer, blockSel);
		float num = GameMath.Mod(byPlayer.Entity.Pos.Yaw, (float)Math.PI * 2f);
		EnumVoxelMaterial enumVoxelMaterial = (EnumVoxelMaterial)Voxels[voxelPos.X, voxelPos.Y, voxelPos.Z];
		if (enumVoxelMaterial != EnumVoxelMaterial.Empty)
		{
			spawnParticles(voxelPos, enumVoxelMaterial, byPlayer);
			switch (toolMode)
			{
			case 0:
				OnHit(voxelPos);
				break;
			case 1:
				OnUpset(voxelPos, BlockFacing.NORTH.FaceWhenRotatedBy(0f, num - (float)Math.PI, 0f));
				break;
			case 2:
				OnUpset(voxelPos, BlockFacing.EAST.FaceWhenRotatedBy(0f, num - (float)Math.PI, 0f));
				break;
			case 3:
				OnUpset(voxelPos, BlockFacing.SOUTH.FaceWhenRotatedBy(0f, num - (float)Math.PI, 0f));
				break;
			case 4:
				OnUpset(voxelPos, BlockFacing.WEST.FaceWhenRotatedBy(0f, num - (float)Math.PI, 0f));
				break;
			case 5:
				OnSplit(voxelPos);
				break;
			}
			RegenMeshAndSelectionBoxes();
			Api.World.BlockAccessor.MarkBlockDirty(Pos);
			Api.World.BlockAccessor.MarkBlockEntityDirty(Pos);
			activeHotbarSlot.Itemstack.Collectible.DamageItem(Api.World, byPlayer.Entity, activeHotbarSlot);
			if (!HasAnyMetalVoxel())
			{
				clearWorkSpace();
				return;
			}
		}
		CheckIfFinished(byPlayer);
		MarkDirty();
	}

	private void spawnParticles(Vec3i voxelPos, EnumVoxelMaterial voxelMat, IPlayer byPlayer)
	{
		float temperature = workItemStack.Collectible.GetTemperature(Api.World, workItemStack);
		if (voxelMat == EnumVoxelMaterial.Metal && temperature > 800f)
		{
			bigMetalSparks.MinPos = Pos.ToVec3d().AddCopy((float)voxelPos.X / 16f, voxYOff + (float)voxelPos.Y / 16f + 0.0625f, (float)voxelPos.Z / 16f);
			bigMetalSparks.AddPos.Set(0.0625, 0.0, 0.0625);
			bigMetalSparks.VertexFlags = (byte)GameMath.Clamp((int)(temperature - 700f) / 2, 32, 128);
			Api.World.SpawnParticles(bigMetalSparks, byPlayer);
			smallMetalSparks.MinPos = Pos.ToVec3d().AddCopy((float)voxelPos.X / 16f, voxYOff + (float)voxelPos.Y / 16f + 0.0625f, (float)voxelPos.Z / 16f);
			smallMetalSparks.VertexFlags = (byte)GameMath.Clamp((int)(temperature - 770f) / 3, 32, 128);
			smallMetalSparks.AddPos.Set(0.0625, 0.0, 0.0625);
			Api.World.SpawnParticles(smallMetalSparks, byPlayer);
		}
		if (voxelMat == EnumVoxelMaterial.Slag)
		{
			slagPieces.Color = workItemStack.Collectible.GetRandomColor(Api as ICoreClientAPI, workItemStack);
			slagPieces.MinPos = Pos.ToVec3d().AddCopy((float)voxelPos.X / 16f, voxYOff + (float)voxelPos.Y / 16f + 0.0625f, (float)voxelPos.Z / 16f);
			slagPieces.ColorByItem = workItemStack.Item;
			Api.World.SpawnParticles(slagPieces, byPlayer);
		}
	}

	internal string PrintDebugText()
	{
		SmithingRecipe selectedRecipe = SelectedRecipe;
		EnumHelveWorkableMode? enumHelveWorkableMode = workItemStack?.Collectible.GetCollectibleInterface<IAnvilWorkable>()?.GetHelveWorkableMode(workItemStack, this);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("Workitem: " + workItemStack);
		stringBuilder.AppendLine("Recipe: " + selectedRecipe?.Name);
		stringBuilder.AppendLine("Matches recipe: " + MatchesRecipe());
		EnumHelveWorkableMode? enumHelveWorkableMode2 = enumHelveWorkableMode;
		stringBuilder.AppendLine("Helve Workable: " + enumHelveWorkableMode2.ToString());
		return stringBuilder.ToString();
	}

	public virtual void OnHelveHammerHit()
	{
		if (workItemStack == null || !CanWorkCurrent)
		{
			return;
		}
		SmithingRecipe selectedRecipe = SelectedRecipe;
		if (selectedRecipe == null)
		{
			return;
		}
		EnumHelveWorkableMode? enumHelveWorkableMode = workItemStack.Collectible.GetCollectibleInterface<IAnvilWorkable>()?.GetHelveWorkableMode(workItemStack, this);
		if (enumHelveWorkableMode == EnumHelveWorkableMode.NotWorkable)
		{
			return;
		}
		rotation = 0;
		int quantityLayers = selectedRecipe.QuantityLayers;
		if (enumHelveWorkableMode == EnumHelveWorkableMode.TestSufficientVoxelsWorkable)
		{
			Vec3i vec3i = findFreeMetalVoxel();
			for (int i = 0; i < 16; i++)
			{
				for (int j = 0; j < 16; j++)
				{
					for (int k = 0; k < 6; k++)
					{
						bool flag = k < quantityLayers && selectedRecipe.Voxels[i, k, j];
						EnumVoxelMaterial enumVoxelMaterial = (EnumVoxelMaterial)Voxels[i, k, j];
						if (enumVoxelMaterial == EnumVoxelMaterial.Slag)
						{
							Voxels[i, k, j] = 0;
							onHelveHitSuccess(enumVoxelMaterial, null, i, k, j);
							return;
						}
						if (flag && vec3i != null && enumVoxelMaterial == EnumVoxelMaterial.Empty)
						{
							Voxels[i, k, j] = 1;
							Voxels[vec3i.X, vec3i.Y, vec3i.Z] = 0;
							onHelveHitSuccess(enumVoxelMaterial, vec3i, i, k, j);
							return;
						}
					}
				}
			}
			if (vec3i != null)
			{
				Voxels[vec3i.X, vec3i.Y, vec3i.Z] = 0;
				onHelveHitSuccess(EnumVoxelMaterial.Metal, null, vec3i.X, vec3i.Y, vec3i.Z);
			}
			return;
		}
		for (int num = 5; num >= 0; num--)
		{
			for (int l = 0; l < 16; l++)
			{
				for (int m = 0; m < 16; m++)
				{
					bool flag2 = num < quantityLayers && selectedRecipe.Voxels[m, num, l];
					EnumVoxelMaterial enumVoxelMaterial2 = (EnumVoxelMaterial)Voxels[m, num, l];
					if ((!flag2 || enumVoxelMaterial2 != EnumVoxelMaterial.Metal) && (flag2 || enumVoxelMaterial2 != EnumVoxelMaterial.Empty))
					{
						if (flag2 && enumVoxelMaterial2 == EnumVoxelMaterial.Empty)
						{
							Voxels[m, num, l] = 1;
						}
						else
						{
							Voxels[m, num, l] = 0;
						}
						onHelveHitSuccess((enumVoxelMaterial2 == EnumVoxelMaterial.Empty) ? EnumVoxelMaterial.Metal : enumVoxelMaterial2, null, m, num, l);
						return;
					}
				}
			}
		}
	}

	private void onHelveHitSuccess(EnumVoxelMaterial mat, Vec3i usableMetalVoxel, int x, int y, int z)
	{
		if (Api.World.Side == EnumAppSide.Client)
		{
			spawnParticles(new Vec3i(x, y, z), (mat == EnumVoxelMaterial.Empty) ? EnumVoxelMaterial.Metal : mat, null);
			if (usableMetalVoxel != null)
			{
				spawnParticles(usableMetalVoxel, EnumVoxelMaterial.Metal, null);
			}
		}
		RegenMeshAndSelectionBoxes();
		CheckIfFinished(null);
	}

	private Vec3i findFreeMetalVoxel()
	{
		SmithingRecipe selectedRecipe = SelectedRecipe;
		int quantityLayers = selectedRecipe.QuantityLayers;
		for (int num = 5; num >= 0; num--)
		{
			for (int i = 0; i < 16; i++)
			{
				for (int j = 0; j < 16; j++)
				{
					bool num2 = num < quantityLayers && selectedRecipe.Voxels[j, num, i];
					EnumVoxelMaterial enumVoxelMaterial = (EnumVoxelMaterial)Voxels[j, num, i];
					if (!num2 && enumVoxelMaterial == EnumVoxelMaterial.Metal)
					{
						return new Vec3i(j, num, i);
					}
				}
			}
		}
		return null;
	}

	public virtual void CheckIfFinished(IPlayer byPlayer)
	{
		if (SelectedRecipe != null && MatchesRecipe() && Api.World is IServerWorldAccessor)
		{
			Voxels = new byte[16, 6, 16];
			ItemStack itemStack = SelectedRecipe.Output.ResolvedItemstack.Clone();
			itemStack.Collectible.SetTemperature(Api.World, itemStack, workItemStack.Collectible.GetTemperature(Api.World, workItemStack));
			workItemStack = null;
			SelectedRecipeId = -1;
			if (byPlayer != null && byPlayer.InventoryManager.TryGiveItemstack(itemStack))
			{
				Api.World.PlaySoundFor(new AssetLocation("sounds/player/collect"), byPlayer, randomizePitch: false, 24f);
			}
			else
			{
				Api.World.SpawnItemEntity(itemStack, Pos.ToVec3d().Add(0.5, 0.626, 0.5));
			}
			Api.World.Logger.Audit("{0} Took 1x{1} from Anvil at {2}.", byPlayer?.PlayerName, itemStack.Collectible.Code, Pos);
			RegenMeshAndSelectionBoxes();
			MarkDirty();
			Api.World.BlockAccessor.MarkBlockDirty(Pos);
			rotation = 0;
		}
	}

	public void ditchWorkItemStack(IPlayer byPlayer = null)
	{
		if (workItemStack == null)
		{
			return;
		}
		ItemStack itemStack;
		if (SelectedRecipe == null)
		{
			itemStack = returnOnCancelStack ?? workItemStack.Collectible.GetCollectibleInterface<IAnvilWorkable>().GetBaseMaterial(workItemStack);
			float temperature = workItemStack.Collectible.GetTemperature(Api.World, workItemStack);
			itemStack.Collectible.SetTemperature(Api.World, itemStack, temperature);
		}
		else
		{
			workItemStack.Attributes.SetBytes("voxels", serializeVoxels(Voxels));
			workItemStack.Attributes.SetInt("selectedRecipeId", SelectedRecipeId);
			workItemStack.Attributes.SetInt("rotation", rotation);
			if (workItemStack.Collectible is ItemIronBloom itemIronBloom)
			{
				workItemStack.Attributes.SetInt("hashCode", itemIronBloom.GetWorkItemHashCode(workItemStack));
			}
			itemStack = workItemStack;
		}
		if (byPlayer == null || !byPlayer.InventoryManager.TryGiveItemstack(itemStack))
		{
			Api.World.SpawnItemEntity(itemStack, Pos);
		}
		Api.World.Logger.Audit("{0} Took 1x{1} from Anvil at {2}.", byPlayer?.PlayerName, itemStack.Collectible.Code, Pos);
		clearWorkSpace();
	}

	protected void clearWorkSpace()
	{
		workItemStack = null;
		Voxels = new byte[16, 6, 16];
		RegenMeshAndSelectionBoxes();
		MarkDirty();
		rotation = 0;
		SelectedRecipeId = -1;
	}

	private bool MatchesRecipe()
	{
		if (SelectedRecipe == null)
		{
			return false;
		}
		int num = Math.Min(6, SelectedRecipe.QuantityLayers);
		bool[,,] array = recipeVoxels;
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < num; j++)
			{
				for (int k = 0; k < 16; k++)
				{
					byte b = (byte)(array[i, j, k] ? 1u : 0u);
					if (Voxels[i, j, k] != b)
					{
						return false;
					}
				}
			}
		}
		return true;
	}

	private bool HasAnyMetalVoxel()
	{
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 6; j++)
			{
				for (int k = 0; k < 16; k++)
				{
					if (Voxels[i, j, k] == 1)
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	public virtual void OnSplit(Vec3i voxelPos)
	{
		if (Voxels[voxelPos.X, voxelPos.Y, voxelPos.Z] == 2)
		{
			for (int i = -1; i <= 1; i++)
			{
				for (int j = -1; j <= 1; j++)
				{
					int num = voxelPos.X + i;
					int num2 = voxelPos.Z + j;
					if (num >= 0 && num2 >= 0 && num < 16 && num2 < 16 && Voxels[num, voxelPos.Y, num2] == 2)
					{
						Voxels[num, voxelPos.Y, num2] = 0;
					}
				}
			}
		}
		Voxels[voxelPos.X, voxelPos.Y, voxelPos.Z] = 0;
	}

	public virtual void OnUpset(Vec3i voxelPos, BlockFacing towardsFace)
	{
		if (Voxels[voxelPos.X, voxelPos.Y, voxelPos.Z] != 1 || (voxelPos.Y < 5 && Voxels[voxelPos.X, voxelPos.Y + 1, voxelPos.Z] != 0))
		{
			return;
		}
		Vec3i vec3i = voxelPos.Clone().Add(towardsFace);
		Vec3i normali = towardsFace.Opposite.Normali;
		if (vec3i.X < 0 || vec3i.X >= 16 || vec3i.Y < 0 || vec3i.Y >= 6 || vec3i.Z < 0 || vec3i.Z >= 16)
		{
			return;
		}
		if (voxelPos.Y > 0)
		{
			if (Voxels[vec3i.X, vec3i.Y, vec3i.Z] == 0 && Voxels[vec3i.X, vec3i.Y - 1, vec3i.Z] != 0)
			{
				if (vec3i.X >= 0 && vec3i.X < 16 && vec3i.Y >= 0 && vec3i.Y < 6 && vec3i.Z >= 0 && vec3i.Z < 16)
				{
					Voxels[vec3i.X, vec3i.Y, vec3i.Z] = 1;
					Voxels[voxelPos.X, voxelPos.Y, voxelPos.Z] = 0;
				}
				return;
			}
			vec3i.Y++;
			if (voxelPos.X + normali.X >= 0 && voxelPos.X + normali.X < 16 && voxelPos.Z + normali.Z >= 0 && voxelPos.Z + normali.Z < 16)
			{
				if (vec3i.Y < 6 && Voxels[vec3i.X, vec3i.Y, vec3i.Z] == 0 && Voxels[vec3i.X, vec3i.Y - 1, vec3i.Z] != 0 && Voxels[voxelPos.X + normali.X, voxelPos.Y, voxelPos.Z + normali.Z] == 0)
				{
					Voxels[vec3i.X, vec3i.Y, vec3i.Z] = 1;
					Voxels[voxelPos.X, voxelPos.Y, voxelPos.Z] = 0;
				}
				else if (!moveVoxelDownwards(voxelPos.Clone(), towardsFace, 1))
				{
					moveVoxelDownwards(voxelPos.Clone(), towardsFace, 2);
				}
			}
		}
		else
		{
			vec3i.Y++;
			if (vec3i.X >= 0 && vec3i.X < 16 && vec3i.Y >= 0 && vec3i.Y < 6 && vec3i.Z >= 0 && vec3i.Z < 16 && voxelPos.X + normali.X >= 0 && voxelPos.X + normali.X < 16 && voxelPos.Z + normali.Z >= 0 && voxelPos.Z + normali.Z < 16 && vec3i.Y < 6 && Voxels[vec3i.X, vec3i.Y, vec3i.Z] == 0 && Voxels[vec3i.X, vec3i.Y - 1, vec3i.Z] != 0 && Voxels[voxelPos.X + normali.X, voxelPos.Y, voxelPos.Z + normali.Z] == 0)
			{
				Voxels[vec3i.X, vec3i.Y, vec3i.Z] = 1;
				Voxels[voxelPos.X, voxelPos.Y, voxelPos.Z] = 0;
			}
		}
	}

	private Vec3i getClosestBfs(Vec3i voxelPos, BlockFacing towardsFace, int maxDist)
	{
		Queue<Vec3i> queue = new Queue<Vec3i>();
		HashSet<Vec3i> hashSet = new HashSet<Vec3i>();
		queue.Enqueue(voxelPos);
		while (queue.Count > 0)
		{
			Vec3i vec3i = queue.Dequeue();
			for (int i = 0; i < BlockFacing.HORIZONTALS.Length; i++)
			{
				BlockFacing towardsFace2 = BlockFacing.HORIZONTALS[i];
				Vec3i vec3i2 = vec3i.Clone().Add(towardsFace2);
				if (vec3i2.X < 0 || vec3i2.X >= 16 || vec3i2.Y < 0 || vec3i2.Y >= 6 || vec3i2.Z < 0 || vec3i2.Z >= 16 || hashSet.Contains(vec3i2))
				{
					continue;
				}
				hashSet.Add(vec3i2);
				double num = vec3i2.X - voxelPos.X;
				double num2 = vec3i2.Z - voxelPos.Z;
				double num3 = GameMath.Sqrt(num * num + num2 * num2);
				if (!(num3 > (double)maxDist))
				{
					num /= num3;
					num2 /= num3;
					if ((towardsFace == null || Math.Abs((float)Math.Acos((double)towardsFace.Normalf.X * num + (double)towardsFace.Normalf.Z * num2)) < 0.43633232f) && Voxels[vec3i2.X, vec3i2.Y, vec3i2.Z] == 0)
					{
						return vec3i2;
					}
					if (Voxels[vec3i2.X, vec3i2.Y, vec3i2.Z] == 1)
					{
						queue.Enqueue(vec3i2);
					}
				}
			}
		}
		return null;
	}

	public virtual void OnHit(Vec3i voxelPos)
	{
		if (Voxels[voxelPos.X, voxelPos.Y, voxelPos.Z] != 1 || voxelPos.Y <= 0)
		{
			return;
		}
		int num = 0;
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				if ((i != 0 || j != 0) && voxelPos.X + i >= 0 && voxelPos.X + i < 16 && voxelPos.Z + j >= 0 && voxelPos.Z + j < 16 && Voxels[voxelPos.X + i, voxelPos.Y, voxelPos.Z + j] == 1)
				{
					num += (moveVoxelDownwards(voxelPos.Clone().Add(i, 0, j), null, 1) ? 1 : 0);
				}
			}
		}
		if (Voxels[voxelPos.X, voxelPos.Y, voxelPos.Z] == 1)
		{
			num += (moveVoxelDownwards(voxelPos.Clone(), null, 1) ? 1 : 0);
		}
		if (num != 0)
		{
			return;
		}
		Vec3i vec3i = null;
		for (int k = -1; k <= 1; k++)
		{
			for (int l = -1; l <= 1; l++)
			{
				if ((k == 0 && l == 0) || voxelPos.X + 2 * k < 0 || voxelPos.X + 2 * k >= 16 || voxelPos.Z + 2 * l < 0 || voxelPos.Z + 2 * l >= 16)
				{
					continue;
				}
				bool flag = Voxels[voxelPos.X + 2 * k, voxelPos.Y, voxelPos.Z + 2 * l] == 0;
				if (Voxels[voxelPos.X + k, voxelPos.Y, voxelPos.Z + l] == 1 && flag)
				{
					Voxels[voxelPos.X + k, voxelPos.Y, voxelPos.Z + l] = 0;
					if (Voxels[voxelPos.X + 2 * k, voxelPos.Y - 1, voxelPos.Z + 2 * l] == 0)
					{
						Voxels[voxelPos.X + 2 * k, voxelPos.Y - 1, voxelPos.Z + 2 * l] = 1;
					}
					else
					{
						Voxels[voxelPos.X + 2 * k, voxelPos.Y, voxelPos.Z + 2 * l] = 1;
					}
				}
				else if (flag)
				{
					vec3i = voxelPos.Clone().Add(k, 0, l);
				}
			}
		}
		if (vec3i != null && Voxels[voxelPos.X, voxelPos.Y, voxelPos.Z] == 1)
		{
			Voxels[voxelPos.X, voxelPos.Y, voxelPos.Z] = 0;
			if (Voxels[vec3i.X, vec3i.Y - 1, vec3i.Z] == 0)
			{
				Voxels[vec3i.X, vec3i.Y - 1, vec3i.Z] = 1;
			}
			else
			{
				Voxels[vec3i.X, vec3i.Y, vec3i.Z] = 1;
			}
		}
	}

	protected bool moveVoxelDownwards(Vec3i voxelPos, BlockFacing towardsFace, int maxDist)
	{
		int y = voxelPos.Y;
		while (voxelPos.Y > 0)
		{
			voxelPos.Y--;
			Vec3i closestBfs = getClosestBfs(voxelPos, towardsFace, maxDist);
			if (closestBfs == null)
			{
				continue;
			}
			Voxels[voxelPos.X, y, voxelPos.Z] = 0;
			for (int i = 0; i <= closestBfs.Y; i++)
			{
				if (Voxels[closestBfs.X, i, closestBfs.Z] == 0)
				{
					Voxels[closestBfs.X, i, closestBfs.Z] = 1;
					return true;
				}
			}
			return true;
		}
		return false;
	}

	protected void RegenMeshAndSelectionBoxes()
	{
		if (workitemRenderer != null)
		{
			workitemRenderer.RegenMesh(workItemStack, Voxels, recipeVoxels);
		}
		List<Cuboidf> list = new List<Cuboidf>();
		list.Add(null);
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 6; j++)
			{
				for (int k = 0; k < 16; k++)
				{
					if (Voxels[i, j, k] != 0)
					{
						float num = j + 10;
						list.Add(new Cuboidf((float)i / 16f, num / 16f, (float)k / 16f, (float)i / 16f + 0.0625f, num / 16f + 0.0625f, (float)k / 16f + 0.0625f));
					}
				}
			}
		}
		selectionBoxes = list.ToArray();
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		workitemRenderer?.Dispose();
		workitemRenderer = null;
		if (Api is ICoreClientAPI coreClientAPI)
		{
			coreClientAPI.Event.ColorsPresetChanged -= RegenMeshAndSelectionBoxes;
		}
	}

	public override void OnBlockBroken(IPlayer byPlayer = null)
	{
		if (workItemStack != null)
		{
			workItemStack.Attributes.SetBytes("voxels", serializeVoxels(Voxels));
			workItemStack.Attributes.SetInt("selectedRecipeId", SelectedRecipeId);
			Api.World.SpawnItemEntity(workItemStack, Pos);
		}
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		Voxels = deserializeVoxels(tree.GetBytes("voxels"));
		workItemStack = tree.GetItemstack("workItemStack");
		SelectedRecipeId = tree.GetInt("selectedRecipeId", -1);
		rotation = tree.GetInt("rotation");
		if (Api != null && workItemStack != null)
		{
			workItemStack.ResolveBlockOrItem(Api.World);
		}
		RegenMeshAndSelectionBoxes();
		MeshAngle = tree.GetFloat("meshAngle", MeshAngle);
		ICoreAPI api = Api;
		if (api != null && api.Side == EnumAppSide.Client)
		{
			((ICoreClientAPI)Api).Tesselator.TesselateBlock(base.Block, out var modeldata);
			currentMesh = modeldata;
			MarkDirty(redrawOnClient: true);
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetBytes("voxels", serializeVoxels(Voxels));
		tree.SetItemstack("workItemStack", workItemStack);
		tree.SetInt("selectedRecipeId", SelectedRecipeId);
		tree.SetInt("rotation", rotation);
		tree.SetFloat("meshAngle", MeshAngle);
	}

	public static byte[] serializeVoxels(byte[,,] voxels)
	{
		byte[] array = new byte[1536 / partsPerByte];
		int num = 0;
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 6; j++)
			{
				for (int k = 0; k < 16; k++)
				{
					int num2 = bitsPerByte * (num % partsPerByte);
					array[num / partsPerByte] |= (byte)((voxels[i, j, k] & 3) << num2);
					num++;
				}
			}
		}
		return array;
	}

	public static byte[,,] deserializeVoxels(byte[] data)
	{
		byte[,,] array = new byte[16, 6, 16];
		if (data == null || data.Length < 1536 / partsPerByte)
		{
			return array;
		}
		int num = 0;
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 6; j++)
			{
				for (int k = 0; k < 16; k++)
				{
					int num2 = bitsPerByte * (num % partsPerByte);
					array[i, j, k] = (byte)((data[num / partsPerByte] >> num2) & 3);
					num++;
				}
			}
		}
		return array;
	}

	protected void SendUseOverPacket(IPlayer byPlayer, Vec3i voxelPos)
	{
		byte[] data;
		using (MemoryStream memoryStream = new MemoryStream())
		{
			BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
			binaryWriter.Write(voxelPos.X);
			binaryWriter.Write(voxelPos.Y);
			binaryWriter.Write(voxelPos.Z);
			data = memoryStream.ToArray();
		}
		((ICoreClientAPI)Api).Network.SendBlockEntityPacket(Pos, 1002, data);
	}

	public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
	{
		if (packetid == 1001)
		{
			int recipeid = SerializerUtil.Deserialize<int>(data);
			SmithingRecipe smithingRecipe = Api.GetSmithingRecipes().FirstOrDefault((SmithingRecipe r) => r.RecipeId == recipeid);
			if (smithingRecipe == null)
			{
				Api.World.Logger.Error("Client tried to selected smithing recipe with id {0}, but no such recipe exists!");
				ditchWorkItemStack(player);
				return;
			}
			List<SmithingRecipe> list = (WorkItemStack?.Collectible as ItemWorkItem)?.GetMatchingRecipes(workItemStack);
			if (list == null || list.FirstOrDefault((SmithingRecipe r) => r.RecipeId == recipeid) == null)
			{
				Api.World.Logger.Error("Client tried to selected smithing recipe with id {0}, but it is not a valid one for the given work item stack!", smithingRecipe.RecipeId);
				ditchWorkItemStack(player);
				return;
			}
			SelectedRecipeId = smithingRecipe.RecipeId;
			MarkDirty();
			Api.World.BlockAccessor.GetChunkAtBlockPos(Pos).MarkModified();
		}
		switch (packetid)
		{
		case 1003:
			ditchWorkItemStack(player);
			break;
		case 1002:
		{
			Vec3i voxelPos;
			using (MemoryStream input = new MemoryStream(data))
			{
				BinaryReader binaryReader = new BinaryReader(input);
				voxelPos = new Vec3i(binaryReader.ReadInt32(), binaryReader.ReadInt32(), binaryReader.ReadInt32());
			}
			OnUseOver(player, voxelPos, new BlockSelection
			{
				Position = Pos
			});
			break;
		}
		}
	}

	internal void OpenDialog(ItemStack ingredient)
	{
		IAnvilWorkable collectibleInterface = ingredient.Collectible.GetCollectibleInterface<IAnvilWorkable>();
		List<SmithingRecipe> recipes = collectibleInterface.GetMatchingRecipes(ingredient);
		List<ItemStack> list = recipes.Select((SmithingRecipe r) => r.Output.ResolvedItemstack).ToList();
		_ = (IClientWorldAccessor)Api.World;
		ICoreClientAPI capi = Api as ICoreClientAPI;
		dlg?.Dispose();
		dlg = new GuiDialogBlockEntityRecipeSelector(Lang.Get("Select smithing recipe"), list.ToArray(), delegate(int selectedIndex)
		{
			SelectedRecipeId = recipes[selectedIndex].RecipeId;
			capi.Network.SendBlockEntityPacket(Pos, 1001, SerializerUtil.Serialize(recipes[selectedIndex].RecipeId));
		}, delegate
		{
			capi.Network.SendBlockEntityPacket(Pos, 1003);
		}, Pos, Api as ICoreClientAPI);
		for (int num = 0; num < recipes.Count; num++)
		{
			ItemStack[] array = new ItemStack[1] { ingredient.GetEmptyClone() };
			array[0].StackSize = (int)Math.Ceiling((double)recipes[num].Voxels.Cast<bool>().Count((bool voxel) => voxel) / (double)collectibleInterface.VoxelCountForHandbook(ingredient));
			(dlg as GuiDialogBlockEntityRecipeSelector).SetIngredientCounts(num, array);
		}
		dlg.TryOpen();
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		dsc.AppendLine(Lang.Get("Tier {0} anvil", OwnMetalTier));
		if (workItemStack != null && SelectedRecipe != null)
		{
			float temperature = workItemStack.Collectible.GetTemperature(Api.World, workItemStack);
			dsc.AppendLine(Lang.Get("Output: {0}", SelectedRecipe.Output?.ResolvedItemstack?.GetName()));
			if (temperature < 25f)
			{
				dsc.AppendLine(Lang.Get("Temperature: Cold"));
			}
			else
			{
				dsc.AppendLine(Lang.Get("Temperature: {0}°C", (int)temperature));
			}
			if (!CanWorkCurrent)
			{
				dsc.AppendLine(Lang.Get("Too cold to work"));
			}
		}
	}

	public override void OnLoadCollectibleMappings(IWorldAccessor worldForResolve, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed, bool resolveImports)
	{
		ItemStack itemStack = workItemStack;
		if (itemStack != null && !itemStack.FixMapping(oldBlockIdMapping, oldItemIdMapping, worldForResolve))
		{
			workItemStack = null;
		}
		workItemStack?.Collectible.OnLoadCollectibleMappings(worldForResolve, new DummySlot(workItemStack), oldBlockIdMapping, oldItemIdMapping, resolveImports);
	}

	public override void OnStoreCollectibleMappings(Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
	{
		if (workItemStack != null)
		{
			if (workItemStack.Class == EnumItemClass.Item)
			{
				itemIdMapping[workItemStack.Id] = workItemStack.Item.Code;
			}
			else
			{
				blockIdMapping[workItemStack.Id] = workItemStack.Block.Code;
			}
			workItemStack.Collectible.OnStoreCollectibleMappings(Api.World, new DummySlot(workItemStack), blockIdMapping, itemIdMapping);
		}
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		workitemRenderer?.Dispose();
		dlg?.TryClose();
		dlg?.Dispose();
		if (Api is ICoreClientAPI coreClientAPI)
		{
			coreClientAPI.Event.ColorsPresetChanged -= RegenMeshAndSelectionBoxes;
		}
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
	{
		mesher.AddMeshData(currentMesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, MeshAngle, 0f));
		return true;
	}

	public void OnTransformed(IWorldAccessor worldAccessor, ITreeAttribute tree, int degreeRotation, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, EnumAxis? flipAxis)
	{
		MeshAngle = tree.GetFloat("meshAngle");
		MeshAngle -= (float)degreeRotation * ((float)Math.PI / 180f);
		tree.SetFloat("meshAngle", MeshAngle);
	}

	public void CoolNow(float amountRel)
	{
		if (workItemStack != null)
		{
			float temperature = workItemStack.Collectible.GetTemperature(Api.World, workItemStack);
			if (temperature > 120f)
			{
				Api.World.PlaySoundAt(new AssetLocation("sounds/effect/extinguish"), Pos, 0.25, null, randomizePitch: false, 16f);
			}
			workItemStack.Collectible.SetTemperature(Api.World, workItemStack, Math.Max(20f, temperature - amountRel * 20f), delayCooldown: false);
			MarkDirty(redrawOnClient: true);
		}
	}
}
