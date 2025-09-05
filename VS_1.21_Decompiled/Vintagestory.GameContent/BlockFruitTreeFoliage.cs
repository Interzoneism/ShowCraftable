using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockFruitTreeFoliage : BlockFruitTreePart
{
	private Block branchBlock;

	public Dictionary<string, DynFoliageProperties> foliageProps = new Dictionary<string, DynFoliageProperties>();

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		return new ItemStack(this);
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		branchBlock = api.World.GetBlock(AssetLocation.Create(Attributes["branchBlock"].AsString(), Code.Domain));
		Dictionary<string, DynFoliageProperties> dictionary = Attributes["foliageProperties"].AsObject<Dictionary<string, DynFoliageProperties>>();
		if (dictionary.TryGetValue("base", out var value))
		{
			foreach (KeyValuePair<string, DynFoliageProperties> item in dictionary)
			{
				if (!(item.Key == "base"))
				{
					item.Value.Rebase(value);
					foliageProps[item.Key] = item.Value;
					AssetLocation prefix = new AssetLocation(item.Value.TexturesBasePath);
					if (api is ICoreClientAPI capi)
					{
						foreach (CompositeTexture value2 in item.Value.Textures.Values)
						{
							value2.Base.WithLocationPrefixOnce(prefix);
							if (value2.BlendedOverlays != null)
							{
								BlendedOverlayTexture[] blendedOverlays = value2.BlendedOverlays;
								for (int i = 0; i < blendedOverlays.Length; i++)
								{
									blendedOverlays[i].Base.WithLocationPrefixOnce(prefix);
								}
							}
						}
						item.Value.LeafParticlesTexture?.Base.WithLocationPrefixOnce(prefix);
						item.Value.BlossomParticlesTexture?.Base.WithLocationPrefixOnce(prefix);
						item.Value.GetOrLoadTexture(capi, "largeleaves-plain");
					}
				}
			}
			return;
		}
		foliageProps = dictionary;
	}

	public override bool ShouldMergeFace(int facingIndex, Block neighbourBlock, int intraChunkIndex3d)
	{
		if (facingIndex == 1 || facingIndex == 2 || facingIndex == 4)
		{
			if (neighbourBlock != this)
			{
				return neighbourBlock == branchBlock;
			}
			return true;
		}
		return false;
	}

	public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
	{
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityFruitTreeFoliage blockEntityFruitTreeFoliage)
		{
			return Lang.Get("fruittree-foliage-" + blockEntityFruitTreeFoliage.TreeType, Lang.Get("foliagestate-" + blockEntityFruitTreeFoliage.FoliageState.ToString().ToLowerInvariant()));
		}
		return base.GetPlacedBlockName(world, pos);
	}

	public override int GetColor(ICoreClientAPI capi, BlockPos pos)
	{
		int color = 10000536;
		BlockEntityFruitTreeFoliage blockEntityFruitTreeFoliage = capi.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityFruitTreeFoliage;
		string text = null;
		string text2 = null;
		if (blockEntityFruitTreeFoliage != null)
		{
			string treeType = blockEntityFruitTreeFoliage.TreeType;
			if (treeType != null && treeType.Length > 0)
			{
				DynFoliageProperties dynFoliageProperties = foliageProps[blockEntityFruitTreeFoliage.TreeType];
				text = dynFoliageProperties.ClimateColorMap;
				text2 = dynFoliageProperties.SeasonColorMap;
				TextureAtlasPosition textureAtlasPosition = blockEntityFruitTreeFoliage["largeleaves-plain"];
				if (textureAtlasPosition != null)
				{
					color = textureAtlasPosition.AvgColor;
				}
			}
		}
		if (text == null)
		{
			text = "climatePlantTint";
		}
		if (text2 == null)
		{
			text2 = "seasonalFoliage";
		}
		return capi.World.ApplyColorMapOnRgba(text, text2, color, pos.X, pos.Y, pos.Z);
	}

	public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing, int rndIndex = -1)
	{
		BlockEntityFruitTreeFoliage blockEntityFruitTreeFoliage = capi.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityFruitTreeFoliage;
		string text = null;
		string text2 = null;
		int textureSubId = 0;
		if (blockEntityFruitTreeFoliage != null)
		{
			string treeType = blockEntityFruitTreeFoliage.TreeType;
			if (treeType != null && treeType.Length > 0)
			{
				DynFoliageProperties dynFoliageProperties = foliageProps[blockEntityFruitTreeFoliage.TreeType];
				text = dynFoliageProperties.ClimateColorMap;
				text2 = dynFoliageProperties.SeasonColorMap;
				if (dynFoliageProperties.Textures.TryGetValue("largeleaves-plain", out var value))
				{
					textureSubId = value.Baked.TextureSubId;
				}
			}
		}
		if (text == null)
		{
			text = "climatePlantTint";
		}
		if (text2 == null)
		{
			text2 = "seasonalFoliage";
		}
		int randomColor = capi.BlockTextureAtlas.GetRandomColor(textureSubId, rndIndex);
		return capi.World.ApplyColorMapOnRgba(text, text2, randomColor, pos.X, pos.Y, pos.Z);
	}
}
