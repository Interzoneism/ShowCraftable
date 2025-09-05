using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class ModSystemNightVision : ModSystem, IRenderer, IDisposable
{
	private ICoreClientAPI capi;

	private ICoreServerAPI sapi;

	private EntityBehaviorPlayerInventory bh;

	private double lastCheckTotalHours;

	public double RenderOrder => 0.0;

	public int RenderRange => 1;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return true;
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		capi = api;
		api.Event.RegisterRenderer(this, EnumRenderStage.Before, "nightvision");
		api.Event.LevelFinalize += Event_LevelFinalize;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		base.StartServerSide(api);
		sapi = api;
		api.Event.RegisterGameTickListener(onTickServer1s, 1000, 200);
	}

	private void onTickServer1s(float dt)
	{
		double totalHours = sapi.World.Calendar.TotalHours;
		double num = totalHours - lastCheckTotalHours;
		if (!(num > 0.05))
		{
			return;
		}
		IPlayer[] allOnlinePlayers = sapi.World.AllOnlinePlayers;
		for (int i = 0; i < allOnlinePlayers.Length; i++)
		{
			IInventory ownInventory = allOnlinePlayers[i].InventoryManager.GetOwnInventory("character");
			if (ownInventory != null)
			{
				ItemSlot itemSlot = ownInventory[12];
				if (itemSlot.Itemstack?.Collectible is ItemNightvisiondevice itemNightvisiondevice)
				{
					itemNightvisiondevice.AddFuelHours(itemSlot.Itemstack, 0.0 - num);
					itemSlot.MarkDirty();
				}
			}
		}
		lastCheckTotalHours = totalHours;
	}

	private void Event_LevelFinalize()
	{
		bh = capi.World.Player.Entity.GetBehavior<EntityBehaviorPlayerInventory>();
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		if (bh?.Inventory != null)
		{
			ItemStack itemStack = bh.Inventory[12]?.Itemstack;
			ItemNightvisiondevice itemNightvisiondevice = itemStack?.Collectible as ItemNightvisiondevice;
			double num = itemNightvisiondevice?.GetFuelHours(itemStack) ?? 0.0;
			if (itemNightvisiondevice != null)
			{
				capi.Render.ShaderUniforms.NightVisionStrength = (float)GameMath.Clamp(num * 20.0, 0.0, 0.8);
			}
			else
			{
				capi.Render.ShaderUniforms.NightVisionStrength = 0f;
			}
		}
	}
}
