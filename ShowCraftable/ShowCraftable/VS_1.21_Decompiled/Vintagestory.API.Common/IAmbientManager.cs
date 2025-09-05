using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public interface IAmbientManager
{
	AmbientModifier Base { get; }

	OrderedDictionary<string, AmbientModifier> CurrentModifiers { get; }

	Vec4f BlendedFogColor { get; }

	Vec3f BlendedAmbientColor { get; }

	float BlendedFogDensity { get; }

	float BlendedFogBrightness { get; }

	float BlendedFlatFogDensity { get; set; }

	float BlendedFlatFogYOffset { get; set; }

	float BlendedFlatFogYPosForShader { get; set; }

	float BlendedFogMin { get; }

	float BlendedCloudBrightness { get; }

	float BlendedCloudDensity { get; }

	float BlendedSceneBrightness { get; }

	void UpdateAmbient(float dt);
}
