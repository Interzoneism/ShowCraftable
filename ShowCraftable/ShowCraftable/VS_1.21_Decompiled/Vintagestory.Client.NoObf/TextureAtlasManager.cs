using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Util;

namespace Vintagestory.Client.NoObf;

public class TextureAtlasManager : AsyncHelper.Multithreaded, ITextureAtlasAPI, ITextureLocationDictionary
{
	internal const int UnknownTextureSubId = 0;

	public static FrameBufferRef atlasFramebuffer;

	private static float[] equalWeight = new float[4] { 0.25f, 0.25f, 0.25f, 0.25f };

	private static int[] pixelsTmp = new int[4];

	public List<TextureAtlas> Atlasses = new List<TextureAtlas>();

	public List<LoadedTexture> AtlasTextures;

	public TextureAtlasPosition[] TextureAtlasPositionsByTextureSubId;

	public TextureAtlasPosition UnknownTexturePos;

	protected OrderedDictionary<AssetLocation, int> textureNamesDict = new OrderedDictionary<AssetLocation, int>();

	protected int reloadIteration;

	protected ClientMain game;

	protected Random rand = new Random();

	protected int textureSubId;

	protected HashSet<string> textureCodes = new HashSet<string>();

	private string itemclass;

	private TextureAtlas currentAtlas;

	private Dictionary<AssetLocation, BitmapRef> overlayTextures = new Dictionary<AssetLocation, BitmapRef>();

	private bool genMipmapsQueued;

	private bool autoRegenMipMaps = true;

	public int Count => textureNamesDict.Count;

	public TextureAtlasPosition UnknownTexturePosition => UnknownTexturePos;

	public int this[AssetLocationAndSource textureLoc] => textureNamesDict[textureLoc];

	public Size2i Size { get; set; }

	public TextureAtlasPosition this[AssetLocation textureLocation]
	{
		get
		{
			if (textureNamesDict.TryGetValue(textureLocation, out var value))
			{
				return TextureAtlasPositionsByTextureSubId[value];
			}
			return null;
		}
	}

	public float SubPixelPaddingX
	{
		get
		{
			float result = 0f;
			if (itemclass == "items")
			{
				result = ClientSettings.ItemAtlasSubPixelPadding / (float)Size.Width;
			}
			if (itemclass == "blocks")
			{
				result = ClientSettings.BlockAtlasSubPixelPadding / (float)Size.Width;
			}
			if (itemclass == "entities")
			{
				result = 0f;
			}
			return result;
		}
	}

	public float SubPixelPaddingY
	{
		get
		{
			float result = 0f;
			if (itemclass == "items")
			{
				result = ClientSettings.ItemAtlasSubPixelPadding / (float)Size.Height;
			}
			if (itemclass == "blocks")
			{
				result = ClientSettings.BlockAtlasSubPixelPadding / (float)Size.Height;
			}
			if (itemclass == "entities")
			{
				result = 0f;
			}
			return result;
		}
	}

	public TextureAtlasPosition[] Positions => TextureAtlasPositionsByTextureSubId;

	List<LoadedTexture> ITextureAtlasAPI.AtlasTextures => AtlasTextures;

	public TextureAtlasManager(ClientMain game)
	{
		this.game = game;
		int val = game.Platform.GlGetMaxTextureSize();
		Size = new Size2i(GameMath.Clamp(val, 512, ClientSettings.MaxTextureAtlasWidth), GameMath.Clamp(val, 512, ClientSettings.MaxTextureAtlasHeight));
		textureNamesDict[new AssetLocationAndSource("unknown")] = textureSubId++;
	}

	public TextureAtlas CreateNewAtlas(string itemclass)
	{
		this.itemclass = itemclass;
		currentAtlas = new TextureAtlas(Size.Width, Size.Height, SubPixelPaddingX, SubPixelPaddingY);
		addCommonTextures();
		Atlasses.Add(currentAtlas);
		return currentAtlas;
	}

	public virtual TextureAtlas RuntimeCreateNewAtlas(string itemclass)
	{
		if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
		{
			throw new InvalidOperationException("Attempting to create an additional texture atlas outside of the main thread. This is not possible as we have only one OpenGL context!");
		}
		TextureAtlas textureAtlas = CreateNewAtlas(itemclass);
		LoadedTexture item = textureAtlas.Upload(game);
		AtlasTextures.Add(item);
		return textureAtlas;
	}

	private void addCommonTextures()
	{
		foreach (KeyValuePair<AssetLocation, int> item in textureNamesDict)
		{
			AssetLocationAndSource assetLocationAndSource = item.Key as AssetLocationAndSource;
			if (assetLocationAndSource.AddToAllAtlasses)
			{
				IAsset asset = game.AssetManager.TryGet(assetLocationAndSource);
				currentAtlas.InsertTexture(item.Value, game.api, asset);
			}
		}
	}

	public bool AddTextureLocation(AssetLocationAndSource loc)
	{
		if (textureNamesDict.ContainsKey(loc))
		{
			return false;
		}
		textureNamesDict[loc] = textureSubId++;
		return true;
	}

	public void SetTextureLocation(AssetLocationAndSource loc)
	{
		textureNamesDict[loc] = textureSubId++;
	}

	public int GetOrAddTextureLocation(AssetLocationAndSource loc)
	{
		if (!textureNamesDict.TryGetValue(loc, out var value))
		{
			value = textureSubId++;
			textureNamesDict[loc] = value;
		}
		return value;
	}

	public bool ContainsKey(AssetLocation loc)
	{
		return textureNamesDict.ContainsKey(loc);
	}

	public void GenFramebuffer()
	{
		DisposeFrameBuffer();
		atlasFramebuffer = game.Platform.CreateFramebuffer(new FramebufferAttrs("Render2DLoadedTexture", Size.Width, Size.Height)
		{
			Attachments = new FramebufferAttrsAttachment[1]
			{
				new FramebufferAttrsAttachment
				{
					AttachmentType = EnumFramebufferAttachment.ColorAttachment0,
					Texture = new RawTexture
					{
						Width = Size.Width,
						Height = Size.Height,
						TextureId = AtlasTextures[0].TextureId
					}
				}
			}
		});
	}

	public void RenderTextureIntoAtlas(int atlasTextureId, LoadedTexture fromTexture, float sourceX, float sourceY, float sourceWidth, float sourceHeight, float targetX, float targetY, float alphaTest = 0f)
	{
		if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
		{
			throw new InvalidOperationException("Attempting to insert a texture into the atlas outside of the main thread. This is not possible as we have only one OpenGL context!");
		}
		game.RenderTextureIntoFrameBuffer(atlasTextureId, fromTexture, sourceX, sourceY, sourceWidth, sourceHeight, atlasFramebuffer, targetX, targetY, alphaTest);
	}

	public bool GetOrInsertTexture(AssetLocation path, out int textureSubId, out TextureAtlasPosition texPos, CreateTextureDelegate onCreate = null, float alphaTest = 0f)
	{
		return GetOrInsertTexture(new AssetLocationAndSource(path), out textureSubId, out texPos, onCreate, alphaTest);
	}

	public bool GetOrInsertTexture(AssetLocationAndSource loc, out int textureSubId, out TextureAtlasPosition texPos, CreateTextureDelegate onCreate = null, float alphaTest = 0f)
	{
		if (onCreate == null)
		{
			onCreate = delegate
			{
				IBitmap bitmap3 = LoadCompositeBitmap(loc);
				if (bitmap3.Width == 0 && bitmap3.Height == 0)
				{
					game.Logger.Warning("GetOrInsertTexture() on path {0}: Bitmap width and height is 0! Either missing or corrupt image file. Will use unknown texture.", loc);
				}
				return bitmap3;
			};
		}
		if (textureNamesDict.TryGetValue(loc, out textureSubId))
		{
			texPos = TextureAtlasPositionsByTextureSubId[textureSubId];
			if (texPos.reloadIteration != reloadIteration)
			{
				IBitmap bitmap = onCreate();
				if (bitmap == null)
				{
					return false;
				}
				runtimeUpdateTexture(bitmap, texPos, alphaTest);
			}
			return true;
		}
		texPos = null;
		textureSubId = 0;
		IBitmap bitmap2 = onCreate();
		if (bitmap2 == null)
		{
			return false;
		}
		bool num = InsertTexture(bitmap2, out textureSubId, out texPos, alphaTest);
		if (num)
		{
			textureNamesDict[loc] = textureSubId;
		}
		return num;
	}

	[Obsolete("Use GetOrInsertTexture() instead. It's more efficient to load the bmp only if the texture was not found in the cache")]
	public bool InsertTextureCached(AssetLocation path, IBitmap bmp, out int textureSubId, out TextureAtlasPosition texPos, float alphaTest = 0f)
	{
		AssetLocationAndSource key = new AssetLocationAndSource(path);
		if (textureNamesDict.TryGetValue(key, out textureSubId))
		{
			texPos = TextureAtlasPositionsByTextureSubId[textureSubId];
			if (texPos.reloadIteration != reloadIteration)
			{
				runtimeUpdateTexture(bmp, texPos, alphaTest);
			}
			return true;
		}
		bool num = InsertTexture(bmp, out textureSubId, out texPos, alphaTest);
		if (num)
		{
			textureNamesDict[key] = textureSubId;
		}
		return num;
	}

	public bool InsertTexture(IBitmap bmp, out int textureSubId, out TextureAtlasPosition texPos, float alphaTest = 0f)
	{
		if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
		{
			throw new InvalidOperationException("Attempting to insert a texture into the atlas outside of the main thread. This is not possible as we have only one OpenGL context!");
		}
		if (!AllocateTextureSpace(bmp.Width, bmp.Height, out textureSubId, out texPos))
		{
			return false;
		}
		runtimeUpdateTexture(bmp, texPos, alphaTest);
		return true;
	}

	private void runtimeUpdateTexture(IBitmap bmp, TextureAtlasPosition texPos, float alphaTest = 0f)
	{
		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_0211: Unknown result type (might be due to invalid IL or missing references)
		//IL_0279: Unknown result type (might be due to invalid IL or missing references)
		if (alphaTest < 0.0001f)
		{
			game.Platform.LoadIntoTexture(bmp, texPos.atlasTextureId, (int)(texPos.x1 * (float)Size.Width), (int)(texPos.y1 * (float)Size.Height));
		}
		else
		{
			bool glScissorFlagEnabled = game.Platform.GlScissorFlagEnabled;
			if (glScissorFlagEnabled)
			{
				game.Platform.GlScissorFlag(enable: false);
			}
			game.Platform.GlToggleBlend(on: false);
			LoadedTexture loadedTexture = new LoadedTexture(game.api, game.Platform.LoadTexture(bmp), bmp.Width, bmp.Height);
			RenderTextureIntoAtlas(texPos.atlasTextureId, loadedTexture, 0f, 0f, bmp.Width, bmp.Height, texPos.x1 * (float)Size.Width, texPos.y1 * (float)Size.Height, alphaTest);
			loadedTexture.Dispose();
			if (glScissorFlagEnabled)
			{
				game.Platform.GlScissorFlag(enable: true);
			}
		}
		if (autoRegenMipMaps && !genMipmapsQueued)
		{
			genMipmapsQueued = true;
			game.EnqueueMainThreadTask(delegate
			{
				game.EnqueueMainThreadTask(delegate
				{
					RegenMipMaps(texPos.atlasNumber);
					genMipmapsQueued = false;
				}, "genmipmaps");
			}, "genmipmaps");
		}
		int width = bmp.Width;
		int height = bmp.Height;
		pixelsTmp[0] = bmp.GetPixel((int)(0.35f * (float)width), (int)(0.35f * (float)height)).ToArgb();
		pixelsTmp[1] = bmp.GetPixel((int)(0.65f * (float)width), (int)(0.35f * (float)height)).ToArgb();
		pixelsTmp[2] = bmp.GetPixel((int)(0.35f * (float)width), (int)(0.65f * (float)height)).ToArgb();
		pixelsTmp[3] = bmp.GetPixel((int)(0.65f * (float)width), (int)(0.65f * (float)height)).ToArgb();
		texPos.AvgColor = ColorUtil.ReverseColorBytes(ColorUtil.ColorAverage(pixelsTmp, equalWeight));
		texPos.RndColors = new int[30];
		for (int num = 0; num < 30; num++)
		{
			int num2 = 0;
			for (int num3 = 0; num3 < 15; num3++)
			{
				num2 = bmp.GetPixel((int)(rand.NextDouble() * (double)width), (int)(rand.NextDouble() * (double)height)).ToArgb();
				if (((num2 >> 24) & 0xFF) > 5)
				{
					break;
				}
			}
			texPos.RndColors[num] = num2;
		}
	}

	public void RegenMipMaps(int atlasNumber)
	{
		game.Platform.BuildMipMaps(AtlasTextures[atlasNumber].TextureId);
	}

	public bool InsertTexture(BitmapRef bmp, AssetLocationAndSource loc, out int textureSubIdOut)
	{
		if (bmp.Width > Size.Width || bmp.Height > Size.Height)
		{
			throw new InvalidOperationException("Cannot insert texture larger than the atlas itself");
		}
		textureSubIdOut = GetOrAddTextureLocation(loc);
		bool flag = currentAtlas.InsertTexture(textureSubIdOut, bmp.Width, bmp.Height, bmp.Pixels);
		if (!flag)
		{
			RuntimeCreateNewAtlas(itemclass);
			return currentAtlas.InsertTexture(textureSubIdOut, bmp.Width, bmp.Height, bmp.Pixels);
		}
		return flag;
	}

	public bool AllocateTextureSpace(int width, int height, out int textureSubId, out TextureAtlasPosition texPos, AssetLocationAndSource loc = null)
	{
		if (width > Size.Width || height > Size.Height)
		{
			throw new InvalidOperationException("Cannot create allocate texture space larger than the atlas itself");
		}
		textureSubId = ((loc == null) ? this.textureSubId++ : GetOrAddTextureLocation(loc));
		TextureAtlasPosition textureAtlasPosition = null;
		int num = 0;
		foreach (TextureAtlas atlass in Atlasses)
		{
			textureAtlasPosition = atlass.AllocateTextureSpace(textureSubId, width, height);
			if (textureAtlasPosition != null)
			{
				break;
			}
			num++;
		}
		if (textureAtlasPosition == null)
		{
			textureAtlasPosition = RuntimeCreateNewAtlas(itemclass).AllocateTextureSpace(textureSubId, width, height);
		}
		textureAtlasPosition.atlasNumber = (byte)num;
		textureAtlasPosition.atlasTextureId = AtlasTextures[num].TextureId;
		texPos = textureAtlasPosition;
		TextureAtlasPositionsByTextureSubId = TextureAtlasPositionsByTextureSubId.Append(texPos);
		return true;
	}

	public void FreeTextureSpace(int textureSubId)
	{
		using List<TextureAtlas>.Enumerator enumerator = Atlasses.GetEnumerator();
		while (enumerator.MoveNext() && !enumerator.Current.FreeTextureSpace(textureSubId))
		{
		}
	}

	public virtual void PopulateTextureAtlassesFromTextures()
	{
		TextureAtlasPositionsByTextureSubId = new TextureAtlasPosition[textureNamesDict.Count];
		BakedBitmap[] bitmaps = new BakedBitmap[textureNamesDict.Count];
		if (itemclass != "entities")
		{
			StartWorkerThread(delegate
			{
				LoadBitmaps(bitmaps);
			});
		}
		LoadBitmaps(bitmaps);
		while (WorkerThreadsInProgress() && !game.disposed)
		{
			Thread.Sleep(10);
		}
		addCommonTextures();
		foreach (KeyValuePair<AssetLocation, int> item in textureNamesDict)
		{
			int value = item.Value;
			BakedBitmap bakedBitmap = bitmaps[value];
			if (bakedBitmap != null && !(item.Key as AssetLocationAndSource).AddToAllAtlasses && !currentAtlas.InsertTexture(value, bakedBitmap.Width, bakedBitmap.Height, bakedBitmap.TexturePixels))
			{
				CreateNewAtlas(itemclass);
				if (!currentAtlas.InsertTexture(value, bakedBitmap.Width, bakedBitmap.Height, bakedBitmap.TexturePixels))
				{
					throw new Exception("Texture bigger than max supported texture size!");
				}
			}
		}
		FinishedOverlays();
	}

	private void LoadBitmaps(BakedBitmap[] bitmaps)
	{
		foreach (KeyValuePair<AssetLocation, int> item in textureNamesDict)
		{
			AssetLocationAndSource assetLocationAndSource = item.Key as AssetLocationAndSource;
			if (AsyncHelper.CanProceedOnThisThread(ref assetLocationAndSource.loadedAlready))
			{
				int value = item.Value;
				BakedBitmap bakedBitmap = LoadCompositeBitmap(game, assetLocationAndSource, overlayTextures);
				bitmaps[value] = bakedBitmap;
			}
		}
	}

	public virtual void ComposeTextureAtlasses_StageA()
	{
		AtlasTextures = new List<LoadedTexture>();
		foreach (TextureAtlas atlass in Atlasses)
		{
			LoadedTexture item = atlass.Upload(game);
			AtlasTextures.Add(item);
		}
		game.Platform.Logger.Notification("Composed {0} {1}x{2} " + itemclass + " texture atlases from {3} textures", AtlasTextures.Count, Size.Width, Size.Height, textureNamesDict.Count);
	}

	public virtual void ComposeTextureAtlasses_StageB()
	{
		foreach (TextureAtlas atlass in Atlasses)
		{
			game.Platform.BuildMipMaps(atlass.textureId);
		}
	}

	public virtual void ComposeTextureAtlasses_StageC()
	{
		int num = 0;
		foreach (TextureAtlas atlass in Atlasses)
		{
			atlass.PopulateAtlasPositions(TextureAtlasPositionsByTextureSubId, num++);
		}
		UnknownTexturePos = TextureAtlasPositionsByTextureSubId[0];
		for (int i = 0; i < TextureAtlasPositionsByTextureSubId.Length; i++)
		{
			TextureAtlasPosition textureAtlasPosition = TextureAtlasPositionsByTextureSubId[i];
			TextureAtlas textureAtlas = Atlasses[textureAtlasPosition.atlasNumber];
			float num2 = textureAtlasPosition.x2 - textureAtlasPosition.x1;
			float num3 = textureAtlasPosition.y2 - textureAtlasPosition.y1;
			pixelsTmp[0] = textureAtlas.GetPixel(textureAtlasPosition.x1 + 0.35f * num2, textureAtlasPosition.y1 + 0.35f * num3);
			pixelsTmp[1] = textureAtlas.GetPixel(textureAtlasPosition.x1 + 0.65f * num2, textureAtlasPosition.y1 + 0.35f * num3);
			pixelsTmp[2] = textureAtlas.GetPixel(textureAtlasPosition.x1 + 0.35f * num2, textureAtlasPosition.y1 + 0.65f * num3);
			pixelsTmp[3] = textureAtlas.GetPixel(textureAtlasPosition.x1 + 0.65f * num2, textureAtlasPosition.y1 + 0.65f * num3);
			textureAtlasPosition.AvgColor = ColorUtil.ReverseColorBytes(ColorUtil.ColorAverage(pixelsTmp, equalWeight));
			textureAtlasPosition.RndColors = new int[30];
			for (int j = 0; j < 30; j++)
			{
				int num4 = 0;
				for (int k = 0; k < 15; k++)
				{
					num4 = textureAtlas.GetPixel((float)((double)textureAtlasPosition.x1 + rand.NextDouble() * (double)num2), (float)((double)textureAtlasPosition.y1 + rand.NextDouble() * (double)num3));
					if (((num4 >> 24) & 0xFF) > 5)
					{
						break;
					}
				}
				textureAtlasPosition.RndColors[j] = num4;
			}
		}
		foreach (TextureAtlas atlass2 in Atlasses)
		{
			atlass2.DisposePixels();
		}
	}

	public virtual TextureAtlasManager ReloadTextures()
	{
		reloadIteration++;
		foreach (TextureAtlas atlass in Atlasses)
		{
			atlass.ReinitPixels();
			foreach (KeyValuePair<AssetLocation, int> item in textureNamesDict)
			{
				TextureAtlasPosition tpos = TextureAtlasPositionsByTextureSubId[item.Value];
				AssetLocationAndSource assetLocationAndSource = item.Key as AssetLocationAndSource;
				if (assetLocationAndSource.AddToAllAtlasses)
				{
					game.AssetManager.TryGet(assetLocationAndSource);
					BitmapRef bitmapRef = game.Platform.CreateBitmapFromPng(game.AssetManager.Get(assetLocationAndSource));
					atlass.UpdateTexture(tpos, bitmapRef.Pixels);
				}
			}
		}
		foreach (KeyValuePair<AssetLocation, int> item2 in textureNamesDict)
		{
			TextureAtlasPosition textureAtlasPosition = TextureAtlasPositionsByTextureSubId[item2.Value];
			AssetLocationAndSource assetLocationAndSource2 = item2.Key as AssetLocationAndSource;
			if (assetLocationAndSource2.AddToAllAtlasses)
			{
				continue;
			}
			int[] pixels;
			if (assetLocationAndSource2.loadedAlready == 2)
			{
				pixels = game.Platform.CreateBitmapFromPng(game.AssetManager.Get(assetLocationAndSource2)).Pixels;
			}
			else
			{
				BakedBitmap bakedBitmap = LoadCompositeBitmap(game, assetLocationAndSource2, overlayTextures);
				int num = (int)Math.Round((textureAtlasPosition.x2 - textureAtlasPosition.x1 + 2f * SubPixelPaddingX) * (float)Size.Width);
				int num2 = (int)Math.Round((textureAtlasPosition.y2 - textureAtlasPosition.y1 + 2f * SubPixelPaddingY) * (float)Size.Height);
				if (num != bakedBitmap.Width || num2 != bakedBitmap.Height)
				{
					game.Platform.Logger.Error("Texture {0} changed in size ({1}x{2} => {3}x{4}). Runtime reload with changing texture sizes is not supported. Will not update.", assetLocationAndSource2, num, num2, bakedBitmap.Width, bakedBitmap.Height);
					continue;
				}
				pixels = bakedBitmap.Pixels;
			}
			Atlasses[textureAtlasPosition.atlasNumber].UpdateTexture(textureAtlasPosition, pixels);
		}
		FinishedOverlays();
		for (int i = 0; i < Atlasses.Count; i++)
		{
			LoadedTexture texAtlas = AtlasTextures[i];
			Atlasses[i].DrawToTexture(game.Platform, texAtlas);
			Atlasses[i].DisposePixels();
		}
		return this;
	}

	private void FinishedOverlays()
	{
		foreach (BitmapRef value in overlayTextures.Values)
		{
			value?.Dispose();
		}
		overlayTextures.Clear();
	}

	public virtual TextureAtlasManager PauseRegenMipmaps()
	{
		autoRegenMipMaps = false;
		return this;
	}

	public virtual TextureAtlasManager ResumeRegenMipmaps()
	{
		autoRegenMipMaps = true;
		for (int i = 0; i < Atlasses.Count; i++)
		{
			RegenMipMaps(i);
		}
		return this;
	}

	public IBitmap LoadCompositeBitmap(AssetLocationAndSource path)
	{
		return LoadCompositeBitmap(game, path);
	}

	public static BakedBitmap LoadCompositeBitmap(ClientMain game, string compositeTextureName)
	{
		return LoadCompositeBitmap(game, new AssetLocationAndSource(compositeTextureName));
	}

	public static AssetLocationAndSource ToTextureAssetLocation(AssetLocationAndSource loc)
	{
		AssetLocationAndSource assetLocationAndSource = new AssetLocationAndSource(loc.Domain, "textures/" + loc.Path, loc.Source);
		assetLocationAndSource.Path = assetLocationAndSource.Path.Replace("@90", "").Replace("@180", "").Replace("@270", "");
		assetLocationAndSource.Path = Regex.Replace(assetLocationAndSource.Path, "å\\d+", "");
		assetLocationAndSource.WithPathAppendixOnce(".png");
		return assetLocationAndSource;
	}

	public static int getRotation(AssetLocationAndSource loc)
	{
		if (loc.Path.Contains("@90"))
		{
			return 90;
		}
		if (loc.Path.Contains("@180"))
		{
			return 180;
		}
		if (loc.Path.Contains("@270"))
		{
			return 270;
		}
		return 0;
	}

	public static int getAlpha(AssetLocationAndSource tex)
	{
		int num = tex.Path.IndexOf('å');
		if (num < 0)
		{
			return 255;
		}
		return tex.Path.Substring(num + 1, Math.Min(tex.Path.Length - num - 1, 3)).ToInt(255);
	}

	public static BakedBitmap LoadCompositeBitmap(ClientMain game, AssetLocationAndSource compositeTextureLocation)
	{
		return LoadCompositeBitmap(game, compositeTextureLocation, null);
	}

	public static BakedBitmap LoadCompositeBitmap(ClientMain game, AssetLocationAndSource compositeTextureLocation, Dictionary<AssetLocation, BitmapRef> cache)
	{
		BakedBitmap bakedBitmap = new BakedBitmap();
		int rotation = getRotation(compositeTextureLocation);
		int alpha = getAlpha(compositeTextureLocation);
		if (!compositeTextureLocation.Path.Contains("++"))
		{
			bakedBitmap.TexturePixels = LoadBitmapPixels(game, compositeTextureLocation, rotation, alpha, null, out var readWidth, out var readHeight);
			bakedBitmap.Width = readWidth;
			bakedBitmap.Height = readHeight;
			return bakedBitmap;
		}
		string[] array = compositeTextureLocation.ToString().Split(new string[1] { "++" }, StringSplitOptions.None);
		for (int i = 0; i < array.Length; i++)
		{
			string[] array2 = array[i].Split('~');
			EnumColorBlendMode blendMode = ((array2.Length > 1) ? ((EnumColorBlendMode)array2[0].ToInt()) : EnumColorBlendMode.Normal);
			AssetLocation assetLocation = AssetLocation.Create((array2.Length > 1) ? array2[1] : array2[0], compositeTextureLocation.Domain);
			if (rotation != 0)
			{
				assetLocation.WithPathAppendixOnce("@" + rotation);
			}
			AssetLocationAndSource assetLocationAndSource = new AssetLocationAndSource(assetLocation, compositeTextureLocation.Source);
			int readWidth2;
			int readHeight2;
			int[] array3 = LoadBitmapPixels(game, assetLocationAndSource, rotation, alpha, cache, out readWidth2, out readHeight2);
			if (bakedBitmap.TexturePixels == null)
			{
				bakedBitmap.TexturePixels = array3;
				bakedBitmap.Width = readWidth2;
				bakedBitmap.Height = readHeight2;
			}
			else if (bakedBitmap.Width != readWidth2 || bakedBitmap.Height != readHeight2)
			{
				game.Platform.Logger.Warning("Textureoverlay {0} ({2}x{3} pixel) is not the same width and height as base texture in composite texture {1} ({4}x{5} pixel), ignoring.", assetLocationAndSource, compositeTextureLocation, readWidth2, readHeight2, bakedBitmap.Width, bakedBitmap.Height);
			}
			else
			{
				for (int j = 0; j < bakedBitmap.TexturePixels.Length; j++)
				{
					bakedBitmap.TexturePixels[j] = ColorBlend.Blend(blendMode, bakedBitmap.TexturePixels[j], array3[j]);
				}
			}
		}
		return bakedBitmap;
	}

	private static int[] LoadBitmapPixels(ClientMain game, AssetLocationAndSource source, int rot, int alpha, Dictionary<AssetLocation, BitmapRef> cache, out int readWidth, out int readHeight)
	{
		BitmapRef value;
		if (cache != null)
		{
			lock (cache)
			{
				if (!cache.TryGetValue(source, out value))
				{
					value = LoadBitmap(game, ToTextureAssetLocation(source));
					cache.Add(source, value);
				}
			}
		}
		else
		{
			value = LoadBitmap(game, ToTextureAssetLocation(source));
		}
		if (value == null)
		{
			readWidth = 0;
			readHeight = 0;
			return null;
		}
		int[] pixelsTransformed = value.GetPixelsTransformed(rot, alpha);
		bool flag = rot % 180 == 90;
		readWidth = (flag ? value.Height : value.Width);
		readHeight = (flag ? value.Width : value.Height);
		if (cache == null)
		{
			value.Dispose();
		}
		return pixelsTransformed;
	}

	public static BitmapRef LoadBitmap(ClientMain game, AssetLocationAndSource textureLoc)
	{
		if (textureLoc == null)
		{
			return null;
		}
		IAsset asset = null;
		try
		{
			asset = game.AssetManager.TryGet(textureLoc);
			byte[] data;
			if (asset == null)
			{
				game.Logger.Warning("Texture asset '{0}' not found (defined in {1}).", textureLoc, textureLoc.Source);
				data = game.AssetManager.Get("textures/unknown.png").Data;
			}
			else
			{
				data = asset.Data;
			}
			BitmapRef bitmapRef = game.Platform.CreateBitmapFromPng(data, data.Length);
			if (bitmapRef.Width / 4 * 4 != bitmapRef.Width)
			{
				game.Platform.Logger.Warning("Texture {0} width is not divisible by 4, will probably glitch when mipmapped", textureLoc);
			}
			else if (bitmapRef.Height / 4 * 4 != bitmapRef.Height)
			{
				game.Platform.Logger.Warning("Texture {0} height is not divisible by 4, will probably glitch when mipmapped", textureLoc);
			}
			return bitmapRef;
		}
		catch (Exception)
		{
			game.Logger.Notification("The quest as to why Fulgen crashes here.");
			game.Logger.Notification("textureLoc={0}", textureLoc);
			game.Logger.Notification("asset={0}", asset);
			throw;
		}
	}

	public virtual void LoadShapeTextureCodes(Shape shape)
	{
		textureCodes.Clear();
		if (shape != null)
		{
			ShapeElement[] elements = shape.Elements;
			foreach (ShapeElement elem in elements)
			{
				AddTexturesForElement(elem);
			}
		}
	}

	private void AddTexturesForElement(ShapeElement elem)
	{
		ShapeElementFace[] facesResolved = elem.FacesResolved;
		foreach (ShapeElementFace shapeElementFace in facesResolved)
		{
			if (shapeElementFace != null && shapeElementFace.Texture.Length > 0)
			{
				textureCodes.Add(shapeElementFace.Texture);
			}
		}
		if (elem.Children != null)
		{
			ShapeElement[] children = elem.Children;
			foreach (ShapeElement elem2 in children)
			{
				AddTexturesForElement(elem2);
			}
		}
	}

	public void ResolveTextureDict(FastSmallDictionary<string, CompositeTexture> texturesDict)
	{
		if (texturesDict.TryGetValue("sides", out var value))
		{
			texturesDict.AddIfNotPresent("west", value);
			texturesDict.AddIfNotPresent("east", value);
			texturesDict.AddIfNotPresent("north", value);
			texturesDict.AddIfNotPresent("south", value);
			texturesDict.AddIfNotPresent("up", value);
			texturesDict.AddIfNotPresent("down", value);
		}
		if (texturesDict.TryGetValue("horizontals", out value))
		{
			texturesDict.AddIfNotPresent("west", value);
			texturesDict.AddIfNotPresent("east", value);
			texturesDict.AddIfNotPresent("north", value);
			texturesDict.AddIfNotPresent("south", value);
		}
		if (texturesDict.TryGetValue("verticals", out value))
		{
			texturesDict.AddIfNotPresent("up", value);
			texturesDict.AddIfNotPresent("down", value);
		}
		if (texturesDict.TryGetValue("westeast", out value))
		{
			texturesDict.AddIfNotPresent("west", value);
			texturesDict.AddIfNotPresent("east", value);
		}
		if (texturesDict.TryGetValue("northsouth", out value))
		{
			texturesDict.AddIfNotPresent("north", value);
			texturesDict.AddIfNotPresent("south", value);
		}
		if (!texturesDict.TryGetValue("all", out value))
		{
			return;
		}
		texturesDict.Remove("all");
		foreach (string textureCode in textureCodes)
		{
			texturesDict.AddIfNotPresent(textureCode, value);
		}
	}

	public virtual void Dispose()
	{
		foreach (TextureAtlas atlass in Atlasses)
		{
			atlass.DisposePixels();
		}
		if (AtlasTextures != null)
		{
			for (int i = 0; i < AtlasTextures.Count; i++)
			{
				AtlasTextures[i].Dispose();
			}
			AtlasTextures.Clear();
			DisposeFrameBuffer();
		}
	}

	private void DisposeFrameBuffer()
	{
		if (atlasFramebuffer != null)
		{
			game.Platform.DisposeFrameBuffer(atlasFramebuffer, disposeTextures: false);
		}
		atlasFramebuffer = null;
	}

	public int GetRandomColor(int textureSubId)
	{
		return TextureAtlasPositionsByTextureSubId[textureSubId].RndColors[rand.Next(30)];
	}

	public int GetRandomColor(int textureSubId, int rndIndex)
	{
		TextureAtlasPosition texPos = TextureAtlasPositionsByTextureSubId[textureSubId];
		return GetRandomColor(texPos, rndIndex);
	}

	public int GetRandomColor(TextureAtlasPosition texPos, int rndIndex)
	{
		if (rndIndex < 0)
		{
			rndIndex = rand.Next(30);
		}
		return texPos.RndColors[rndIndex];
	}

	public int[] GetRandomColors(TextureAtlasPosition texPos)
	{
		return texPos.RndColors;
	}

	public int GetAverageColor(int textureSubId)
	{
		return TextureAtlasPositionsByTextureSubId[textureSubId].AvgColor;
	}

	public bool InsertTexture(byte[] bytes, out int textureSubId, out TextureAtlasPosition texPos, float alphaTest = 0f)
	{
		BitmapExternal bmp = game.api.Render.BitmapCreateFromPng(bytes);
		return InsertTexture(bmp, out textureSubId, out texPos, alphaTest);
	}

	public bool InsertTextureCached(AssetLocation path, byte[] bytes, out int textureSubId, out TextureAtlasPosition texPos, float alphaTest = 0f)
	{
		BitmapExternal bitmap = game.api.Render.BitmapCreateFromPng(bytes);
		return GetOrInsertTexture(path, out textureSubId, out texPos, () => bitmap, alphaTest);
	}

	public bool GetOrInsertTexture(CompositeTexture ct, out int textureSubId, out TextureAtlasPosition texPos, float alphaTest = 0f)
	{
		ct.Bake(game.AssetManager);
		AssetLocationAndSource alocs = new AssetLocationAndSource(ct.Baked.BakedName, "Shape file ", ct.Base);
		return GetOrInsertTexture(ct.Baked.BakedName, out textureSubId, out texPos, () => LoadCompositeBitmap(game, alocs), alphaTest);
	}

	public virtual void CollectAndBakeTexturesFromShape(Shape compositeShape, IDictionary<string, CompositeTexture> targetDict, AssetLocation baseLoc)
	{
		throw new NotImplementedException();
	}
}
