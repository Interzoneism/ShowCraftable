using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockPan : Block, ITexPositionSource, IContainedMeshSource
{
	private ITexPositionSource ownTextureSource;

	private TextureAtlasPosition matTexPosition;

	private ILoadedSound sound;

	private Dictionary<string, PanningDrop[]> dropsBySourceMat;

	private AssetLocation shapeEmpty;

	private AssetLocation shapeFull;

	private WorldInteraction[] interactions;

	public Size2i AtlasSize { get; set; }

	public TextureAtlasPosition this[string textureCode]
	{
		get
		{
			if (textureCode == "material")
			{
				return matTexPosition;
			}
			return ownTextureSource[textureCode];
		}
	}

	public override string GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity forEntity)
	{
		if (GetBlockMaterialCode(activeHotbarSlot.Itemstack) == null)
		{
			return null;
		}
		return base.GetHeldTpUseAnimation(activeHotbarSlot, forEntity);
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		dropsBySourceMat = Attributes["panningDrops"].AsObject<Dictionary<string, PanningDrop[]>>();
		shapeEmpty = Attributes["shapeEmpty"].AsObject((AssetLocation)"game:block/wood/pan/empty");
		shapeFull = Attributes["shapeFull"].AsObject((AssetLocation)"game:block/wood/pan/filled");
		shapeEmpty = shapeEmpty?.WithPathPrefix("shapes/")?.WithPathAppendix(".json");
		shapeFull = shapeFull?.WithPathPrefix("shapes/")?.WithPathAppendix(".json");
		if (!api.World.Config.GetAsBool("loreContent"))
		{
			string[] array = dropsBySourceMat.Keys.ToArray();
			foreach (string key in array)
			{
				PanningDrop[] array2 = dropsBySourceMat[key];
				List<PanningDrop> list = new List<PanningDrop>();
				PanningDrop[] array3 = array2;
				foreach (PanningDrop panningDrop in array3)
				{
					if (!panningDrop.ManMade)
					{
						list.Add(panningDrop);
					}
				}
				dropsBySourceMat[key] = list.ToArray();
			}
		}
		foreach (PanningDrop[] value in dropsBySourceMat.Values)
		{
			for (int k = 0; k < value.Length; k++)
			{
				if (!value[k].Code.Path.Contains("{rocktype}"))
				{
					value[k].Resolve(api.World, "panningdrop");
				}
			}
		}
		if (api.Side != EnumAppSide.Client)
		{
			return;
		}
		_ = api;
		interactions = ObjectCacheUtil.GetOrCreate(api, "panInteractions", delegate
		{
			List<ItemStack> list2 = new List<ItemStack>();
			foreach (Block block in api.World.Blocks)
			{
				if (!block.IsMissing && block.CreativeInventoryTabs != null && block.CreativeInventoryTabs.Length != 0 && IsPannableMaterial(block))
				{
					list2.Add(new ItemStack(block));
				}
			}
			ItemStack[] stacksArray = list2.ToArray();
			return new WorldInteraction[2]
			{
				new WorldInteraction
				{
					ActionLangCode = "heldhelp-addmaterialtopan",
					MouseButton = EnumMouseButton.Right,
					Itemstacks = list2.ToArray(),
					GetMatchingStacks = delegate
					{
						ItemStack itemstack = (api as ICoreClientAPI).World.Player.InventoryManager.ActiveHotbarSlot.Itemstack;
						return (GetBlockMaterialCode(itemstack) != null) ? null : stacksArray;
					}
				},
				new WorldInteraction
				{
					ActionLangCode = "heldhelp-pan",
					MouseButton = EnumMouseButton.Right,
					ShouldApply = delegate
					{
						ItemStack itemstack = (api as ICoreClientAPI).World.Player.InventoryManager.ActiveHotbarSlot.Itemstack;
						return GetBlockMaterialCode(itemstack) != null;
					}
				}
			};
		});
	}

	private ItemStack Resolve(EnumItemClass type, string code)
	{
		if (type == EnumItemClass.Block)
		{
			Block block = api.World.GetBlock(new AssetLocation(code));
			if (block == null)
			{
				api.World.Logger.Error("Failed resolving panning block drop with code {0}. Will skip.", code);
				return null;
			}
			return new ItemStack(block);
		}
		Item item = api.World.GetItem(new AssetLocation(code));
		if (item == null)
		{
			api.World.Logger.Error("Failed resolving panning item drop with code {0}. Will skip.", code);
			return null;
		}
		return new ItemStack(item);
	}

	public string GetBlockMaterialCode(ItemStack stack)
	{
		return stack?.Attributes?.GetString("materialBlockCode");
	}

	public void SetMaterial(ItemSlot slot, Block block)
	{
		slot.Itemstack.Attributes.SetString("materialBlockCode", block.Code.ToShortString());
	}

	public void RemoveMaterial(ItemSlot slot)
	{
		slot.Itemstack.Attributes.RemoveAttribute("materialBlockCode");
	}

	public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
	{
		string blockMaterialCode = GetBlockMaterialCode(itemstack);
		if (blockMaterialCode != null)
		{
			string key = "pan-filled-" + blockMaterialCode + target;
			renderinfo.ModelRef = ObjectCacheUtil.GetOrCreate(capi, key, delegate
			{
				MeshData data = GenMesh(itemstack, capi.BlockTextureAtlas, null);
				return capi.Render.UploadMultiTextureMesh(data);
			});
		}
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
	{
		handling = EnumHandHandling.PreventDefault;
		if (!firstEvent)
		{
			return;
		}
		IPlayer player = (byEntity as EntityPlayer)?.Player;
		if (player != null && (blockSel == null || byEntity.World.Claims.TryAccess(player, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak)))
		{
			string blockMaterialCode = GetBlockMaterialCode(slot.Itemstack);
			if (byEntity.Controls.ShiftKey)
			{
				base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
			}
			else if (!byEntity.FeetInLiquid && api.Side == EnumAppSide.Client && blockMaterialCode != null)
			{
				(api as ICoreClientAPI).TriggerIngameError(this, "notinwater", Lang.Get("ingameerror-panning-notinwater"));
			}
			else if (blockMaterialCode == null && blockSel != null)
			{
				TryTakeMaterial(slot, byEntity, blockSel.Position);
				slot.Itemstack.TempAttributes.SetBool("canpan", value: false);
			}
			else if (blockMaterialCode != null)
			{
				slot.Itemstack.TempAttributes.SetBool("canpan", value: true);
			}
		}
	}

	public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		if ((byEntity.Controls.TriesToMove || byEntity.Controls.Jump) && !byEntity.Controls.Sneak)
		{
			return false;
		}
		IPlayer player = (byEntity as EntityPlayer)?.Player;
		if (player == null)
		{
			return false;
		}
		if (blockSel != null && !byEntity.World.Claims.TryAccess(player, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
		{
			return false;
		}
		string blockMaterialCode = GetBlockMaterialCode(slot.Itemstack);
		if (blockMaterialCode == null || !slot.Itemstack.TempAttributes.GetBool("canpan"))
		{
			return false;
		}
		Vec3d xYZ = byEntity.Pos.AheadCopy(0.4000000059604645).XYZ;
		xYZ.Y += byEntity.LocalEyePos.Y - 0.4000000059604645;
		if (secondsUsed > 0.5f && api.World.Rand.NextDouble() > 0.5)
		{
			Block block = api.World.GetBlock(new AssetLocation(blockMaterialCode));
			Vec3d vec3d = xYZ.Clone();
			vec3d.X += GameMath.Sin((0f - secondsUsed) * 20f) / 5f;
			vec3d.Z += GameMath.Cos((0f - secondsUsed) * 20f) / 5f;
			vec3d.Y -= 0.07000000029802322;
			byEntity.World.SpawnCubeParticles(vec3d, new ItemStack(block), 0.3f, (int)(1.5f + (float)api.World.Rand.NextDouble()), 0.3f + (float)api.World.Rand.NextDouble() / 6f, (byEntity as EntityPlayer)?.Player);
		}
		if (byEntity.World is IClientWorldAccessor)
		{
			ModelTransform modelTransform = new ModelTransform();
			modelTransform.EnsureDefaultValues();
			modelTransform.Origin.Set(0f, 0f, 0f);
			if (secondsUsed > 0.5f)
			{
				modelTransform.Translation.X = Math.Min(0.25f, GameMath.Cos(10f * secondsUsed) / 4f);
				modelTransform.Translation.Y = Math.Min(0.15f, GameMath.Sin(10f * secondsUsed) / 6.666f);
				if (sound == null)
				{
					sound = (api as ICoreClientAPI).World.LoadSound(new SoundParams
					{
						Location = new AssetLocation("sounds/player/panning.ogg"),
						ShouldLoop = false,
						RelativePosition = true,
						Position = new Vec3f(),
						DisposeOnFinish = true,
						Volume = 0.5f,
						Range = 8f
					});
					sound.Start();
				}
			}
			modelTransform.Translation.X -= Math.Min(1.6f, secondsUsed * 4f * 1.57f);
			modelTransform.Translation.Y -= Math.Min(0.1f, secondsUsed * 2f);
			modelTransform.Translation.Z -= Math.Min(1f, secondsUsed * 180f);
			modelTransform.Scale = 1f + Math.Min(0.6f, 2f * secondsUsed);
			return secondsUsed <= 4f;
		}
		return true;
	}

	public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
	{
		if (cancelReason == EnumItemUseCancelReason.ReleasedMouse)
		{
			return false;
		}
		if (api.Side == EnumAppSide.Client)
		{
			sound?.Stop();
			sound = null;
		}
		return true;
	}

	public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		sound?.Stop();
		sound = null;
		if (secondsUsed >= 3.4f)
		{
			string blockMaterialCode = GetBlockMaterialCode(slot.Itemstack);
			if (api.Side == EnumAppSide.Server && blockMaterialCode != null)
			{
				CreateDrop(byEntity, blockMaterialCode);
			}
			RemoveMaterial(slot);
			slot.MarkDirty();
			byEntity.GetBehavior<EntityBehaviorHunger>()?.ConsumeSaturation(4f);
		}
	}

	private void CreateDrop(EntityAgent byEntity, string fromBlockCode)
	{
		IPlayer player = (byEntity as EntityPlayer)?.Player;
		PanningDrop[] array = null;
		foreach (string key in dropsBySourceMat.Keys)
		{
			if (WildcardUtil.Match(key, fromBlockCode))
			{
				array = dropsBySourceMat[key];
			}
		}
		if (array == null)
		{
			throw new InvalidOperationException("Coding error, no drops defined for source mat " + fromBlockCode);
		}
		string newValue = api.World.GetBlock(new AssetLocation(fromBlockCode))?.Variant["rock"];
		array.Shuffle(api.World.Rand);
		for (int i = 0; i < array.Length; i++)
		{
			PanningDrop panningDrop = array[i];
			double num = api.World.Rand.NextDouble();
			float num2 = 1f;
			if (panningDrop.DropModbyStat != null)
			{
				num2 = byEntity.Stats.GetBlended(panningDrop.DropModbyStat);
			}
			float num3 = panningDrop.Chance.nextFloat() * num2;
			ItemStack itemStack = panningDrop.ResolvedItemstack;
			if (array[i].Code.Path.Contains("{rocktype}"))
			{
				itemStack = Resolve(array[i].Type, array[i].Code.Path.Replace("{rocktype}", newValue));
			}
			if (num < (double)num3 && itemStack != null)
			{
				itemStack = itemStack.Clone();
				if (player == null || !player.InventoryManager.TryGiveItemstack(itemStack, slotNotifyEffect: true))
				{
					api.World.SpawnItemEntity(itemStack, byEntity.ServerPos.XYZ);
				}
				break;
			}
		}
	}

	public virtual bool IsPannableMaterial(Block block)
	{
		return block.Attributes?.IsTrue("pannable") ?? false;
	}

	protected virtual void TryTakeMaterial(ItemSlot slot, EntityAgent byEntity, BlockPos position)
	{
		Block block = api.World.BlockAccessor.GetBlock(position);
		if (!IsPannableMaterial(block))
		{
			return;
		}
		if (api.World.BlockAccessor.GetBlock(position.UpCopy()).Id != 0)
		{
			if (api.Side == EnumAppSide.Client)
			{
				(api as ICoreClientAPI).TriggerIngameError(this, "noair", Lang.Get("ingameerror-panning-requireairabove"));
			}
			return;
		}
		string text = block.Variant["layer"];
		if (text != null)
		{
			string domainAndPath = block.FirstCodePart() + "-" + block.FirstCodePart(1);
			Block block2 = api.World.GetBlock(new AssetLocation(domainAndPath));
			SetMaterial(slot, block2);
			if (text == "1")
			{
				api.World.BlockAccessor.SetBlock(0, position);
			}
			else
			{
				AssetLocation blockCode = block.CodeWithVariant("layer", (int.Parse(text) - 1).ToString() ?? "");
				Block block3 = api.World.GetBlock(blockCode);
				api.World.BlockAccessor.SetBlock(block3.BlockId, position);
			}
			api.World.BlockAccessor.TriggerNeighbourBlockUpdate(position);
		}
		else
		{
			string text2 = block.Attributes["pannedBlock"].AsString();
			Block block4 = ((text2 == null) ? api.World.GetBlock(block.CodeWithVariant("layer", "7")) : api.World.GetBlock(AssetLocation.Create(text2, block.Code.Domain)));
			if (block4 != null)
			{
				SetMaterial(slot, block);
				api.World.BlockAccessor.SetBlock(block4.BlockId, position);
				api.World.BlockAccessor.TriggerNeighbourBlockUpdate(position);
			}
			else
			{
				api.Logger.Warning("Missing \"pannedBlock\" attribute for pannable block " + block.Code.ToShortString());
			}
		}
		slot.MarkDirty();
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
	{
		return interactions.Append(base.GetHeldInteractionHelp(inSlot));
	}

	public MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos)
	{
		string blockMaterialCode = GetBlockMaterialCode(itemstack);
		ICoreClientAPI coreClientAPI = api as ICoreClientAPI;
		AssetLocation shapePath = ((blockMaterialCode != null) ? shapeFull : shapeEmpty);
		Shape shapeBase = Vintagestory.API.Common.Shape.TryGet(coreClientAPI, shapePath);
		Block block = null;
		if (blockMaterialCode != null)
		{
			block = coreClientAPI.World.GetBlock(new AssetLocation(blockMaterialCode));
		}
		AtlasSize = coreClientAPI.BlockTextureAtlas.Size;
		if (block != null)
		{
			matTexPosition = coreClientAPI.BlockTextureAtlas.GetPosition(block, "up");
		}
		ownTextureSource = coreClientAPI.Tesselator.GetTextureSource(this);
		MeshData modeldata;
		if (block == null)
		{
			coreClientAPI.Tesselator.TesselateBlock(this, out modeldata);
		}
		else
		{
			coreClientAPI.Tesselator.TesselateShape("filledpan", shapeBase, out modeldata, this, null, 0, 0, 0);
		}
		return modeldata;
	}

	public string GetMeshCacheKey(ItemStack itemstack)
	{
		string blockMaterialCode = GetBlockMaterialCode(itemstack);
		if (blockMaterialCode == null)
		{
			return itemstack.Collectible.Code;
		}
		return string.Concat(itemstack.Collectible.Code, "pan-filled-", blockMaterialCode);
	}
}
