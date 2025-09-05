using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf;

public class SystemHighlightBlocks : ClientSystem
{
	private Dictionary<int, BlockHighlight> highlightsByslotId = new Dictionary<int, BlockHighlight>();

	public override string Name => "hibl";

	public SystemHighlightBlocks(ClientMain game)
		: base(game)
	{
		game.PacketHandlers[52] = HandlePacket;
		game.eventManager.RegisterRenderer(OnRenderFrame3DTransparent, EnumRenderStage.OIT, Name, 0.89);
		game.eventManager.OnHighlightBlocks += EventManager_OnHighlightBlocks;
	}

	private void EventManager_OnHighlightBlocks(IPlayer player, int slotId, List<BlockPos> blocks, List<int> colors, EnumHighlightBlocksMode mode = EnumHighlightBlocksMode.Absolute, EnumHighlightShape shape = EnumHighlightShape.Arbitrary, float scale = 1f)
	{
		BlockHighlight orCreateHighlight = getOrCreateHighlight(slotId);
		orCreateHighlight.mode = mode;
		orCreateHighlight.shape = shape;
		orCreateHighlight.Scale = scale;
		orCreateHighlight.TesselateModel(game, blocks.ToArray(), colors?.ToArray());
	}

	private void HandlePacket(Packet_Server packet)
	{
		BlockHighlight orCreateHighlight = getOrCreateHighlight(packet.HighlightBlocks.Slotid);
		if (packet.HighlightBlocks.Blocks.Length == 0)
		{
			orCreateHighlight.Dispose(game);
			highlightsByslotId.Remove(packet.HighlightBlocks.Slotid);
			return;
		}
		orCreateHighlight.mode = (EnumHighlightBlocksMode)packet.HighlightBlocks.Mode;
		orCreateHighlight.shape = (EnumHighlightShape)packet.HighlightBlocks.Shape;
		orCreateHighlight.Scale = CollectibleNet.DeserializeFloatVeryPrecise(packet.HighlightBlocks.Scale);
		BlockPos[] positions = BlockTypeNet.UnpackBlockPositions(packet.HighlightBlocks.Blocks);
		int colorsCount = packet.HighlightBlocks.ColorsCount;
		int[] array = new int[colorsCount];
		if (colorsCount > 0)
		{
			Array.Copy(packet.HighlightBlocks.Colors, array, colorsCount);
		}
		orCreateHighlight.TesselateModel(game, positions, array);
	}

	public void OnRenderFrame3DTransparent(float deltaTime)
	{
		if (highlightsByslotId.Count == 0)
		{
			return;
		}
		ShaderProgramBlockhighlights blockhighlights = ShaderPrograms.Blockhighlights;
		blockhighlights.Use();
		Vec3d cameraPos = game.EntityPlayer.CameraPos;
		foreach (var (_, blockHighlight2) in highlightsByslotId)
		{
			if (blockHighlight2.modelRef == null)
			{
				continue;
			}
			if (blockHighlight2.mode == EnumHighlightBlocksMode.CenteredToSelectedBlock || blockHighlight2.mode == EnumHighlightBlocksMode.AttachedToSelectedBlock)
			{
				if (game.BlockSelection == null || game.BlockSelection.Position == null)
				{
					continue;
				}
				blockHighlight2.origin.X = game.BlockSelection.Position.X + game.BlockSelection.Face.Normali.X;
				blockHighlight2.origin.Y = game.BlockSelection.Position.Y + game.BlockSelection.Face.Normali.Y;
				blockHighlight2.origin.Z = game.BlockSelection.Position.Z + game.BlockSelection.Face.Normali.Z;
			}
			if (blockHighlight2.mode == EnumHighlightBlocksMode.AttachedToSelectedBlock)
			{
				blockHighlight2.origin.X += blockHighlight2.attachmentPoints[game.BlockSelection.Face.Index].X;
				blockHighlight2.origin.Y += blockHighlight2.attachmentPoints[game.BlockSelection.Face.Index].Y;
				blockHighlight2.origin.Z += blockHighlight2.attachmentPoints[game.BlockSelection.Face.Index].Z;
			}
			game.GlPushMatrix();
			game.GlLoadMatrix(game.MainCamera.CameraMatrixOrigin);
			if (blockHighlight2.mode == EnumHighlightBlocksMode.CenteredToBlockSelectionIndex || blockHighlight2.mode == EnumHighlightBlocksMode.AttachedToBlockSelectionIndex)
			{
				if (game.BlockSelection == null || game.BlockSelection.Position == null)
				{
					game.GlPopMatrix();
					continue;
				}
				Cuboidf[] blockIntersectionBoxes = game.GetBlockIntersectionBoxes(game.BlockSelection.Position);
				if (blockIntersectionBoxes == null || blockIntersectionBoxes.Length == 0 || game.BlockSelection.SelectionBoxIndex >= blockIntersectionBoxes.Length)
				{
					game.GlPopMatrix();
					continue;
				}
				BlockPos position = game.BlockSelection.Position;
				float scale = blockHighlight2.Scale;
				Vec3d hitPosition = game.BlockSelection.HitPosition;
				int index = game.BlockSelection.Face.Index;
				double num2;
				double num3;
				double num4;
				if (blockHighlight2.mode == EnumHighlightBlocksMode.AttachedToBlockSelectionIndex && blockHighlight2.shape != EnumHighlightShape.Cube)
				{
					num2 = (float)position.X + (float)(int)(hitPosition.X * 16.0) / 16f + (float)blockHighlight2.attachmentPoints[index].X * scale;
					num3 = (float)position.Y + (float)(int)(hitPosition.Y * 16.0) / 16f + (float)blockHighlight2.attachmentPoints[index].Y * scale;
					num4 = (float)position.Z + (float)(int)(hitPosition.Z * 16.0) / 16f + (float)blockHighlight2.attachmentPoints[index].Z * scale;
				}
				else
				{
					num2 = (float)position.X + (float)(int)(hitPosition.X * 16.0) / 16f;
					num3 = (float)position.Y + (float)(int)(hitPosition.Y * 16.0) / 16f;
					num4 = (float)position.Z + (float)(int)(hitPosition.Z * 16.0) / 16f;
				}
				if (blockHighlight2.mode == EnumHighlightBlocksMode.AttachedToBlockSelectionIndex && blockHighlight2.shape == EnumHighlightShape.Cube)
				{
					if (blockHighlight2.attachmentPoints[index].X < 0)
					{
						num2 -= Math.Ceiling((float)blockHighlight2.Size.X / 2f) * (double)scale;
					}
					else if (blockHighlight2.attachmentPoints[index].X > 0)
					{
						num2 += (double)((float)blockHighlight2.attachmentPoints[index].X * scale);
					}
					if (blockHighlight2.attachmentPoints[index].Y < 0)
					{
						num3 -= Math.Ceiling((float)blockHighlight2.Size.Y / 2f) * (double)scale;
					}
					else if (blockHighlight2.attachmentPoints[index].Y > 0)
					{
						num3 += (double)((float)blockHighlight2.attachmentPoints[index].Y * scale);
					}
					if (blockHighlight2.attachmentPoints[index].Z < 0)
					{
						num4 -= Math.Ceiling((float)blockHighlight2.Size.Z / 2f) * (double)scale;
					}
					else if (blockHighlight2.attachmentPoints[index].Z > 0)
					{
						num4 += (double)((float)blockHighlight2.attachmentPoints[index].Z * scale);
					}
				}
				game.GlTranslate((float)(num2 - cameraPos.X), (float)(num3 - cameraPos.Y), (float)(num4 - cameraPos.Z));
				game.GlScale(scale, scale, scale);
			}
			else
			{
				game.GlTranslate((float)((double)blockHighlight2.origin.X - cameraPos.X), (float)((double)blockHighlight2.origin.Y - cameraPos.Y), (float)((double)blockHighlight2.origin.Z - cameraPos.Z));
			}
			blockhighlights.ProjectionMatrix = game.CurrentProjectionMatrix;
			blockhighlights.ModelViewMatrix = game.CurrentModelViewMatrix;
			game.Platform.RenderMesh(blockHighlight2.modelRef);
			game.GlPopMatrix();
		}
		blockhighlights.Stop();
	}

	public override void Dispose(ClientMain game)
	{
		foreach (KeyValuePair<int, BlockHighlight> item in highlightsByslotId)
		{
			item.Value.Dispose(game);
		}
		highlightsByslotId.Clear();
	}

	private BlockHighlight getOrCreateHighlight(int slotId)
	{
		if (!highlightsByslotId.TryGetValue(slotId, out var value))
		{
			return highlightsByslotId[slotId] = new BlockHighlight();
		}
		return value;
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Render;
	}
}
