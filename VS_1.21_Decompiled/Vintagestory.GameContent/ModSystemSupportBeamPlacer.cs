using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class ModSystemSupportBeamPlacer : ModSystem, IRenderer, IDisposable
{
	protected Dictionary<string, MeshData[]> origBeamMeshes = new Dictionary<string, MeshData[]>();

	private Dictionary<string, BeamPlacerWorkSpace> workspaceByPlayer = new Dictionary<string, BeamPlacerWorkSpace>();

	private ICoreAPI api;

	private ICoreClientAPI capi;

	public Matrixf ModelMat = new Matrixf();

	public double RenderOrder => 0.5;

	public int RenderRange => 12;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return true;
	}

	public override void Start(ICoreAPI api)
	{
		this.api = api;
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		capi = api;
		api.Event.RegisterRenderer(this, EnumRenderStage.Opaque, "beamplacer");
	}

	public bool CancelPlace(BlockSupportBeam blockSupportBeam, EntityAgent byEntity)
	{
		BeamPlacerWorkSpace workSpace = getWorkSpace(byEntity);
		if (workSpace.nowBuilding)
		{
			workSpace.nowBuilding = false;
			workSpace.startOffset = null;
			workSpace.endOffset = null;
			return true;
		}
		return false;
	}

	public Vec3f snapToGrid(IVec3 pos, int gridSize)
	{
		return new Vec3f((float)(int)Math.Round(pos.XAsFloat * (float)gridSize) / (float)gridSize, (float)(int)Math.Round(pos.YAsFloat * (float)gridSize) / (float)gridSize, (float)(int)Math.Round(pos.ZAsFloat * (float)gridSize) / (float)gridSize);
	}

	public void OnInteract(Block block, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, bool partialEnds)
	{
		if (blockSel != null)
		{
			BeamPlacerWorkSpace workSpace = getWorkSpace(byEntity);
			if (!workSpace.nowBuilding)
			{
				beginPlace(workSpace, block, byEntity, blockSel, partialEnds);
			}
			else
			{
				completePlace(workSpace, byEntity, slot);
			}
		}
	}

	private void beginPlace(BeamPlacerWorkSpace ws, Block block, EntityAgent byEntity, BlockSelection blockSel, bool partialEnds)
	{
		ws.GridSize = (byEntity.Controls.CtrlKey ? 16 : 4);
		ws.currentMeshes = getOrCreateBeamMeshes(block, (block as BlockSupportBeam)?.PartialEnds ?? false);
		if (api.World.BlockAccessor.GetBlockEntity(blockSel.Position)?.GetBehavior<BEBehaviorSupportBeam>() == null)
		{
			Block block2 = api.World.BlockAccessor.GetBlock(blockSel.Position);
			if (block2.Replaceable >= 6000)
			{
				ws.startPos = blockSel.Position.Copy();
				ws.startOffset = snapToGrid(blockSel.HitPosition, ws.GridSize);
			}
			else
			{
				BlockFacing face = blockSel.Face;
				BlockPos blockPos = blockSel.Position.AddCopy(face);
				block2 = api.World.BlockAccessor.GetBlock(blockPos);
				if (api.World.BlockAccessor.GetBlockEntity(blockPos)?.GetBehavior<BEBehaviorSupportBeam>() == null && block2.Replaceable < 6000)
				{
					(api as ICoreClientAPI)?.TriggerIngameError(this, "notplaceablehere", Lang.Get("Cannot place here, a block is in the way"));
					return;
				}
				ws.startPos = blockPos;
				ws.startOffset = snapToGrid(blockSel.HitPosition, ws.GridSize).Sub(blockSel.Face.Normali);
			}
		}
		else
		{
			ws.startPos = blockSel.Position.Copy();
			ws.startOffset = snapToGrid(blockSel.HitPosition, ws.GridSize);
		}
		ws.endOffset = null;
		ws.nowBuilding = true;
		ws.block = block;
		ws.onFacing = blockSel.Face;
	}

	private void completePlace(BeamPlacerWorkSpace ws, EntityAgent byEntity, ItemSlot slot)
	{
		ws.nowBuilding = false;
		BlockEntity blockEntity = api.World.BlockAccessor.GetBlockEntity(ws.startPos);
		BEBehaviorSupportBeam bEBehaviorSupportBeam = blockEntity?.GetBehavior<BEBehaviorSupportBeam>();
		EntityPlayer entityPlayer = byEntity as EntityPlayer;
		Vec3f endOffset = getEndOffset(entityPlayer.Player, ws);
		if (endOffset.DistanceTo(ws.startOffset) < 0.01f)
		{
			return;
		}
		if (bEBehaviorSupportBeam == null)
		{
			if (api.World.BlockAccessor.GetBlock(ws.startPos).Replaceable < 6000)
			{
				(api as ICoreClientAPI)?.TriggerIngameError(this, "notplaceablehere", Lang.Get("Cannot place here, a block is in the way"));
				return;
			}
			IPlayer player = (byEntity as EntityPlayer)?.Player;
			if (!api.World.Claims.TryAccess(player, ws.startPos, EnumBlockAccessFlags.BuildOrBreak))
			{
				player.InventoryManager.ActiveHotbarSlot.MarkDirty();
				return;
			}
			if (entityPlayer.Player.WorldData.CurrentGameMode != EnumGameMode.Creative)
			{
				int num = (int)Math.Ceiling(endOffset.DistanceTo(ws.startOffset));
				if (slot.StackSize < num)
				{
					(api as ICoreClientAPI)?.TriggerIngameError(this, "notenoughitems", Lang.Get("You need {0} beams to place a beam at this lenth", num));
					return;
				}
			}
			api.World.BlockAccessor.SetBlock(ws.block.Id, ws.startPos);
			blockEntity = api.World.BlockAccessor.GetBlockEntity(ws.startPos);
			bEBehaviorSupportBeam = blockEntity?.GetBehavior<BEBehaviorSupportBeam>();
		}
		if (entityPlayer.Player.WorldData.CurrentGameMode != EnumGameMode.Creative)
		{
			int num2 = (int)Math.Ceiling(endOffset.DistanceTo(ws.startOffset));
			if (slot.StackSize < num2)
			{
				(api as ICoreClientAPI)?.TriggerIngameError(this, "notenoughitems", Lang.Get("You need {0} beams to place a beam at this lenth", num2));
				return;
			}
			slot.TakeOut(num2);
			slot.MarkDirty();
		}
		bEBehaviorSupportBeam.AddBeam(ws.startOffset, endOffset, ws.onFacing, ws.block);
		blockEntity.MarkDirty(redrawOnClient: true);
	}

	public MeshData[] getOrCreateBeamMeshes(Block block, bool partialEnds, ITexPositionSource texSource = null, string texSourceKey = null)
	{
		if (capi == null)
		{
			return null;
		}
		if (texSource != null)
		{
			capi.Tesselator.TesselateShape(texSourceKey, capi.TesselatorManager.GetCachedShape(block.Shape.Base), out var modeldata, texSource, null, 0, 0, 0);
			return new MeshData[1] { modeldata };
		}
		string key = string.Concat(block.Code, "-", partialEnds.ToString());
		if (!origBeamMeshes.TryGetValue(key, out var value))
		{
			if (partialEnds)
			{
				value = (origBeamMeshes[key] = new MeshData[4]);
				for (int i = 0; i < 4; i++)
				{
					AssetLocation assetLocation = block.Shape.Base.Clone().WithFilename(((i + 1) * 4).ToString() ?? "");
					Shape shape = capi.Assets.Get(assetLocation.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json")).ToObject<Shape>();
					capi.Tesselator.TesselateShape(block, shape, out var modeldata2);
					value[i] = modeldata2;
				}
			}
			else
			{
				value = (origBeamMeshes[key] = new MeshData[1]);
				capi.Tesselator.TesselateShape(block, capi.TesselatorManager.GetCachedShape(block.Shape.Base), out var modeldata3);
				value[0] = modeldata3;
			}
		}
		return value;
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		BeamPlacerWorkSpace workSpace = getWorkSpace(capi.World.Player.PlayerUID);
		if (!workSpace.nowBuilding)
		{
			return;
		}
		Vec3f endOffset = getEndOffset(capi.World.Player, workSpace);
		if (!((double)workSpace.startOffset.DistanceTo(endOffset) < 0.1))
		{
			if (workSpace.endOffset != endOffset)
			{
				workSpace.endOffset = endOffset;
				reloadMeshRef();
			}
			if (workSpace.currentMeshRef != null)
			{
				IShaderProgram currentActiveShader = capi.Render.CurrentActiveShader;
				currentActiveShader?.Stop();
				IStandardShaderProgram standardShaderProgram = capi.Render.PreparedStandardShader(workSpace.startPos.X, workSpace.startPos.InternalY, workSpace.startPos.Z);
				Vec3d cameraPos = capi.World.Player.Entity.CameraPos;
				standardShaderProgram.Use();
				standardShaderProgram.ModelMatrix = ModelMat.Identity().Translate((double)workSpace.startPos.X - cameraPos.X, (double)workSpace.startPos.InternalY - cameraPos.Y, (double)workSpace.startPos.Z - cameraPos.Z).Values;
				standardShaderProgram.ViewMatrix = capi.Render.CameraMatrixOriginf;
				standardShaderProgram.ProjectionMatrix = capi.Render.CurrentProjectionMatrix;
				capi.Render.RenderMultiTextureMesh(workSpace.currentMeshRef, "tex");
				standardShaderProgram.Stop();
				currentActiveShader?.Use();
			}
		}
	}

	protected Vec3f getEndOffset(IPlayer player, BeamPlacerWorkSpace ws)
	{
		Vec3d pos;
		if (player.CurrentBlockSelection != null)
		{
			BlockSelection currentBlockSelection = player.CurrentBlockSelection;
			pos = currentBlockSelection.Position.ToVec3d().Sub(ws.startPos).Add(currentBlockSelection.HitPosition);
		}
		else
		{
			pos = player.Entity.SidedPos.AheadCopy(2.0).XYZ.Add(player.Entity.LocalEyePos).Sub(ws.startPos);
		}
		Vec3f vec3f = snapToGrid(pos, ws.GridSize);
		double num = vec3f.X - ws.startOffset.X;
		double num2 = vec3f.Y - ws.startOffset.Y;
		double num3 = vec3f.Z - ws.startOffset.Z;
		double val = Math.Sqrt(num3 * num3 + num2 * num2 + num * num);
		double y = Math.Sqrt(num3 * num3 + num * num);
		float num4 = -(float)Math.PI / 2f - (float)Math.Atan2(0.0 - num, 0.0 - num3);
		float num5 = (float)Math.Atan2(y, num2);
		if (player.Entity.Controls.ShiftKey)
		{
			float num6 = 15f;
			num4 = (float)Math.Round(num4 * (180f / (float)Math.PI) / num6) * num6 * ((float)Math.PI / 180f);
			num5 = (float)Math.Round(num5 * (180f / (float)Math.PI) / num6) * num6 * ((float)Math.PI / 180f);
		}
		double num7 = Math.Cos(num4);
		double num8 = Math.Sin(num4);
		double num9 = Math.Cos(num5);
		double num10 = Math.Sin(num5);
		val = Math.Min(val, 20.0);
		return new Vec3f(ws.startOffset.X + (float)(val * num10 * num7), ws.startOffset.Y + (float)(val * num9), ws.startOffset.Z + (float)(val * num10 * num8));
	}

	private void reloadMeshRef()
	{
		BeamPlacerWorkSpace workSpace = getWorkSpace(capi.World.Player.PlayerUID);
		workSpace.currentMeshRef?.Dispose();
		MeshData data = generateMesh(workSpace.startOffset, workSpace.endOffset, workSpace.onFacing, workSpace.currentMeshes, workSpace.block.Attributes?["slumpPerMeter"].AsFloat() ?? 0f);
		workSpace.currentMeshRef = capi.Render.UploadMultiTextureMesh(data);
	}

	public static MeshData generateMesh(Vec3f start, Vec3f end, BlockFacing facing, MeshData[] origMeshes, float slumpPerMeter)
	{
		MeshData meshData = new MeshData(4, 6).WithRenderpasses().WithXyzFaces().WithColorMaps();
		float[] array = new float[16];
		double num = end.X - start.X;
		double num2 = end.Y - start.Y;
		double num3 = end.Z - start.Z;
		double num4 = Math.Sqrt(num3 * num3 + num2 * num2 + num * num);
		double y = Math.Sqrt(num3 * num3 + num * num);
		double num5 = 1.0 / Math.Max(1.0, num4);
		Vec3f vec3f = new Vec3f((float)(num * num5), (float)(num2 * num5), (float)(num3 * num5));
		float num6 = (float)Math.Atan2(0.0 - num, 0.0 - num3) + (float)Math.PI / 2f;
		float num7 = (float)Math.Atan2(y, 0.0 - num2) + (float)Math.PI / 2f;
		float val = Math.Abs((float)(Math.Sin(num6) * Math.Cos(num6)));
		float val2 = Math.Abs((float)(Math.Sin(num7) * Math.Cos(num7)));
		float num8 = Math.Max(val, val2);
		float num9 = 0.0625f * num8 * 4f;
		float num10 = 0f;
		num4 += (double)num9;
		for (float num11 = 0f - num9; (double)num11 < num4; num11 += 1f)
		{
			double num12 = Math.Min(1.0, num4 - (double)num11);
			if (!(num12 < 0.01))
			{
				Vec3f vec3f2 = start + num11 * vec3f;
				float num13 = (float)((double)num11 - num4 / 2.0);
				float rad = num7 + num13 * slumpPerMeter;
				num10 += (float)Math.Sin(num13 * slumpPerMeter);
				if (origMeshes.Length > 1 && num4 < 1.125)
				{
					num12 = num4;
					num11 += 1f;
				}
				int num14 = GameMath.Clamp((int)Math.Round((num12 - 0.25) * (double)origMeshes.Length), 0, origMeshes.Length - 1);
				float num15 = (float)(num14 + 1) / 4f;
				float num16 = ((origMeshes.Length == 1) ? ((float)num12) : ((float)num12 / num15));
				Mat4f.Identity(array);
				Mat4f.Translate(array, array, vec3f2.X, vec3f2.Y + num10, vec3f2.Z);
				Mat4f.RotateY(array, array, num6);
				Mat4f.RotateZ(array, array, rad);
				Mat4f.Scale(array, array, new float[3] { num16, 1f, 1f });
				Mat4f.Translate(array, array, -1f, -0.125f, -0.5f);
				MeshData meshData2 = origMeshes[num14].Clone();
				meshData2.MatrixTransform(array);
				meshData.AddMeshData(meshData2);
			}
		}
		return meshData;
	}

	public static float[] GetAlignMatrix(IVec3 startPos, IVec3 endPos, BlockFacing facing)
	{
		double num = startPos.XAsDouble - endPos.XAsDouble;
		double num2 = startPos.YAsDouble - endPos.YAsDouble;
		double num3 = startPos.ZAsDouble - endPos.ZAsDouble;
		double num4 = Math.Sqrt(num3 * num3 + num2 * num2 + num * num);
		double y = Math.Sqrt(num3 * num3 + num * num);
		float rad = (float)Math.Atan2(num, num3) + (float)Math.PI / 2f;
		float rad2 = (float)Math.Atan2(y, num2) + (float)Math.PI / 2f;
		float[] array = new float[16];
		Mat4f.Identity(array);
		Mat4f.Translate(array, array, (float)startPos.XAsDouble, (float)startPos.YAsDouble, (float)startPos.ZAsDouble);
		Mat4f.RotateY(array, array, rad);
		Mat4f.RotateZ(array, array, rad2);
		Mat4f.Scale(array, array, new float[3]
		{
			(float)num4,
			1f,
			1f
		});
		Mat4f.Translate(array, array, -1f, -0.125f, -0.5f);
		return array;
	}

	private BeamPlacerWorkSpace getWorkSpace(EntityAgent forEntity)
	{
		return getWorkSpace((forEntity as EntityPlayer)?.PlayerUID);
	}

	private BeamPlacerWorkSpace getWorkSpace(string playerUID)
	{
		if (workspaceByPlayer.TryGetValue(playerUID, out var value))
		{
			return value;
		}
		return workspaceByPlayer[playerUID] = new BeamPlacerWorkSpace();
	}

	public void OnBeamRemoved(Vec3d start, Vec3d end)
	{
		StartEnd startend = new StartEnd
		{
			Start = start,
			End = end
		};
		chunkremove(start.AsBlockPos, startend);
		chunkremove(end.AsBlockPos, startend);
	}

	public void OnBeamAdded(Vec3d start, Vec3d end)
	{
		StartEnd startend = new StartEnd
		{
			Start = start,
			End = end
		};
		chunkadd(start.AsBlockPos, startend);
		chunkadd(end.AsBlockPos, startend);
	}

	private void chunkadd(BlockPos blockpos, StartEnd startend)
	{
		GetSbData(blockpos)?.Beams.Add(startend);
	}

	private void chunkremove(BlockPos blockpos, StartEnd startend)
	{
		GetSbData(blockpos)?.Beams.Remove(startend);
	}

	public double GetStableMostBeam(BlockPos blockpos, out StartEnd beamstartend)
	{
		SupportBeamsData sbData = GetSbData(blockpos);
		if (sbData.Beams == null || sbData.Beams.Count == 0)
		{
			beamstartend = null;
			return 99999.0;
		}
		double num = 99999.0;
		StartEnd startEnd = null;
		Vec3d point = blockpos.ToVec3d();
		foreach (StartEnd beam in sbData.Beams)
		{
			bool num2;
			if (!((beam.Start - beam.End).Length() * 1.5 < Math.Abs(beam.End.Y - beam.Start.Y)))
			{
				if (!isBeamStableAt(beam.Start))
				{
					continue;
				}
				num2 = isBeamStableAt(beam.End);
			}
			else
			{
				if (isBeamStableAt(beam.Start))
				{
					goto IL_00de;
				}
				num2 = isBeamStableAt(beam.End);
			}
			if (!num2)
			{
				continue;
			}
			goto IL_00de;
			IL_00de:
			double num3 = DistanceToLine(point, beam.Start, beam.End);
			if (num3 < num)
			{
				num = num3;
			}
		}
		beamstartend = startEnd;
		return num;
	}

	private static double DistanceToLine(Vec3d point, Vec3d start, Vec3d end)
	{
		Vec3d vec3d = end - start;
		double num = vec3d.Length();
		double num2 = 0.0;
		if (num != 0.0)
		{
			num2 = Math.Clamp((point - start).Dot(vec3d) / (num * num), 0.0, 1.0);
		}
		return point.DistanceTo(start + vec3d * num2);
	}

	private bool isBeamStableAt(Vec3d start)
	{
		if (BlockBehaviorUnstableRock.getVerticalSupportStrength(api.World, start.AsBlockPos) <= 0 && BlockBehaviorUnstableRock.getVerticalSupportStrength(api.World, start.Add(-0.0625, 0.0, -0.0625).AsBlockPos) <= 0)
		{
			return BlockBehaviorUnstableRock.getVerticalSupportStrength(api.World, start.Add(0.0625, 0.0, 0.0625).AsBlockPos) > 0;
		}
		return true;
	}

	public SupportBeamsData GetSbData(BlockPos pos)
	{
		return GetSbData(pos.X / 32, pos.Y / 32, pos.Z / 32);
	}

	public SupportBeamsData GetSbData(int chunkx, int chunky, int chunkz)
	{
		IWorldChunk chunk = api.World.BlockAccessor.GetChunk(chunkx, chunky, chunkz);
		if (chunk == null)
		{
			return null;
		}
		object value;
		SupportBeamsData supportBeamsData = (SupportBeamsData)(chunk.LiveModData.TryGetValue("supportbeams", out value) ? ((SupportBeamsData)value) : (chunk.LiveModData["supportbeams"] = chunk.GetModdata<SupportBeamsData>("supportbeams")));
		if (supportBeamsData == null)
		{
			supportBeamsData = (SupportBeamsData)(chunk.LiveModData["supportbeams"] = new SupportBeamsData());
		}
		return supportBeamsData;
	}
}
