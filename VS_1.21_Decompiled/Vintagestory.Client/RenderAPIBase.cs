using System;
using System.Collections.Generic;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace Vintagestory.Client;

public abstract class RenderAPIBase : IRenderAPI
{
	internal ClientPlatformAbstract plat;

	internal MeshRef whiteRectangleRef;

	internal bool useSSBOs = true;

	private Stack<ElementBounds> scissorBoundsStacks = new Stack<ElementBounds>();

	public abstract ICoreClientAPI Api { get; }

	public bool UseSSBOs => useSSBOs;

	public string DecorativeFontName => GuiStyle.DecorativeFontName;

	public string StandardFontName => GuiStyle.StandardFontName;

	public int FrameWidth => plat.WindowSize.Width;

	public int FrameHeight => plat.WindowSize.Height;

	public float LineWidth
	{
		set
		{
			plat.GLLineWidth(value);
		}
	}

	public IStandardShaderProgram StandardShader => (IStandardShaderProgram)ShaderRegistry.getProgram(EnumShaderProgram.Standard);

	public IShaderProgram CurrentActiveShader => ShaderProgramBase.CurrentShaderProgram;

	public Stack<ElementBounds> ScissorStack => scissorBoundsStacks;

	public virtual EnumRenderStage CurrentRenderStage
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public virtual double[] PerspectiveViewMat
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public virtual double[] PerspectiveProjectionMat
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public virtual EnumCameraMode CameraType
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public virtual StackMatrix4 MvMatrix
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public virtual StackMatrix4 PMatrix
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public virtual float[] CurrentModelviewMatrix
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public virtual double[] CameraMatrixOrigin
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public virtual float[] CameraMatrixOriginf
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public virtual float[] CurrentProjectionMatrix
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public virtual float[] CurrentShadowProjectionMatrix
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public virtual Vec3f AmbientColor
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public virtual Vec4f FogColor
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public virtual float FogMin
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public virtual float FogDensity
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public virtual ModelTransform CameraOffset
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public virtual DefaultShaderUniforms ShaderUniforms
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public List<FrameBufferRef> FrameBuffers => ScreenManager.Platform.FrameBuffers;

	public virtual int TextureSize
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public virtual FrustumCulling DefaultFrustumCuller
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public virtual PerceptionEffects PerceptionEffects
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public FrameBufferRef FrameBuffer
	{
		set
		{
			ScreenManager.Platform.LoadFrameBuffer(value);
		}
	}

	public WireframeModes WireframeDebugRender { get; set; } = new WireframeModes();

	public bool CameraStuck { get; set; }

	public RenderAPIBase(ClientPlatformAbstract plat)
	{
		this.plat = plat;
		MeshData rectangle = LineMeshUtil.GetRectangle(-1);
		whiteRectangleRef = plat.UploadMesh(rectangle);
	}

	public BitmapExternal BitmapCreateFromPng(byte[] pngdata)
	{
		return (BitmapExternal)plat.CreateBitmapFromPng(pngdata, pngdata.Length);
	}

	public void CheckGlError(string message = "")
	{
		plat.CheckGlError(message);
	}

	public string GlGetError()
	{
		return plat.GlGetError();
	}

	public void BindTexture2d(int textureid)
	{
		plat.BindTexture2d(textureid);
	}

	public MeshRef UploadMesh(MeshData data)
	{
		return plat.UploadMesh(data);
	}

	public MultiTextureMeshRef UploadMultiTextureMesh(MeshData data)
	{
		MeshData[] array = data.SplitByTextureId();
		MeshRef[] array2 = new MeshRef[array.Length];
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i] = plat.UploadMesh(array[i]);
		}
		return new MultiTextureMeshRef(array2, data.TextureIds);
	}

	public void DeleteMesh(MeshRef meshref)
	{
		plat.DeleteMesh(meshref);
	}

	public void RenderMeshInstanced(MeshRef meshRef, int quantity = 1)
	{
		plat.RenderMeshInstanced(meshRef, quantity);
	}

	public void RenderMesh(MeshRef meshRef)
	{
		plat.RenderMesh(meshRef);
	}

	public void RenderMultiTextureMesh(MultiTextureMeshRef mmr, string textureSampleName, int textureNumber = 0)
	{
		for (int i = 0; i < mmr.meshrefs.Length; i++)
		{
			MeshRef vao = mmr.meshrefs[i];
			CurrentActiveShader.BindTexture2D(textureSampleName, mmr.textureids[i], textureNumber);
			plat.RenderMesh(vao);
		}
	}

	public void RenderMesh(MeshRef meshRef, int[] indicesStarts, int[] indicesSizes, int groupCount)
	{
		plat.RenderMesh(meshRef, indicesStarts, indicesSizes, groupCount, useSSBOs);
	}

	public CairoFont GetFont(double unscaledFontSize, string fontName, double[] color, double[] strokeColor = null)
	{
		return new CairoFont(unscaledFontSize, fontName, color, strokeColor);
	}

	public int GetUniformLocation(int shaderProgramNumber, string name)
	{
		return plat.GetUniformLocation(ShaderRegistry.getProgram((EnumShaderProgram)shaderProgramNumber), name);
	}

	public void GLDeleteTexture(int textureId)
	{
		plat.GLDeleteTexture(textureId);
	}

	public void GlDisableCullFace()
	{
		plat.GlDisableCullFace();
	}

	public void GlGenerateTex2DMipmaps()
	{
		plat.GlGenerateTex2DMipmaps();
	}

	public void GlToggleBlend(bool blend, EnumBlendMode blendMode = EnumBlendMode.Standard)
	{
		plat.GlToggleBlend(blend, blendMode);
	}

	public void UpdateMesh(MeshRef meshRef, MeshData updatedata)
	{
		plat.UpdateMesh(meshRef, updatedata);
	}

	public void UpdateChunkMesh(MeshRef meshRef, MeshData updatedata)
	{
		if (useSSBOs && (updatedata.CustomInts == null || updatedata.CustomInts.InterleaveStride < 8))
		{
			plat.UpdateSSBOMesh(meshRef, updatedata);
		}
		else
		{
			plat.UpdateMesh(meshRef, updatedata);
		}
	}

	public MeshRef AllocateEmptyMesh(int xyzSize, int normalsSize, int uvSize, int rgbaSize, int flagsSize, int indicesSize, CustomMeshDataPartFloat customFloats, CustomMeshDataPartShort customShorts, CustomMeshDataPartByte customBytes, CustomMeshDataPartInt customInts, EnumDrawMode drawMode = EnumDrawMode.Triangles, bool staticDraw = true)
	{
		if (useSSBOs && (customInts == null || customInts.InterleaveStride < 8))
		{
			return plat.AllocateEmptySSBOMesh(xyzSize, normalsSize, uvSize, rgbaSize, flagsSize, indicesSize, customFloats, customShorts, customBytes, customInts, drawMode, staticDraw);
		}
		return plat.AllocateEmptyMesh(xyzSize, normalsSize, uvSize, rgbaSize, flagsSize, indicesSize, customFloats, customShorts, customBytes, customInts, drawMode, staticDraw);
	}

	public void GlEnableCullFace()
	{
		plat.GlEnableCullFace();
	}

	public void GLDisableDepthTest()
	{
		plat.GlDisableDepthTest();
	}

	public void GLEnableDepthTest()
	{
		plat.GlEnableDepthTest();
	}

	public IShaderProgram GetEngineShader(EnumShaderProgram program)
	{
		return ShaderRegistry.getProgram(program);
	}

	public IShaderProgram GetShader(int shaderProgramNumber)
	{
		return ShaderRegistry.getProgram(shaderProgramNumber);
	}

	public void PushScissor(ElementBounds bounds, bool stacking = false)
	{
		if (bounds == null)
		{
			plat.GlScissorFlag(enable: false);
		}
		else
		{
			if (stacking && scissorBoundsStacks.Count > 0)
			{
				ElementBounds elementBounds = scissorBoundsStacks.Peek();
				double renderX = elementBounds.renderX;
				double renderY = elementBounds.renderY;
				double val = renderX + elementBounds.InnerWidth;
				double val2 = renderY + elementBounds.InnerHeight;
				double renderX2 = bounds.renderX;
				double renderY2 = bounds.renderY;
				double val3 = renderX2 + bounds.InnerWidth;
				double val4 = renderY2 + bounds.InnerHeight;
				int x = (int)Math.Max(renderX2, renderX);
				int y = (int)((double)plat.WindowSize.Height - Math.Max(renderY2, renderY) - (Math.Min(val4, val2) - Math.Max(renderY2, renderY)));
				int val5 = (int)(Math.Min(val3, val) - Math.Max(renderX2, renderX));
				int val6 = (int)(Math.Min(val4, val2) - Math.Max(renderY2, renderY));
				plat.GlScissor(x, y, Math.Max(0, val5), Math.Max(0, val6));
			}
			else
			{
				plat.GlScissor((int)bounds.renderX, (int)((double)plat.WindowSize.Height - bounds.renderY - bounds.InnerHeight), (int)bounds.InnerWidth, (int)bounds.InnerHeight);
			}
			plat.GlScissorFlag(enable: true);
		}
		scissorBoundsStacks.Push(bounds);
	}

	public void PopScissor()
	{
		scissorBoundsStacks.Pop();
		if (scissorBoundsStacks.Count > 0)
		{
			ElementBounds elementBounds = scissorBoundsStacks.Peek();
			if (elementBounds == null)
			{
				plat.GlScissorFlag(enable: false);
				return;
			}
			plat.GlScissor((int)elementBounds.renderX, (int)((double)plat.WindowSize.Height - elementBounds.renderY - elementBounds.InnerHeight), (int)elementBounds.InnerWidth, (int)elementBounds.InnerHeight);
			plat.GlScissorFlag(enable: true);
		}
		else
		{
			plat.GlScissorFlag(enable: false);
		}
	}

	public void GlScissor(int x, int y, int width, int height)
	{
		plat.GlScissor(x, y, width, height);
	}

	public void GlScissorFlag(bool enable)
	{
		plat.GlScissorFlag(enable);
	}

	public int GlGetMaxTextureSize()
	{
		return plat.GlGetMaxTextureSize();
	}

	public virtual int LoadCairoTexture(ImageSurface surface, bool linearMag)
	{
		return plat.LoadCairoTexture(surface, linearMag);
	}

	public int LoadTextureFromBgra(int[] bgraPixels, int width, int height, bool linearMag, int clampMode)
	{
		LoadedTexture intoTexture = new LoadedTexture(null, 0, width, height);
		plat.LoadOrUpdateTextureFromBgra(bgraPixels, linearMag, clampMode, ref intoTexture);
		intoTexture.TextureId = 0;
		return intoTexture.TextureId;
	}

	public int LoadTextureFromRgba(int[] rgbaPixels, int width, int height, bool linearMag, int clampMode)
	{
		LoadedTexture intoTexture = new LoadedTexture(null, 0, width, height);
		plat.LoadOrUpdateTextureFromRgba(rgbaPixels, linearMag, clampMode, ref intoTexture);
		intoTexture.TextureId = 0;
		return intoTexture.TextureId;
	}

	public void LoadOrUpdateTextureFromBgra(int[] bgraPixels, bool linearMag, int clampMode, ref LoadedTexture intoTexture)
	{
		plat.LoadOrUpdateTextureFromBgra(bgraPixels, linearMag, clampMode, ref intoTexture);
	}

	public void LoadOrUpdateTextureFromRgba(int[] rgbaPixels, bool linearMag, int clampMode, ref LoadedTexture intoTexture)
	{
		plat.LoadOrUpdateTextureFromRgba(rgbaPixels, linearMag, clampMode, ref intoTexture);
	}

	internal virtual void Dispose()
	{
		whiteRectangleRef?.Dispose();
	}

	public virtual ItemRenderInfo GetItemStackRenderInfo(ItemStack itemstack, EnumItemRenderTarget ground, float dt)
	{
		throw new NotImplementedException();
	}

	public virtual ItemRenderInfo GetItemStackRenderInfo(ItemSlot inSlot, EnumItemRenderTarget ground, float dt)
	{
		throw new NotImplementedException();
	}

	public virtual void GlMatrixModeModelView()
	{
		throw new NotImplementedException();
	}

	public virtual void GlPushMatrix()
	{
	}

	public virtual void GlPopMatrix()
	{
	}

	public virtual void GlLoadMatrix(double[] matrix)
	{
		throw new NotImplementedException();
	}

	public virtual void GlTranslate(float x, float y, float z)
	{
	}

	public virtual void GlTranslate(double x, double y, double z)
	{
		throw new NotImplementedException();
	}

	public virtual void GlScale(float x, float y, float z)
	{
		throw new NotImplementedException();
	}

	public virtual void GlRotate(float angle, float x, float y, float z)
	{
		throw new NotImplementedException();
	}

	public virtual Vec4f GetLightRGBs(int x, int y, int z)
	{
		throw new NotImplementedException();
	}

	public virtual bool RemoveTexture(AssetLocation name)
	{
		throw new NotImplementedException();
	}

	public virtual IStandardShaderProgram PreparedStandardShader(int posX, int posY, int posZ, Vec4f colorMul = null)
	{
		throw new NotImplementedException();
	}

	public virtual void RenderTextureIntoTexture(LoadedTexture fromTexture, float sourceX, float sourceY, float sourceWidth, float sourceHeight, LoadedTexture intoTexture, float targetX, float targetY, float alphaTest = 0.005f)
	{
		throw new NotImplementedException();
	}

	public virtual void RenderItemstackToGui(ItemStack itemstack, double posX, double posY, double posZ, float size, int color, bool shading = true, bool rotate = false, bool showStackSize = true)
	{
		throw new NotImplementedException();
	}

	public virtual void RenderItemstackToGui(ItemSlot inSlot, double posX, double posY, double posZ, float size, int color, bool shading = true, bool rotate = false, bool showStackSize = true)
	{
		throw new NotImplementedException();
	}

	public virtual void RenderItemstackToGui(ItemSlot inSlot, double posX, double posY, double posZ, float size, int color, float dt, bool shading = true, bool rotate = false, bool showStackSize = true)
	{
		throw new NotImplementedException();
	}

	public virtual void Render2DTexture(int textureid, float x1, float y1, float width, float height, float z = 50f)
	{
		throw new NotImplementedException();
	}

	public virtual void Render2DTexture(MeshRef quadModel, int textureid, float x1, float y1, float width, float height, float z = 50f)
	{
		throw new NotImplementedException();
	}

	public virtual void Render2DTexture(MultiTextureMeshRef quadModel, float x1, float y1, float width, float height, float z = 50f)
	{
		throw new NotImplementedException();
	}

	public virtual void Render2DTexturePremultipliedAlpha(int textureid, float posX, float posY, float width, float height, float z = 50f, Vec4f color = null)
	{
		throw new NotImplementedException();
	}

	public virtual void Render2DTexturePremultipliedAlpha(int textureid, double posX, double posY, double width, double height, float z = 50f, Vec4f color = null)
	{
		throw new NotImplementedException();
	}

	public virtual void Render2DTexturePremultipliedAlpha(int textureid, ElementBounds bounds, float z = 50f, Vec4f color = null)
	{
		throw new NotImplementedException();
	}

	public virtual void RenderTexture(int textureid, double posX, double posY, double width, double height, float z = 50f, Vec4f color = null)
	{
		throw new NotImplementedException();
	}

	public virtual void Render2DTexture(int textureid, float posX, float posY, float width, float height, float z = 50f, Vec4f color = null)
	{
		throw new NotImplementedException();
	}

	public virtual void Render2DTexture(int textureid, ElementBounds bounds, float z = 50f, Vec4f color = null)
	{
		throw new NotImplementedException();
	}

	public virtual void Render2DLoadedTexture(LoadedTexture textTexture, float posX, float posY, float z = 50f)
	{
		throw new NotImplementedException();
	}

	public virtual void RenderRectangle(float posX, float posY, float posZ, float width, float height, int color)
	{
		throw new NotImplementedException();
	}

	public virtual void RenderEntityToGui(float dt, Entity entity, double posX, double posY, double posZ, float yawDelta, float size, int color)
	{
		throw new NotImplementedException();
	}

	public void GLDepthMask(bool on)
	{
		plat.GlDepthMask(on);
	}

	public virtual void GetOrLoadTexture(AssetLocation name, ref LoadedTexture intoTexture)
	{
		throw new NotImplementedException();
	}

	public virtual void GetOrLoadTexture(AssetLocation name, BitmapRef bmp, ref LoadedTexture intoTexture)
	{
		throw new NotImplementedException();
	}

	public virtual int GetOrLoadTexture(AssetLocation name)
	{
		throw new NotImplementedException();
	}

	public virtual TextureAtlasPosition GetTextureAtlasPosition(ItemStack itemstack)
	{
		throw new NotImplementedException();
	}

	public virtual void RenderLine(BlockPos origin, float posX1, float posY1, float posZ1, float posX2, float posY2, float posZ2, int color)
	{
		throw new NotImplementedException();
	}

	public virtual FrameBufferRef CreateFrameBuffer(LoadedTexture intoTexture)
	{
		throw new NotImplementedException();
	}

	public virtual void RenderTextureIntoFrameBuffer(int atlasTextureId, LoadedTexture fromTexture, float sourceX, float sourceY, float sourceWidth, float sourceHeight, FrameBufferRef fb, float targetX, float targetY, float alphaTest = 0.005f)
	{
		throw new NotImplementedException();
	}

	public virtual void DestroyFrameBuffer(FrameBufferRef fb)
	{
		throw new NotImplementedException();
	}

	public virtual bool RenderItemStackToAtlas(ItemStack stack, ITextureAtlasAPI atlas, int size, Action<int> onComplete, int color = -1, float sepiaLevel = 0f, float scale = 1f)
	{
		throw new NotImplementedException();
	}

	public virtual void AddPointLight(IPointLight pointlight)
	{
		throw new NotImplementedException();
	}

	public virtual void RemovePointLight(IPointLight pointlight)
	{
		throw new NotImplementedException();
	}

	public void GlViewport(int x, int y, int width, int height)
	{
		plat.GlViewport(x, y, width, height);
	}

	public void LoadTexture(IBitmap bmp, ref LoadedTexture intoTexture, bool linearMag = false, int clampMode = 0, bool generateMipmaps = false)
	{
		int num = plat.LoadTexture(bmp, linearMag, clampMode, generateMipmaps);
		if (num != intoTexture.TextureId && intoTexture.TextureId != 0)
		{
			intoTexture.Dispose();
		}
		intoTexture.TextureId = num;
		intoTexture.Width = bmp.Width;
		intoTexture.Height = bmp.Height;
	}

	public virtual void Reset3DProjection()
	{
		throw new NotImplementedException();
	}

	public virtual void Set3DProjection(float zfar, float fov)
	{
		throw new NotImplementedException();
	}

	public UBORef CreateUBO(IShaderProgram shaderProgram, int bindingPoint, string blockName, int size)
	{
		return plat.CreateUBO((shaderProgram as ShaderProgramBase).ProgramId, bindingPoint, blockName, size);
	}
}
