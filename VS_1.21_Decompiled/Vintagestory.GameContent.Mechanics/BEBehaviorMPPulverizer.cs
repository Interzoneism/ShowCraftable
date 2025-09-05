using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent.Mechanics;

public class BEBehaviorMPPulverizer : BEBehaviorMPBase
{
	private static SimpleParticleProperties bitsParticles;

	private static SimpleParticleProperties dustParticles;

	private SimpleParticleProperties slideDustParticles;

	private AssetLocation hitSound = new AssetLocation("sounds/effect/crusher-impact");

	private AssetLocation crushSound = new AssetLocation("sounds/effect/stonecrush");

	public float prevProgressLeft;

	public float prevProgressRight;

	public int leftDir;

	public int rightDir;

	private Vec4f leftOffset;

	private Vec4f rightOffset;

	public BEPulverizer bepu;

	private Vec3d leftSlidePos;

	private Vec3d rightSlidePos;

	private Vec3d hitPos = new Vec3d();

	static BEBehaviorMPPulverizer()
	{
		float minQuantity = 1f;
		float addQuantity = 5f;
		float minQuantity2 = 1f;
		float addQuantity2 = 20f;
		bitsParticles = new SimpleParticleProperties(1f, 3f, ColorUtil.ToRgba(40, 220, 220, 220), new Vec3d(), new Vec3d(), new Vec3f(-0.25f, -0.25f, -0.25f), new Vec3f(0.25f, 0.25f, 0.25f), 1f, 1f, 0.1f, 0.4f, EnumParticleModel.Quad);
		bitsParticles.AddPos.Set(0.0625, 1.0 / 32.0, 0.0625);
		bitsParticles.AddQuantity = 20f;
		bitsParticles.MinVelocity.Set(-1f, 0f, -1f);
		bitsParticles.AddVelocity.Set(2f, 2f, 2f);
		bitsParticles.WithTerrainCollision = false;
		bitsParticles.ParticleModel = EnumParticleModel.Cube;
		bitsParticles.LifeLength = 1.5f;
		bitsParticles.SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -0.4f);
		bitsParticles.AddQuantity = addQuantity2;
		bitsParticles.MinQuantity = minQuantity2;
		dustParticles = new SimpleParticleProperties(1f, 3f, ColorUtil.ToRgba(40, 220, 220, 220), new Vec3d(), new Vec3d(), new Vec3f(-0.25f, -0.25f, -0.25f), new Vec3f(0.25f, 0.25f, 0.25f), 1f, 1f, 0.1f, 0.3f, EnumParticleModel.Quad);
		dustParticles.AddPos.Set(0.0625, 1.0 / 32.0, 0.0625);
		dustParticles.AddQuantity = 5f;
		dustParticles.MinVelocity.Set(-0.1f, 0f, -0.1f);
		dustParticles.AddVelocity.Set(0.2f, 0.1f, 0.2f);
		dustParticles.WithTerrainCollision = false;
		dustParticles.ParticleModel = EnumParticleModel.Quad;
		dustParticles.LifeLength = 1.5f;
		dustParticles.SelfPropelled = true;
		dustParticles.GravityEffect = 0f;
		dustParticles.SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, 0.4f);
		dustParticles.OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -16f);
		dustParticles.MinQuantity = minQuantity;
		dustParticles.AddQuantity = addQuantity;
	}

	public BEBehaviorMPPulverizer(BlockEntity blockentity)
		: base(blockentity)
	{
	}

	public override void Initialize(ICoreAPI api, JsonObject properties)
	{
		base.Initialize(api, properties);
		bepu = Blockentity as BEPulverizer;
		Matrixf mat = bepu.mat;
		leftOffset = mat.TransformVector(new Vec4f(-7f / 32f, 0.25f, -9f / 32f, 0f));
		rightOffset = mat.TransformVector(new Vec4f(7f / 32f, 0.25f, -9f / 32f, 0f));
		slideDustParticles = new SimpleParticleProperties(1f, 3f, ColorUtil.ToRgba(40, 220, 220, 220), new Vec3d(), new Vec3d(), new Vec3f(-0.25f, -0.25f, -0.25f), new Vec3f(0.25f, 0.25f, 0.25f), 1f, 1f, 0.1f, 0.2f, EnumParticleModel.Quad);
		slideDustParticles.AddPos.Set(0.0625, 1.0 / 32.0, 0.0625);
		slideDustParticles.WithTerrainCollision = false;
		slideDustParticles.ParticleModel = EnumParticleModel.Quad;
		slideDustParticles.LifeLength = 0.75f;
		slideDustParticles.SelfPropelled = true;
		slideDustParticles.GravityEffect = 0f;
		slideDustParticles.SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, 0.4f);
		slideDustParticles.OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -16f);
		slideDustParticles.MinQuantity = 1f;
		slideDustParticles.AddQuantity = 3f;
		Vec4f vec4f = mat.TransformVector(new Vec4f(-0.1f, -0.1f, 0.2f, 0f));
		slideDustParticles.MinVelocity.Set(vec4f.X, vec4f.Y, vec4f.Z);
		vec4f = mat.TransformVector(new Vec4f(0.2f, -0.05f, 0.2f, 0f));
		slideDustParticles.AddVelocity.Set(vec4f.X, vec4f.Y, vec4f.Z);
		leftSlidePos = mat.TransformVector(new Vec4f(-7f / 32f, 0.25f, -5f / 32f, 0f)).XYZ.ToVec3d().Add(Position).Add(0.5, 0.0, 0.5);
		rightSlidePos = mat.TransformVector(new Vec4f(7f / 32f, 0.25f, -5f / 32f, 0f)).XYZ.ToVec3d().Add(Position).Add(0.5, 0.0, 0.5);
	}

	public override float GetResistance()
	{
		bepu = Blockentity as BEPulverizer;
		if (!bepu.hasAxle)
		{
			return 0.005f;
		}
		return 0.085f;
	}

	public override void JoinNetwork(MechanicalNetwork network)
	{
		base.JoinNetwork(network);
		float num = ((network == null) ? 0f : (Math.Abs(network.Speed * base.GearedRatio) * 1.6f));
		if (num > 1f)
		{
			network.Speed /= num;
			network.clientSpeed /= num;
		}
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
	{
		base.OnTesselation(mesher, tesselator);
		switch (BlockFacing.FromCode(base.Block.Variant["side"]).Index)
		{
		case 0:
			AxisSign = new int[3] { -1, 0, 0 };
			break;
		case 2:
			AxisSign = new int[3] { 1, 0, 0 };
			break;
		}
		return true;
	}

	internal void OnClientSideImpact(bool right)
	{
		if (bepu.IsComplete)
		{
			Vec4f vec4f = (right ? rightOffset : leftOffset);
			int slotId = ((!right) ? 1 : 0);
			hitPos.Set((float)Position.X + 0.5f + vec4f.X, (float)Position.InternalY + vec4f.Y, (float)Position.Z + 0.5f + vec4f.Z);
			Api.World.PlaySoundAt(hitSound, hitPos.X, hitPos.Y, hitPos.Z, null, randomizePitch: true, 8f);
			if (!bepu.Inventory[slotId].Empty)
			{
				ItemStack itemstack = bepu.Inventory[slotId].Itemstack;
				Api.World.PlaySoundAt(crushSound, hitPos.X, hitPos.Y, hitPos.Z, null, randomizePitch: true, 8f);
				dustParticles.Color = (bitsParticles.Color = itemstack.Collectible.GetRandomColor(Api as ICoreClientAPI, itemstack));
				dustParticles.Color &= 16777215;
				dustParticles.Color |= -939524096;
				dustParticles.MinPos.Set(hitPos.X - 1.0 / 32.0, hitPos.Y, hitPos.Z - 1.0 / 32.0);
				bitsParticles.MinPos.Set(hitPos.X - 1.0 / 32.0, hitPos.Y, hitPos.Z - 1.0 / 32.0);
				slideDustParticles.MinPos.Set(right ? rightSlidePos : leftSlidePos);
				slideDustParticles.Color = dustParticles.Color;
				Api.World.SpawnParticles(bitsParticles);
				Api.World.SpawnParticles(dustParticles);
				Api.World.SpawnParticles(slideDustParticles);
			}
		}
	}
}
