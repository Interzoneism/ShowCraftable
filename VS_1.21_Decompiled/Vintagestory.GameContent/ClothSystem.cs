using System;
using System.Collections.Generic;
using System.Linq;
using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

[ProtoContract]
public class ClothSystem
{
	[ProtoMember(1)]
	public int ClothId;

	[ProtoMember(2)]
	private EnumClothType clothType;

	[ProtoMember(3)]
	private List<PointList> Points2d = new List<PointList>();

	[ProtoMember(4)]
	private List<ClothConstraint> Constraints = new List<ClothConstraint>();

	public static float Resolution = 2f;

	public float StretchWarn = 0.6f;

	public float StretchRip = 0.75f;

	public bool LineDebug;

	public bool boyant;

	protected ICoreClientAPI capi;

	public ICoreAPI api;

	public Vec3d windSpeed = new Vec3d();

	public ParticlePhysics pp;

	protected NormalizedSimplexNoise noiseGen;

	protected float[] tmpMat = new float[16];

	protected Vec3f distToCam = new Vec3f();

	protected AssetLocation ropeSectionModel;

	protected MeshData debugUpdateMesh;

	protected MeshRef debugMeshRef;

	public float secondsOverStretched;

	private double minLen = 1.5;

	private double maxLen = 10.0;

	private Matrixf mat = new Matrixf();

	private float accum;

	[ProtoMember(5)]
	public bool Active { get; set; }

	public bool PinnedAnywhere
	{
		get
		{
			foreach (PointList item in Points2d)
			{
				foreach (ClothPoint point in item.Points)
				{
					if (point.Pinned)
					{
						return true;
					}
				}
			}
			return false;
		}
	}

	public double MaxExtension
	{
		get
		{
			if (Constraints.Count != 0)
			{
				return Constraints.Max((ClothConstraint c) => c.Extension);
			}
			return 0.0;
		}
	}

	public Vec3d CenterPosition
	{
		get
		{
			Vec3d vec3d = new Vec3d();
			int num = 0;
			foreach (PointList item in Points2d)
			{
				foreach (ClothPoint point in item.Points)
				{
					_ = point;
					num++;
				}
			}
			foreach (PointList item2 in Points2d)
			{
				foreach (ClothPoint point2 in item2.Points)
				{
					vec3d.Add(point2.Pos.X / (double)num, point2.Pos.Y / (double)num, point2.Pos.Z / (double)num);
				}
			}
			return vec3d;
		}
	}

	public ClothPoint FirstPoint => Points2d[0].Points[0];

	public ClothPoint LastPoint
	{
		get
		{
			List<ClothPoint> points = Points2d[Points2d.Count - 1].Points;
			return points[points.Count - 1];
		}
	}

	public ClothPoint[] Ends => new ClothPoint[2] { FirstPoint, LastPoint };

	public static ClothSystem CreateCloth(ICoreAPI api, ClothManager cm, Vec3d start, Vec3d end)
	{
		return new ClothSystem(api, cm, start, end, EnumClothType.Cloth);
	}

	public static ClothSystem CreateRope(ICoreAPI api, ClothManager cm, Vec3d start, Vec3d end, AssetLocation clothSectionModel)
	{
		return new ClothSystem(api, cm, start, end, EnumClothType.Rope, clothSectionModel);
	}

	private ClothSystem()
	{
	}

	public bool ChangeRopeLength(double len)
	{
		PointList pointList = Points2d[0];
		double num = (float)pointList.Points.Count / Resolution;
		bool flag = len > 0.0;
		if (flag && len + num > maxLen)
		{
			return false;
		}
		if (!flag && len + num < minLen)
		{
			return false;
		}
		int num2 = pointList.Points.Max((ClothPoint clothPoint2) => clothPoint2.PointIndex) + 1;
		ClothPoint firstPoint = FirstPoint;
		Entity pinnedToEntity = firstPoint.PinnedToEntity;
		BlockPos pinnedToBlockPos = firstPoint.PinnedToBlockPos;
		Vec3f pinnedToOffset = firstPoint.pinnedToOffset;
		firstPoint.UnPin();
		float num3 = 1f / Resolution;
		int num4 = Math.Abs((int)(len * (double)Resolution));
		if (flag)
		{
			for (int num5 = 0; num5 <= num4; num5++)
			{
				pointList.Points.Insert(0, new ClothPoint(this, num2++, firstPoint.Pos.X + (double)(num3 * (float)(num5 + 1)), firstPoint.Pos.Y, firstPoint.Pos.Z));
				ClothPoint p = pointList.Points[0];
				ClothPoint p2 = pointList.Points[1];
				ClothConstraint item = new ClothConstraint(p, p2);
				Constraints.Add(item);
			}
		}
		else
		{
			for (int num6 = 0; num6 <= num4; num6++)
			{
				ClothPoint clothPoint = pointList.Points[0];
				pointList.Points.RemoveAt(0);
				for (int num7 = 0; num7 < Constraints.Count; num7++)
				{
					ClothConstraint clothConstraint = Constraints[num7];
					if (clothConstraint.Point1 == clothPoint || clothConstraint.Point2 == clothPoint)
					{
						Constraints.RemoveAt(num7);
						num7--;
					}
				}
			}
		}
		if (pinnedToEntity != null)
		{
			FirstPoint.PinTo(pinnedToEntity, pinnedToOffset);
		}
		if (pinnedToBlockPos != null)
		{
			FirstPoint.PinTo(pinnedToBlockPos, pinnedToOffset);
		}
		genDebugMesh();
		return true;
	}

	private ClothSystem(ICoreAPI api, ClothManager cm, Vec3d start, Vec3d end, EnumClothType clothType, AssetLocation ropeSectionModel = null)
	{
		this.clothType = clothType;
		this.ropeSectionModel = ropeSectionModel;
		Init(api, cm);
		_ = 1f / Resolution;
		Vec3d vec3d = end - start;
		if (clothType == EnumClothType.Rope)
		{
			double num = vec3d.Length();
			PointList pointList = new PointList();
			Points2d.Add(pointList);
			int num2 = (int)(num * (double)Resolution);
			for (int i = 0; i <= num2; i++)
			{
				float num3 = (float)i / (float)num2;
				pointList.Points.Add(new ClothPoint(this, i, start.X + vec3d.X * (double)num3, start.Y + vec3d.Y * (double)num3, start.Z + vec3d.Z * (double)num3));
				if (i > 0)
				{
					ClothPoint p = pointList.Points[i - 1];
					ClothPoint p2 = pointList.Points[i];
					ClothConstraint item = new ClothConstraint(p, p2);
					Constraints.Add(item);
				}
			}
		}
		if (clothType != EnumClothType.Cloth)
		{
			return;
		}
		double num4 = (end - start).HorLength();
		double num5 = Math.Abs(end.Y - start.Y);
		int num6 = (int)(num4 * (double)Resolution);
		int num7 = (int)(num5 * (double)Resolution);
		int num8 = 0;
		for (int j = 0; j < num6; j++)
		{
			Points2d.Add(new PointList());
			for (int k = 0; k < num7; k++)
			{
				double num9 = (double)j / num4;
				double num10 = (double)k / num5;
				Points2d[j].Points.Add(new ClothPoint(this, num8++, start.X + vec3d.X * num9, start.Y + vec3d.Y * num10, start.Z + vec3d.Z * num9));
				if (j > 0)
				{
					ClothPoint p3 = Points2d[j - 1].Points[k];
					ClothPoint p4 = Points2d[j].Points[k];
					ClothConstraint item2 = new ClothConstraint(p3, p4);
					Constraints.Add(item2);
				}
				if (k > 0)
				{
					ClothPoint p5 = Points2d[j].Points[k - 1];
					ClothPoint p6 = Points2d[j].Points[k];
					ClothConstraint item3 = new ClothConstraint(p5, p6);
					Constraints.Add(item3);
				}
			}
		}
	}

	public void genDebugMesh()
	{
		if (capi != null)
		{
			debugMeshRef?.Dispose();
			debugUpdateMesh = new MeshData(20, 15, withNormals: false, withUv: false);
			int num = 0;
			for (int i = 0; i < Constraints.Count; i++)
			{
				_ = Constraints[i];
				int color = ((i % 2 > 0) ? (-1) : ColorUtil.BlackArgb);
				debugUpdateMesh.AddVertexSkipTex(0f, 0f, 0f, color);
				debugUpdateMesh.AddVertexSkipTex(0f, 0f, 0f, color);
				debugUpdateMesh.AddIndex(num++);
				debugUpdateMesh.AddIndex(num++);
			}
			debugUpdateMesh.mode = EnumDrawMode.Lines;
			debugMeshRef = capi.Render.UploadMesh(debugUpdateMesh);
			debugUpdateMesh.Indices = null;
			debugUpdateMesh.Rgba = null;
		}
	}

	public void Init(ICoreAPI api, ClothManager cm)
	{
		this.api = api;
		capi = api as ICoreClientAPI;
		pp = cm.partPhysics;
		noiseGen = NormalizedSimplexNoise.FromDefaultOctaves(4, 100.0, 0.9, api.World.Seed + CenterPosition.GetHashCode());
	}

	public void WalkPoints(Action<ClothPoint> onPoint)
	{
		foreach (PointList item in Points2d)
		{
			foreach (ClothPoint point in item.Points)
			{
				onPoint(point);
			}
		}
	}

	public int UpdateMesh(MeshData updateMesh, float dt)
	{
		CustomMeshDataPartFloat customFloats = updateMesh.CustomFloats;
		Vec3d cameraPos = capi.World.Player.Entity.CameraPos;
		int count = customFloats.Count;
		Vec4f vec4f = new Vec4f();
		if (Constraints.Count > 0)
		{
			vec4f = api.World.BlockAccessor.GetLightRGBs(Constraints[Constraints.Count / 2].Point1.Pos.AsBlockPos);
		}
		for (int i = 0; i < Constraints.Count; i++)
		{
			ClothConstraint clothConstraint = Constraints[i];
			Vec3d pos = clothConstraint.Point1.Pos;
			Vec3d pos2 = clothConstraint.Point2.Pos;
			double num = pos.X - pos2.X;
			double num2 = pos.Y - pos2.Y;
			double num3 = pos.Z - pos2.Z;
			float rad = (float)Math.Atan2(num, num3) + (float)Math.PI / 2f;
			float rad2 = (float)Math.Atan2(Math.Sqrt(num3 * num3 + num * num), num2) + (float)Math.PI / 2f;
			double num4 = pos.X + (pos.X - pos2.X) / 2.0;
			double num5 = pos.Y + (pos.Y - pos2.Y) / 2.0;
			double num6 = pos.Z + (pos.Z - pos2.Z) / 2.0;
			distToCam.Set((float)(num4 - cameraPos.X), (float)(num5 - cameraPos.Y), (float)(num6 - cameraPos.Z));
			Mat4f.Identity(tmpMat);
			Mat4f.Translate(tmpMat, tmpMat, 0f, 1f / 32f, 0f);
			Mat4f.Translate(tmpMat, tmpMat, distToCam.X, distToCam.Y, distToCam.Z);
			Mat4f.RotateY(tmpMat, tmpMat, rad);
			Mat4f.RotateZ(tmpMat, tmpMat, rad2);
			float rad3 = (float)i / 5f;
			Mat4f.RotateX(tmpMat, tmpMat, rad3);
			float num7 = GameMath.Sqrt(num * num + num2 * num2 + num3 * num3);
			Mat4f.Scale(tmpMat, tmpMat, new float[3] { num7, 1f, 1f });
			Mat4f.Translate(tmpMat, tmpMat, -1.5f, -1f / 32f, -0.5f);
			int num8 = count + i * 20;
			customFloats.Values[num8++] = vec4f.R;
			customFloats.Values[num8++] = vec4f.G;
			customFloats.Values[num8++] = vec4f.B;
			customFloats.Values[num8++] = vec4f.A;
			for (int j = 0; j < 16; j++)
			{
				customFloats.Values[num8 + j] = tmpMat[j];
			}
		}
		return Constraints.Count;
	}

	public void setRenderCenterPos()
	{
		for (int i = 0; i < Constraints.Count; i++)
		{
			ClothConstraint clothConstraint = Constraints[i];
			Vec3d pos = clothConstraint.Point1.Pos;
			Vec3d pos2 = clothConstraint.Point2.Pos;
			double x = pos.X + (pos.X - pos2.X) / 2.0;
			double y = pos.Y + (pos.Y - pos2.Y) / 2.0;
			double z = pos.Z + (pos.Z - pos2.Z) / 2.0;
			clothConstraint.renderCenterPos.X = x;
			clothConstraint.renderCenterPos.Y = y;
			clothConstraint.renderCenterPos.Z = z;
		}
	}

	public void CustomRender(float dt)
	{
		if (LineDebug && capi != null)
		{
			if (debugMeshRef == null)
			{
				genDebugMesh();
			}
			BlockPos asBlockPos = CenterPosition.AsBlockPos;
			for (int i = 0; i < Constraints.Count; i++)
			{
				ClothConstraint clothConstraint = Constraints[i];
				Vec3d pos = clothConstraint.Point1.Pos;
				Vec3d pos2 = clothConstraint.Point2.Pos;
				debugUpdateMesh.xyz[i * 6] = (float)(pos.X - (double)asBlockPos.X);
				debugUpdateMesh.xyz[i * 6 + 1] = (float)(pos.Y - (double)asBlockPos.Y) + 0.005f;
				debugUpdateMesh.xyz[i * 6 + 2] = (float)(pos.Z - (double)asBlockPos.Z);
				debugUpdateMesh.xyz[i * 6 + 3] = (float)(pos2.X - (double)asBlockPos.X);
				debugUpdateMesh.xyz[i * 6 + 4] = (float)(pos2.Y - (double)asBlockPos.Y) + 0.005f;
				debugUpdateMesh.xyz[i * 6 + 5] = (float)(pos2.Z - (double)asBlockPos.Z);
			}
			capi.Render.UpdateMesh(debugMeshRef, debugUpdateMesh);
			IShaderProgram program = capi.Shader.GetProgram(23);
			program.Use();
			capi.Render.LineWidth = 6f;
			capi.Render.BindTexture2d(0);
			capi.Render.GLDisableDepthTest();
			Vec3d cameraPos = capi.World.Player.Entity.CameraPos;
			mat.Set(capi.Render.CameraMatrixOrigin);
			mat.Translate((float)((double)asBlockPos.X - cameraPos.X), (float)((double)asBlockPos.Y - cameraPos.Y), (float)((double)asBlockPos.Z - cameraPos.Z));
			program.UniformMatrix("projectionMatrix", capi.Render.CurrentProjectionMatrix);
			program.UniformMatrix("modelViewMatrix", mat.Values);
			capi.Render.RenderMesh(debugMeshRef);
			program.Stop();
			capi.Render.GLEnableDepthTest();
		}
	}

	public void updateFixedStep(float dt)
	{
		accum += dt;
		if (accum > 1f)
		{
			accum = 0.25f;
		}
		float physicsTickTime = pp.PhysicsTickTime;
		while (accum >= physicsTickTime)
		{
			accum -= physicsTickTime;
			tickNow(physicsTickTime);
		}
	}

	private void tickNow(float pdt)
	{
		for (int num = Constraints.Count - 1; num >= 0; num--)
		{
			Constraints[num].satisfy(pdt);
		}
		for (int num2 = Points2d.Count - 1; num2 >= 0; num2--)
		{
			for (int num3 = Points2d[num2].Points.Count - 1; num3 >= 0; num3--)
			{
				Points2d[num2].Points[num3].update(pdt, api.World);
			}
		}
	}

	public void slowTick3s()
	{
		if (!double.IsNaN(CenterPosition.X))
		{
			windSpeed = api.World.BlockAccessor.GetWindSpeedAt(CenterPosition) * (0.2 + noiseGen.Noise(0.0, api.World.Calendar.TotalHours * 50.0 % 2000.0) * 0.8);
		}
	}

	public void restoreReferences()
	{
		if (!Active)
		{
			return;
		}
		Dictionary<int, ClothPoint> pointsByIndex = new Dictionary<int, ClothPoint>();
		WalkPoints(delegate(ClothPoint p)
		{
			pointsByIndex[p.PointIndex] = p;
			p.restoreReferences(this, api.World);
		});
		foreach (ClothConstraint constraint in Constraints)
		{
			constraint.RestorePoints(pointsByIndex);
		}
	}

	public void updateActiveState(EnumActiveStateChange stateChange)
	{
		if ((!Active || stateChange != EnumActiveStateChange.RegionNowLoaded) && (Active || stateChange != EnumActiveStateChange.RegionNowUnloaded))
		{
			bool active = Active;
			Active = true;
			WalkPoints(delegate(ClothPoint p)
			{
				Active &= api.World.BlockAccessor.GetChunkAtBlockPos((int)p.Pos.X, (int)p.Pos.Y, (int)p.Pos.Z) != null;
			});
			if (!active && Active)
			{
				restoreReferences();
			}
		}
	}

	public void CollectDirtyPoints(List<ClothPointPacket> packets)
	{
		for (int i = 0; i < Points2d.Count; i++)
		{
			for (int j = 0; j < Points2d[i].Points.Count; j++)
			{
				ClothPoint clothPoint = Points2d[i].Points[j];
				if (clothPoint.Dirty)
				{
					packets.Add(new ClothPointPacket
					{
						ClothId = ClothId,
						PointX = i,
						PointY = j,
						Point = clothPoint
					});
					clothPoint.Dirty = false;
				}
			}
		}
	}

	public void updatePoint(ClothPointPacket msg)
	{
		if (msg.PointX >= Points2d.Count)
		{
			api.Logger.Error($"ClothSystem: {ClothId} got invalid Points2d update index for {msg.PointX}/{Points2d.Count}");
		}
		else if (msg.PointY >= Points2d[msg.PointX].Points.Count)
		{
			api.Logger.Error($"ClothSystem: {ClothId} got invalid Points2d[{msg.PointX}] update index for {msg.PointY}/{Points2d[msg.PointX].Points.Count}");
		}
		else
		{
			Points2d[msg.PointX].Points[msg.PointY].updateFromPoint(msg.Point, api.World);
		}
	}

	public void OnPinnnedEntityLoaded(Entity entity)
	{
		if (FirstPoint.pinnedToEntityId == entity.EntityId)
		{
			FirstPoint.restoreReferences(entity);
		}
		if (LastPoint.pinnedToEntityId == entity.EntityId)
		{
			LastPoint.restoreReferences(entity);
		}
	}
}
