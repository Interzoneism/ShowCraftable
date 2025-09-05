using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class SystemSoundEngine : ClientSystem, IRenderer, IDisposable
{
	public static Vec3f Zero = new Vec3f();

	public static float NowReverbness = 0f;

	public static float TargetReverbness = 0f;

	private bool scanning;

	public static Cuboidi RoomLocation = new Cuboidi();

	private AABBIntersectionTest intersectionTester;

	private bool glitchActive;

	private bool prevSubmerged;

	private int prevReverbKey = -999;

	public override string Name => "soen";

	public double RenderOrder => 1.0;

	public int RenderRange => 1;

	public SystemSoundEngine(ClientMain game)
		: base(game)
	{
		game.eventManager.RegisterRenderer(this, EnumRenderStage.Before, "updateAudioListener");
		game.RegisterGameTickListener(OnGameTick100ms, 100);
		game.RegisterGameTickListener(OnGameTick500ms, 500);
		ClientSettings.Inst.AddWatcher<int>("soundLevel", OnSoundLevelChanged);
		ClientSettings.Inst.AddWatcher<int>("entitySoundLevel", OnSoundLevelChanged);
		ClientSettings.Inst.AddWatcher<int>("ambientSoundLevel", OnSoundLevelChanged);
		ClientSettings.Inst.AddWatcher<int>("weatherSoundLevel", OnSoundLevelChanged);
		game.api.ChatCommands.GetOrCreate("debug").BeginSub("sound").WithDescription("sound")
			.BeginSub("list")
			.WithDescription("list")
			.HandleWith(onListSounds)
			.EndSub()
			.EndSub();
		intersectionTester = new AABBIntersectionTest(new OffthreadBaSupplier(game));
	}

	private TextCommandResult onListSounds(TextCommandCallingArgs args)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("Active sounds: ");
		stringBuilder.AppendLine("IsPlaying | Location | Sound path");
		foreach (ILoadedSound activeSound in game.ActiveSounds)
		{
			if (!activeSound.IsDisposed)
			{
				stringBuilder.AppendLine($"{activeSound.IsPlaying} | {activeSound.Params.Position} | {activeSound.Params.Location.ToShortString()}");
			}
		}
		game.Logger.Notification(stringBuilder.ToString());
		return TextCommandResult.Success("Active sounds printed to client-main.txt");
	}

	public override void OnBlockTexturesLoaded()
	{
		game.SoundConfig = game.Platform.AssetManager.TryGet("sounds/soundconfig.json")?.ToObject<SoundConfig>();
		if (game.SoundConfig == null)
		{
			game.SoundConfig = new SoundConfig();
		}
		for (int i = 0; i < game.Blocks.Count; i++)
		{
			Block block = game.Blocks[i];
			if (block != null && !(block.Code == null))
			{
				if (block.Sounds == null)
				{
					block.Sounds = new BlockSounds();
				}
				if (block.Sounds.Walk == null)
				{
					block.Sounds.Walk = game.SoundConfig.defaultBlockSounds.Walk;
				}
				if (block.Sounds.Place == null)
				{
					block.Sounds.Place = game.SoundConfig.defaultBlockSounds.Place;
				}
				if (block.Sounds.Hit == null)
				{
					block.Sounds.Hit = game.SoundConfig.defaultBlockSounds.Hit;
				}
				if (block.Sounds.Break == null)
				{
					block.Sounds.Break = game.SoundConfig.defaultBlockSounds.Break;
				}
			}
		}
	}

	private void OnGameTick500ms(float dt)
	{
		if (game.IsPaused)
		{
			return;
		}
		int count = game.ActiveSounds.Count;
		while (count-- > 0)
		{
			ILoadedSound loadedSound = game.ActiveSounds.Dequeue();
			if (loadedSound == null)
			{
				game.Logger.Error("Found a null sound in the ActiveSounds queue, something is incorrectly programmed. Skipping over it.");
				continue;
			}
			SoundParams soundParams = loadedSound.Params;
			if (soundParams != null && soundParams.DisposeOnFinish && loadedSound.HasStopped && loadedSound.HasReverbStopped(game.ElapsedMilliseconds))
			{
				loadedSound.Dispose();
			}
			else if (!loadedSound.IsDisposed)
			{
				game.ActiveSounds.Enqueue(loadedSound);
			}
		}
		if (!scanning)
		{
			TyronThreadPool.QueueLongDurationTask(scanReverbnessOffthread);
		}
	}

	private void scanReverbnessOffthread()
	{
		scanning = true;
		EntityPos entityPos = game.player.Entity.Pos.Copy().Add(game.player.Entity.LocalEyePos.X, game.player.Entity.LocalEyePos.Y, game.player.Entity.LocalEyePos.Z);
		Vec3d vec3d = game.player.Entity.Pos.XYZ.Add(game.player.Entity.LocalEyePos);
		Vec3d vec3d2 = vec3d.Clone();
		Vec3d vec3d3 = vec3d.Clone();
		BlockSelection blockSelection = new BlockSelection();
		new EntitySelection();
		double num = 0.0;
		_ = game.World.BlockAccessor;
		for (float num2 = 0f; num2 < 360f; num2 += 45f)
		{
			for (float num3 = -90f; num3 <= 90f; num3 += 45f)
			{
				int num4 = 0;
				num4 = ((num3 <= -45f) ? BlockFacing.UP.Index : ((!(num3 >= 45f)) ? BlockFacing.HorizontalFromYaw(num2).Opposite.Index : BlockFacing.DOWN.Index));
				Ray ray = Ray.FromAngles(vec3d, num3 * ((float)Math.PI / 180f), num2 * ((float)Math.PI / 180f), 35f);
				intersectionTester.LoadRayAndPos(ray);
				float maxDistance = (float)ray.Length;
				blockSelection = intersectionTester.GetSelectedBlock(maxDistance, (BlockPos pos, Block block2) => true, testCollide: true);
				Block block = blockSelection?.Block;
				if (block != null && (block.BlockMaterial == EnumBlockMaterial.Metal || block.BlockMaterial == EnumBlockMaterial.Ore || block.BlockMaterial == EnumBlockMaterial.Mantle || block.BlockMaterial == EnumBlockMaterial.Ice || block.BlockMaterial == EnumBlockMaterial.Ceramic || block.BlockMaterial == EnumBlockMaterial.Brick || block.BlockMaterial == EnumBlockMaterial.Stone) && block.SideIsSolid(blockSelection.Position, num4))
				{
					Vec3d fullPosition = blockSelection.FullPosition;
					float num5 = fullPosition.DistanceTo(vec3d);
					num += (Math.Log(num5 + 1f) / 18.0 - 0.07) * 3.0;
					vec3d2.Set(Math.Min(vec3d2.X, fullPosition.X), Math.Min(vec3d2.Y, fullPosition.Y), Math.Min(vec3d2.Z, fullPosition.Z));
					vec3d3.Set(Math.Max(vec3d3.X, fullPosition.X), Math.Max(vec3d3.Y, fullPosition.Y), Math.Max(vec3d3.Z, fullPosition.Z));
				}
				else
				{
					num -= 0.2;
					entityPos.Yaw = num2;
					entityPos.Pitch = num3;
					entityPos.AheadCopy(35.0);
					vec3d2.Set(Math.Min(vec3d2.X, entityPos.X), Math.Min(vec3d2.Y, entityPos.InternalY), Math.Min(vec3d2.Z, entityPos.Z));
					vec3d3.Set(Math.Max(vec3d3.X, entityPos.X), Math.Max(vec3d3.Y, entityPos.InternalY), Math.Max(vec3d3.Z, entityPos.Z));
				}
			}
		}
		TargetReverbness = (float)num;
		RoomLocation = new Cuboidi(vec3d2.AsBlockPos, vec3d3.AsBlockPos).GrowBy(10, 10, 10);
		scanning = false;
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		Vec3f viewVector = game.EntityPlayer.Pos.GetViewVector();
		Vec3d vec3d = game.EntityPlayer.Pos.XYZ.Add(game.EntityPlayer.LocalEyePos);
		game.Platform.UpdateAudioListener((float)vec3d.X, (float)vec3d.Y, (float)vec3d.Z, viewVector.X, 0f, viewVector.Z);
		NowReverbness += (TargetReverbness - NowReverbness) * deltaTime / 1.5f;
	}

	private void OnGameTick100ms(float dt)
	{
		if (game.api.renderapi.ShaderUniforms.GlitchStrength > 0.5f)
		{
			glitchActive = true;
			float t = GameMath.Clamp(game.api.renderapi.ShaderUniforms.GlitchStrength * 2f, 0f, 1f);
			foreach (ILoadedSound activeSound in game.ActiveSounds)
			{
				if (activeSound.Params.SoundType != EnumSoundType.SoundGlitchunaffected && activeSound.Params.SoundType != EnumSoundType.AmbientGlitchunaffected && activeSound.Params.SoundType != EnumSoundType.MusicGlitchunaffected)
				{
					float num = (float)game.Rand.NextDouble() * 0.75f;
					int num2 = game.Rand.Next(2) * 2 - 1;
					activeSound.SetPitchOffset(GameMath.Mix(0f, num * (float)num2 - 0.2f, t));
				}
			}
		}
		else if (glitchActive)
		{
			glitchActive = false;
			foreach (ILoadedSound activeSound2 in game.ActiveSounds)
			{
				if (activeSound2.Params.SoundType != EnumSoundType.SoundGlitchunaffected && activeSound2.Params.SoundType != EnumSoundType.AmbientGlitchunaffected && activeSound2.Params.SoundType != EnumSoundType.MusicGlitchunaffected)
				{
					activeSound2.SetPitchOffset(0f);
				}
			}
		}
		if (submerged() && !prevSubmerged)
		{
			prevSubmerged = true;
			foreach (ILoadedSound activeSound3 in game.ActiveSounds)
			{
				if (!activeSound3.IsDisposed)
				{
					activeSound3.SetLowPassfiltering(0.06f);
					if (!glitchActive && activeSound3.Params.SoundType != EnumSoundType.Music && activeSound3.Params.SoundType != EnumSoundType.MusicGlitchunaffected)
					{
						activeSound3.SetPitchOffset(-0.15f);
					}
				}
			}
		}
		else if (prevSubmerged && !submerged())
		{
			prevSubmerged = false;
			foreach (ILoadedSound activeSound4 in game.ActiveSounds)
			{
				if (!activeSound4.IsDisposed)
				{
					activeSound4.SetLowPassfiltering(1f);
					if (!glitchActive)
					{
						activeSound4.SetPitchOffset(0f);
					}
				}
			}
		}
		if (prevReverbKey == reverbKey())
		{
			return;
		}
		prevReverbKey = reverbKey();
		foreach (ILoadedSound activeSound5 in game.ActiveSounds)
		{
			if (!activeSound5.IsDisposed && activeSound5.IsReady)
			{
				if (activeSound5.Params.Position == null || activeSound5.Params.Position == Zero || RoomLocation.ContainsOrTouches(activeSound5.Params.Position))
				{
					activeSound5.SetReverb(Math.Max(0f, NowReverbness));
				}
				else
				{
					activeSound5.SetReverb(0f);
				}
			}
		}
	}

	private int reverbKey()
	{
		if (!submerged())
		{
			return (int)(NowReverbness * 10f);
		}
		return 0;
	}

	private bool submerged()
	{
		if (!(game.EyesInWaterDepth() > 0f))
		{
			return game.EyesInLavaDepth() > 0f;
		}
		return true;
	}

	public override void Dispose(ClientMain game)
	{
		while (game.ActiveSounds.Count > 0)
		{
			ILoadedSound loadedSound = game.ActiveSounds.Dequeue();
			loadedSound?.Stop();
			loadedSound?.Dispose();
		}
	}

	private void OnSoundLevelChanged(int newValue)
	{
		int count = game.ActiveSounds.Count;
		while (count-- > 0)
		{
			ILoadedSound loadedSound = game.ActiveSounds.Dequeue();
			loadedSound.SetVolume();
			game.ActiveSounds.Enqueue(loadedSound);
		}
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Misc;
	}

	public void Dispose()
	{
	}
}
