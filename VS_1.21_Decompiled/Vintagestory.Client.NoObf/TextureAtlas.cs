using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf;

public class TextureAtlas
{
	public Dictionary<int, QuadBoundsf> textureBounds;

	public bool Full;

	private TextureAtlasNode rootNode;

	private int[] atlasPixels;

	internal int textureId;

	public int width;

	public int height;

	private float subPixelPaddingx;

	private float subPixelPaddingy;

	public TextureAtlas(int width, int height, float subPixelPaddingx, float subPixelPaddingy)
	{
		atlasPixels = new int[width * height];
		this.subPixelPaddingx = subPixelPaddingx;
		this.subPixelPaddingy = subPixelPaddingy;
		this.width = width;
		this.height = height;
		rootNode = new TextureAtlasNode(0, 0, width, height);
	}

	public bool InsertTexture(int textureSubId, ICoreClientAPI capi, IAsset asset)
	{
		return InsertTexture(textureSubId, asset.ToBitmap(capi));
	}

	public bool InsertTexture(int textureSubId, IBitmap bmp, bool copyPixels = true)
	{
		if (copyPixels)
		{
			return InsertTexture(textureSubId, bmp.Width, bmp.Height, bmp.Pixels);
		}
		return InsertTexture(textureSubId, bmp.Width, bmp.Height, null);
	}

	public bool InsertTexture(int textureSubId, int width, int height, int[] pixels)
	{
		TextureAtlasNode freeNode = rootNode.GetFreeNode(textureSubId, width, height);
		if (freeNode != null)
		{
			freeNode.textureSubId = textureSubId;
			int x = freeNode.bounds.x1;
			int y = freeNode.bounds.y1;
			int num = AtlasWidth();
			if (pixels != null)
			{
				if (pixels.Length % 4 == 0)
				{
					int num2 = num - width;
					int num3 = y * num + x - num2;
					for (int i = 0; i < pixels.Length; i += 4)
					{
						if (i % width == 0)
						{
							num3 += num2;
						}
						atlasPixels[num3] = pixels[i];
						atlasPixels[num3 + 1] = pixels[i + 1];
						atlasPixels[num3 + 2] = pixels[i + 2];
						atlasPixels[num3 + 3] = pixels[i + 3];
						num3 += 4;
					}
				}
				else
				{
					for (int j = 0; j < height; j++)
					{
						int num4 = (y + j) * num + x;
						for (int k = 0; k < width; k++)
						{
							atlasPixels[num4 + k] = pixels[j * width + k];
						}
					}
				}
			}
			return true;
		}
		return false;
	}

	public void UpdateTexture(TextureAtlasPosition tpos, int[] pixels)
	{
		int num = AtlasWidth();
		int num2 = AtlasHeight();
		int num3 = (int)(tpos.x1 * (float)num);
		int num4 = (int)(tpos.y1 * (float)num2);
		int num5 = (int)Math.Round((tpos.x2 - tpos.x1 + 2f * subPixelPaddingx) * (float)num);
		int num6 = (int)Math.Round((tpos.y2 - tpos.y1 + 2f * subPixelPaddingy) * (float)num2);
		for (int i = 0; i < num5; i++)
		{
			for (int j = 0; j < num6; j++)
			{
				atlasPixels[(j + num4) * num + num3 + i] = pixels[j * num5 + i];
			}
		}
	}

	public TextureAtlasPosition AllocateTextureSpace(int textureSubId, int width, int height)
	{
		TextureAtlasNode freeNode = rootNode.GetFreeNode(textureSubId, width, height);
		if (freeNode != null)
		{
			freeNode.textureSubId = textureSubId;
			return new TextureAtlasPosition
			{
				x1 = (float)freeNode.bounds.x1 / (float)AtlasWidth(),
				y1 = (float)freeNode.bounds.y1 / (float)AtlasHeight(),
				x2 = (float)freeNode.bounds.x2 / (float)AtlasWidth(),
				y2 = (float)freeNode.bounds.y2 / (float)AtlasHeight()
			};
		}
		return null;
	}

	public bool FreeTextureSpace(int textureSubId)
	{
		return FreeTextureSpace(rootNode, textureSubId);
	}

	private bool FreeTextureSpace(TextureAtlasNode node, int textureSubId)
	{
		if (node.textureSubId == textureSubId)
		{
			node.textureSubId = null;
			return true;
		}
		if (node.left != null && FreeTextureSpace(node.left, textureSubId))
		{
			return true;
		}
		if (node.right != null && FreeTextureSpace(node.right, textureSubId))
		{
			return true;
		}
		return false;
	}

	public int AtlasWidth()
	{
		return width;
	}

	public int AtlasHeight()
	{
		return height;
	}

	public void Export(string filename, ClientMain game, int atlasTextureId)
	{
		ShaderProgramBase currentShaderProgram = ShaderProgramBase.CurrentShaderProgram;
		currentShaderProgram?.Stop();
		ShaderProgramGui gui = ShaderPrograms.Gui;
		gui.Use();
		FrameBufferRef frameBufferRef = game.Platform.CreateFramebuffer(new FramebufferAttrs("PngExport", width, height)
		{
			Attachments = new FramebufferAttrsAttachment[1]
			{
				new FramebufferAttrsAttachment
				{
					AttachmentType = EnumFramebufferAttachment.ColorAttachment0,
					Texture = new RawTexture
					{
						Width = width,
						Height = height,
						PixelFormat = EnumTexturePixelFormat.Rgba,
						PixelInternalFormat = EnumTextureInternalFormat.Rgba8
					}
				}
			}
		});
		game.Platform.LoadFrameBuffer(frameBufferRef);
		game.Platform.GlEnableDepthTest();
		game.Platform.GlDisableCullFace();
		game.Platform.GlToggleBlend(on: true);
		game.OrthoMode(width, height);
		float[] clearColor = new float[4];
		game.Platform.ClearFrameBuffer(frameBufferRef, clearColor);
		game.api.renderapi.Render2DTexture(atlasTextureId, 0f, 0f, width, height, 50f);
		BitmapRef bitmapRef = game.Platform.GrabScreenshot(width, height, scaleScreenshot: false, flip: true, withAlpha: true);
		game.OrthoMode(game.Width, game.Height);
		game.Platform.UnloadFrameBuffer(frameBufferRef);
		game.Platform.DisposeFrameBuffer(frameBufferRef);
		if (File.Exists(filename + ".png"))
		{
			bitmapRef.Save(filename + "2.png");
		}
		else
		{
			bitmapRef.Save(filename + ".png");
		}
		gui.Stop();
		currentShaderProgram?.Use();
	}

	public int GetPixel(float x, float y)
	{
		int num = (int)GameMath.Clamp(x * (float)width, 0f, width - 1);
		int num2 = (int)GameMath.Clamp(y * (float)height, 0f, height - 1);
		return atlasPixels[num2 * width + num];
	}

	public LoadedTexture Upload(ClientMain game)
	{
		LoadedTexture intoTexture = new LoadedTexture(game.api, 0, AtlasWidth(), AtlasHeight());
		game.Platform.LoadOrUpdateTextureFromBgra_DeferMipMap(atlasPixels, linearMag: false, 1, ref intoTexture);
		textureId = intoTexture.TextureId;
		return intoTexture;
	}

	public void PopulateAtlasPositions(TextureAtlasPosition[] positions, int atlasNumber)
	{
		rootNode.PopulateAtlasPositions(positions, textureId, atlasNumber, AtlasWidth(), AtlasHeight(), subPixelPaddingx, subPixelPaddingy);
	}

	public void DrawToTexture(ClientPlatformAbstract platform, LoadedTexture texAtlas)
	{
		platform.LoadOrUpdateTextureFromBgra(atlasPixels, linearMag: false, 1, ref texAtlas);
	}

	public void DisposePixels()
	{
		atlasPixels = null;
	}

	public void ReinitPixels()
	{
		atlasPixels = new int[width * height];
	}
}
