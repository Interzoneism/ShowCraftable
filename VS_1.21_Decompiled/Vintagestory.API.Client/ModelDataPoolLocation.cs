using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public class ModelDataPoolLocation
{
	public static int VisibleBufIndex;

	public int PoolId;

	public int IndicesStart;

	public int IndicesEnd;

	public int VerticesStart;

	public int VerticesEnd;

	public Sphere FrustumCullSphere;

	public bool FrustumVisible;

	public Bools CullVisible = new Bools(a: true, b: true);

	public int LodLevel;

	public bool Hide;

	public int TransitionCounter;

	private bool UpdateVisibleFlag(bool inFrustum)
	{
		FrustumVisible = inFrustum;
		return FrustumVisible;
	}

	public bool IsVisible(EnumFrustumCullMode mode, FrustumCulling culler)
	{
		switch (mode)
		{
		case EnumFrustumCullMode.CullInstant:
			if (!Hide && CullVisible[VisibleBufIndex])
			{
				return culler.InFrustum(FrustumCullSphere);
			}
			return false;
		case EnumFrustumCullMode.CullInstantShadowPassNear:
			if (!Hide && CullVisible[VisibleBufIndex])
			{
				return culler.InFrustumShadowPass(FrustumCullSphere);
			}
			return false;
		case EnumFrustumCullMode.CullInstantShadowPassFar:
			if (!Hide && CullVisible[VisibleBufIndex] && culler.InFrustumShadowPass(FrustumCullSphere))
			{
				return LodLevel >= 1;
			}
			return false;
		case EnumFrustumCullMode.CullNormal:
			if (!Hide && CullVisible[VisibleBufIndex])
			{
				return UpdateVisibleFlag(culler.InFrustumAndRange(FrustumCullSphere, FrustumVisible, LodLevel));
			}
			return false;
		default:
			return !Hide;
		}
	}
}
