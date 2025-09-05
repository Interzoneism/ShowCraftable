using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class SystemRenderDebugWireframes : ClientSystem
{
	private WireframeCube chunkWf;

	private WireframeCube entityWf;

	private WireframeCube beWf;

	private WireframeModes wfmodes => game.api.renderapi.WireframeDebugRender;

	public override string Name => "debwf";

	public SystemRenderDebugWireframes(ClientMain game)
		: base(game)
	{
		chunkWf = WireframeCube.CreateCenterOriginCube(game.api);
		entityWf = WireframeCube.CreateCenterOriginCube(game.api, -1);
		beWf = WireframeCube.CreateCenterOriginCube(game.api, -939523896);
		game.eventManager.RegisterRenderer(OnRenderFrame3D, EnumRenderStage.Opaque, Name, 0.5);
	}

	public override void Dispose(ClientMain game)
	{
		chunkWf.Dispose();
		entityWf.Dispose();
		beWf.Dispose();
	}

	public void OnRenderFrame3D(float deltaTime)
	{
		int dimension = game.EntityPlayer.Pos.Dimension;
		if (wfmodes.Entity)
		{
			foreach (Entity value in game.LoadedEntities.Values)
			{
				if (value.Pos.Dimension == dimension && value.SelectionBox != null)
				{
					float num = value.SelectionBox.XSize / 2f;
					float num2 = value.SelectionBox.YSize / 2f;
					float num3 = value.SelectionBox.ZSize / 2f;
					double x;
					double num4;
					double z;
					if (value == game.EntityPlayer)
					{
						x = game.EntityPlayer.CameraPos.X;
						num4 = game.EntityPlayer.CameraPos.Y + (double)(dimension * 32768);
						z = game.EntityPlayer.CameraPos.Z;
					}
					else
					{
						x = value.Pos.X;
						num4 = value.Pos.InternalY;
						z = value.Pos.Z;
					}
					double posx = x + (double)value.SelectionBox.X1 + (double)num;
					double posy = num4 + (double)value.SelectionBox.Y1 + (double)num2;
					double posz = z + (double)value.SelectionBox.Z1 + (double)num3;
					float lineWidth = ((game.EntitySelection != null && value.EntityId == game.EntitySelection.Entity.EntityId) ? 3f : 1f);
					entityWf.Render(game.api, posx, posy, posz, num, num2, num3, lineWidth, new Vec4f(0f, 0f, 1f, 1f));
					float num5 = value.SelectionBox.XSize / 2f;
					float num6 = value.SelectionBox.YSize / 2f;
					float num7 = value.SelectionBox.ZSize / 2f;
					if (num5 != num || num6 != num2 || num7 != num3)
					{
						posx = x + (double)value.SelectionBox.X1 + (double)num5;
						posy = num4 + (double)value.SelectionBox.Y1 + (double)num6;
						posz = z + (double)value.SelectionBox.Z1 + (double)num7;
						entityWf.Render(game.api, posx, posy, posz, num5, num6, num7, lineWidth, new Vec4f(0f, 0f, 1f, 1f));
					}
					float num8 = value.CollisionBox.XSize / 2f;
					float num9 = value.CollisionBox.YSize / 2f;
					float num10 = value.CollisionBox.ZSize / 2f;
					posx = x + (double)value.CollisionBox.X1 + (double)num8;
					posy = num4 + (double)value.CollisionBox.Y1 + (double)num9;
					posz = z + (double)value.CollisionBox.Z1 + (double)num10;
					entityWf.Render(game.api, posx, posy, posz, num8, num9, num10, lineWidth, new Vec4f(1f, 0f, 0f, 1f));
				}
			}
		}
		if (wfmodes.Chunk)
		{
			int clientChunkSize = game.WorldMap.ClientChunkSize;
			BlockPos blockPos = game.EntityPlayer.Pos.AsBlockPos / clientChunkSize * clientChunkSize + clientChunkSize / 2;
			chunkWf.Render(game.api, (float)blockPos.X + 0.01f, (float)blockPos.InternalY + 0.01f, (float)blockPos.Z + 0.01f, clientChunkSize / 2, clientChunkSize / 2, clientChunkSize / 2, 8f);
		}
		if (wfmodes.ServerChunk)
		{
			int serverChunkSize = game.WorldMap.ServerChunkSize;
			BlockPos blockPos2 = game.EntityPlayer.Pos.AsBlockPos / serverChunkSize * serverChunkSize + serverChunkSize / 2;
			chunkWf.Render(game.api, (float)blockPos2.X + 0.01f, (float)blockPos2.InternalY + 0.01f, (float)blockPos2.Z + 0.01f, serverChunkSize / 2, serverChunkSize / 2, serverChunkSize / 2, 8f);
		}
		if (wfmodes.Region)
		{
			int regionSize = game.WorldMap.RegionSize;
			BlockPos blockPos3 = game.EntityPlayer.Pos.AsBlockPos / regionSize * regionSize + regionSize / 2;
			chunkWf.Render(game.api, (float)blockPos3.X + 0.01f, (float)blockPos3.InternalY + 0.01f, (float)blockPos3.Z + 0.01f, regionSize / 2, regionSize / 2, regionSize / 2, 16f);
		}
		if (wfmodes.LandClaim && game.WorldMap.LandClaims != null)
		{
			foreach (LandClaim landClaim in game.WorldMap.LandClaims)
			{
				Vec4f color = new Vec4f(1f, 1f, 0.5f, 1f);
				foreach (Cuboidi area in landClaim.Areas)
				{
					int num11 = Math.Min(area.X1, area.X2);
					int num12 = Math.Min(area.Y1, area.Y2);
					int num13 = Math.Min(area.Z1, area.Z2);
					entityWf.Render(game.api, (float)num11 + (float)area.SizeX / 2f, (float)num12 + (float)area.SizeY / 2f, (float)num13 + (float)area.SizeZ / 2f, (float)area.SizeX / 2f, (float)area.SizeY / 2f, (float)area.SizeZ / 2f, 4f, color);
				}
			}
		}
		if (wfmodes.Structures)
		{
			int regionSize2 = game.WorldMap.RegionSize;
			int regionX = (int)(game.EntityPlayer.Pos.X / (double)regionSize2);
			int regionZ = (int)(game.EntityPlayer.Pos.Z / (double)regionSize2);
			IMapRegion mapRegion = game.WorldMap.GetMapRegion(regionX, regionZ);
			if (mapRegion != null)
			{
				Vec4f color2 = new Vec4f(1f, 0f, 1f, 1f);
				foreach (GeneratedStructure generatedStructure in mapRegion.GeneratedStructures)
				{
					entityWf.Render(game.api, (float)generatedStructure.Location.X1 + (float)generatedStructure.Location.SizeX / 2f, (float)generatedStructure.Location.Y1 + (float)generatedStructure.Location.SizeY / 2f, (float)generatedStructure.Location.Z1 + (float)generatedStructure.Location.SizeZ / 2f, (float)generatedStructure.Location.SizeX / 2f, (float)generatedStructure.Location.SizeY / 2f, (float)generatedStructure.Location.SizeZ / 2f, 2f, color2);
				}
			}
		}
		if (wfmodes.BlockEntity)
		{
			BlockPos blockPos4 = game.EntityPlayer.Pos.AsBlockPos / 32;
			int num14 = blockPos4.dimension * 1024;
			for (int i = -1; i <= 1; i++)
			{
				for (int j = -1; j <= 1; j++)
				{
					for (int k = -1; k <= 1; k++)
					{
						ClientChunk clientChunk = game.WorldMap.GetClientChunk(blockPos4.X + i, blockPos4.Y + num14 + j, blockPos4.Z + k);
						if (clientChunk == null)
						{
							continue;
						}
						foreach (KeyValuePair<BlockPos, BlockEntity> blockEntity in clientChunk.BlockEntities)
						{
							BlockPos key = blockEntity.Key;
							beWf.Render(game.api, (float)key.X + 0.5f, (float)key.InternalY + 0.5f, (float)key.Z + 0.5f, 0.5f, 0.5f, 0.5f, 1f);
						}
					}
				}
			}
		}
		if (wfmodes.Inside)
		{
			BlockPos blockPos5 = new BlockPos();
			Block insideTorsoBlockSoundSource = game.player.Entity.GetInsideTorsoBlockSoundSource(blockPos5);
			renderBoxes(insideTorsoBlockSoundSource, blockPos5, new Vec4f(1f, 0.7f, 0.2f, 1f));
			insideTorsoBlockSoundSource = game.player.Entity.GetInsideLegsBlockSoundSource(blockPos5);
			renderBoxes(insideTorsoBlockSoundSource, blockPos5, new Vec4f(1f, 1f, 0f, 1f));
			EntityPos sidedPos = game.player.Entity.SidedPos;
			insideTorsoBlockSoundSource = game.player.Entity.GetNearestBlockSoundSource(blockPos5, -0.03, 4, usecollisionboxes: true);
			renderBoxes(insideTorsoBlockSoundSource, blockPos5, new Vec4f(1f, 0f, 1f, 1f));
			blockPos5.Set((int)sidedPos.X, (int)(sidedPos.Y + 0.10000000149011612), (int)sidedPos.Z);
			insideTorsoBlockSoundSource = game.blockAccessor.GetBlock(blockPos5, 2);
			if (insideTorsoBlockSoundSource.Id != 0)
			{
				renderBoxes(insideTorsoBlockSoundSource, blockPos5, new Vec4f(0f, 1f, 1f, 1f));
			}
		}
	}

	private void renderBoxes(Block block, BlockPos tmpPos, Vec4f color)
	{
		if (block != null)
		{
			Cuboidf[] selectionBoxes = block.GetSelectionBoxes(game.blockAccessor, tmpPos);
			foreach (Cuboidf cuboidf in selectionBoxes)
			{
				entityWf.Render(game.api, (float)tmpPos.X + cuboidf.MidX, (float)tmpPos.Y + cuboidf.MidY, (float)tmpPos.Z + cuboidf.MidZ, cuboidf.XSize / 2f, cuboidf.YSize / 2f, cuboidf.ZSize / 2f, 1f, color);
			}
		}
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Misc;
	}
}
