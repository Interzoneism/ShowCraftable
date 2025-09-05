using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace Vintagestory.GameContent;

public class EntityBehaviorSelectionBoxes : EntityBehavior, IRenderer, IDisposable
{
	private ICoreClientAPI capi;

	private Matrixf mvmat = new Matrixf();

	private bool debug;

	private bool rendererRegistered;

	public AttachmentPointAndPose[] selectionBoxes = Array.Empty<AttachmentPointAndPose>();

	private string[] selectionBoxCodes;

	public WireframeCube BoxWireframe;

	private float accum;

	private static Cuboidd standardbox = new Cuboidd(0.0, 0.0, 0.0, 1.0, 1.0, 1.0);

	private Vec3d hitPositionOBBSpace;

	private Vec3d hitPositionAABBSpace;

	public double RenderOrder => 1.0;

	public int RenderRange => 24;

	public EntityBehaviorSelectionBoxes(Entity entity)
		: base(entity)
	{
	}

	public void Dispose()
	{
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		base.Initialize(properties, attributes);
		capi = entity.Api as ICoreClientAPI;
		if (capi != null)
		{
			debug = capi.Settings.Bool["debugEntitySelectionBoxes"];
		}
		setupWireframe();
		entity.trickleDownRayIntersects = true;
		entity.requirePosesOnServer = true;
		selectionBoxCodes = attributes["selectionBoxes"].AsArray(Array.Empty<string>());
		if (selectionBoxCodes.Length == 0)
		{
			capi.World.Logger.Warning("EntityBehaviorSelectionBoxes, missing selectionBoxes property. Will ignore.");
		}
	}

	public override void OnTesselated()
	{
		loadSelectionBoxes();
	}

	private void loadSelectionBoxes()
	{
		List<AttachmentPointAndPose> list = new List<AttachmentPointAndPose>();
		string[] array = selectionBoxCodes;
		foreach (string code in array)
		{
			AttachmentPointAndPose attachmentPointAndPose = entity.AnimManager?.Animator?.GetAttachmentPointPose(code);
			if (attachmentPointAndPose != null)
			{
				AttachmentPointAndPose item = new AttachmentPointAndPose
				{
					AnimModelMatrix = attachmentPointAndPose.AnimModelMatrix,
					AttachPoint = attachmentPointAndPose.AttachPoint,
					CachedPose = attachmentPointAndPose.CachedPose
				};
				list.Add(item);
			}
		}
		selectionBoxes = list.ToArray();
	}

	public override void OnGameTick(float deltaTime)
	{
		if (capi != null && (accum += deltaTime) >= 1f)
		{
			accum = 0f;
			debug = capi.Settings.Bool["debugEntitySelectionBoxes"];
			setupWireframe();
		}
		base.OnGameTick(deltaTime);
	}

	private void setupWireframe()
	{
		if (!rendererRegistered)
		{
			if (capi != null)
			{
				capi.Event.RegisterRenderer(this, EnumRenderStage.AfterFinalComposition, "selectionboxesbhdebug");
				BoxWireframe = WireframeCube.CreateUnitCube(capi, -1);
			}
			rendererRegistered = true;
		}
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		if (capi.HideGuis)
		{
			return;
		}
		int hitIndex = getHitIndex();
		if (hitIndex < 0 && capi.World.Player.CurrentEntitySelection?.Entity != entity)
		{
			return;
		}
		EntityPlayer eplr = capi.World.Player.Entity;
		if (debug)
		{
			for (int i = 0; i < selectionBoxes.Length; i++)
			{
				if (hitIndex != i)
				{
					Render(eplr, i, ColorUtil.WhiteArgbVec);
				}
			}
			if (hitIndex >= 0)
			{
				Render(eplr, hitIndex, new Vec4f(1f, 0f, 0f, 1f));
			}
		}
		else if (hitIndex >= 0)
		{
			Render(eplr, hitIndex, new Vec4f(0f, 0f, 0f, 0.5f));
		}
	}

	private void Render(EntityPlayer eplr, int i, Vec4f color)
	{
		AttachmentPointAndPose apap = selectionBoxes[i];
		EntityPos pos = entity.Pos;
		mvmat.Identity();
		mvmat.Set(capi.Render.CameraMatrixOrigin);
		IMountable mountable = entity.GetInterface<IMountable>();
		IMountableSeat seatOfMountedEntity;
		if (mountable != null && (seatOfMountedEntity = mountable.GetSeatOfMountedEntity(eplr)) != null)
		{
			Vec3d vec3d = seatOfMountedEntity.SeatPosition.XYZ - seatOfMountedEntity.MountSupplier.Position.XYZ;
			mvmat.Translate(0f - (float)vec3d.X, 0f - (float)vec3d.Y, 0f - (float)vec3d.Z);
		}
		else
		{
			mvmat.Translate(pos.X - eplr.CameraPos.X, pos.InternalY - eplr.CameraPos.Y, pos.Z - eplr.CameraPos.Z);
		}
		applyBoxTransform(mvmat, apap);
		BoxWireframe.Render(capi, mvmat, 1.6f, color);
	}

	private int getHitIndex()
	{
		EntityPlayer entityPlayer = capi.World.Player.Entity;
		Ray pickingray = Ray.FromAngles(entityPlayer.SidedPos.XYZ + entityPlayer.LocalEyePos - entity.SidedPos.XYZ, entityPlayer.SidedPos.Pitch, entityPlayer.SidedPos.Yaw, capi.World.Player.WorldData.PickingRange);
		return getHitIndex(pickingray);
	}

	private void applyBoxTransform(Matrixf mvmat, AttachmentPointAndPose apap)
	{
		EntityShapeRenderer entityShapeRenderer = entity.Properties.Client.Renderer as EntityShapeRenderer;
		mvmat.RotateY((float)Math.PI / 2f + entity.SidedPos.Yaw);
		if (entityShapeRenderer != null)
		{
			mvmat.Translate(0f, entity.SelectionBox.Y2 / 2f, 0f);
			mvmat.RotateX(entityShapeRenderer.xangle);
			mvmat.RotateY(entityShapeRenderer.yangle);
			mvmat.RotateZ(entityShapeRenderer.zangle);
			mvmat.Translate(0f, (0f - entity.SelectionBox.Y2) / 2f, 0f);
		}
		mvmat.Translate(0f, 0.7f, 0f);
		mvmat.RotateX(entityShapeRenderer?.nowSwivelRad ?? 0f);
		mvmat.Translate(0f, -0.7f, 0f);
		float size = entity.Properties.Client.Size;
		mvmat.Scale(size, size, size);
		mvmat.Translate(-0.5f, 0f, -0.5f);
		mvmat.Mul(apap.AnimModelMatrix);
		ShapeElement parentElement = apap.AttachPoint.ParentElement;
		float x = (float)(parentElement.To[0] - parentElement.From[0]) / 16f;
		float y = (float)(parentElement.To[1] - parentElement.From[1]) / 16f;
		float z = (float)(parentElement.To[2] - parentElement.From[2]) / 16f;
		mvmat.Scale(x, y, z);
	}

	public override bool IntersectsRay(Ray ray, AABBIntersectionTest intersectionTester, out double intersectionDistance, ref int selectionBoxIndex, ref EnumHandling handled)
	{
		Ray pickingray = new Ray(ray.origin - entity.SidedPos.XYZ, ray.dir);
		int hitIndex = getHitIndex(pickingray);
		if (hitIndex >= 0)
		{
			intersectionDistance = hitPositionAABBSpace.Length();
			intersectionTester.hitPosition = hitPositionAABBSpace.AddCopy(entity.SidedPos.XYZ);
			selectionBoxIndex = 1 + hitIndex;
			handled = EnumHandling.PreventDefault;
			return true;
		}
		intersectionDistance = double.MaxValue;
		return false;
	}

	private int getHitIndex(Ray pickingray)
	{
		int num = -1;
		double num2 = double.MaxValue;
		double num3 = pickingray.Length * pickingray.Length;
		for (int i = 0; i < selectionBoxes.Length; i++)
		{
			AttachmentPointAndPose apap = selectionBoxes[i];
			mvmat.Identity();
			applyBoxTransform(mvmat, apap);
			Matrixf matrixf = mvmat.Clone().Invert();
			Vec4d vec4d = matrixf.TransformVector(new Vec4d(pickingray.origin.X, pickingray.origin.Y, pickingray.origin.Z, 1.0));
			Vec4d vec4d2 = matrixf.TransformVector(new Vec4d(pickingray.dir.X, pickingray.dir.Y, pickingray.dir.Z, 0.0));
			Ray r = new Ray(vec4d.XYZ, vec4d2.XYZ);
			if (Testintersection(standardbox, r))
			{
				Vec4d vec4d3 = mvmat.TransformVector(new Vec4d(hitPositionOBBSpace.X, hitPositionOBBSpace.Y, hitPositionOBBSpace.Z, 1.0));
				double num4 = (vec4d3.XYZ - pickingray.origin).LengthSq();
				if ((num < 0 || !(num2 < num4)) && !(num3 < num4))
				{
					hitPositionAABBSpace = vec4d3.XYZ;
					num2 = num4;
					num = i;
				}
			}
		}
		return num;
	}

	public Vec3d GetCenterPosOfBox(int selectionBoxIndex)
	{
		if (selectionBoxIndex >= selectionBoxes.Length)
		{
			return null;
		}
		AttachmentPointAndPose attachmentPointAndPose = selectionBoxes[selectionBoxIndex];
		mvmat.Identity();
		applyBoxTransform(mvmat, attachmentPointAndPose);
		ShapeElement parentElement = attachmentPointAndPose.AttachPoint.ParentElement;
		Vec4d vec = new Vec4d((parentElement.To[0] - parentElement.From[0]) / 2.0 / 16.0, (parentElement.To[1] - parentElement.From[1]) / 2.0 / 16.0, (parentElement.To[2] - parentElement.From[2]) / 2.0 / 16.0, 1.0);
		Vec3d a = entity.Pos.XYZ;
		EntityPlayer entityPlayer = capi.World.Player.Entity;
		if (entityPlayer?.MountedOn?.Entity == entity)
		{
			Vec3d vec3d = entityPlayer.MountedOn.SeatPosition.XYZ - entity.Pos.XYZ;
			a = new Vec3d(entityPlayer.CameraPos.X - vec3d.X, entityPlayer.CameraPos.Y - vec3d.Y, entityPlayer.CameraPos.Z - vec3d.Z);
		}
		return mvmat.TransformVector(vec).XYZ.Add(a);
	}

	public bool Testintersection(Cuboidd b, Ray r)
	{
		double num = b.X2 - b.X1;
		double num2 = b.Y2 - b.Y1;
		double num3 = b.Z2 - b.Z1;
		for (int i = 0; i < 6; i++)
		{
			BlockFacing blockFacing = BlockFacing.ALLFACES[i];
			Vec3i normali = blockFacing.Normali;
			double num4 = (double)normali.X * r.dir.X + (double)normali.Y * r.dir.Y + (double)normali.Z * r.dir.Z;
			if (!(num4 < -1E-05))
			{
				continue;
			}
			Vec3d vec3d = blockFacing.PlaneCenter.ToVec3d().Mul(num, num2, num3).Add(b.X1, b.Y1, b.Z1);
			Vec3d vec3d2 = Vec3d.Sub(vec3d, r.origin);
			double num5 = (vec3d2.X * (double)normali.X + vec3d2.Y * (double)normali.Y + vec3d2.Z * (double)normali.Z) / num4;
			if (num5 >= 0.0)
			{
				hitPositionOBBSpace = new Vec3d(r.origin.X + r.dir.X * num5, r.origin.Y + r.dir.Y * num5, r.origin.Z + r.dir.Z * num5);
				Vec3d vec3d3 = Vec3d.Sub(hitPositionOBBSpace, vec3d);
				if (Math.Abs(vec3d3.X) <= num / 2.0 && Math.Abs(vec3d3.Y) <= num2 / 2.0 && Math.Abs(vec3d3.Z) <= num3 / 2.0)
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool IsAPCode(EntitySelection es, string apcode)
	{
		EntityBehaviorSelectionBoxes behavior = entity.GetBehavior<EntityBehaviorSelectionBoxes>();
		if (behavior != null)
		{
			int num = ((es != null) ? (es.SelectionBoxIndex - 1) : (-1));
			AttachmentPointAndPose[] array = behavior.selectionBoxes;
			if (num > 0 && array.Length >= num)
			{
				return array[num].AttachPoint.Code == apcode;
			}
		}
		return false;
	}

	public override void GetInfoText(StringBuilder infotext)
	{
		int hitIndex = getHitIndex();
		if (hitIndex >= 0)
		{
			if (capi.Settings.Bool["extendedDebugInfo"])
			{
				infotext.AppendLine("<font color=\"#bbbbbb\">looking at AP " + selectionBoxes[hitIndex].AttachPoint.Code + "</font>");
			}
			infotext.AppendLine(Lang.GetMatching("creature-" + entity.Code.ToShortString() + "-selectionbox-" + selectionBoxes[hitIndex].AttachPoint.Code));
		}
		base.GetInfoText(infotext);
	}

	public override void OnEntityDespawn(EntityDespawnData despawn)
	{
		base.OnEntityDespawn(despawn);
		capi?.Event.UnregisterRenderer(this, EnumRenderStage.AfterFinalComposition);
		BoxWireframe?.Dispose();
	}

	public override string PropertyName()
	{
		return "selectionboxes";
	}
}
