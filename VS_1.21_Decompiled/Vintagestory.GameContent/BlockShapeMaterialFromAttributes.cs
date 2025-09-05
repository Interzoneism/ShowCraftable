using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.ServerMods;

namespace Vintagestory.GameContent;

public class BlockShapeMaterialFromAttributes : Block
{
	private string[] types;

	private string[] materials;

	public Dictionary<string, CompositeTexture> TexturesBSMFA;

	public CompositeShape Cshape;

	public virtual string MeshKey { get; } = "BSMFA";

	public virtual string MeshKeyInventory => MeshKey + "Inventory";

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		LoadTypes();
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

	public virtual void LoadTypes()
	{
		types = Attributes["types"].AsArray<string>();
		Cshape = Attributes["shape"].AsObject<CompositeShape>();
		TexturesBSMFA = Attributes["textures"].AsObject<Dictionary<string, CompositeTexture>>();
		RegistryObjectVariantGroup registryObjectVariantGroup = Attributes["materials"].AsObject<RegistryObjectVariantGroup>();
		materials = registryObjectVariantGroup.States;
		if (registryObjectVariantGroup.LoadFromProperties != null)
		{
			StandardWorldProperty standardWorldProperty = api.Assets.TryGet(registryObjectVariantGroup.LoadFromProperties.WithPathPrefixOnce("worldproperties/").WithPathAppendixOnce(".json")).ToObject<StandardWorldProperty>();
			materials = standardWorldProperty.Variants.Select((WorldPropertyVariant p) => p.Code.Path).ToArray().Append<string>(materials);
		}
		List<JsonItemStack> list = new List<JsonItemStack>();
		string[] array = types;
		foreach (string text in array)
		{
			string[] array2 = materials;
			foreach (string text2 in array2)
			{
				JsonItemStack jsonItemStack = new JsonItemStack();
				jsonItemStack.Code = Code;
				jsonItemStack.Type = EnumItemClass.Block;
				jsonItemStack.Attributes = new JsonObject(JToken.Parse("{ \"type\": \"" + text + "\", \"material\": \"" + text2 + "\" }"));
				JsonItemStack jsonItemStack2 = jsonItemStack;
				jsonItemStack2.Resolve(api.World, string.Concat(Code, " type"));
				list.Add(jsonItemStack2);
			}
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

	public virtual MeshData GetOrCreateMesh(string type, string material, string? cachekeyextra = null, ITexPositionSource? overrideTexturesource = null)
	{
		Dictionary<string, MeshData> orCreate = ObjectCacheUtil.GetOrCreate(api, MeshKey, () => new Dictionary<string, MeshData>());
		ICoreClientAPI capi = (ICoreClientAPI)api;
		string key = type + "-" + material + cachekeyextra;
		if (overrideTexturesource != null || !orCreate.TryGetValue(key, out var value))
		{
			CompositeShape compositeShape = Cshape.Clone();
			compositeShape.Base.Path = compositeShape.Base.Path.Replace("{type}", type).Replace("{material}", material);
			value = capi.TesselatorManager.CreateMesh(string.Concat(Code, " block"), compositeShape, (Shape shape, string name) => new ShapeTextureSource(capi, shape, name, TexturesBSMFA, (string p) => p.Replace("{type}", type).Replace("{material}", material)), overrideTexturesource);
			if (overrideTexturesource == null)
			{
				orCreate[key] = value;
			}
		}
		return value;
	}

	public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
	{
		base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
		Dictionary<string, MultiTextureMeshRef> orCreate = ObjectCacheUtil.GetOrCreate(capi, MeshKeyInventory, () => new Dictionary<string, MultiTextureMeshRef>());
		string text = itemstack.Attributes.GetString("type", "");
		string text2 = itemstack.Attributes.GetString("material", "");
		string key = text + "-" + text2;
		if (!orCreate.TryGetValue(key, out var value))
		{
			MeshData orCreateMesh = GetOrCreateMesh(text, text2);
			value = (orCreate[key] = capi.Render.UploadMultiTextureMesh(orCreateMesh));
		}
		renderinfo.ModelRef = value;
	}

	public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
	{
		bool num = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
		if (num)
		{
			BEBehaviorShapeMaterialFromAttributes behavior = world.BlockAccessor.GetBlockEntity(blockSel.Position).GetBehavior<BEBehaviorShapeMaterialFromAttributes>();
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

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		string text = inSlot.Itemstack.Attributes.GetString("material", "oak");
		dsc.AppendLine(Lang.Get("Material: {0}", Lang.Get("material-" + text)));
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		return new ItemStack[1] { OnPickBlock(world, pos) };
	}

	public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		BlockDropItemStack[] dropsForHandbook = base.GetDropsForHandbook(handbookStack, forPlayer);
		dropsForHandbook[0] = dropsForHandbook[0].Clone();
		dropsForHandbook[0].ResolvedItemstack.SetFrom(handbookStack);
		return dropsForHandbook;
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		ItemStack itemStack = base.OnPickBlock(world, pos);
		BEBehaviorShapeMaterialFromAttributes bEBehaviorShapeMaterialFromAttributes = world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorShapeMaterialFromAttributes>();
		if (bEBehaviorShapeMaterialFromAttributes != null)
		{
			itemStack.Attributes.SetString("type", bEBehaviorShapeMaterialFromAttributes.Type);
			itemStack.Attributes.SetString("material", bEBehaviorShapeMaterialFromAttributes.Material);
		}
		return itemStack;
	}

	public void Rotate(EntityAgent byEntity, BlockSelection blockSel, int dir)
	{
		(GetBlockEntity<BlockEntityGeneric>(blockSel.Position)?.GetBehavior<BEBehaviorShapeMaterialFromAttributes>())?.Rotate(byEntity, blockSel, dir);
	}
}
