using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public interface IRenderAPI
{
	WireframeModes WireframeDebugRender { get; }

	PerceptionEffects PerceptionEffects { get; }

	Stack<ElementBounds> ScissorStack { get; }

	int TextureSize { get; }

	FrustumCulling DefaultFrustumCuller { get; }

	List<FrameBufferRef> FrameBuffers { get; }

	FrameBufferRef FrameBuffer { set; }

	DefaultShaderUniforms ShaderUniforms { get; }

	ModelTransform CameraOffset { get; }

	EnumRenderStage CurrentRenderStage { get; }

	double[] PerspectiveViewMat { get; }

	double[] PerspectiveProjectionMat { get; }

	[Obsolete("Please use ElementGeometrics.DecorativeFontName instead")]
	string DecorativeFontName { get; }

	[Obsolete("Please use ElementGeometrics.StandardFontName instead.")]
	string StandardFontName { get; }

	int FrameWidth { get; }

	int FrameHeight { get; }

	EnumCameraMode CameraType { get; }

	bool CameraStuck { get; }

	StackMatrix4 MvMatrix { get; }

	StackMatrix4 PMatrix { get; }

	float LineWidth { set; }

	float[] CurrentModelviewMatrix { get; }

	double[] CameraMatrixOrigin { get; }

	float[] CameraMatrixOriginf { get; }

	float[] CurrentProjectionMatrix { get; }

	float[] CurrentShadowProjectionMatrix { get; }

	IStandardShaderProgram StandardShader { get; }

	IShaderProgram CurrentActiveShader { get; }

	Vec3f AmbientColor { get; }

	Vec4f FogColor { get; }

	float FogMin { get; }

	float FogDensity { get; }

	bool UseSSBOs { get; }

	ItemRenderInfo GetItemStackRenderInfo(ItemSlot inSlot, EnumItemRenderTarget ground, float dt);

	void Reset3DProjection();

	void Set3DProjection(float zfar, float fov);

	string GlGetError();

	void CheckGlError(string message = "");

	void GlMatrixModeModelView();

	void GlPushMatrix();

	void GlPopMatrix();

	void GlLoadMatrix(double[] matrix);

	void GlTranslate(float x, float y, float z);

	void GlTranslate(double x, double y, double z);

	void GlScale(float x, float y, float z);

	void GlRotate(float angle, float x, float y, float z);

	void GlEnableCullFace();

	void GlDisableCullFace();

	void GLEnableDepthTest();

	void GLDisableDepthTest();

	void GlViewport(int x, int y, int width, int height);

	void GLDepthMask(bool on);

	void GlGenerateTex2DMipmaps();

	void GlToggleBlend(bool blend, EnumBlendMode blendMode = EnumBlendMode.Standard);

	void PushScissor(ElementBounds bounds, bool stacking = false);

	void PopScissor();

	void GlScissor(int x, int y, int width, int height);

	void GlScissorFlag(bool enable);

	BitmapExternal BitmapCreateFromPng(byte[] pngdata);

	[Obsolete("Use LoadOrUpdateTextureFromBgra(int[] bgraPixels, bool linearMag, int clampMode, ref LoadedTexture intoTexture); instead. This method cannot warn you of memory leaks when the texture is not properly disposed.")]
	int LoadTextureFromBgra(int[] bgraPixels, int width, int height, bool linearMag, int clampMode);

	[Obsolete("Use LoadOrUpdateTextureFromRgba(int[] bgraPixels, bool linearMag, int clampMode, ref LoadedTexture intoTexture); instead. This method cannot warn you of memory leaks when the texture is not properly disposed.")]
	int LoadTextureFromRgba(int[] rgbaPixels, int width, int height, bool linearMag, int clampMode);

	void LoadOrUpdateTextureFromBgra(int[] bgraPixels, bool linearMag, int clampMode, ref LoadedTexture intoTexture);

	void LoadOrUpdateTextureFromRgba(int[] rgbaPixels, bool linearMag, int clampMode, ref LoadedTexture intoTexture);

	void LoadTexture(IBitmap bmp, ref LoadedTexture intoTexture, bool linearMag = false, int clampMode = 0, bool generateMipmaps = false);

	void GLDeleteTexture(int textureId);

	int GlGetMaxTextureSize();

	void BindTexture2d(int textureid);

	int GetOrLoadTexture(AssetLocation name);

	void GetOrLoadTexture(AssetLocation name, ref LoadedTexture intoTexture);

	void GetOrLoadTexture(AssetLocation name, BitmapRef bmp, ref LoadedTexture intoTexture);

	bool RemoveTexture(AssetLocation name);

	int GetUniformLocation(int shaderProgramNumber, string name);

	IShaderProgram GetEngineShader(EnumShaderProgram program);

	IShaderProgram GetShader(int shaderProgramNumber);

	IStandardShaderProgram PreparedStandardShader(int posX, int posY, int posZ, Vec4f colorMul = null);

	MeshRef AllocateEmptyMesh(int xyzSize, int normalSize, int uvSize, int rgbaSize, int flagsSize, int indicesSize, CustomMeshDataPartFloat customFloats, CustomMeshDataPartShort customShorts, CustomMeshDataPartByte customBytes, CustomMeshDataPartInt customInts, EnumDrawMode drawMode = EnumDrawMode.Triangles, bool staticDraw = true);

	MeshRef UploadMesh(MeshData data);

	MultiTextureMeshRef UploadMultiTextureMesh(MeshData data);

	void UpdateMesh(MeshRef meshRef, MeshData updatedata);

	void UpdateChunkMesh(MeshRef meshRef, MeshData updatedata);

	void DeleteMesh(MeshRef vao);

	UBORef CreateUBO(IShaderProgram shaderProgram, int bindingPoint, string blockName, int size);

	void RenderMesh(MeshRef meshRef);

	void RenderMultiTextureMesh(MultiTextureMeshRef mmr, string textureSampleName, int textureNumber = 0);

	void RenderMeshInstanced(MeshRef meshRef, int quantity = 1);

	void RenderMesh(MeshRef meshRef, int[] indicesStarts, int[] indicesSizes, int groupCount);

	void RenderTextureIntoTexture(LoadedTexture fromTexture, float sourceX, float sourceY, float sourceWidth, float sourceHeight, LoadedTexture intoTexture, float targetX, float targetY, float alphaTest = 0.005f);

	[Obsolete("Use RenderItemstackToGui(inSlot, ....) instead")]
	void RenderItemstackToGui(ItemStack itemstack, double posX, double posY, double posZ, float size, int color, bool shading = true, bool rotate = false, bool showStackSize = true);

	void RenderItemstackToGui(ItemSlot inSlot, double posX, double posY, double posZ, float size, int color, bool shading = true, bool rotate = false, bool showStackSize = true);

	void RenderItemstackToGui(ItemSlot inSlot, double posX, double posY, double posZ, float size, int color, float dt, bool shading = true, bool rotate = false, bool showStackSize = true);

	bool RenderItemStackToAtlas(ItemStack stack, ITextureAtlasAPI atlas, int size, Action<int> onComplete, int color = -1, float sepiaLevel = 0f, float scale = 1f);

	TextureAtlasPosition GetTextureAtlasPosition(ItemStack itemstack);

	void RenderEntityToGui(float dt, Entity entity, double posX, double posY, double posZ, float yawDelta, float size, int color);

	void Render2DTexturePremultipliedAlpha(int textureid, float posX, float posY, float width, float height, float z = 50f, Vec4f color = null);

	void Render2DTexturePremultipliedAlpha(int textureid, double posX, double posY, double width, double height, float z = 50f, Vec4f color = null);

	void Render2DTexturePremultipliedAlpha(int textureid, ElementBounds bounds, float z = 50f, Vec4f color = null);

	void RenderTexture(int textureid, double posX, double posY, double width, double height, float z = 50f, Vec4f color = null);

	void Render2DTexture(int textureid, float posX, float posY, float width, float height, float z = 50f, Vec4f color = null);

	void Render2DTexture(MeshRef quadModel, int textureid, float posX, float posY, float width, float height, float z = 50f);

	void Render2DTexture(MultiTextureMeshRef quadModel, float posX, float posY, float width, float height, float z = 50f);

	void Render2DTexture(int textureid, ElementBounds bounds, float z = 50f, Vec4f color = null);

	void Render2DLoadedTexture(LoadedTexture textTexture, float posX, float posY, float z = 50f);

	void RenderRectangle(float posX, float posY, float posZ, float width, float height, int color);

	void RenderLine(BlockPos origin, float posX1, float posY1, float posZ1, float posX2, float posY2, float posZ2, int color);

	FrameBufferRef CreateFrameBuffer(LoadedTexture intoTexture);

	void RenderTextureIntoFrameBuffer(int atlasTextureId, LoadedTexture fromTexture, float sourceX, float sourceY, float sourceWidth, float sourceHeight, FrameBufferRef fb, float targetX, float targetY, float alphaTest = 0.005f);

	void DestroyFrameBuffer(FrameBufferRef fb);

	void AddPointLight(IPointLight pointlight);

	void RemovePointLight(IPointLight pointlight);
}
