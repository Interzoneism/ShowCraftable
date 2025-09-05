using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockGenericTypedContainer : Block, IAttachableToEntity, IWearableShapeSupplier
{
	private string defaultType;

	private string variantByGroup;

	private string variantByGroupInventory;

	public int RequiresBehindSlots { get; set; }

	public string Subtype
	{
		get
		{
			if (variantByGroup != null)
			{
				return Variant[variantByGroup];
			}
			return "";
		}
	}

	public string SubtypeInventory
	{
		get
		{
			if (variantByGroupInventory != null)
			{
				return Variant[variantByGroupInventory];
			}
			return "";
		}
	}

	Shape IWearableShapeSupplier.GetShape(ItemStack stack, Entity forEntity, string texturePrefixCode)
	{
		string key = stack.Attributes.GetString("type", defaultType);
		string shapename = Attributes["shape"][key].AsString();
		Shape shape = GetShape(forEntity.World.Api, shapename);
		shape.SubclassForStepParenting(texturePrefixCode);
		return shape;
	}

	public string GetCategoryCode(ItemStack stack)
	{
		string key = stack.Attributes?.GetString("type", defaultType);
		return Attributes["attachableCategoryCode"][key].AsString("chest");
	}

	public CompositeShape GetAttachedShape(ItemStack stack, string slotCode)
	{
		return null;
	}

	public void CollectTextures(ItemStack stack, Shape shape, string texturePrefixCode, Dictionary<string, CompositeTexture> intoDict)
	{
		string text = stack.Attributes.GetString("type", defaultType);
		foreach (string key in shape.Textures.Keys)
		{
			intoDict[texturePrefixCode + key] = Textures[text + "-" + key];
		}
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
		return Code.ToShortString() + "-" + stack.Attributes.GetString("type", defaultType) + "-";
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		defaultType = Attributes["defaultType"].AsString("normal-generic");
		variantByGroup = Attributes["variantByGroup"].AsString();
		variantByGroupInventory = Attributes["variantByGroupInventory"].AsString();
	}

	public string GetType(IBlockAccessor blockAccessor, BlockPos pos)
	{
		if (blockAccessor.GetBlockEntity(pos) is BlockEntityGenericTypedContainer blockEntityGenericTypedContainer)
		{
			return blockEntityGenericTypedContainer.type;
		}
		return defaultType;
	}

	public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		BlockEntityGenericTypedContainer blockEntityGenericTypedContainer = blockAccessor.GetBlockEntity(pos) as BlockEntityGenericTypedContainer;
		if (blockEntityGenericTypedContainer?.collisionSelectionBoxes != null)
		{
			return blockEntityGenericTypedContainer.collisionSelectionBoxes;
		}
		return base.GetCollisionBoxes(blockAccessor, pos);
	}

	public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		BlockEntityGenericTypedContainer blockEntityGenericTypedContainer = blockAccessor.GetBlockEntity(pos) as BlockEntityGenericTypedContainer;
		if (blockEntityGenericTypedContainer?.collisionSelectionBoxes != null)
		{
			return blockEntityGenericTypedContainer.collisionSelectionBoxes;
		}
		return base.GetSelectionBoxes(blockAccessor, pos);
	}

	public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
	{
		bool num = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
		if (num && world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityGenericTypedContainer blockEntityGenericTypedContainer)
		{
			BlockPos blockPos = (blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position);
			double y = byPlayer.Entity.Pos.X - ((double)blockPos.X + blockSel.HitPosition.X);
			double x = (double)(float)byPlayer.Entity.Pos.Z - ((double)blockPos.Z + blockSel.HitPosition.Z);
			float num2 = (float)Math.Atan2(y, x);
			string type = blockEntityGenericTypedContainer.type;
			string obj = Attributes?["rotatatableInterval"][type]?.AsString("22.5deg") ?? "22.5deg";
			if (obj == "22.5degnot45deg")
			{
				float num3 = (float)(int)Math.Round(num2 / ((float)Math.PI / 2f)) * ((float)Math.PI / 2f);
				float num4 = (float)Math.PI / 8f;
				if (Math.Abs(num2 - num3) >= num4)
				{
					blockEntityGenericTypedContainer.MeshAngle = num3 + (float)Math.PI / 8f * (float)Math.Sign(num2 - num3);
				}
				else
				{
					blockEntityGenericTypedContainer.MeshAngle = num3;
				}
			}
			if (obj == "22.5deg")
			{
				float num5 = (float)Math.PI / 8f;
				float meshAngle = (float)(int)Math.Round(num2 / num5) * num5;
				blockEntityGenericTypedContainer.MeshAngle = meshAngle;
			}
		}
		return num;
	}

	public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
	{
		Dictionary<string, MultiTextureMeshRef> meshrefs = new Dictionary<string, MultiTextureMeshRef>();
		string key = "genericTypedContainerMeshRefs" + FirstCodePart() + SubtypeInventory;
		meshrefs = ObjectCacheUtil.GetOrCreate(capi, key, delegate
		{
			foreach (KeyValuePair<string, MeshData> item in GenGuiMeshes(capi))
			{
				meshrefs[item.Key] = capi.Render.UploadMultiTextureMesh(item.Value);
			}
			return meshrefs;
		});
		string text = itemstack.Attributes.GetString("type", defaultType);
		if (!meshrefs.TryGetValue(text, out renderinfo.ModelRef))
		{
			MeshData data = GenGuiMesh(capi, text);
			meshrefs[text] = (renderinfo.ModelRef = capi.Render.UploadMultiTextureMesh(data));
		}
		base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
	}

	public override void OnUnloaded(ICoreAPI api)
	{
		if (!(api is ICoreClientAPI coreClientAPI))
		{
			return;
		}
		string key = "genericTypedContainerMeshRefs" + FirstCodePart() + SubtypeInventory;
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

	private MeshData GenGuiMesh(ICoreClientAPI capi, string type)
	{
		string shapename = Attributes["shape"][type].AsString();
		return GenMesh(capi, type, shapename);
	}

	public Dictionary<string, MeshData> GenGuiMeshes(ICoreClientAPI capi)
	{
		string[] array = Attributes["types"].AsArray<string>();
		Dictionary<string, MeshData> dictionary = new Dictionary<string, MeshData>();
		string[] array2 = array;
		foreach (string text in array2)
		{
			string shapename = Attributes["shape"][text].AsString();
			dictionary[text] = GenMesh(capi, text, shapename, null, (ShapeInventory == null) ? null : new Vec3f(ShapeInventory.rotateX, ShapeInventory.rotateY, ShapeInventory.rotateZ));
		}
		return dictionary;
	}

	public Shape GetShape(ICoreAPI capi, string shapename)
	{
		if (shapename == null)
		{
			return null;
		}
		AssetLocation assetLocation = AssetLocation.Create(shapename, Code.Domain).WithPathPrefixOnce("shapes/");
		Shape shape = Vintagestory.API.Common.Shape.TryGet(capi, string.Concat(assetLocation, ".json"));
		if (shape == null)
		{
			shape = Vintagestory.API.Common.Shape.TryGet(capi, string.Concat(assetLocation, "1.json"));
		}
		return shape;
	}

	public MeshData GenMesh(ICoreClientAPI capi, string type, string shapename, ITesselatorAPI tesselator = null, Vec3f rotation = null, int altTexNumber = 0)
	{
		Shape shape = GetShape(capi, shapename);
		if (tesselator == null)
		{
			tesselator = capi.Tesselator;
		}
		if (shape == null)
		{
			capi.Logger.Warning("Container block {0}, type: {1}: Shape file {2} not found!", Code, type, shapename);
			return new MeshData();
		}
		GenericContainerTextureSource texSource = new GenericContainerTextureSource
		{
			blockTextureSource = tesselator.GetTextureSource(this, altTexNumber),
			curType = type
		};
		TesselationMetaData meta = new TesselationMetaData
		{
			TexSource = texSource,
			WithJointIds = true,
			WithDamageEffect = true,
			TypeForLogging = "typedcontainer",
			Rotation = ((rotation == null) ? new Vec3f(Shape.rotateX, Shape.rotateY, Shape.rotateZ) : rotation)
		};
		tesselator.TesselateShape(meta, shape, out var modeldata);
		return modeldata;
	}

	public override void GetDecal(IWorldAccessor world, BlockPos pos, ITexPositionSource decalTexSource, ref MeshData decalModelData, ref MeshData blockModelData)
	{
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityGenericTypedContainer blockEntityGenericTypedContainer)
		{
			ICoreClientAPI coreClientAPI = api as ICoreClientAPI;
			string text = Attributes["shape"][blockEntityGenericTypedContainer.type].AsString();
			if (text == null)
			{
				base.GetDecal(world, pos, decalTexSource, ref decalModelData, ref blockModelData);
				return;
			}
			blockModelData = GenMesh(coreClientAPI, blockEntityGenericTypedContainer.type, text);
			AssetLocation assetLocation = AssetLocation.Create(text, Code.Domain).WithPathPrefixOnce("shapes/");
			Shape shape = Vintagestory.API.Common.Shape.TryGet(coreClientAPI, string.Concat(assetLocation, ".json"));
			if (shape == null)
			{
				shape = Vintagestory.API.Common.Shape.TryGet(coreClientAPI, string.Concat(assetLocation, "1.json"));
			}
			GenericContainerTextureSource texSource = new GenericContainerTextureSource
			{
				blockTextureSource = decalTexSource,
				curType = blockEntityGenericTypedContainer.type
			};
			coreClientAPI.Tesselator.TesselateShape("typedcontainer-decal", shape, out var modeldata, texSource, null, 0, 0, 0);
			decalModelData = modeldata;
			decalModelData.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, blockEntityGenericTypedContainer.MeshAngle, 0f);
		}
		else
		{
			base.GetDecal(world, pos, decalTexSource, ref decalModelData, ref blockModelData);
		}
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		ItemStack itemStack = new ItemStack(world.GetBlock(CodeWithVariant("side", "east")));
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityGenericTypedContainer blockEntityGenericTypedContainer)
		{
			itemStack.Attributes.SetString("type", blockEntityGenericTypedContainer.type);
		}
		else
		{
			itemStack.Attributes.SetString("type", defaultType);
		}
		return itemStack;
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
			JsonObject jsonObject = Attributes["drop"];
			if (jsonObject != null && jsonObject[GetType(world.BlockAccessor, pos)]?.AsBool() == true && array != null)
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

	public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		string text = handbookStack.Attributes?.GetString("type");
		if (text == null)
		{
			api.World.Logger.Warning("BlockGenericTypedContainer.GetDropsForHandbook(): type not set for block " + handbookStack.Collectible?.Code);
			return Array.Empty<BlockDropItemStack>();
		}
		JsonObject attributes = Attributes;
		if (attributes == null || attributes["drop"]?[text]?.AsBool() != false)
		{
			return new BlockDropItemStack[1]
			{
				new BlockDropItemStack(handbookStack)
			};
		}
		return Array.Empty<BlockDropItemStack>();
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		return new ItemStack[1]
		{
			new ItemStack(world.GetBlock(CodeWithVariant("side", "east")))
		};
	}

	public override string GetHeldItemName(ItemStack itemStack)
	{
		string text = itemStack.Attributes.GetString("type", defaultType);
		return Lang.GetMatching(Code?.Domain + ":block-" + text + "-" + Code?.Path);
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		string text = inSlot.Itemstack.Attributes.GetString("type");
		if (text != null)
		{
			int? num = inSlot.Itemstack.ItemAttributes?["quantitySlots"]?[text]?.AsInt();
			dsc.AppendLine("\n" + Lang.Get("Storage Slots: {0}", num));
		}
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

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return new WorldInteraction[1]
		{
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-chest-open",
				MouseButton = EnumMouseButton.Right
			}
		}.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
	}

	public virtual bool IsAttachable(Entity toEntity, ItemStack itemStack)
	{
		if (toEntity is EntityPlayer)
		{
			return false;
		}
		ITreeAttribute attributes = itemStack.Attributes;
		if (attributes != null && attributes.HasAttribute("animalSerialized"))
		{
			return false;
		}
		return true;
	}
}
