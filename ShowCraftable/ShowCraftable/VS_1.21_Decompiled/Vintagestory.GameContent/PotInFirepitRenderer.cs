using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class PotInFirepitRenderer : IInFirepitRenderer, IRenderer, IDisposable
{
	private ICoreClientAPI capi;

	private MultiTextureMeshRef potWithFoodRef;

	private MultiTextureMeshRef potRef;

	private MultiTextureMeshRef lidRef;

	private BlockPos pos;

	private float temp;

	private ILoadedSound cookingSound;

	private bool isInOutputSlot;

	private Matrixf ModelMat = new Matrixf();

	public double RenderOrder => 0.5;

	public int RenderRange => 20;

	public PotInFirepitRenderer(ICoreClientAPI capi, ItemStack stack, BlockPos pos, bool isInOutputSlot)
	{
		this.capi = capi;
		this.pos = pos;
		this.isInOutputSlot = isInOutputSlot;
		BlockCookedContainer blockCookedContainer = capi.World.GetBlock(stack.Collectible.CodeWithVariant("type", "cooked")) as BlockCookedContainer;
		if (isInOutputSlot)
		{
			MealMeshCache modSystem = capi.ModLoader.GetModSystem<MealMeshCache>();
			potWithFoodRef = modSystem.GetOrCreateMealInContainerMeshRef(blockCookedContainer, blockCookedContainer.GetCookingRecipe(capi.World, stack), blockCookedContainer.GetNonEmptyContents(capi.World, stack), new Vec3f(0f, 5f / 32f, 0f));
			return;
		}
		string text = "shapes/block/clay/pot-";
		capi.Tesselator.TesselateShape(blockCookedContainer, Shape.TryGet(capi, text + "opened-empty.json"), out var modeldata);
		potRef = capi.Render.UploadMultiTextureMesh(modeldata);
		capi.Tesselator.TesselateShape(blockCookedContainer, Shape.TryGet(capi, text + "part-lid.json"), out var modeldata2);
		lidRef = capi.Render.UploadMultiTextureMesh(modeldata2);
	}

	public void Dispose()
	{
		potRef?.Dispose();
		lidRef?.Dispose();
		cookingSound?.Stop();
		cookingSound?.Dispose();
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		IRenderAPI render = capi.Render;
		Vec3d cameraPos = capi.World.Player.Entity.CameraPos;
		render.GlDisableCullFace();
		render.GlToggleBlend(blend: true);
		IStandardShaderProgram standardShaderProgram = render.PreparedStandardShader(pos.X, pos.Y, pos.Z);
		standardShaderProgram.DontWarpVertices = 0;
		standardShaderProgram.AddRenderFlags = 0;
		standardShaderProgram.RgbaAmbientIn = render.AmbientColor;
		standardShaderProgram.RgbaFogIn = render.FogColor;
		standardShaderProgram.FogMinIn = render.FogMin;
		standardShaderProgram.FogDensityIn = render.FogDensity;
		standardShaderProgram.RgbaTint = ColorUtil.WhiteArgbVec;
		standardShaderProgram.NormalShaded = 1;
		standardShaderProgram.ExtraGodray = 0f;
		standardShaderProgram.SsaoAttn = 0f;
		standardShaderProgram.AlphaTest = 0.05f;
		standardShaderProgram.OverlayOpacity = 0f;
		standardShaderProgram.ModelMatrix = ModelMat.Identity().Translate((double)pos.X - cameraPos.X + 0.0010000000474974513, (double)pos.Y - cameraPos.Y, (double)pos.Z - cameraPos.Z - 0.0010000000474974513).Translate(0f, 0.0625f, 0f)
			.Values;
		standardShaderProgram.ViewMatrix = render.CameraMatrixOriginf;
		standardShaderProgram.ProjectionMatrix = render.CurrentProjectionMatrix;
		render.RenderMultiTextureMesh((potRef == null) ? potWithFoodRef : potRef, "tex");
		if (!isInOutputSlot)
		{
			float num = GameMath.Sin((float)capi.World.ElapsedMilliseconds / 300f) * 5f / 16f;
			float num2 = GameMath.Cos((float)capi.World.ElapsedMilliseconds / 300f) * 5f / 16f;
			float num3 = GameMath.Clamp((temp - 50f) / 50f, 0f, 1f);
			standardShaderProgram.ModelMatrix = ModelMat.Identity().Translate((double)pos.X - cameraPos.X, (double)pos.Y - cameraPos.Y, (double)pos.Z - cameraPos.Z).Translate(0f, 13f / 32f, 0f)
				.Translate(0f - num, 0f, 0f - num2)
				.RotateX(num3 * GameMath.Sin((float)capi.World.ElapsedMilliseconds / 50f) / 60f)
				.RotateZ(num3 * GameMath.Sin((float)capi.World.ElapsedMilliseconds / 50f) / 60f)
				.Translate(num, 0f, num2)
				.Values;
			standardShaderProgram.ViewMatrix = render.CameraMatrixOriginf;
			standardShaderProgram.ProjectionMatrix = render.CurrentProjectionMatrix;
			render.RenderMultiTextureMesh(lidRef, "tex");
		}
		standardShaderProgram.Stop();
	}

	public void OnUpdate(float temperature)
	{
		temp = temperature;
		float num = GameMath.Clamp((temp - 50f) / 50f, 0f, 1f);
		SetCookingSoundVolume(isInOutputSlot ? 0f : num);
	}

	public void OnCookingComplete()
	{
		isInOutputSlot = true;
	}

	public void SetCookingSoundVolume(float volume)
	{
		if (volume > 0f)
		{
			if (cookingSound == null)
			{
				cookingSound = capi.World.LoadSound(new SoundParams
				{
					Location = new AssetLocation("sounds/effect/cooking.ogg"),
					ShouldLoop = true,
					Position = pos.ToVec3f().Add(0.5f, 0.25f, 0.5f),
					DisposeOnFinish = false,
					Range = 10f,
					ReferenceDistance = 3f,
					Volume = volume
				});
				cookingSound.Start();
			}
			else
			{
				cookingSound.SetVolume(volume);
			}
		}
		else if (cookingSound != null)
		{
			cookingSound.Stop();
			cookingSound.Dispose();
			cookingSound = null;
		}
	}
}
