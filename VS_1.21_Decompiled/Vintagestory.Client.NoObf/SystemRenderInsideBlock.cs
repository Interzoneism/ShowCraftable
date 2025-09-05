using Vintagestory.API.Client;
using Vintagestory.API.Client.Tesselation;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class SystemRenderInsideBlock : ClientSystem
{
	protected Block[] insideBlocks;

	protected MeshRef[] meshRefs;

	protected int atlasTextureId;

	protected Matrixf ModelMat = new Matrixf();

	protected Vec3d[] testPositions;

	public int[] lightExt;

	public Block[] blockExt;

	private int extChunkSize;

	private BlockPos tmpPos = new BlockPos();

	public override string Name => "rib";

	public SystemRenderInsideBlock(ClientMain game)
		: base(game)
	{
		game.eventManager.RegisterRenderer(OnRenderFrame3D, EnumRenderStage.Opaque, Name, 0.45);
		testPositions = new Vec3d[18]
		{
			new Vec3d(0.0, 0.0, 0.0),
			new Vec3d(0.0, 1.0, 0.0),
			new Vec3d(0.0, 0.0, -1.0),
			new Vec3d(1.0, 0.0, -1.0),
			new Vec3d(1.0, 0.0, 0.0),
			new Vec3d(1.0, 0.0, 1.0),
			new Vec3d(0.0, 0.0, 1.0),
			new Vec3d(-1.0, 0.0, 1.0),
			new Vec3d(-1.0, 0.0, 0.0),
			new Vec3d(-1.0, 0.0, -1.0),
			new Vec3d(0.0, -1.0, -1.0),
			new Vec3d(1.0, -1.0, -1.0),
			new Vec3d(1.0, -1.0, 0.0),
			new Vec3d(1.0, -1.0, 1.0),
			new Vec3d(0.0, -1.0, 1.0),
			new Vec3d(-1.0, -1.0, 1.0),
			new Vec3d(-1.0, -1.0, 0.0),
			new Vec3d(-1.0, -1.0, -1.0)
		};
		insideBlocks = new Block[testPositions.Length];
		meshRefs = new MeshRef[testPositions.Length];
	}

	internal override void OnLevelFinalize()
	{
		base.OnLevelFinalize();
		extChunkSize = 34;
		lightExt = new int[extChunkSize * extChunkSize * extChunkSize];
		blockExt = new Block[extChunkSize * extChunkSize * extChunkSize];
	}

	private void OnRenderFrame3D(float dt)
	{
		EntityPlayer entityPlayer = game.EntityPlayer;
		if (entityPlayer == null || game.player.worlddata.CurrentGameMode == EnumGameMode.Creative || game.player.worlddata.CurrentGameMode == EnumGameMode.Spectator)
		{
			return;
		}
		Vec3d vec3d = game.api.World.Player.Entity.CameraPos.Clone().Add(game.api.World.Player.Entity.LocalEyePos);
		game.MainCamera.ZNear = GameMath.Clamp(0.1f - (float)ClientSettings.FieldOfView / 90f / 25f, 0.025f, 0.1f);
		for (int i = 0; i < testPositions.Length; i++)
		{
			Vec3d obj = testPositions[i];
			double num = obj.X * (double)game.MainCamera.ZNear * 1.5;
			double num2 = obj.Y * (double)game.MainCamera.ZNear * 1.5;
			double num3 = obj.Z * (double)game.MainCamera.ZNear * 1.5;
			tmpPos.Set((int)(vec3d.X + num), (int)(vec3d.Y + num2), (int)(vec3d.Z + num3));
			Block block = game.BlockAccessor.GetBlock(tmpPos);
			if (block != null && (block.SideOpaque[0] || block.SideOpaque[1] || block.SideOpaque[2] || block.SideOpaque[3] || block.SideOpaque[4] || block.SideOpaque[5]))
			{
				if (block != insideBlocks[i])
				{
					meshRefs[i]?.Dispose();
					MeshData sourceMesh = game.api.TesselatorManager.GetDefaultBlockMesh(block);
					int num4 = tmpPos.X % 32;
					int num5 = tmpPos.X % 32;
					int num6 = tmpPos.X % 32;
					int num7 = ((num5 + 1) * extChunkSize + (num6 + 1)) * extChunkSize + (num4 + 1);
					blockExt[num7] = block;
					blockExt[num7 + TileSideEnum.MoveIndex[5]] = game.BlockAccessor.GetBlock(tmpPos.X, tmpPos.Y - 1, tmpPos.Z);
					blockExt[num7 + TileSideEnum.MoveIndex[4]] = game.BlockAccessor.GetBlock(tmpPos.X, tmpPos.Y + 1, tmpPos.Z);
					blockExt[num7 + TileSideEnum.MoveIndex[0]] = game.BlockAccessor.GetBlock(tmpPos.X, tmpPos.Y, tmpPos.Z - 1);
					blockExt[num7 + TileSideEnum.MoveIndex[1]] = game.BlockAccessor.GetBlock(tmpPos.X + 1, tmpPos.Y, tmpPos.Z);
					blockExt[num7 + TileSideEnum.MoveIndex[2]] = game.BlockAccessor.GetBlock(tmpPos.X, tmpPos.Y, tmpPos.Z + 1);
					blockExt[num7 + TileSideEnum.MoveIndex[3]] = game.BlockAccessor.GetBlock(tmpPos.X - 1, tmpPos.Y - 1, tmpPos.Z);
					block.OnJsonTesselation(ref sourceMesh, ref lightExt, tmpPos, blockExt, num7);
					meshRefs[i] = game.api.Render.UploadMesh(sourceMesh);
					insideBlocks[i] = block;
					int textureSubId = block.FirstTextureInventory.Baked.TextureSubId;
					atlasTextureId = game.api.BlockTextureAtlas.Positions[textureSubId].atlasTextureId;
				}
				IRenderAPI render = game.api.Render;
				render.GlDisableCullFace();
				render.GlToggleBlend(blend: true);
				IStandardShaderProgram standardShaderProgram = render.PreparedStandardShader((int)entityPlayer.Pos.X, (int)entityPlayer.Pos.Y, (int)entityPlayer.Pos.Z);
				standardShaderProgram.Tex2D = atlasTextureId;
				standardShaderProgram.SsaoAttn = 1f;
				standardShaderProgram.ModelMatrix = ModelMat.Identity().Translate((double)(int)(vec3d.X + num) - vec3d.X + entityPlayer.LocalEyePos.X, (double)(int)(vec3d.Y + num2) - vec3d.Y + entityPlayer.LocalEyePos.Y, (double)(int)(vec3d.Z + num3) - vec3d.Z + entityPlayer.LocalEyePos.Z).Scale(0.999f, 0.999f, 0.999f)
					.Values;
				standardShaderProgram.ExtraZOffset = -0.0001f;
				standardShaderProgram.ViewMatrix = render.CameraMatrixOriginf;
				standardShaderProgram.ProjectionMatrix = render.CurrentProjectionMatrix;
				render.RenderMesh(meshRefs[i]);
				standardShaderProgram.SsaoAttn = 0f;
				standardShaderProgram.Stop();
			}
		}
	}

	public override void Dispose(ClientMain game)
	{
		for (int i = 0; i < meshRefs.Length; i++)
		{
			meshRefs[i]?.Dispose();
		}
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Render;
	}
}
