using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace Vintagestory.API.Client;

[DocumentAsJson]
public class CompositeTexture
{
	public const char AlphaSeparator = 'å';

	public const string AlphaSeparatorRegexSearch = "å\\d+";

	public const string OverlaysSeparator = "++";

	public const char BlendmodeSeparator = '~';

	[DocumentAsJson]
	public AssetLocation Base;

	[DocumentAsJson]
	public BlendedOverlayTexture[] BlendedOverlays;

	[DocumentAsJson]
	public CompositeTexture[] Alternates;

	[DocumentAsJson]
	public CompositeTexture[] Tiles;

	[DocumentAsJson]
	public int TilesWidth;

	public BakedCompositeTexture Baked;

	[DocumentAsJson]
	public int Rotation;

	[DocumentAsJson]
	public int Alpha = 255;

	[ThreadStatic]
	public static Dictionary<AssetLocation, CompositeTexture> basicTexturesCache;

	[ThreadStatic]
	public static Dictionary<AssetLocation, List<IAsset>> wildcardsCache;

	public AssetLocation WildCardNoFiles;

	[DocumentAsJson]
	public AssetLocation[] Overlays
	{
		set
		{
			BlendedOverlays = value.Select((AssetLocation o) => new BlendedOverlayTexture
			{
				Base = o
			}).ToArray();
		}
	}

	public AssetLocation AnyWildCardNoFiles
	{
		get
		{
			if (WildCardNoFiles != null)
			{
				return WildCardNoFiles;
			}
			if (Alternates != null)
			{
				AssetLocation assetLocation = Alternates.Select((CompositeTexture ct) => ct.WildCardNoFiles).FirstOrDefault();
				if (assetLocation != null)
				{
					return assetLocation;
				}
			}
			return null;
		}
	}

	public CompositeTexture()
	{
	}

	public CompositeTexture(AssetLocation Base)
	{
		this.Base = Base;
	}

	public CompositeTexture Clone()
	{
		CompositeTexture[] array = null;
		if (Alternates != null)
		{
			array = new CompositeTexture[Alternates.Length];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = Alternates[i].CloneWithoutAlternates();
			}
		}
		CompositeTexture[] array2 = null;
		if (Tiles != null)
		{
			array2 = new CompositeTexture[Tiles.Length];
			for (int j = 0; j < array2.Length; j++)
			{
				array2[j] = array2[j].CloneWithoutAlternates();
			}
		}
		CompositeTexture compositeTexture = new CompositeTexture
		{
			Base = Base.Clone(),
			Alternates = array,
			Tiles = array2,
			Rotation = Rotation,
			Alpha = Alpha,
			TilesWidth = TilesWidth
		};
		if (BlendedOverlays != null)
		{
			compositeTexture.BlendedOverlays = new BlendedOverlayTexture[BlendedOverlays.Length];
			for (int k = 0; k < compositeTexture.BlendedOverlays.Length; k++)
			{
				compositeTexture.BlendedOverlays[k] = BlendedOverlays[k].Clone();
			}
		}
		return compositeTexture;
	}

	internal CompositeTexture CloneWithoutAlternates()
	{
		CompositeTexture compositeTexture = new CompositeTexture
		{
			Base = Base.Clone(),
			Rotation = Rotation,
			Alpha = Alpha,
			TilesWidth = TilesWidth
		};
		if (BlendedOverlays != null)
		{
			compositeTexture.BlendedOverlays = new BlendedOverlayTexture[BlendedOverlays.Length];
			for (int i = 0; i < compositeTexture.BlendedOverlays.Length; i++)
			{
				compositeTexture.BlendedOverlays[i] = BlendedOverlays[i].Clone();
			}
		}
		if (Tiles != null)
		{
			compositeTexture.Tiles = new CompositeTexture[Tiles.Length];
			for (int j = 0; j < compositeTexture.Tiles.Length; j++)
			{
				compositeTexture.Tiles[j] = compositeTexture.Tiles[j].CloneWithoutAlternates();
			}
		}
		return compositeTexture;
	}

	public bool IsBasic()
	{
		if (Rotation != 0 || Alpha != 255)
		{
			return false;
		}
		if (Alternates == null && BlendedOverlays == null)
		{
			return Tiles == null;
		}
		return false;
	}

	public void Bake(IAssetManager assetManager)
	{
		if (Baked == null)
		{
			Baked = Bake(assetManager, this);
		}
	}

	public void RuntimeBake(ICoreClientAPI capi, ITextureAtlasAPI intoAtlas)
	{
		Baked = Bake(capi.Assets, this);
		RuntimeInsert(capi, intoAtlas, Baked);
		if (Baked.BakedVariants != null)
		{
			BakedCompositeTexture[] bakedVariants = Baked.BakedVariants;
			foreach (BakedCompositeTexture btex in bakedVariants)
			{
				RuntimeInsert(capi, intoAtlas, btex);
			}
		}
	}

	private bool RuntimeInsert(ICoreClientAPI capi, ITextureAtlasAPI intoAtlas, BakedCompositeTexture btex)
	{
		BitmapRef bitmapRef = capi.Assets.Get(btex.BakedName).ToBitmap(capi);
		if (intoAtlas.InsertTexture(bitmapRef, out var textureSubId, out var _))
		{
			btex.TextureSubId = textureSubId;
			capi.Render.RemoveTexture(btex.BakedName);
			return true;
		}
		bitmapRef.Dispose();
		return false;
	}

	public static BakedCompositeTexture Bake(IAssetManager assetManager, CompositeTexture ct)
	{
		BakedCompositeTexture bakedCompositeTexture = new BakedCompositeTexture();
		ct.WildCardNoFiles = null;
		if (ct.Base.EndsWithWildCard)
		{
			if (wildcardsCache == null)
			{
				wildcardsCache = new Dictionary<AssetLocation, List<IAsset>>();
			}
			if (!wildcardsCache.TryGetValue(ct.Base, out var value))
			{
				List<IAsset> list = (wildcardsCache[ct.Base] = assetManager.GetManyInCategory("textures", ct.Base.Path.Substring(0, ct.Base.Path.Length - 1), ct.Base.Domain));
				value = list;
			}
			if (value.Count == 0)
			{
				ct.WildCardNoFiles = ct.Base;
				ct.Base = new AssetLocation("unknown");
			}
			else if (value.Count == 1)
			{
				ct.Base = value[0].Location.CloneWithoutPrefixAndEnding("textures/".Length);
			}
			else
			{
				int num = ((ct.Alternates != null) ? ct.Alternates.Length : 0);
				CompositeTexture[] array = new CompositeTexture[num + value.Count - 1];
				if (ct.Alternates != null)
				{
					Array.Copy(ct.Alternates, array, ct.Alternates.Length);
				}
				if (basicTexturesCache == null)
				{
					basicTexturesCache = new Dictionary<AssetLocation, CompositeTexture>();
				}
				for (int i = 0; i < value.Count; i++)
				{
					AssetLocation assetLocation = value[i].Location.CloneWithoutPrefixAndEnding("textures/".Length);
					if (i == 0)
					{
						ct.Base = assetLocation;
						continue;
					}
					CompositeTexture value2;
					if (ct.Rotation == 0 && ct.Alpha == 255)
					{
						if (!basicTexturesCache.TryGetValue(assetLocation, out value2))
						{
							CompositeTexture compositeTexture = (basicTexturesCache[assetLocation] = new CompositeTexture(assetLocation));
							value2 = compositeTexture;
						}
					}
					else
					{
						value2 = new CompositeTexture(assetLocation);
						value2.Rotation = ct.Rotation;
						value2.Alpha = ct.Alpha;
					}
					array[num + i - 1] = value2;
				}
				ct.Alternates = array;
			}
		}
		bakedCompositeTexture.BakedName = ct.Base.Clone();
		if (ct.BlendedOverlays != null)
		{
			bakedCompositeTexture.TextureFilenames = new AssetLocation[ct.BlendedOverlays.Length + 1];
			bakedCompositeTexture.TextureFilenames[0] = ct.Base;
			for (int j = 0; j < ct.BlendedOverlays.Length; j++)
			{
				BlendedOverlayTexture blendedOverlayTexture = ct.BlendedOverlays[j];
				bakedCompositeTexture.TextureFilenames[j + 1] = blendedOverlayTexture.Base;
				AssetLocation bakedName = bakedCompositeTexture.BakedName;
				string[] obj = new string[5] { bakedName.Path, "++", null, null, null };
				int blendMode = (int)blendedOverlayTexture.BlendMode;
				obj[2] = blendMode.ToString();
				obj[3] = "~";
				obj[4] = blendedOverlayTexture.Base.ToString();
				bakedName.Path = string.Concat(obj);
			}
		}
		else
		{
			bakedCompositeTexture.TextureFilenames = new AssetLocation[1] { ct.Base };
		}
		if (ct.Rotation != 0)
		{
			if (ct.Rotation != 90 && ct.Rotation != 180 && ct.Rotation != 270)
			{
				throw new Exception(string.Concat("Texture definition ", ct.Base, " has a rotation thats not 0, 90, 180 or 270. These are the only allowed values!"));
			}
			AssetLocation bakedName2 = bakedCompositeTexture.BakedName;
			bakedName2.Path = bakedName2.Path + "@" + ct.Rotation;
		}
		if (ct.Alpha != 255)
		{
			if (ct.Alpha < 0 || ct.Alpha > 255)
			{
				throw new Exception(string.Concat("Texture definition ", ct.Base, " has a alpha value outside the 0..255 range."));
			}
			AssetLocation bakedName3 = bakedCompositeTexture.BakedName;
			bakedName3.Path = bakedName3.Path + "å" + ct.Alpha;
		}
		if (ct.Alternates != null)
		{
			bakedCompositeTexture.BakedVariants = new BakedCompositeTexture[ct.Alternates.Length + 1];
			bakedCompositeTexture.BakedVariants[0] = bakedCompositeTexture;
			for (int k = 0; k < ct.Alternates.Length; k++)
			{
				bakedCompositeTexture.BakedVariants[k + 1] = Bake(assetManager, ct.Alternates[k]);
			}
		}
		if (ct.Tiles != null)
		{
			List<BakedCompositeTexture> list2 = new List<BakedCompositeTexture>();
			for (int l = 0; l < ct.Tiles.Length; l++)
			{
				if (ct.Tiles[l].Base.EndsWithWildCard)
				{
					if (wildcardsCache == null)
					{
						wildcardsCache = new Dictionary<AssetLocation, List<IAsset>>();
					}
					string text = ct.Base.Path.Substring(0, ct.Base.Path.Length - 1);
					List<IAsset> list = (wildcardsCache[ct.Base] = assetManager.GetManyInCategory("textures", text, ct.Base.Domain));
					List<IAsset> source = list;
					int len = "textures".Length + text.Length + "/".Length;
					List<IAsset> list3 = source.OrderBy((IAsset asset) => asset.Location.Path.Substring(len).RemoveFileEnding().ToInt()).ToList();
					for (int num2 = 0; num2 < list3.Count; num2++)
					{
						CompositeTexture compositeTexture3 = new CompositeTexture(list3[num2].Location.CloneWithoutPrefixAndEnding("textures/".Length));
						compositeTexture3.Rotation = ct.Rotation;
						compositeTexture3.Alpha = ct.Alpha;
						compositeTexture3.BlendedOverlays = ct.BlendedOverlays;
						BakedCompositeTexture bakedCompositeTexture2 = Bake(assetManager, compositeTexture3);
						bakedCompositeTexture2.TilesWidth = ct.TilesWidth;
						list2.Add(bakedCompositeTexture2);
					}
				}
				else
				{
					CompositeTexture compositeTexture4 = ct.Tiles[l];
					compositeTexture4.BlendedOverlays = ct.BlendedOverlays;
					BakedCompositeTexture bakedCompositeTexture3 = Bake(assetManager, compositeTexture4);
					bakedCompositeTexture3.TilesWidth = ct.TilesWidth;
					list2.Add(bakedCompositeTexture3);
				}
			}
			bakedCompositeTexture.BakedTiles = list2.ToArray();
		}
		return bakedCompositeTexture;
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(Base.ToString());
		stringBuilder.Append("@");
		stringBuilder.Append(Rotation);
		stringBuilder.Append("a");
		stringBuilder.Append(Alpha);
		if (Alternates != null)
		{
			stringBuilder.Append("alts:");
			CompositeTexture[] alternates = Alternates;
			for (int i = 0; i < alternates.Length; i++)
			{
				alternates[i].ToString(stringBuilder);
				stringBuilder.Append(",");
			}
		}
		if (BlendedOverlays != null)
		{
			stringBuilder.Append("ovs:");
			BlendedOverlayTexture[] blendedOverlays = BlendedOverlays;
			for (int i = 0; i < blendedOverlays.Length; i++)
			{
				blendedOverlays[i].ToString(stringBuilder);
				stringBuilder.Append(",");
			}
		}
		return stringBuilder.ToString();
	}

	public void ToString(StringBuilder sb)
	{
		sb.Append(Base.ToString());
		sb.Append("@");
		sb.Append(Rotation);
		sb.Append("a");
		sb.Append(Alpha);
		if (Alternates != null)
		{
			sb.Append("alts:");
			CompositeTexture[] alternates = Alternates;
			foreach (CompositeTexture compositeTexture in alternates)
			{
				sb.Append(compositeTexture.ToString());
				sb.Append(",");
			}
		}
		if (BlendedOverlays != null)
		{
			sb.Append("ovs:");
			BlendedOverlayTexture[] blendedOverlays = BlendedOverlays;
			foreach (BlendedOverlayTexture blendedOverlayTexture in blendedOverlays)
			{
				sb.Append(blendedOverlayTexture.ToString());
				sb.Append(",");
			}
		}
	}

	public void FillPlaceholder(string search, string replace)
	{
		Base.Path = Base.Path.Replace(search, replace);
		if (BlendedOverlays != null)
		{
			BlendedOverlays.Foreach(delegate(BlendedOverlayTexture ov)
			{
				ov.Base.Path = ov.Base.Path.Replace(search, replace);
			});
		}
		if (Alternates != null)
		{
			Alternates.Foreach(delegate(CompositeTexture alt)
			{
				alt.FillPlaceholder(search, replace);
			});
		}
		if (Tiles != null)
		{
			Tiles.Foreach(delegate(CompositeTexture tile)
			{
				tile.FillPlaceholder(search, replace);
			});
		}
	}
}
