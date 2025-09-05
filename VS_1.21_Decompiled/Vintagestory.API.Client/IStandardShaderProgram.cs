using System;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public interface IStandardShaderProgram : IShaderProgram, IDisposable
{
	int Tex2D { set; }

	int ShadowMapNear2D { set; }

	int ShadowMapFar2D { set; }

	int NormalShaded { set; }

	int TempGlowMode { set; }

	float ZNear { set; }

	float ZFar { set; }

	float AlphaTest { set; }

	float DamageEffect { set; }

	float ExtraZOffset { set; }

	float ExtraGodray { set; }

	Vec3f RgbaAmbientIn { set; }

	Vec4f RgbaLightIn { set; }

	Vec4f RgbaGlowIn { set; }

	Vec4f RgbaFogIn { set; }

	Vec4f RgbaTint { set; }

	Vec4f AverageColor { set; }

	float FogMinIn { set; }

	float FogDensityIn { set; }

	float[] ProjectionMatrix { set; }

	float[] ModelMatrix { set; }

	float[] ViewMatrix { set; }

	int ExtraGlow { set; }

	float[] ToShadowMapSpaceMatrixFar { set; }

	float[] ToShadowMapSpaceMatrixNear { set; }

	float WaterWaveCounter { set; }

	int DontWarpVertices { set; }

	int AddRenderFlags { set; }

	int Tex2dOverlay2D { set; }

	float OverlayOpacity { set; }

	float SsaoAttn { set; }

	Vec2f OverlayTextureSize { set; }

	Vec2f BaseTextureSize { set; }

	Vec2f BaseUvOrigin { set; }
}
