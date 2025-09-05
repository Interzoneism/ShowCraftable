using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockLantern : Block, ITexPositionSource, IAttachableToEntity
{
	private IAttachableToEntity attrAtta;

	private string curMat;

	private string curLining;

	private ITexPositionSource glassTextureSource;

	private ITexPositionSource tmpTextureSource;

	public int RequiresBehindSlots { get; set; }

	public Size2i AtlasSize { get; set; }

	public TextureAtlasPosition this[string textureCode] => textureCode switch
	{
		"material" => tmpTextureSource[curMat], 
		"material-deco" => tmpTextureSource["deco-" + curMat], 
		"lining" => tmpTextureSource[(curLining == "plain") ? curMat : curLining], 
		"glass" => glassTextureSource["material"], 
		_ => tmpTextureSource[textureCode], 
	};

	string IAttachableToEntity.GetCategoryCode(ItemStack stack)
	{
		return attrAtta?.GetCategoryCode(stack);
	}

	CompositeShape IAttachableToEntity.GetAttachedShape(ItemStack stack, string slotCode)
	{
		return attrAtta.GetAttachedShape(stack, slotCode);
	}

	string[] IAttachableToEntity.GetDisableElements(ItemStack stack)
	{
		return attrAtta.GetDisableElements(stack);
	}

	string[] IAttachableToEntity.GetKeepElements(ItemStack stack)
	{
		return attrAtta.GetKeepElements(stack);
	}

	string IAttachableToEntity.GetTexturePrefixCode(ItemStack stack)
	{
		return attrAtta.GetTexturePrefixCode(stack);
	}

	void IAttachableToEntity.CollectTextures(ItemStack itemstack, Shape intoShape, string texturePrefixCode, Dictionary<string, CompositeTexture> intoDict)
	{
		string text = itemstack.Attributes.GetString("material");
		string text2 = itemstack.Attributes.GetString("lining");
		string text3 = itemstack.Attributes.GetString("glass", "quartz");
		Block block = api.World.GetBlock(new AssetLocation("glass-" + text3));
		intoShape.Textures["glass"] = block.Textures["material"].Base;
		intoShape.Textures["material"] = Textures[text].Base;
		intoShape.Textures["lining"] = Textures[(text2 == null || text2 == "plain") ? text : text2].Base;
		intoShape.Textures["material-deco"] = Textures["deco-" + text].Base;
	}

	public bool IsAttachable(Entity toEntity, ItemStack itemStack)
	{
		return true;
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		attrAtta = IAttachableToEntity.FromAttributes(this);
	}

	public override string GetHeldTpIdleAnimation(ItemSlot activeHotbarSlot, Entity forEntity, EnumHand hand)
	{
		IPlayer player = (forEntity as EntityPlayer)?.Player;
		if (forEntity.AnimManager.IsAnimationActive("sleep", "wave", "cheer", "shrug", "cry", "nod", "facepalm", "bow", "laugh", "rage", "scythe", "bowaim", "bowhit", "spearidle"))
		{
			return null;
		}
		if (player?.InventoryManager?.ActiveHotbarSlot != null && !player.InventoryManager.ActiveHotbarSlot.Empty && hand == EnumHand.Left)
		{
			ItemStack itemstack = player.InventoryManager.ActiveHotbarSlot.Itemstack;
			if (itemstack != null && itemstack.Collectible?.GetHeldTpIdleAnimation(player.InventoryManager.ActiveHotbarSlot, forEntity, EnumHand.Right) != null)
			{
				return null;
			}
			if (player != null && player.Entity?.Controls.LeftMouseDown == true && itemstack != null && itemstack.Collectible?.GetHeldTpHitAnimation(player.InventoryManager.ActiveHotbarSlot, forEntity) != null)
			{
				return null;
			}
		}
		if (hand != EnumHand.Left)
		{
			return "holdinglanternrighthand";
		}
		return "holdinglanternlefthand";
	}

	public override byte[] GetLightHsv(IBlockAccessor blockAccessor, BlockPos pos, ItemStack stack = null)
	{
		if (pos != null && blockAccessor.GetBlockEntity(pos) is BELantern bELantern)
		{
			return bELantern.GetLightHsv();
		}
		if (stack != null)
		{
			string text = stack.Attributes.GetString("lining");
			stack.Attributes.GetString("material");
			int num = LightHsv[2] + ((text != "plain") ? 2 : 0);
			byte[] array = new byte[3]
			{
				LightHsv[0],
				LightHsv[1],
				(byte)num
			};
			BELantern.setLightColor(LightHsv, array, stack.Attributes.GetString("glass"));
			return array;
		}
		if (pos != null)
		{
			int num2 = LightHsv[2] + 3;
			return new byte[3]
			{
				LightHsv[0],
				LightHsv[1],
				(byte)num2
			};
		}
		return base.GetLightHsv(blockAccessor, pos, stack);
	}

	public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
	{
		Dictionary<string, MultiTextureMeshRef> orCreate = ObjectCacheUtil.GetOrCreate(capi, "blockLanternGuiMeshRefs", () => new Dictionary<string, MultiTextureMeshRef>());
		string text = itemstack.Attributes.GetString("material");
		string text2 = itemstack.Attributes.GetString("lining");
		string text3 = itemstack.Attributes.GetString("glass", "quartz");
		string key = text + "-" + text2 + "-" + text3;
		if (!orCreate.TryGetValue(key, out var value))
		{
			AssetLocation shapePath = Shape.Base.CopyWithPathPrefixAndAppendixOnce("shapes/", ".json");
			Shape shape = Vintagestory.API.Common.Shape.TryGet(capi, shapePath);
			MeshData data = GenMesh(capi, text, text2, text3, shape);
			value = (orCreate[key] = capi.Render.UploadMultiTextureMesh(data));
		}
		renderinfo.ModelRef = value;
		renderinfo.CullFaces = false;
	}

	public override void OnUnloaded(ICoreAPI api)
	{
		if (!(api is ICoreClientAPI coreClientAPI) || !coreClientAPI.ObjectCache.TryGetValue("blockLanternGuiMeshRefs", out var value))
		{
			return;
		}
		foreach (KeyValuePair<string, MultiTextureMeshRef> item in value as Dictionary<string, MultiTextureMeshRef>)
		{
			item.Value.Dispose();
		}
		coreClientAPI.ObjectCache.Remove("blockLanternGuiMeshRefs");
	}

	public MeshData GenMesh(ICoreClientAPI capi, string material, string lining, string glassMaterial, Shape shape = null, ITesselatorAPI tesselator = null)
	{
		if (tesselator == null)
		{
			tesselator = capi.Tesselator;
		}
		tmpTextureSource = tesselator.GetTextureSource(this);
		if (shape == null)
		{
			shape = Vintagestory.API.Common.Shape.TryGet(capi, "shapes/" + Shape.Base.Path + ".json");
		}
		if (shape == null)
		{
			return null;
		}
		AtlasSize = capi.BlockTextureAtlas.Size;
		curMat = material;
		curLining = lining;
		Block block = capi.World.GetBlock(new AssetLocation("glass-" + glassMaterial));
		glassTextureSource = tesselator.GetTextureSource(block);
		tesselator.TesselateShape("blocklantern", shape, out var modeldata, this, new Vec3f(Shape.rotateX, Shape.rotateY, Shape.rotateZ), 0, 0, 0);
		return modeldata;
	}

	public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
	{
		if (!base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack))
		{
			return false;
		}
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BELantern bELantern)
		{
			string material = byItemStack.Attributes.GetString("material");
			string lining = byItemStack.Attributes.GetString("lining");
			string glass = byItemStack.Attributes.GetString("glass");
			bELantern.DidPlace(material, lining, glass);
			BlockPos blockPos = (blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position);
			double y = byPlayer.Entity.Pos.X - ((double)blockPos.X + blockSel.HitPosition.X);
			double x = byPlayer.Entity.Pos.Z - ((double)blockPos.Z + blockSel.HitPosition.Z);
			float num = (float)Math.Atan2(y, x);
			float num2 = (float)Math.PI / 8f;
			float meshAngle = (float)(int)Math.Round(num / num2) * num2;
			bELantern.MeshAngle = meshAngle;
		}
		return true;
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		ItemStack itemStack = new ItemStack(world.GetBlock(CodeWithParts("up")));
		if (world.BlockAccessor.GetBlockEntity(pos) is BELantern bELantern)
		{
			itemStack.Attributes.SetString("material", bELantern.material);
			itemStack.Attributes.SetString("lining", bELantern.lining);
			itemStack.Attributes.SetString("glass", bELantern.glass);
		}
		else
		{
			itemStack.Attributes.SetString("material", "copper");
			itemStack.Attributes.SetString("lining", "plain");
			itemStack.Attributes.SetString("glass", "plain");
		}
		return itemStack;
	}

	public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		return new BlockDropItemStack[1]
		{
			new BlockDropItemStack(handbookStack)
		};
	}

	public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		bool flag = false;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handling = EnumHandling.PassThrough;
			obj.OnBlockBroken(world, pos, byPlayer, ref handling);
			if (handling == EnumHandling.PreventDefault)
			{
				flag = true;
			}
			if (handling == EnumHandling.PreventSubsequent)
			{
				return;
			}
		}
		if (flag)
		{
			return;
		}
		if (world.Side == EnumAppSide.Server && (byPlayer == null || byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative))
		{
			ItemStack[] array = new ItemStack[1] { OnPickBlock(world, pos) };
			if (array != null)
			{
				for (int j = 0; j < array.Length; j++)
				{
					world.SpawnItemEntity(array[j], pos);
				}
			}
			world.PlaySoundAt(Sounds.GetBreakSound(byPlayer), pos, -0.5, byPlayer);
		}
		if (EntityClass != null)
		{
			world.BlockAccessor.GetBlockEntity(pos)?.OnBlockBroken(byPlayer);
		}
		world.BlockAccessor.SetBlock(0, pos);
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (!byPlayer.Entity.Controls.ShiftKey && (world.BlockAccessor.GetBlockEntity(blockSel.Position) as BELantern).Interact(byPlayer))
		{
			return true;
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override string GetHeldItemName(ItemStack itemStack)
	{
		string text = itemStack.Attributes.GetString("material");
		return Lang.GetMatching(Code?.Domain + ":block-" + Code?.Path + "-" + text);
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		string text = inSlot.Itemstack.Attributes.GetString("material");
		string text2 = inSlot.Itemstack.Attributes.GetString("lining");
		string text3 = inSlot.Itemstack.Attributes.GetString("glass");
		dsc.AppendLine(Lang.Get("Material: {0}", Lang.Get("material-" + text)));
		dsc.AppendLine(Lang.Get("Lining: {0}", (text2 == "plain") ? "-" : Lang.Get("material-" + text2)));
		if (text3 != null)
		{
			dsc.AppendLine(Lang.Get("Glass: {0}", Lang.Get("glass-" + text3)));
		}
	}

	public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing, int rndIndex = -1)
	{
		if (capi.World.BlockAccessor.GetBlockEntity(pos) is BELantern bELantern && Textures.TryGetValue(bELantern.material, out var value))
		{
			return capi.BlockTextureAtlas.GetRandomColor(value.Baked.TextureSubId, rndIndex);
		}
		return base.GetRandomColor(capi, pos, facing, rndIndex);
	}

	public override List<ItemStack> GetHandBookStacks(ICoreClientAPI capi)
	{
		if (Code == null)
		{
			return null;
		}
		bool flag = CreativeInventoryTabs != null && CreativeInventoryTabs.Length != 0;
		bool flag2 = CreativeInventoryStacks != null && CreativeInventoryStacks.Length != 0;
		JsonObject attributes = Attributes;
		bool flag3 = attributes != null && attributes["handbook"]?["include"].AsBool() == true;
		JsonObject attributes2 = Attributes;
		if (attributes2 != null && attributes2["handbook"]?["exclude"].AsBool() == true)
		{
			return null;
		}
		if (!flag3 && !flag && !flag2)
		{
			return null;
		}
		List<ItemStack> list = new List<ItemStack>();
		if (flag2)
		{
			for (int i = 0; i < CreativeInventoryStacks.Length; i++)
			{
				for (int j = 0; j < CreativeInventoryStacks[i].Stacks.Length; j++)
				{
					ItemStack stack = CreativeInventoryStacks[i].Stacks[j].ResolvedItemstack;
					stack.ResolveBlockOrItem(capi.World);
					stack = stack.Clone();
					stack.StackSize = stack.Collectible.MaxStackSize;
					if (!list.Any((ItemStack itemStack5) => itemStack5.Equals(stack)))
					{
						list.Add(stack);
						ItemStack itemStack = stack.Clone();
						itemStack.Attributes.SetString("glass", "plain");
						list.Add(itemStack);
						ItemStack itemStack2 = stack.Clone();
						ItemStack itemStack3 = stack.Clone();
						ItemStack itemStack4 = stack.Clone();
						itemStack2.Attributes.SetString("lining", "silver");
						itemStack3.Attributes.SetString("lining", "gold");
						itemStack4.Attributes.SetString("lining", "electrum");
						list.Add(itemStack2);
						list.Add(itemStack3);
						list.Add(itemStack4);
					}
				}
			}
		}
		else
		{
			list.Add(new ItemStack(this));
		}
		return list;
	}
}
