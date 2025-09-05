using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class SystemPlayerEnvAwarenessTracker : ClientSystem
{
	private TrackedPlayerProperties currentProperties = new TrackedPlayerProperties();

	public override string Name => "pltr";

	private TrackedPlayerProperties latestProperties => game.playerProperties;

	public SystemPlayerEnvAwarenessTracker(ClientMain game)
		: base(game)
	{
		game.RegisterGameTickListener(OnGameTick, 20);
		game.RegisterGameTickListener(OnGameTick1s, 1000);
	}

	private void OnGameTick1s(float dt)
	{
		GlobalConstants.CurrentDistanceToRainfallClient = game.blockAccessor.GetDistanceToRainFall(game.EntityPlayer.Pos.AsBlockPos, 12, 4);
	}

	public override void OnOwnPlayerDataReceived()
	{
		base.OnOwnPlayerDataReceived();
		BlockPos asBlockPos = game.EntityPlayer.Pos.AsBlockPos;
		currentProperties.PlayerChunkPos.X = asBlockPos.X / game.WorldMap.ClientChunkSize;
		currentProperties.PlayerChunkPos.Y = asBlockPos.InternalY / game.WorldMap.ClientChunkSize;
		currentProperties.PlayerChunkPos.Z = asBlockPos.Z / game.WorldMap.ClientChunkSize;
	}

	public void OnGameTick(float dt)
	{
		latestProperties.EyesInWaterColorShift = game.GetEyesInWaterColorShift();
		latestProperties.EyesInWaterDepth = game.EyesInWaterDepth();
		latestProperties.EyesInLavaColorShift = game.GetEyesInLavaColorShift();
		latestProperties.EyesInLavaDepth = game.EyesInLavaDepth();
		latestProperties.DayLight = game.GameWorldCalendar.DayLightStrength;
		latestProperties.MoonLight = game.GameWorldCalendar.MoonLightStrength;
		BlockPos asBlockPos = game.EntityPlayer.Pos.AsBlockPos;
		latestProperties.PlayerChunkPos.X = asBlockPos.X / game.WorldMap.ClientChunkSize;
		latestProperties.PlayerChunkPos.Y = asBlockPos.InternalY / game.WorldMap.ClientChunkSize;
		latestProperties.PlayerChunkPos.Z = asBlockPos.Z / game.WorldMap.ClientChunkSize;
		latestProperties.PlayerPosDiv8.X = asBlockPos.X / 8;
		latestProperties.PlayerPosDiv8.Y = asBlockPos.InternalY / 8;
		latestProperties.PlayerPosDiv8.Z = asBlockPos.Z / 8;
		latestProperties.FallSpeed = game.EntityPlayer.Pos.Motion.Length();
		latestProperties.DistanceToSpawnPoint = (int)game.EntityPlayer.Pos.DistanceTo(game.player.SpawnPosition.ToVec3d());
		double y = game.EntityPlayer.Pos.Y;
		currentProperties.posY = (latestProperties.posY = (((double)game.SeaLevel < y) ? ((float)(y / (double)game.SeaLevel)) : ((float)((y - (double)game.SeaLevel) / (double)(game.WorldMap.MapSizeY - game.SeaLevel)))));
		currentProperties.sunSlight = (latestProperties.sunSlight = game.WorldMap.RelaxedBlockAccess.GetLightLevel(asBlockPos, EnumLightLevelType.OnlySunLight));
		currentProperties.Playstyle = (latestProperties.Playstyle = game.ServerInfo.Playstyle);
		currentProperties.PlayListCode = (latestProperties.PlayListCode = game.ServerInfo.PlayListCode);
		if (Math.Abs(latestProperties.FallSpeed - currentProperties.FallSpeed) > 0.005)
		{
			Trigger(EnumProperty.FallSpeed);
		}
		if (Math.Abs(latestProperties.EyesInWaterDepth - currentProperties.EyesInWaterDepth) > 0.005f || currentProperties.EyesInWaterDepth == 0f)
		{
			Trigger(EnumProperty.EyesInWaterDepth);
		}
		if (latestProperties.EyesInWaterColorShift != currentProperties.EyesInWaterColorShift)
		{
			Trigger(EnumProperty.EyesInWaterColorShift);
		}
		if (Math.Abs(latestProperties.EyesInLavaDepth - currentProperties.EyesInLavaDepth) > 0.005f)
		{
			Trigger(EnumProperty.EyesInLavaDepth);
		}
		if (latestProperties.EyesInLavaColorShift != currentProperties.EyesInLavaColorShift)
		{
			Trigger(EnumProperty.EyesInLavaColorShift);
		}
		if (latestProperties.DayLight != currentProperties.DayLight)
		{
			Trigger(EnumProperty.DayLight);
		}
		if (latestProperties.MoonLight != currentProperties.MoonLight)
		{
			Trigger(EnumProperty.MoonLight);
		}
		if (!latestProperties.PlayerChunkPos.Equals(currentProperties.PlayerChunkPos))
		{
			Trigger(EnumProperty.PlayerChunkPos);
		}
		if (!latestProperties.PlayerPosDiv8.Equals(currentProperties.PlayerPosDiv8))
		{
			Trigger(EnumProperty.PlayerPosDiv8);
		}
		currentProperties.EyesInWaterColorShift = latestProperties.EyesInWaterColorShift;
		currentProperties.EyesInWaterDepth = latestProperties.EyesInWaterDepth;
		currentProperties.EyesInLavaColorShift = latestProperties.EyesInLavaColorShift;
		currentProperties.EyesInLavaDepth = latestProperties.EyesInLavaDepth;
		currentProperties.DayLight = latestProperties.DayLight;
		currentProperties.PlayerChunkPos.Set(latestProperties.PlayerChunkPos);
		currentProperties.PlayerPosDiv8.Set(latestProperties.PlayerPosDiv8);
		currentProperties.FallSpeed = latestProperties.FallSpeed;
	}

	public void Trigger(EnumProperty property)
	{
		List<OnPlayerPropertyChanged> value = null;
		game.eventManager?.OnPlayerPropertyChanged.TryGetValue(property, out value);
		if (value == null)
		{
			return;
		}
		foreach (OnPlayerPropertyChanged item in value)
		{
			item(currentProperties, latestProperties);
		}
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Misc;
	}
}
