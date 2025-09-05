using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.ServerMods;

namespace Vintagestory.GameContent;

public class BlockMaterialFromAttributes : Block
{
	public Dictionary<string, CompositeTexture> TexturesBMFA;

	public virtual string MeshKey => "BMA";

	public virtual string MeshKeyInventory => MeshKey + "Inventory";

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		LoadTypes();
	}

	public virtual void LoadTypes()
	{
		RegistryObjectVariantGroup registryObjectVariantGroup = Attributes["materials"].AsObject<RegistryObjectVariantGroup>();
		TexturesBMFA = Attributes["textures"].AsObject<Dictionary<string, CompositeTexture>>();
		string[] array = registryObjectVariantGroup.States;
		if (registryObjectVariantGroup.LoadFromProperties != null)
		{
			array = api.Assets.TryGet(registryObjectVariantGroup.LoadFromProperties.WithPathPrefixOnce("worldproperties/").WithPathAppendixOnce(".json")).ToObject<StandardWorldProperty>().Variants.Select((WorldPropertyVariant p) => p.Code.Path).ToArray().Append(array);
		}
		List<JsonItemStack> list = new List<JsonItemStack>();
		string[] array2 = array;
		foreach (string text in array2)
		{
			JsonItemStack jsonItemStack = new JsonItemStack
			{
				Code = Code,
				Type = EnumItemClass.Block,
				Attributes = new JsonObject(JToken.Parse("{ \"material\": \"" + text + "\" }"))
			};
			jsonItemStack.Resolve(api.World, string.Concat(Code, " type"));
			list.Add(jsonItemStack);
		}
		CreativeInventoryStacks = new CreativeTabAndStackList[1]
		{
			new CreativeTabAndStackList
			{
				Stacks = list.ToArray(),
				Tabs = new string[2] { "general", "decorative" }
			}
		};
	}

	public override void OnUnloaded(ICoreAPI api)
	{
		Dictionary<string, MeshData> dictionary = ObjectCacheUtil.TryGet<Dictionary<string, MeshData>>(api, MeshKey);
		string key;
		if (dictionary != null && dictionary.Count > 0)
		{
			foreach (KeyValuePair<string, MeshData> item in dictionary)
			{
				item.Deconstruct(out key, out var value);
				value.Dispose();
			}
			ObjectCacheUtil.Delete(api, MeshKey);
		}
		Dictionary<string, MultiTextureMeshRef> dictionary2 = ObjectCacheUtil.TryGet<Dictionary<string, MultiTextureMeshRef>>(api, MeshKeyInventory);
		if (dictionary2 != null && dictionary2.Count > 0)
		{
			foreach (KeyValuePair<string, MultiTextureMeshRef> item2 in dictionary2)
			{
				item2.Deconstruct(out key, out var value2);
				value2.Dispose();
			}
			ObjectCacheUtil.Delete(api, MeshKeyInventory);
		}
		base.OnUnloaded(api);
	}

	public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
	{
		Dictionary<string, MultiTextureMeshRef> orCreate = ObjectCacheUtil.GetOrCreate(capi, MeshKeyInventory, () => new Dictionary<string, MultiTextureMeshRef>());
		string text = itemstack.Attributes.GetString("material", "");
		string key = Variant["type"] + text;
		if (!orCreate.TryGetValue(key, out var value))
		{
			MeshData orCreateMesh = GetOrCreateMesh(text);
			value = (orCreate[key] = capi.Render.UploadMultiTextureMesh(orCreateMesh));
		}
		renderinfo.ModelRef = value;
	}

	public virtual MeshData GetOrCreateMesh(string material, ITexPositionSource? overrideTexturesource = null)
	{
		Dictionary<string, MeshData> orCreate = ObjectCacheUtil.GetOrCreate(api, MeshKey, () => new Dictionary<string, MeshData>());
		ICoreClientAPI capi = (ICoreClientAPI)api;
		string key = Variant["type"] + material;
		if (overrideTexturesource != null || !orCreate.TryGetValue(key, out var value))
		{
			CompositeShape cshape = Shape.Clone();
			value = capi.TesselatorManager.CreateMesh(string.Concat(Code, " block"), cshape, (Shape shape, string name) => new ShapeTextureSource(capi, shape, name, TexturesBMFA, (string p) => p.Replace("{material}", material)), overrideTexturesource);
			if (overrideTexturesource == null)
			{
				orCreate[key] = value;
			}
		}
		return value;
	}

	public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
	{
		bool num = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
		if (num)
		{
			BEBehaviorMaterialFromAttributes behavior = world.BlockAccessor.GetBlockEntity(blockSel.Position).GetBehavior<BEBehaviorMaterialFromAttributes>();
			if (behavior != null)
			{
				BlockPos blockPos = (blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position);
				double y = byPlayer.Entity.Pos.X - ((double)blockPos.X + blockSel.HitPosition.X);
				double x = (double)(float)byPlayer.Entity.Pos.Z - ((double)blockPos.Z + blockSel.HitPosition.Z);
				float num2 = (float)Math.Atan2(y, x);
				float num3 = (float)Math.PI / 2f;
				float meshAngleY = (float)(int)Math.Round(num2 / num3) * num3;
				behavior.MeshAngleY = meshAngleY;
				behavior.Init();
			}
		}
		return num;
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		return new ItemStack[1] { OnPickBlock(world, pos) };
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		ItemStack itemStack = base.OnPickBlock(world, pos);
		BEBehaviorMaterialFromAttributes bEBehaviorMaterialFromAttributes = world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorMaterialFromAttributes>();
		if (bEBehaviorMaterialFromAttributes != null)
		{
			itemStack.Attributes.SetString("material", bEBehaviorMaterialFromAttributes.Material);
		}
		return itemStack;
	}

	public override string GetHeldItemName(ItemStack itemStack)
	{
		return "block-" + Code.Path + "-" + itemStack.Attributes.GetString("material");
	}
}
