using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class ModSystemGliding : ModSystem
{
	private ICoreClientAPI capi;

	protected ILoadedSound glideSound;

	private bool HasGlider
	{
		get
		{
			foreach (ItemSlot item in capi.World.Player.InventoryManager.GetOwnInventory("backpack"))
			{
				if (item is ItemSlotBackpack && item.Itemstack?.Collectible is ItemGlider)
				{
					return true;
				}
			}
			return false;
		}
	}

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return true;
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		capi = api;
		api.Input.InWorldAction += Input_InWorldAction;
		api.Event.RegisterGameTickListener(onClientTick, 20, 1);
	}

	private void onClientTick(float dt)
	{
		ToggleglideSounds(capi.World.Player.Entity.Controls.Gliding);
		IPlayer[] allOnlinePlayers = capi.World.AllOnlinePlayers;
		foreach (IPlayer player in allOnlinePlayers)
		{
			if (player.Entity == null)
			{
				continue;
			}
			float num = 15f;
			float num2 = player.Entity.Attributes.GetFloat("glidingAccum");
			int num3 = player.Entity.Attributes.GetInt("unfoldStep");
			if (player.Entity.Controls.Gliding)
			{
				num2 = Math.Min(3.01f / num, num2 + dt);
				if (!HasGlider)
				{
					player.Entity.Controls.Gliding = false;
					player.Entity.WalkPitch = 0f;
				}
			}
			else
			{
				num2 = Math.Max(0f, num2 - dt);
			}
			int num4 = (int)(num2 * num);
			if (num3 != num4)
			{
				num3 = num4;
				player.Entity.MarkShapeModified();
				player.Entity.Attributes.SetInt("unfoldStep", num3);
			}
			player.Entity.Attributes.SetFloat("glidingAccum", num2);
		}
	}

	public void ToggleglideSounds(bool on)
	{
		if (on)
		{
			if (glideSound == null || !glideSound.IsPlaying)
			{
				glideSound = capi.World.LoadSound(new SoundParams
				{
					Location = new AssetLocation("sounds/effect/gliding.ogg"),
					ShouldLoop = true,
					Position = null,
					RelativePosition = true,
					DisposeOnFinish = false,
					Volume = 0f
				});
				if (glideSound != null)
				{
					glideSound.Start();
					glideSound.PlaybackPosition = glideSound.SoundLengthSeconds * (float)capi.World.Rand.NextDouble();
					glideSound.FadeIn(1f, delegate
					{
					});
				}
			}
		}
		else
		{
			glideSound?.Stop();
			glideSound?.Dispose();
			glideSound = null;
		}
	}

	private void Input_InWorldAction(EnumEntityAction action, bool on, ref EnumHandling handled)
	{
		EntityPlayer entity = capi.World.Player.Entity;
		if (action == EnumEntityAction.Jump && on && !entity.OnGround && HasGlider && !entity.Controls.IsFlying)
		{
			entity.Controls.Gliding = true;
			entity.Controls.IsFlying = true;
			entity.MarkShapeModified();
		}
		if (action == EnumEntityAction.Glide && !on)
		{
			entity.MarkShapeModified();
		}
	}
}
