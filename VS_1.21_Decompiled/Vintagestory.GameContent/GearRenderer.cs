using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class GearRenderer : IRenderer, IDisposable
{
	private MeshRef gearMeshref;

	private Matrixf matrixf;

	private float counter;

	private ICoreClientAPI capi;

	private IShaderProgram prog;

	private List<MachineGear> mgears = new List<MachineGear>();

	private AnimationUtil tripodAnim;

	private Vec3d tripodPos = new Vec3d();

	private double tripodAccum;

	private LoadedTexture rustTexture;

	private EntityBehaviorTemporalStabilityAffected bh;

	private float raiseyRelGears;

	private float raiseyRelTripod;

	public double RenderOrder => 1.0;

	public int RenderRange => 100;

	public GearRenderer(ICoreClientAPI capi)
	{
		this.capi = capi;
		capi.Event.RegisterRenderer(this, EnumRenderStage.Opaque, "machinegearrenderer");
		matrixf = new Matrixf();
		capi.Event.ReloadShader += LoadShader;
		LoadShader();
	}

	public void Init()
	{
		Shape shape = Shape.TryGet(capi, "shapes/block/machine/machinegear2.json");
		Block block = capi.World.GetBlock(new AssetLocation("platepile"));
		if (block != null)
		{
			capi.Tesselator.TesselateShape(block, shape, out var modeldata);
			gearMeshref = capi.Render.UploadMesh(modeldata);
			genGears();
			rustTexture = new LoadedTexture(capi);
			AssetLocation name = new AssetLocation("textures/block/metal/tarnished/rust.png");
			capi.Render.GetOrLoadTexture(name, ref rustTexture);
			shape = Shape.TryGet(capi, "shapes/entity/lore/supermech/thunderlord.json");
			tripodAnim = new AnimationUtil(capi, tripodPos);
			tripodAnim.InitializeShapeAndAnimator("tripod", shape, capi.Tesselator.GetTextureSource(block), null, out var _);
			tripodAnim.StartAnimation(new AnimationMetaData
			{
				Animation = "walk",
				Code = "walk",
				BlendMode = EnumAnimationBlendMode.Average,
				AnimationSpeed = 0.1f
			});
			tripodAnim.renderer.ScaleX = 30f;
			tripodAnim.renderer.ScaleY = 30f;
			tripodAnim.renderer.ScaleZ = 30f;
			tripodAnim.renderer.FogAffectedness = 0.15f;
			tripodAnim.renderer.LightAffected = false;
			tripodAnim.renderer.StabilityAffected = false;
			tripodAnim.renderer.ShouldRender = true;
			bh = capi.World.Player.Entity.GetBehavior<EntityBehaviorTemporalStabilityAffected>();
		}
	}

	private void genGears()
	{
		Random rand = capi.World.Rand;
		mgears.Clear();
		double num = rand.NextDouble() * 6.2831854820251465;
		int num2 = 6;
		float num3 = (float)Math.PI * 2f / (float)num2;
		for (int i = 0; i < num2; i++)
		{
			double num4 = 150.0 + rand.NextDouble() * 300.0;
			num4 *= 5.0;
			num += (double)num3 + rand.NextDouble() * (double)num3 * 0.1 - (double)num3 * 0.05;
			float num5 = 20f + (float)rand.NextDouble() * 30f;
			num5 *= 15f;
			MachineGear item = new MachineGear
			{
				Position = new Vec3d(GameMath.Sin(num) * num4, num5 / 2f, GameMath.Cos(num) * num4),
				Rot = new Vec3d(0.0, rand.NextDouble() * 6.2831854820251465, rand.NextDouble() - 0.5),
				Velocity = (float)rand.NextDouble() * 0.2f,
				Size = num5
			};
			mgears.Add(item);
		}
	}

	public bool LoadShader()
	{
		prog = capi.Shader.NewShaderProgram();
		prog.VertexShader = capi.Shader.NewShader(EnumShaderType.VertexShader);
		prog.FragmentShader = capi.Shader.NewShader(EnumShaderType.FragmentShader);
		capi.Shader.RegisterFileShaderProgram("machinegear", prog);
		return prog.Compile();
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		if (tripodAnim != null)
		{
			if (capi.IsGamePaused)
			{
				deltaTime = 0f;
			}
			float num = 0f;
			if (bh != null)
			{
				num = GameMath.Clamp((float)bh.GlichEffectStrength * 5f - 3f, 0f, 1f);
			}
			raiseyRelGears += (num - raiseyRelGears) * deltaTime;
			capi.Render.GlToggleBlend(blend: true);
			if (raiseyRelGears >= 0.01f)
			{
				renderGears(deltaTime);
			}
			if (!capi.IsGamePaused)
			{
				tripodAnim.renderer.ShouldRender = raiseyRelTripod > 0.01f;
				updateSuperMechState(deltaTime, stage);
			}
			capi.Render.GlToggleBlend(blend: false);
		}
	}

	private void updateSuperMechState(float deltaTime, EnumRenderStage stage)
	{
		float num = 0f;
		if (bh != null)
		{
			num = GameMath.Clamp((float)bh.GlichEffectStrength * 5f - 1.75f, 0f, 1f);
		}
		raiseyRelTripod += (num - raiseyRelTripod) * deltaTime / 3f;
		EntityPos pos = capi.World.Player.Entity.Pos;
		tripodAccum += (double)deltaTime / 50.0 * (double)(0.33f + raiseyRelTripod) * 1.2000000476837158;
		tripodAccum %= 500000.0;
		float num2 = (1f - raiseyRelTripod) * 900f;
		tripodPos.X = pos.X + Math.Sin(tripodAccum) * (300.0 + (double)num2);
		tripodPos.Y = capi.World.SeaLevel;
		tripodPos.Z = pos.Z + Math.Cos(tripodAccum) * (300.0 + (double)num2);
		tripodAnim.renderer.rotationDeg.Y = (float)(tripodAccum % 6.2831854820251465 + 3.1415927410125732) * (180f / (float)Math.PI);
		tripodAnim.renderer.renderColor.Set(0.5f, 0.5f, 0.5f, Math.Min(1f, raiseyRelTripod * 2f));
		tripodAnim.renderer.FogAffectedness = 1f - GameMath.Clamp(raiseyRelGears * 2.2f - 0.5f, 0f, 0.9f);
		tripodAnim.OnRenderFrame(deltaTime, stage);
	}

	private void renderGears(float deltaTime)
	{
		prog.Use();
		prog.Uniform("rgbaFogIn", capi.Render.FogColor);
		prog.Uniform("fogMinIn", capi.Render.FogMin);
		prog.Uniform("fogDensityIn", capi.Render.FogDensity);
		prog.Uniform("rgbaAmbientIn", capi.Render.AmbientColor);
		prog.Uniform("rgbaLightIn", new Vec4f(1f, 1f, 1f, 1f));
		prog.UniformMatrix("projectionMatrix", capi.Render.CurrentProjectionMatrix);
		prog.Uniform("counter", counter);
		int num = 0;
		EntityPos pos = capi.World.Player.Entity.Pos;
		foreach (MachineGear mgear in mgears)
		{
			num++;
			matrixf.Identity();
			mgear.Position.Y = Math.Max(mgear.Position.Y, capi.World.BlockAccessor.GetTerrainMapheightAt(new BlockPos((int)(mgear.Position.X + pos.X), 0, (int)(mgear.Position.Z + pos.Z))));
			float num2 = (float)mgear.Position.X;
			float y = (float)(mgear.Position.Y - pos.Y - (double)((1f - raiseyRelGears) * mgear.Size * 1.5f));
			float num3 = (float)mgear.Position.Z;
			GameMath.Sqrt(num2 * num2 + num3 * num3);
			matrixf.Mul(capi.Render.CameraMatrixOriginf);
			matrixf.Translate(num2, y, num3);
			matrixf.RotateY((float)mgear.Rot.Y);
			matrixf.RotateX((float)mgear.Rot.Z + (float)Math.PI / 2f);
			float size = mgear.Size;
			matrixf.Scale(size, size, size);
			matrixf.Translate(0.5f, 0.5f, 0.5f);
			matrixf.RotateY(counter * mgear.Velocity);
			matrixf.Translate(-0.5f, -0.5f, -0.5f);
			prog.Uniform("alpha", 1f);
			prog.UniformMatrix("modelViewMatrix", matrixf.Values);
			prog.Uniform("worldPos", new Vec4f(num2, y, num3, 0f));
			prog.Uniform("riftIndex", num);
			capi.Render.RenderMesh(gearMeshref);
		}
		counter = GameMath.Mod(counter + deltaTime, (float)Math.PI * 200f);
		prog.Stop();
	}

	public void Dispose()
	{
		gearMeshref?.Dispose();
	}
}
