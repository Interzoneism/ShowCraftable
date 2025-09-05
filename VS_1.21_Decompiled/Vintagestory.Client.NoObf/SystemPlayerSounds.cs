using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class SystemPlayerSounds : ClientSystem
{
	private ILoadedSound FlySound;

	private ILoadedSound UnderWaterSound;

	private Dictionary<AmbientSound, AmbientSound> ambientSounds = new Dictionary<AmbientSound, AmbientSound>();

	private WireframeCube[] wireframes;

	private bool fallActive;

	private bool underwaterActive;

	private float targetVolume;

	private float curVolume;

	private double flySpeed;

	public override string Name => "plso";

	public SystemPlayerSounds(ClientMain game)
		: base(game)
	{
		game.eventManager.RegisterPlayerPropertyChangedWatcher(EnumProperty.FallSpeed, OnFallSpeedChange);
		game.eventManager.RegisterPlayerPropertyChangedWatcher(EnumProperty.EyesInWaterDepth, OnSwimDepthChange);
		game.RegisterGameTickListener(OnGameTick, 20);
		game.eventManager.OnAmbientSoundsScanComplete = OnAmbientSoundScan;
		game.eventManager.RegisterRenderer(Render3D, EnumRenderStage.Opaque, "playersoundswireframe", 0.9);
		wireframes = new WireframeCube[6]
		{
			WireframeCube.CreateUnitCube(game.api, ColorUtil.ToRgba(128, 255, 255, 0)),
			WireframeCube.CreateUnitCube(game.api, ColorUtil.ToRgba(128, 255, 0, 0)),
			WireframeCube.CreateUnitCube(game.api, ColorUtil.ToRgba(128, 0, 255, 0)),
			WireframeCube.CreateUnitCube(game.api, ColorUtil.ToRgba(128, 0, 0, 255)),
			WireframeCube.CreateUnitCube(game.api, ColorUtil.ToRgba(128, 0, 255, 255)),
			WireframeCube.CreateUnitCube(game.api, ColorUtil.ToRgba(128, 255, 0, 255))
		};
	}

	public override void OnBlockTexturesLoaded()
	{
		FlySound = game.LoadSound(new SoundParams
		{
			Location = new AssetLocation("sounds/environment/wind.ogg"),
			ShouldLoop = true,
			RelativePosition = true,
			DisposeOnFinish = false
		});
		UnderWaterSound = game.LoadSound(new SoundParams
		{
			Location = new AssetLocation("sounds/environment/underwater.ogg"),
			ShouldLoop = true,
			RelativePosition = true,
			DisposeOnFinish = false
		});
	}

	private void Render3D(float dt)
	{
		updateFlySound(dt);
		if (!game.api.renderapi.WireframeDebugRender.AmbientSounds)
		{
			return;
		}
		int num = 0;
		foreach (AmbientSound key in ambientSounds.Keys)
		{
			key.RenderWireFrame(game, wireframes[num % wireframes.Length]);
			num++;
		}
	}

	private void updateFlySound(float dt)
	{
		bool flag = Math.Abs(flySpeed) - 0.05000000074505806 > 0.2;
		if (flag && !fallActive && !FlySound.IsPlaying)
		{
			FlySound.Start();
		}
		if (!flag && (double)curVolume < 0.08 && FlySound.IsPlaying)
		{
			FlySound.Stop();
		}
		if (FlySound.IsPlaying)
		{
			targetVolume = (flag ? Math.Min(1f, Math.Abs((float)flySpeed)) : 0f);
			curVolume = GameMath.Clamp(curVolume + (targetVolume - curVolume) * dt * (float)(flag ? 1 : 5), 0f, 1f);
			FlySound.SetVolume(curVolume);
		}
		fallActive = flag;
	}

	private void OnAmbientSoundScan(List<AmbientSound> newAmbientSounds)
	{
		HashSet<AmbientSound> hashSet = new HashSet<AmbientSound>(ambientSounds.Keys);
		foreach (AmbientSound newAmbientSound in newAmbientSounds)
		{
			hashSet.Remove(newAmbientSound);
			if (ambientSounds.ContainsKey(newAmbientSound))
			{
				AmbientSound ambientSound = ambientSounds[newAmbientSound];
				ambientSound.QuantityNearbyBlocks = newAmbientSound.QuantityNearbyBlocks;
				ambientSound.BoundingBoxes = newAmbientSound.BoundingBoxes;
				ambientSound.VolumeMul = newAmbientSound.VolumeMul;
				ambientSound.FadeToNewVolumne();
				continue;
			}
			ambientSounds[newAmbientSound] = newAmbientSound;
			newAmbientSound.Sound = game.LoadSound(new SoundParams
			{
				Location = newAmbientSound.AssetLoc,
				ShouldLoop = true,
				RelativePosition = false,
				DisposeOnFinish = false,
				Volume = 0.01f,
				Position = new Vec3f(),
				Range = 40f,
				SoundType = newAmbientSound.SoundType
			});
			newAmbientSound.updatePosition(game.EntityPlayer.Pos);
			newAmbientSound.Sound.Start();
			newAmbientSound.Sound.PlaybackPosition = (float)game.Rand.NextDouble() * newAmbientSound.Sound.SoundLengthSeconds;
			newAmbientSound.FadeToNewVolumne();
		}
		foreach (AmbientSound item in hashSet)
		{
			item.Sound.FadeOut(1f, delegate(ILoadedSound loadedsound)
			{
				loadedsound.Stop();
				loadedsound.Dispose();
			});
			ambientSounds.Remove(item);
		}
	}

	public void OnGameTick(float dt)
	{
		foreach (KeyValuePair<AmbientSound, AmbientSound> ambientSound in ambientSounds)
		{
			ambientSound.Value.updatePosition(game.EntityPlayer.Pos);
		}
	}

	internal int GetSoundCount(string[] soundwalk)
	{
		int num = 0;
		if (soundwalk == null)
		{
			return 0;
		}
		for (int i = 0; i < soundwalk.Length; i++)
		{
			if (soundwalk[i] != null)
			{
				num++;
			}
		}
		return num;
	}

	private void OnSwimDepthChange(TrackedPlayerProperties oldValues, TrackedPlayerProperties newValues)
	{
		bool flag = Math.Abs(newValues.EyesInWaterDepth) > 0f;
		if (flag && !underwaterActive)
		{
			UnderWaterSound.Start();
		}
		if (!flag && underwaterActive)
		{
			UnderWaterSound.Stop();
		}
		if (flag)
		{
			UnderWaterSound.SetVolume(Math.Min(0.1f, newValues.EyesInWaterDepth / 2f));
		}
		underwaterActive = flag;
	}

	private void OnFallSpeedChange(TrackedPlayerProperties oldValues, TrackedPlayerProperties newValues)
	{
		flySpeed = newValues.FallSpeed;
	}

	public override void Dispose(ClientMain game)
	{
		foreach (AmbientSound key in ambientSounds.Keys)
		{
			key.Sound?.Dispose();
		}
		FlySound?.Dispose();
		UnderWaterSound?.Dispose();
		if (wireframes != null)
		{
			WireframeCube[] array = wireframes;
			for (int i = 0; i < array.Length; i++)
			{
				array[i]?.Dispose();
			}
		}
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Misc;
	}
}
