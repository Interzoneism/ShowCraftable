using System;
using System.Collections.Generic;
using Cairo;
using OpenTK.Graphics.OpenGL;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.Client.NoObf;

public class InventoryItemRenderer : IRenderer, IDisposable
{
	private ClientMain game;

	private Dictionary<string, LoadedTexture> StackSizeTextures = new Dictionary<string, LoadedTexture>();

	private MeshRef quadModelRef;

	private CairoFont stackSizeFont;

	private Matrixf modelMat = new Matrixf();

	private Queue<AtlasRenderTask> queue = new Queue<AtlasRenderTask>();

	private int[] clearPixels;

	public double RenderOrder => 9.0;

	public int RenderRange => 24;

	public InventoryItemRenderer(ClientMain game)
	{
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		this.game = game;
		MeshData quad = QuadMeshUtil.GetQuad();
		quad.Uv = new float[8] { 1f, 1f, 0f, 1f, 0f, 0f, 1f, 0f };
		quad.Rgba = new byte[16];
		quad.Rgba.Fill(byte.MaxValue);
		quadModelRef = game.Platform.UploadMesh(quad);
		stackSizeFont = CairoFont.WhiteSmallText().WithFontSize((float)GuiStyle.SmallishFontSize);
		stackSizeFont.FontWeight = (FontWeight)1;
		stackSizeFont.Color = new double[4] { 1.0, 1.0, 1.0, 1.0 };
		stackSizeFont.StrokeColor = new double[4] { 0.0, 0.0, 0.0, 1.0 };
		stackSizeFont.StrokeWidth = (double)ClientSettings.GUIScale + 0.25;
		ClientSettings.Inst.AddWatcher<float>("guiScale", delegate
		{
			stackSizeFont.StrokeWidth = (double)ClientSettings.GUIScale + 0.25;
			foreach (KeyValuePair<string, LoadedTexture> stackSizeTexture in StackSizeTextures)
			{
				stackSizeTexture.Value?.Dispose();
			}
			StackSizeTextures.Clear();
		});
		game.eventManager.RegisterRenderer(this, EnumRenderStage.Ortho, "renderstacktoatlas");
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		while (queue.Count > 0)
		{
			AtlasRenderTask atlasRenderTask = queue.Dequeue();
			RenderItemStackToAtlas(atlasRenderTask.Stack, atlasRenderTask.Atlas, atlasRenderTask.Size, atlasRenderTask.OnComplete, atlasRenderTask.Color, atlasRenderTask.SepiaLevel, atlasRenderTask.Scale);
		}
	}

	public bool RenderItemStackToAtlas(ItemStack stack, ITextureAtlasAPI atlas, int size, Action<int> onComplete, int color = -1, float sepiaLevel = 0f, float scale = 1f)
	{
		if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
		{
			game.EnqueueMainThreadTask(delegate
			{
				queue.Enqueue(new AtlasRenderTask
				{
					Stack = stack,
					Atlas = atlas,
					Size = size,
					Color = color,
					SepiaLevel = sepiaLevel,
					OnComplete = onComplete,
					Scale = scale
				});
			}, "enqueueRenderTask");
			return false;
		}
		if (game.currentRenderStage != EnumRenderStage.Ortho)
		{
			queue.Enqueue(new AtlasRenderTask
			{
				Stack = stack,
				Atlas = atlas,
				Size = size,
				OnComplete = onComplete
			});
			return false;
		}
		if (!atlas.AllocateTextureSpace(size, size, out var textureSubId, out var texPos))
		{
			throw new Exception($"Was not able to allocate texture space of size {size}x{size} for item stack '{stack.GetName()}', maybe atlas is full?");
		}
		FramebufferAttrsAttachment[] attachments = new FramebufferAttrsAttachment[2]
		{
			new FramebufferAttrsAttachment
			{
				AttachmentType = EnumFramebufferAttachment.ColorAttachment0,
				Texture = new RawTexture
				{
					Width = atlas.Size.Width,
					Height = atlas.Size.Height,
					TextureId = texPos.atlasTextureId,
					PixelFormat = EnumTexturePixelFormat.Rgba,
					PixelInternalFormat = EnumTextureInternalFormat.Rgba8
				}
			},
			new FramebufferAttrsAttachment
			{
				AttachmentType = EnumFramebufferAttachment.DepthAttachment,
				Texture = new RawTexture
				{
					Width = atlas.Size.Width,
					Height = atlas.Size.Height,
					PixelFormat = EnumTexturePixelFormat.DepthComponent,
					PixelInternalFormat = EnumTextureInternalFormat.DepthComponent32
				}
			}
		};
		FrameBufferRef frameBufferRef = game.Platform.CreateFramebuffer(new FramebufferAttrs("atlasRenderer", atlas.Size.Width, atlas.Size.Height)
		{
			Attachments = attachments
		});
		game.Platform.LoadFrameBuffer(frameBufferRef);
		game.Platform.GlEnableDepthTest();
		game.Platform.GlDisableCullFace();
		game.Platform.GlToggleBlend(on: true);
		game.Platform.ClearFrameBuffer(frameBufferRef, null, clearDepthBuffer: true, clearColorBuffers: false);
		game.OrthoMode(atlas.Size.Width, atlas.Size.Height, inverseY: true);
		float num = texPos.x1 * (float)atlas.Size.Width + (float)size / 2f;
		float num2 = texPos.y1 * (float)atlas.Size.Height + (float)size / 2f;
		game.guiShaderProg.SepiaLevel = sepiaLevel;
		if (clearPixels == null || clearPixels.Length < size * size)
		{
			clearPixels = new int[size * size];
		}
		game.Platform.BindTexture2d(texPos.atlasTextureId);
		GL.TexSubImage2D<int>((TextureTarget)3553, 0, (int)(texPos.x1 * (float)atlas.Size.Width), (int)(texPos.y1 * (float)atlas.Size.Height), size, size, (PixelFormat)32993, (PixelType)5121, clearPixels);
		game.api.renderapi.inventoryItemRenderer.RenderItemstackToGui(new DummySlot(stack), num, num2, 500.0, (float)(size / 2) * scale, color, shading: true, origRotate: false, showStackSize: false);
		game.PerspectiveMode();
		game.guiShaderProg.SepiaLevel = 0f;
		game.Platform.LoadFrameBuffer(EnumFrameBuffer.Default);
		frameBufferRef.ColorTextureIds = Array.Empty<int>();
		game.Platform.DisposeFrameBuffer(frameBufferRef);
		onComplete(textureSubId);
		return true;
	}

	public void RenderEntityToGui(float dt, Entity entity, double posX, double posY, double posZ, float yawDelta, float size, int color)
	{
		game.guiShaderProg.RgbaIn = new Vec4f(1f, 1f, 1f, 1f);
		game.guiShaderProg.ExtraGlow = 0;
		game.guiShaderProg.ApplyColor = 1;
		game.guiShaderProg.Tex2d2D = game.EntityAtlasManager.AtlasTextures[0].TextureId;
		game.guiShaderProg.AlphaTest = 0.1f;
		game.guiShaderProg.NoTexture = 0f;
		game.guiShaderProg.OverlayOpacity = 0f;
		game.guiShaderProg.NormalShaded = 1;
		entity.Properties.Client.Renderer.RenderToGui(dt, posX, posY, posZ, yawDelta, size);
		game.guiShaderProg.NormalShaded = 0;
	}

	public void RenderItemstackToGui(ItemSlot inSlot, double posX, double posY, double posZ, float size, int color, bool shading = true, bool origRotate = false, bool showStackSize = true)
	{
		RenderItemstackToGui(inSlot, posX, posY, posZ, size, color, 0f, shading, origRotate, showStackSize);
	}

	public void RenderItemstackToGui(ItemSlot inSlot, double posX, double posY, double posZ, float size, int color, float dt, bool shading = true, bool origRotate = false, bool showStackSize = true)
	{
		ItemStack itemstack = inSlot.Itemstack;
		ItemRenderInfo itemStackRenderInfo = GetItemStackRenderInfo(game, inSlot, EnumItemRenderTarget.Gui, dt);
		if (itemStackRenderInfo.ModelRef == null)
		{
			return;
		}
		itemstack.Collectible.InGuiIdle(game, itemstack);
		ModelTransform transform = itemStackRenderInfo.Transform;
		if (transform == null)
		{
			return;
		}
		bool flag = itemstack.Class == EnumItemClass.Block;
		bool flag2 = origRotate && itemStackRenderInfo.Transform.Rotate;
		modelMat.Identity();
		modelMat.Translate((int)posX - ((itemstack.Class == EnumItemClass.Item) ? 3 : 0), (int)posY - ((itemstack.Class == EnumItemClass.Item) ? 1 : 0), (float)posZ);
		modelMat.Translate((double)transform.Origin.X + GuiElement.scaled(transform.Translation.X), (double)transform.Origin.Y + GuiElement.scaled(transform.Translation.Y), (double)(transform.Origin.Z * size) + GuiElement.scaled(transform.Translation.Z));
		modelMat.Scale(size * transform.ScaleXYZ.X, size * transform.ScaleXYZ.Y, size * transform.ScaleXYZ.Z);
		modelMat.RotateXDeg(transform.Rotation.X + (flag ? 180f : 0f));
		modelMat.RotateYDeg(transform.Rotation.Y - (float)((!flag) ? 1 : (-1)) * (flag2 ? ((float)game.Platform.EllapsedMs / 50f) : 0f));
		modelMat.RotateZDeg(transform.Rotation.Z);
		modelMat.Translate(0f - transform.Origin.X, 0f - transform.Origin.Y, 0f - transform.Origin.Z);
		int num = (int)itemstack.Collectible.GetTemperature(game, itemstack);
		float[] incandescenceColorAsColor4f = ColorUtil.GetIncandescenceColorAsColor4f(num);
		float[] array = ColorUtil.ToRGBAFloats(color);
		int num2 = GameMath.Clamp((num - 550) / 2, 0, 255);
		bool flag3 = itemstack.Attributes.HasAttribute("temperature");
		game.guiShaderProg.NormalShaded = (itemStackRenderInfo.NormalShaded ? 1 : 0);
		game.guiShaderProg.RgbaIn = new Vec4f(array[0], array[1], array[2], array[3]);
		game.guiShaderProg.ExtraGlow = num2;
		game.guiShaderProg.TempGlowMode = (flag3 ? 1 : 0);
		game.guiShaderProg.RgbaGlowIn = (flag3 ? new Vec4f(incandescenceColorAsColor4f[0], incandescenceColorAsColor4f[1], incandescenceColorAsColor4f[2], (float)num2 / 255f) : new Vec4f(1f, 1f, 1f, (float)num2 / 255f));
		game.guiShaderProg.ApplyColor = (itemStackRenderInfo.ApplyColor ? 1 : 0);
		game.guiShaderProg.AlphaTest = itemStackRenderInfo.AlphaTest;
		game.guiShaderProg.OverlayOpacity = itemStackRenderInfo.OverlayOpacity;
		if (itemStackRenderInfo.OverlayTexture != null && itemStackRenderInfo.OverlayOpacity > 0f)
		{
			game.guiShaderProg.Tex2dOverlay2D = itemStackRenderInfo.OverlayTexture.TextureId;
			game.guiShaderProg.OverlayTextureSize = new Vec2f(itemStackRenderInfo.OverlayTexture.Width, itemStackRenderInfo.OverlayTexture.Height);
			game.guiShaderProg.BaseTextureSize = new Vec2f(itemStackRenderInfo.TextureSize.Width, itemStackRenderInfo.TextureSize.Height);
			TextureAtlasPosition textureAtlasPosition = GetTextureAtlasPosition(game, itemstack);
			game.guiShaderProg.BaseUvOrigin = new Vec2f(textureAtlasPosition.x1, textureAtlasPosition.y1);
		}
		game.guiShaderProg.ModelMatrix = modelMat.Values;
		game.guiShaderProg.ProjectionMatrix = game.CurrentProjectionMatrix;
		game.guiShaderProg.ModelViewMatrix = modelMat.ReverseMul(game.CurrentModelViewMatrix).Values;
		game.guiShaderProg.ApplyModelMat = 1;
		if (game.api.eventapi.itemStackRenderersByTarget[(int)itemstack.Collectible.ItemClass][0].TryGetValue(itemstack.Collectible.Id, out var value))
		{
			value(inSlot, itemStackRenderInfo, modelMat, posX, posY, posZ, size, color, origRotate, showStackSize);
			game.guiShaderProg.ApplyModelMat = 0;
			game.guiShaderProg.NormalShaded = 0;
			game.guiShaderProg.RgbaGlowIn = new Vec4f(0f, 0f, 0f, 0f);
			game.guiShaderProg.AlphaTest = 0f;
			return;
		}
		game.guiShaderProg.DamageEffect = itemStackRenderInfo.DamageEffect;
		game.api.renderapi.RenderMultiTextureMesh(itemStackRenderInfo.ModelRef, "tex2d");
		game.guiShaderProg.ApplyModelMat = 0;
		game.guiShaderProg.NormalShaded = 0;
		game.guiShaderProg.TempGlowMode = 0;
		game.guiShaderProg.DamageEffect = 0f;
		LoadedTexture value2 = null;
		if (itemstack.StackSize != 1 && showStackSize)
		{
			float num3 = size / (float)GuiElement.scaled(25.600000381469727);
			string key = itemstack.StackSize + "-" + (int)(num3 * 100f);
			if (!StackSizeTextures.TryGetValue(key, out value2))
			{
				value2 = (StackSizeTextures[key] = GenStackSizeTexture(itemstack.StackSize, num3));
			}
		}
		if (value2 != null)
		{
			float num4 = size / (float)GuiElement.scaled(25.600000381469727);
			game.Platform.GlToggleBlend(on: true, EnumBlendMode.PremultipliedAlpha);
			game.Render2DLoadedTexture(value2, (int)(posX + (double)size + 1.0 - (double)value2.Width), (int)(posY + (double)num4 * GuiElement.scaled(3.0) - GuiElement.scaled(4.0)), (int)posZ + 100);
			game.Platform.GlToggleBlend(on: true);
		}
		game.guiShaderProg.AlphaTest = 0f;
		game.guiShaderProg.RgbaGlowIn = new Vec4f(0f, 0f, 0f, 0f);
	}

	private LoadedTexture GenStackSizeTexture(int stackSize, float fontSizeMultiplier = 1f)
	{
		CairoFont cairoFont = stackSizeFont.Clone();
		cairoFont.UnscaledFontsize *= fontSizeMultiplier;
		return game.api.guiapi.TextTexture.GenTextTexture(stackSize.ToString() ?? "", cairoFont);
	}

	public static ItemRenderInfo GetItemStackRenderInfo(ClientMain game, ItemSlot inSlot, EnumItemRenderTarget target, float dt)
	{
		ItemStack itemstack = inSlot.Itemstack;
		if (itemstack == null || itemstack.Collectible.Code == null)
		{
			return new ItemRenderInfo();
		}
		ItemRenderInfo renderinfo = new ItemRenderInfo();
		renderinfo.dt = dt;
		switch (target)
		{
		case EnumItemRenderTarget.Ground:
			renderinfo.Transform = itemstack.Collectible.GroundTransform;
			break;
		case EnumItemRenderTarget.Gui:
			renderinfo.Transform = itemstack.Collectible.GuiTransform;
			break;
		case EnumItemRenderTarget.HandTp:
			renderinfo.Transform = itemstack.Collectible.TpHandTransform;
			break;
		case EnumItemRenderTarget.HandTpOff:
			renderinfo.Transform = itemstack.Collectible.TpOffHandTransform ?? itemstack.Collectible.TpHandTransform;
			break;
		}
		if (itemstack.Collectible?.Code == null)
		{
			renderinfo.ModelRef = ((itemstack.Block == null) ? game.TesselatorManager.unknownItemModelRef : game.TesselatorManager.unknownBlockModelRef);
		}
		else if (itemstack.Class == EnumItemClass.Block)
		{
			renderinfo.ModelRef = game.TesselatorManager.blockModelRefsInventory[itemstack.Id];
		}
		else
		{
			int num = (itemstack.TempAttributes.HasAttribute("renderVariant") ? itemstack.TempAttributes.GetInt("renderVariant") : itemstack.Attributes.GetInt("renderVariant"));
			if (num != 0 && (num < 0 || game.TesselatorManager.altItemModelRefsInventory[itemstack.Id] == null || game.TesselatorManager.altItemModelRefsInventory[itemstack.Id].Length < num - 1))
			{
				game.Logger.Warning("Itemstack {0} has an invalid renderVariant {1}. No such model variant exists. Will reset to 0", itemstack.GetName(), num);
				itemstack.TempAttributes.SetInt("renderVariant", 0);
				num = 0;
			}
			if (num == 0)
			{
				renderinfo.ModelRef = game.TesselatorManager.itemModelRefsInventory[itemstack.Id];
			}
			else
			{
				renderinfo.ModelRef = game.TesselatorManager.altItemModelRefsInventory[itemstack.Id][num - 1];
			}
		}
		ItemRenderInfo itemRenderInfo = renderinfo;
		int normalShaded;
		if (itemstack.Class != EnumItemClass.Block)
		{
			CompositeShape shape = itemstack.Item.Shape;
			normalShaded = ((shape != null && !shape.VoxelizeTexture) ? 1 : 0);
		}
		else
		{
			CompositeShape shape2 = itemstack.Block.Shape;
			normalShaded = ((shape2 != null && !shape2.VoxelizeTexture) ? 1 : 0);
		}
		itemRenderInfo.NormalShaded = (byte)normalShaded != 0;
		renderinfo.TextureSize.Width = ((itemstack.Class == EnumItemClass.Block) ? game.BlockAtlasManager.Size.Width : game.ItemAtlasManager.Size.Width);
		renderinfo.TextureSize.Height = ((itemstack.Class == EnumItemClass.Block) ? game.BlockAtlasManager.Size.Height : game.ItemAtlasManager.Size.Height);
		renderinfo.HalfTransparent = itemstack.Block != null && (itemstack.Block.RenderPass == EnumChunkRenderPass.Meta || itemstack.Block.RenderPass == EnumChunkRenderPass.Transparent);
		renderinfo.AlphaTest = itemstack.Collectible.RenderAlphaTest;
		renderinfo.CullFaces = itemstack.Block != null && (itemstack.Block.RenderPass == EnumChunkRenderPass.Opaque || itemstack.Block.RenderPass == EnumChunkRenderPass.TopSoil);
		renderinfo.ApplyColor = renderinfo.NormalShaded;
		TransitionState transitionState = itemstack.Collectible.UpdateAndGetTransitionState(game, inSlot, EnumTransitionType.Perish);
		if (transitionState != null && transitionState.TransitionLevel > 0f)
		{
			renderinfo.SetRotOverlay(game.api, transitionState.TransitionLevel);
		}
		renderinfo.InSlot = inSlot;
		inSlot.OnBeforeRender(renderinfo);
		itemstack.Collectible.OnBeforeRender(game.api, itemstack, target, ref renderinfo);
		return renderinfo;
	}

	public static TextureAtlasPosition GetTextureAtlasPosition(ClientMain game, IItemStack itemstack)
	{
		int index = BlockFacing.UP.Index;
		if (itemstack.Collectible.Code == null)
		{
			return game.BlockAtlasManager.UnknownTexturePos;
		}
		if (itemstack.Class == EnumItemClass.Block)
		{
			int num = game.FastBlockTextureSubidsByBlockAndFace[itemstack.Id][index];
			return game.BlockAtlasManager.TextureAtlasPositionsByTextureSubId[num];
		}
		if (itemstack.Item.FirstTexture == null)
		{
			return game.BlockAtlasManager.UnknownTexturePos;
		}
		int textureSubId = itemstack.Item.FirstTexture.Baked.TextureSubId;
		return game.ItemAtlasManager.TextureAtlasPositionsByTextureSubId[textureSubId];
	}

	public int GetCurrentBlockOrItemTextureId(int side)
	{
		ItemSlot activeHotbarSlot = game.player.inventoryMgr.ActiveHotbarSlot;
		if (activeHotbarSlot != null && activeHotbarSlot.Itemstack.Class == EnumItemClass.Block)
		{
			return game.FastBlockTextureSubidsByBlockAndFace[activeHotbarSlot.Itemstack.Id][side];
		}
		return 0;
	}

	public int GetBlockOrItemTextureId(BlockFacing facing, IItemStack itemstack)
	{
		if (itemstack.Class != EnumItemClass.Block)
		{
			return 0;
		}
		return game.FastBlockTextureSubidsByBlockAndFace[itemstack.Id][facing.Index];
	}

	public void Dispose()
	{
		quadModelRef?.Dispose();
		foreach (KeyValuePair<string, LoadedTexture> stackSizeTexture in StackSizeTextures)
		{
			stackSizeTexture.Value?.Dispose();
		}
	}
}
