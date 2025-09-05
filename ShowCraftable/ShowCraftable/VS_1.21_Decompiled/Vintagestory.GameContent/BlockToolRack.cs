using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockToolRack : Block
{
	private static bool collectedToolTextures;

	private WorldInteraction[] interactions;

	public static Dictionary<Item, ToolTextures> ToolTextureSubIds(ICoreAPI api)
	{
		object value;
		return (Dictionary<Item, ToolTextures>)(api.ObjectCache.TryGetValue("toolTextureSubIds", out value) ? (value as Dictionary<Item, ToolTextures>) : (api.ObjectCache["toolTextureSubIds"] = new Dictionary<Item, ToolTextures>()));
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		collectedToolTextures = false;
		if (api.Side != EnumAppSide.Client)
		{
			return;
		}
		ICoreClientAPI capi = api as ICoreClientAPI;
		interactions = ObjectCacheUtil.GetOrCreate(api, "toolrackBlockInteractions", delegate
		{
			List<ItemStack> list = new List<ItemStack>();
			foreach (CollectibleObject collectible in api.World.Collectibles)
			{
				if (!collectible.Tool.HasValue)
				{
					JsonObject attributes = collectible.Attributes;
					if (attributes == null || !attributes["rackable"].AsBool())
					{
						continue;
					}
				}
				List<ItemStack> handBookStacks = collectible.GetHandBookStacks(capi);
				if (handBookStacks != null)
				{
					list.AddRange(handBookStacks);
				}
			}
			return new WorldInteraction[2]
			{
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-toolrack-place",
					HotKeyCode = null,
					MouseButton = EnumMouseButton.Right,
					Itemstacks = list.ToArray()
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-toolrack-take",
					HotKeyCode = null,
					MouseButton = EnumMouseButton.Right
				}
			};
		});
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		BlockEntity blockEntity = world.BlockAccessor.GetBlockEntity(blockSel.Position);
		if (blockEntity is BlockEntityToolrack)
		{
			return ((BlockEntityToolrack)blockEntity).OnPlayerInteract(byPlayer, blockSel.HitPosition);
		}
		return false;
	}

	public override void OnCollectTextures(ICoreAPI api, ITextureLocationDictionary textureDict)
	{
		base.OnCollectTextures(api, textureDict);
		if (collectedToolTextures)
		{
			return;
		}
		collectedToolTextures = true;
		Dictionary<Item, ToolTextures> dictionary = ToolTextureSubIds(api);
		dictionary.Clear();
		IList<Item> items = api.World.Items;
		for (int i = 0; i < items.Count; i++)
		{
			Item item = items[i];
			if (!item.Tool.HasValue)
			{
				JsonObject attributes = item.Attributes;
				if (attributes == null || !attributes["rackable"].AsBool())
				{
					continue;
				}
			}
			ToolTextures toolTextures = new ToolTextures();
			if (item.Shape != null)
			{
				Shape cachedShape = (api as ICoreClientAPI).TesselatorManager.GetCachedShape(item.Shape.Base);
				if (cachedShape != null)
				{
					foreach (KeyValuePair<string, AssetLocation> texture in cachedShape.Textures)
					{
						CompositeTexture compositeTexture = new CompositeTexture(texture.Value.Clone());
						compositeTexture.Bake(api.Assets);
						textureDict.GetOrAddTextureLocation(new AssetLocationAndSource(compositeTexture.Baked.BakedName, "Shape code ", item.Shape.Base));
						toolTextures.TextureSubIdsByCode[texture.Key] = textureDict[new AssetLocationAndSource(compositeTexture.Baked.BakedName)];
					}
				}
			}
			foreach (KeyValuePair<string, CompositeTexture> texture2 in item.Textures)
			{
				texture2.Value.Bake(api.Assets);
				textureDict.GetOrAddTextureLocation(new AssetLocationAndSource(texture2.Value.Baked.BakedName, "Item code ", item.Code));
				toolTextures.TextureSubIdsByCode[texture2.Key] = textureDict[new AssetLocationAndSource(texture2.Value.Baked.BakedName)];
			}
			dictionary[item] = toolTextures;
		}
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
	}
}
