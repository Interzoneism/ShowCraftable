using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemSkillTimeswitch : Item, ISkillItemRenderer
{
	private LoadedTexture iconTex;

	private int slotTexId;

	private ICoreClientAPI capi;

	public static float timeSwitchCooldown;

	private ElementBounds renderBounds = new ElementBounds();

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
	{
		if (api is ICoreClientAPI coreClientAPI)
		{
			UseTimeSwitchSkillClient(coreClientAPI);
		}
	}

	public static void UseTimeSwitchSkillClient(ICoreClientAPI capi)
	{
		if (!(timeSwitchCooldown > 0f))
		{
			capi.SendChatMessage("/timeswitch toggle");
			capi.World.AddCameraShake(0.25f);
			timeSwitchCooldown = 3f / (float)((capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Survival) ? 1 : 3);
		}
	}

	public override void OnLoaded(ICoreAPI api)
	{
		if (api.Side != EnumAppSide.Client)
		{
			return;
		}
		capi = api as ICoreClientAPI;
		JsonObject attributes = Attributes;
		if (attributes != null && attributes["iconPath"].Exists)
		{
			AssetLocation iconloc = AssetLocation.Create(Attributes["iconPath"].ToString(), Code.Domain).WithPathPrefix("textures/");
			iconTex = ObjectCacheUtil.GetOrCreate(api, "skillicon-" + Code, () => capi.Gui.LoadSvgWithPadding(iconloc, 64, 64, 5, -1));
			double absSlotWidth = GuiElement.scaled(GuiElementPassiveItemSlot.unscaledSlotSize);
			double absSlotHeight = GuiElement.scaled(GuiElementPassiveItemSlot.unscaledSlotSize);
			slotTexId = GuiElementItemSlotGridBase.DrawSlotBackground(capi, absSlotWidth, absSlotHeight, new double[4]
			{
				74.0 / 85.0,
				62.0 / 85.0,
				142.0 / 255.0,
				1.0
			}, GuiStyle.DialogSlotFrontColor, null);
		}
		base.OnLoaded(api);
	}

	public void Render(float dt, float x, float y, float z)
	{
		if (capi.World.Player.WorldData.CurrentGameMode != EnumGameMode.Spectator)
		{
			float num = ((float)capi.World.Rand.NextDouble() * 60f - 30f) * Math.Max(0f, timeSwitchCooldown - 2.4f);
			float num2 = ((float)capi.World.Rand.NextDouble() * 60f - 30f) * Math.Max(0f, timeSwitchCooldown - 2.4f);
			x += num;
			y += num2;
			float num3 = 0.61538464f * RuntimeEnv.GUIScale;
			float num4 = (float)GuiElement.scaled(GuiElementPassiveItemSlot.unscaledSlotSize);
			capi.Render.Render2DTexture(slotTexId, x - (float)GuiElement.scaled(4.0), y - (float)GuiElement.scaled(5.0), num4, num4, z - 300f);
			capi.Render.Render2DTexture(iconTex.TextureId, x, y, (float)iconTex.Width * num3, (float)iconTex.Height * num3, z);
			double num5 = (float)iconTex.Height * 8f / 13f * GameMath.Clamp(timeSwitchCooldown / 3f * 2.5f, 0f, 1f);
			renderBounds.ParentBounds = capi.Gui.WindowBounds;
			renderBounds.fixedX = x / RuntimeEnv.GUIScale;
			renderBounds.fixedY = (double)(y / RuntimeEnv.GUIScale) + num5;
			renderBounds.fixedWidth = (float)iconTex.Width * 8f / 13f;
			renderBounds.fixedHeight = (double)((float)iconTex.Height * 8f / 13f) - num5;
			renderBounds.CalcWorldBounds();
			capi.Render.PushScissor(renderBounds);
			Vec4f color = new Vec4f((float)GuiStyle.ColorTime1[0], (float)GuiStyle.ColorTime1[1], (float)GuiStyle.ColorTime1[2], (float)GuiStyle.ColorTime1[3]);
			capi.Render.Render2DTexture(iconTex.TextureId, x, y, (float)iconTex.Width * num3, (float)iconTex.Height * num3, z, color);
			timeSwitchCooldown = Math.Max(0f, timeSwitchCooldown - dt);
			capi.Render.PopScissor();
			capi.Render.CheckGlError();
		}
	}
}
