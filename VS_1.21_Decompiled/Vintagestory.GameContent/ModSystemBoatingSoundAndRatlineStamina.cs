using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class ModSystemBoatingSoundAndRatlineStamina : ModSystem
{
	public ILoadedSound travelSound;

	public ILoadedSound idleSound;

	private ICoreAPI api;

	private ICoreClientAPI capi;

	private bool soundsActive;

	private float accum;

	private ModSystemProgressBar mspb;

	private IProgressBar progressBar;

	private Dictionary<string, EntityPlayer> playersOnRatlines = new Dictionary<string, EntityPlayer>();

	private double lastUpdateTotalHours;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return true;
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		this.api = api;
		capi = api;
		capi.Event.LevelFinalize += Event_LevelFinalize;
		capi.Event.RegisterGameTickListener(onTick, 0, 123);
		capi.Event.EntityMounted += Event_EntityMounted;
		capi.Event.EntityUnmounted += Event_EntityUnmounted;
		mspb = capi.ModLoader.GetModSystem<ModSystemProgressBar>();
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		this.api = api;
		api.Event.RegisterGameTickListener(onTickServer, 200);
		api.Event.EntityMounted += Event_EntityMounted;
	}

	private void Event_EntityUnmounted(EntityAgent mountingEntity, IMountableSeat mountedSeat)
	{
		mspb.RemoveProgressbar(progressBar);
		progressBar = null;
	}

	private void Event_EntityMounted(EntityAgent mountingEntity, IMountableSeat mountedSeat)
	{
		bool flag = false;
		if (mountingEntity is EntityPlayer entityPlayer)
		{
			JsonObject attributes = mountedSeat.Config.Attributes;
			if (attributes != null && attributes.IsTrue("tireWhenMounted"))
			{
				flag = true;
				playersOnRatlines[entityPlayer.PlayerUID] = entityPlayer;
				if (!entityPlayer.WatchedAttributes.HasAttribute("remainingMountedStrengthHours"))
				{
					entityPlayer.WatchedAttributes.SetFloat("remainingMountedStrengthHours", 2f);
				}
			}
		}
		if (api.Side == EnumAppSide.Client && progressBar == null && flag)
		{
			progressBar = mspb.AddProgressbar();
		}
	}

	private void onTickServer(float dt)
	{
		float num = (float)(api.World.Calendar.TotalHours - lastUpdateTotalHours);
		if ((double)num < 0.1)
		{
			return;
		}
		List<string> list = new List<string>();
		foreach (EntityPlayer value in playersOnRatlines.Values)
		{
			bool num2 = value.MountedOn != null && (value.MountedOn.Config.Attributes?.IsTrue("tireWhenMounted") ?? false);
			float num3 = value.WatchedAttributes.GetFloat("remainingMountedStrengthHours");
			num3 -= num;
			value.WatchedAttributes.SetFloat("remainingMountedStrengthHours", num3);
			if (num2)
			{
				if (num3 < 0f)
				{
					value.TryUnmount();
				}
			}
			else if (num3 < -1f)
			{
				value.WatchedAttributes.RemoveAttribute("remainingMountedStrengthHours");
				list.Add(value.PlayerUID);
			}
		}
		foreach (string item in list)
		{
			playersOnRatlines.Remove(item);
		}
		lastUpdateTotalHours = api.World.Calendar.TotalHours;
	}

	private void onTick(float dt)
	{
		EntityPlayer entity = capi.World.Player.Entity;
		if (progressBar != null && entity.WatchedAttributes.HasAttribute("remainingMountedStrengthHours"))
		{
			progressBar.Progress = entity.WatchedAttributes.GetFloat("remainingMountedStrengthHours") / 2f;
		}
		if (entity.MountedOn is EntityBoatSeat entityBoatSeat)
		{
			NowInMotion((float)entityBoatSeat.Entity.Pos.Motion.Length(), dt);
		}
		else
		{
			NotMounted();
		}
	}

	private void Event_LevelFinalize()
	{
		travelSound = capi.World.LoadSound(new SoundParams
		{
			Location = new AssetLocation("sounds/raft-moving.ogg"),
			ShouldLoop = true,
			RelativePosition = false,
			DisposeOnFinish = false,
			Volume = 0f
		});
		idleSound = capi.World.LoadSound(new SoundParams
		{
			Location = new AssetLocation("sounds/raft-idle.ogg"),
			ShouldLoop = true,
			RelativePosition = false,
			DisposeOnFinish = false,
			Volume = 0.35f
		});
	}

	public void NowInMotion(float velocity, float dt)
	{
		accum += dt;
		if ((double)accum < 0.2)
		{
			return;
		}
		accum = 0f;
		if (!soundsActive)
		{
			idleSound.Start();
			soundsActive = true;
		}
		if ((double)velocity > 0.01)
		{
			if (!travelSound.IsPlaying)
			{
				travelSound.Start();
			}
			float num = GameMath.Clamp((velocity - 0.025f) * 7f, 0f, 1f);
			travelSound.FadeTo(num, 0.5f, null);
		}
		else if (travelSound.IsPlaying)
		{
			travelSound.FadeTo(0.0, 0.5f, delegate
			{
				travelSound.Stop();
			});
		}
	}

	public override void Dispose()
	{
		travelSound?.Dispose();
		idleSound?.Dispose();
	}

	public void NotMounted()
	{
		if (soundsActive)
		{
			idleSound.Stop();
			travelSound.SetVolume(0f);
			travelSound.Stop();
		}
		soundsActive = false;
	}
}
