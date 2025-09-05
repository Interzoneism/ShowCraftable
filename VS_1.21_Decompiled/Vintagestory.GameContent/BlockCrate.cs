using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockCrate : BlockContainer, ITexPositionSource, IAttachableToEntity, IWearableShapeSupplier
{
	private string curType;

	private LabelProps nowTeselatingLabel;

	private ITexPositionSource tmpTextureSource;

	private TextureAtlasPosition labelTexturePos;

	public CrateProperties Props;

	private Vec3f origin = new Vec3f(0.5f, 0.5f, 0.5f);

	private Cuboidf[] closedCollBoxes = new Cuboidf[1]
	{
		new Cuboidf(0.0625f, 0f, 0.0625f, 0.9375f, 0.9375f, 0.9375f)
	};

	public Size2i AtlasSize => tmpTextureSource.AtlasSize;

	public string Subtype
	{
		get
		{
			if (Props.VariantByGroup != null)
			{
				return Variant[Props.VariantByGroup];
			}
			return "";
		}
	}

	public string SubtypeInventory
	{
		get
		{
			if (Props?.VariantByGroupInventory != null)
			{
				return Variant[Props.VariantByGroupInventory];
			}
			return "";
		}
	}

	public int RequiresBehindSlots { get; set; }

	public TextureAtlasPosition this[string textureCode]
	{
		get
		{
			if (nowTeselatingLabel != null)
			{
				return labelTexturePos;
			}
			TextureAtlasPosition textureAtlasPosition = tmpTextureSource[curType + "-" + textureCode];
			if (textureAtlasPosition == null)
			{
				textureAtlasPosition = tmpTextureSource[textureCode];
			}
			if (textureAtlasPosition == null)
			{
				textureAtlasPosition = (api as ICoreClientAPI).BlockTextureAtlas.UnknownTexturePosition;
			}
			return textureAtlasPosition;
		}
	}

	public virtual bool IsAttachable(Entity toEntity, ItemStack itemStack)
	{
		if (toEntity is EntityPlayer)
		{
			return false;
		}
		return true;
	}

	public void CollectTextures(ItemStack stack, Shape shape, string texturePrefixCode, Dictionary<string, CompositeTexture> intoDict)
	{
		string text = stack.Attributes.GetString("type");
		foreach (string key in shape.Textures.Keys)
		{
			Textures.TryGetValue(text + "-" + key, out var value);
			if (value != null)
			{
				intoDict[texturePrefixCode + key] = value;
				continue;
			}
			Textures.TryGetValue(key, out var value2);
			intoDict[texturePrefixCode + key] = value2;
		}
	}

	public string GetCategoryCode(ItemStack stack)
	{
		return "crate";
	}

	public Shape GetShape(ItemStack stack, Entity forEntity, string texturePrefixCode)
	{
		string type = stack.Attributes.GetString("type", Props.DefaultType);
		stack.Attributes.GetString("label");
		string text = stack.Attributes.GetString("lidState", "closed");
		CompositeShape compositeShape = Props[type].Shape;
		if (ShapeInventory != null)
		{
			new Vec3f(ShapeInventory.rotateX, ShapeInventory.rotateY, ShapeInventory.rotateZ);
		}
		ItemStack[] nonEmptyContents = GetNonEmptyContents(api.World, stack);
		if (nonEmptyContents != null && nonEmptyContents.Length != 0)
		{
			_ = nonEmptyContents[0];
		}
		if (text == "opened")
		{
			compositeShape = compositeShape.Clone();
			compositeShape.Base.Path = compositeShape.Base.Path.Replace("closed", "opened");
		}
		AssetLocation shapePath = compositeShape.Base.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/");
		Shape shape = Vintagestory.API.Common.Shape.TryGet(api, shapePath);
		shape.SubclassForStepParenting(texturePrefixCode);
		return shape;
	}

	public CompositeShape GetAttachedShape(ItemStack stack, string slotCode)
	{
		return null;
	}

	public string[] GetDisableElements(ItemStack stack)
	{
		return null;
	}

	public string[] GetKeepElements(ItemStack stack)
	{
		return null;
	}

	public string GetTexturePrefixCode(ItemStack stack)
	{
		return GetKey(stack);
	}

	public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos)
	{
		return true;
	}

	public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		if (blockAccessor.GetBlockEntity(pos) is BlockEntityCrate blockEntityCrate)
		{
			return blockEntityCrate.GetSelectionBoxes();
		}
		return base.GetSelectionBoxes(blockAccessor, pos);
	}

	public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		if (blockAccessor.GetBlockEntity(pos) is BlockEntityCrate { LidState: "closed" })
		{
			return closedCollBoxes;
		}
		return base.GetCollisionBoxes(blockAccessor, pos);
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		Props = Attributes.AsObject<CrateProperties>(null, Code.Domain);
		PlacedPriorityInteract = true;
	}

	public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
	{
		bool num = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
		if (num && world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityCrate blockEntityCrate)
		{
			BlockPos blockPos = (blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position);
			double y = byPlayer.Entity.Pos.X - ((double)blockPos.X + blockSel.HitPosition.X);
			double x = (double)(float)byPlayer.Entity.Pos.Z - ((double)blockPos.Z + blockSel.HitPosition.Z);
			float num2 = (float)Math.Atan2(y, x);
			string type = blockEntityCrate.type;
			string rotatatableInterval = Props[type].RotatatableInterval;
			if (rotatatableInterval == "22.5degnot45deg")
			{
				float num3 = (float)(int)Math.Round(num2 / ((float)Math.PI / 2f)) * ((float)Math.PI / 2f);
				float num4 = (float)Math.PI / 8f;
				if (Math.Abs(num2 - num3) >= num4)
				{
					blockEntityCrate.MeshAngle = num3 + (float)Math.PI / 8f * (float)Math.Sign(num2 - num3);
				}
				else
				{
					blockEntityCrate.MeshAngle = num3;
				}
			}
			if (rotatatableInterval == "22.5deg")
			{
				float num5 = (float)Math.PI / 8f;
				float meshAngle = (float)(int)Math.Round(num2 / num5) * num5;
				blockEntityCrate.MeshAngle = meshAngle;
			}
		}
		return num;
	}

	public string GetKey(ItemStack itemstack)
	{
		string text = itemstack.Attributes.GetString("type", Props.DefaultType);
		string text2 = itemstack.Attributes.GetString("label");
		string text3 = itemstack.Attributes.GetString("lidState", "closed");
		return text + "-" + text2 + "-" + text3;
	}

	public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
	{
		string key = "crateMeshRefs" + FirstCodePart() + SubtypeInventory;
		Dictionary<string, MultiTextureMeshRef> orCreate = ObjectCacheUtil.GetOrCreate(capi, key, () => new Dictionary<string, MultiTextureMeshRef>());
		string key2 = GetKey(itemstack);
		if (!orCreate.TryGetValue(key2, out renderinfo.ModelRef))
		{
			string type = itemstack.Attributes.GetString("type", Props.DefaultType);
			string label = itemstack.Attributes.GetString("label");
			string lidState = itemstack.Attributes.GetString("lidState", "closed");
			CompositeShape shape = Props[type].Shape;
			Vec3f rotation = ((ShapeInventory == null) ? null : new Vec3f(ShapeInventory.rotateX, ShapeInventory.rotateY, ShapeInventory.rotateZ));
			ItemStack[] nonEmptyContents = GetNonEmptyContents(capi.World, itemstack);
			ItemStack contentStack = ((nonEmptyContents == null || nonEmptyContents.Length == 0) ? null : nonEmptyContents[0]);
			MeshData data = GenMesh(capi, contentStack, type, label, lidState, shape, rotation);
			orCreate[key2] = (renderinfo.ModelRef = capi.Render.UploadMultiTextureMesh(data));
		}
	}

	public override void OnUnloaded(ICoreAPI api)
	{
		if (!(api is ICoreClientAPI coreClientAPI))
		{
			return;
		}
		string key = "crateMeshRefs" + FirstCodePart() + SubtypeInventory;
		Dictionary<string, MultiTextureMeshRef> dictionary = ObjectCacheUtil.TryGet<Dictionary<string, MultiTextureMeshRef>>(api, key);
		if (dictionary == null)
		{
			return;
		}
		foreach (KeyValuePair<string, MultiTextureMeshRef> item in dictionary)
		{
			item.Value.Dispose();
		}
		coreClientAPI.ObjectCache.Remove(key);
	}

	public Shape GetShape(ICoreClientAPI capi, string type, CompositeShape cshape)
	{
		if (cshape?.Base == null)
		{
			return null;
		}
		ITesselatorAPI tesselator = capi.Tesselator;
		tmpTextureSource = tesselator.GetTextureSource(this, 0, returnNullWhenMissing: true);
		AssetLocation shapePath = cshape.Base.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/");
		Shape result = Vintagestory.API.Common.Shape.TryGet(capi, shapePath);
		curType = type;
		return result;
	}

	public MeshData GenMesh(ICoreClientAPI capi, ItemStack contentStack, string type, string label, string lidState, CompositeShape cshape, Vec3f rotation = null)
	{
		if (lidState == "opened")
		{
			cshape = cshape.Clone();
			cshape.Base.Path = cshape.Base.Path.Replace("closed", "opened");
		}
		Shape shape = GetShape(capi, type, cshape);
		ITesselatorAPI tesselator = capi.Tesselator;
		if (shape == null)
		{
			return new MeshData();
		}
		curType = type;
		tesselator.TesselateShape("crate", shape, out var modeldata, this, (rotation == null) ? new Vec3f(Shape.rotateX, Shape.rotateY, Shape.rotateZ) : rotation, 0, 0, 0);
		if (label != null && Props.Labels.TryGetValue(label, out var value))
		{
			MeshData sourceMesh = GenLabelMesh(capi, label, tmpTextureSource[value.Texture], editableVariant: false, rotation);
			modeldata.AddMeshData(sourceMesh);
		}
		if (contentStack != null && lidState != "closed")
		{
			MeshData meshData = genContentMesh(capi, contentStack, rotation);
			if (meshData != null)
			{
				modeldata.AddMeshData(meshData);
			}
		}
		return modeldata;
	}

	public MeshData GenLabelMesh(ICoreClientAPI capi, string label, TextureAtlasPosition texPos, bool editableVariant, Vec3f rotation = null)
	{
		Props.Labels.TryGetValue(label, out var value);
		if (value == null)
		{
			throw new ArgumentException("No label props found for this label");
		}
		AssetLocation shapePath = (editableVariant ? value.EditableShape : value.Shape).Base.Clone().WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/");
		Shape shapeBase = Vintagestory.API.Common.Shape.TryGet(capi, shapePath);
		Vec3f meshRotationDeg = ((rotation == null) ? new Vec3f(value.Shape.rotateX, value.Shape.rotateY, value.Shape.rotateZ) : rotation);
		nowTeselatingLabel = value;
		labelTexturePos = texPos;
		tmpTextureSource = capi.Tesselator.GetTextureSource(this, 0, returnNullWhenMissing: true);
		capi.Tesselator.TesselateShape("cratelabel", shapeBase, out var modeldata, this, meshRotationDeg, 0, 0, 0);
		nowTeselatingLabel = null;
		return modeldata;
	}

	protected MeshData genContentMesh(ICoreClientAPI capi, ItemStack contentStack, Vec3f rotation = null)
	{
		float fillHeight;
		ITexPositionSource contentTexture = BlockBarrel.getContentTexture(capi, contentStack, out fillHeight);
		if (contentTexture != null)
		{
			Shape shapeBase = Vintagestory.API.Common.Shape.TryGet(api, "shapes/block/wood/crate/contents.json");
			capi.Tesselator.TesselateShape("cratecontents", shapeBase, out var modeldata, contentTexture, rotation, 0, 0, 0);
			modeldata.Translate(0f, fillHeight * 1.1f, 0f);
			return modeldata;
		}
		return null;
	}

	public override void GetDecal(IWorldAccessor world, BlockPos pos, ITexPositionSource decalTexSource, ref MeshData decalModelData, ref MeshData blockModelData)
	{
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityCrate blockEntityCrate)
		{
			decalModelData.Rotate(origin, 0f, blockEntityCrate.MeshAngle, 0f);
			decalModelData.Scale(origin, 0.9375f, 1f, 0.9375f);
		}
		else
		{
			base.GetDecal(world, pos, decalTexSource, ref decalModelData, ref blockModelData);
		}
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		ItemStack itemStack = new ItemStack(this);
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityCrate blockEntityCrate)
		{
			itemStack.Attributes.SetString("type", blockEntityCrate.type);
			if (blockEntityCrate.label != null && blockEntityCrate.label.Length > 0)
			{
				itemStack.Attributes.SetString("label", blockEntityCrate.label);
			}
			itemStack.Attributes.SetString("lidState", blockEntityCrate.preferredLidState);
		}
		else
		{
			itemStack.Attributes.SetString("type", Props.DefaultType);
		}
		return itemStack;
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		return new ItemStack[1] { OnPickBlock(world, pos) };
	}

	public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		return new BlockDropItemStack[1]
		{
			new BlockDropItemStack(handbookStack)
		};
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityCrate blockEntityCrate)
		{
			return blockEntityCrate.OnBlockInteractStart(byPlayer, blockSel);
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override string GetHeldItemName(ItemStack itemStack)
	{
		string text = itemStack.Attributes.GetString("type", Props.DefaultType);
		string text2 = itemStack.Attributes.GetString("lidState", "closed");
		if (text2.Length == 0)
		{
			text2 = "closed";
		}
		return Lang.GetMatching(Code?.Domain + ":block-" + text + "-" + Code?.Path, Lang.Get("cratelidstate-" + text2, "closed"));
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		string text = inSlot.Itemstack.Attributes.GetString("type", Props.DefaultType);
		if (text != null)
		{
			int quantitySlots = Props[text].QuantitySlots;
			dsc.AppendLine("\n" + Lang.Get("Storage Slots: {0}", quantitySlots));
		}
	}

	public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
	{
		return base.GetPlacedBlockInfo(world, pos, forPlayer);
	}

	public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing, int rndIndex = -1)
	{
		if (capi.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityGenericTypedContainer blockEntityGenericTypedContainer)
		{
			if (!Textures.TryGetValue(blockEntityGenericTypedContainer.type + "-lid", out var value))
			{
				Textures.TryGetValue(blockEntityGenericTypedContainer.type + "-top", out value);
			}
			return capi.BlockTextureAtlas.GetRandomColor((value?.Baked != null) ? value.Baked.TextureSubId : 0, rndIndex);
		}
		return base.GetRandomColor(capi, pos, facing, rndIndex);
	}

	public string GetType(IBlockAccessor blockAccessor, BlockPos pos)
	{
		if (blockAccessor.GetBlockEntity(pos) is BlockEntityGenericTypedContainer blockEntityGenericTypedContainer)
		{
			return blockEntityGenericTypedContainer.type;
		}
		return Props.DefaultType;
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer).Append(new WorldInteraction
		{
			ActionLangCode = "blockhelp-crate-add",
			MouseButton = EnumMouseButton.Right,
			HotKeyCode = "shift"
		}, new WorldInteraction
		{
			ActionLangCode = "blockhelp-crate-addall",
			MouseButton = EnumMouseButton.Right,
			HotKeyCodes = new string[2] { "shift", "ctrl" }
		}, new WorldInteraction
		{
			ActionLangCode = "blockhelp-crate-remove",
			MouseButton = EnumMouseButton.Right,
			HotKeyCode = null
		}, new WorldInteraction
		{
			ActionLangCode = "blockhelp-crate-removeall",
			MouseButton = EnumMouseButton.Right,
			HotKeyCode = "ctrl"
		});
	}
}
