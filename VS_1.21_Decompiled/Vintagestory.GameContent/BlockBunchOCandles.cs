using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockBunchOCandles : Block
{
	internal int QuantityCandles;

	internal Vec3f[] candleWickPositions = new Vec3f[9]
	{
		new Vec3f(3.8f, 4f, 3.8f),
		new Vec3f(7.8f, 7f, 4.8f),
		new Vec3f(12.8f, 2f, 1.8f),
		new Vec3f(4.8f, 5f, 9.8f),
		new Vec3f(7.8f, 2f, 8.8f),
		new Vec3f(12.8f, 6f, 12.8f),
		new Vec3f(11.8f, 4f, 6.8f),
		new Vec3f(1.8f, 1f, 12.8f),
		new Vec3f(6.8f, 4f, 13.8f)
	};

	private Vec3f[][] candleWickPositionsByRot = new Vec3f[4][];

	internal void initRotations()
	{
		for (int i = 0; i < 4; i++)
		{
			Matrixf matrixf = new Matrixf();
			matrixf.Translate(0.5f, 0.5f, 0.5f);
			matrixf.RotateYDeg(i * 90);
			matrixf.Translate(-0.5f, -0.5f, -0.5f);
			Vec3f[] array = (candleWickPositionsByRot[i] = new Vec3f[candleWickPositions.Length]);
			for (int j = 0; j < array.Length; j++)
			{
				Vec4f vec4f = matrixf.TransformVector(new Vec4f(candleWickPositions[j].X / 16f, candleWickPositions[j].Y / 16f, candleWickPositions[j].Z / 16f, 1f));
				array[j] = new Vec3f(vec4f.X, vec4f.Y, vec4f.Z);
			}
		}
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		initRotations();
		QuantityCandles = Variant["quantity"].ToInt();
	}

	public override void OnAsyncClientParticleTick(IAsyncParticleManager manager, BlockPos pos, float windAffectednessAtPos, float secondsTicking)
	{
		if (ParticleProperties == null || ParticleProperties.Length == 0)
		{
			return;
		}
		int num = GameMath.MurmurHash3Mod(pos.X, pos.Y, pos.Z, 4);
		Vec3f[] array = candleWickPositionsByRot[num];
		for (int i = 0; i < ParticleProperties.Length; i++)
		{
			AdvancedParticleProperties advancedParticleProperties = ParticleProperties[i];
			advancedParticleProperties.WindAffectednesAtPos = windAffectednessAtPos;
			for (int j = 0; j < QuantityCandles; j++)
			{
				Vec3f vec3f = array[j];
				advancedParticleProperties.basePos.X = (float)pos.X + vec3f.X;
				advancedParticleProperties.basePos.Y = (float)pos.InternalY + vec3f.Y;
				advancedParticleProperties.basePos.Z = (float)pos.Z + vec3f.Z;
				manager.Spawn(advancedParticleProperties);
			}
		}
	}
}
