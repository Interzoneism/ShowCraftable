using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class SystemRenderAim : ClientSystem
{
	private int aimTextureId;

	private int aimHostileTextureId;

	public override string Name => "remi";

	public SystemRenderAim(ClientMain game)
		: base(game)
	{
		game.eventManager.RegisterRenderer(OnRenderFrame2DOverlay, EnumRenderStage.Ortho, Name, 1.02);
	}

	public override void OnBlockTexturesLoaded()
	{
		aimTextureId = game.GetOrLoadCachedTexture(new AssetLocation("gui/target.png"));
		aimHostileTextureId = game.GetOrLoadCachedTexture(new AssetLocation("gui/targethostile.png"));
	}

	public void OnRenderFrame2DOverlay(float deltaTime)
	{
		if (game.MouseGrabbed)
		{
			DrawAim(game);
		}
	}

	internal void DrawAim(ClientMain game)
	{
		if (game.MainCamera.CameraMode != EnumCameraMode.FirstPerson || game.Player.WorldData.CurrentGameMode == EnumGameMode.Spectator)
		{
			return;
		}
		int num = 32;
		int num2 = 32;
		Entity entity = game.EntitySelection?.Entity;
		ItemStack itemStack = game.Player?.InventoryManager?.ActiveHotbarSlot?.Itemstack;
		float num3 = itemStack?.Collectible.GetAttackRange(itemStack) ?? GlobalConstants.DefaultAttackRange;
		int textureid = aimTextureId;
		if (entity != null && game.EntityPlayer != null)
		{
			Cuboidd cuboidd = entity.SelectionBox.ToDouble().Translate(entity.Pos.X, entity.Pos.Y, entity.Pos.Z);
			EntityPos sidedPos = game.EntityPlayer.SidedPos;
			if (cuboidd.ShortestDistanceFrom(sidedPos.X + game.EntityPlayer.LocalEyePos.X, sidedPos.Y + game.EntityPlayer.LocalEyePos.Y, sidedPos.Z + game.EntityPlayer.LocalEyePos.Z) <= (double)num3 - 0.08)
			{
				textureid = aimHostileTextureId;
			}
		}
		game.Render2DTexture(textureid, game.Width / 2 - num / 2, game.Height / 2 - num2 / 2, num, num2, 10000f);
	}

	public override void Dispose(ClientMain game)
	{
		game.Platform.GLDeleteTexture(aimTextureId);
		game.Platform.GLDeleteTexture(aimHostileTextureId);
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Render;
	}
}
