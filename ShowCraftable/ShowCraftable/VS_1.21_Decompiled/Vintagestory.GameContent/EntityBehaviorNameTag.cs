using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityBehaviorNameTag : EntityBehavior, IRenderer, IDisposable
{
	protected LoadedTexture nameTagTexture;

	protected bool showNameTagOnlyWhenTargeted;

	protected NameTagRendererDelegate nameTagRenderHandler;

	private ICoreClientAPI capi;

	protected int renderRange = 999;

	private IPlayer player;

	public string DisplayName
	{
		get
		{
			if (capi != null && TriggeredNameReveal && !IsNameRevealedFor(capi.World.Player.PlayerUID))
			{
				return UnrevealedDisplayName;
			}
			return entity.WatchedAttributes.GetTreeAttribute("nametag")?.GetString("name");
		}
	}

	public string UnrevealedDisplayName { get; set; }

	public bool ShowOnlyWhenTargeted
	{
		get
		{
			return entity.WatchedAttributes.GetTreeAttribute("nametag")?.GetBool("showtagonlywhentargeted") ?? false;
		}
		set
		{
			entity.WatchedAttributes.GetTreeAttribute("nametag")?.SetBool("showtagonlywhentargeted", value);
		}
	}

	public bool TriggeredNameReveal { get; set; }

	public int RenderRange
	{
		get
		{
			return entity.WatchedAttributes.GetTreeAttribute("nametag").GetInt("renderRange");
		}
		set
		{
			entity.WatchedAttributes.GetTreeAttribute("nametag")?.SetInt("renderRange", value);
		}
	}

	public double RenderOrder => 1.0;

	protected bool IsSelf => entity.EntityId == capi.World.Player.Entity.EntityId;

	public bool IsNameRevealedFor(string playeruid)
	{
		ITreeAttribute treeAttribute = entity.WatchedAttributes.GetTreeAttribute("nametag");
		if (treeAttribute == null)
		{
			return false;
		}
		return treeAttribute.GetTreeAttribute("nameRevealedFor")?.HasAttribute(playeruid) == true;
	}

	public void SetNameRevealedFor(string playeruid)
	{
		ITreeAttribute treeAttribute = entity.WatchedAttributes.GetTreeAttribute("nametag");
		if (treeAttribute == null)
		{
			treeAttribute = (ITreeAttribute)(entity.WatchedAttributes["nametag"] = new TreeAttribute());
		}
		ITreeAttribute treeAttribute2 = treeAttribute?.GetTreeAttribute("nameRevealedFor");
		if (treeAttribute2 == null)
		{
			treeAttribute2 = (ITreeAttribute)(treeAttribute["nameRevealedFor"] = new TreeAttribute());
		}
		treeAttribute2.SetBool(playeruid, value: true);
		OnNameChanged();
	}

	public EntityBehaviorNameTag(Entity entity)
		: base(entity)
	{
		ITreeAttribute treeAttribute = entity.WatchedAttributes.GetTreeAttribute("nametag");
		if (treeAttribute == null)
		{
			entity.WatchedAttributes.SetAttribute("nametag", treeAttribute = new TreeAttribute());
			treeAttribute.SetString("name", "");
			treeAttribute.SetInt("showtagonlywhentargeted", 0);
			treeAttribute.SetInt("renderRange", 999);
			entity.WatchedAttributes.MarkPathDirty("nametag");
		}
	}

	public override void Initialize(EntityProperties entityType, JsonObject attributes)
	{
		base.Initialize(entityType, attributes);
		if ((DisplayName == null || DisplayName.Length == 0) && attributes["selectFromRandomName"].Exists)
		{
			string[] array = attributes["selectFromRandomName"].AsArray<string>();
			SetName(array[entity.World.Rand.Next(array.Length)]);
		}
		TriggeredNameReveal = attributes["triggeredNameReveal"].AsBool();
		RenderRange = attributes["renderRange"].AsInt(999);
		ShowOnlyWhenTargeted = attributes["showtagonlywhentargeted"].AsBool();
		UnrevealedDisplayName = Lang.Get(attributes["unrevealedDisplayName"].AsString("nametag-default-unrevealedname"));
		entity.WatchedAttributes.OnModified.Add(new TreeModifiedListener
		{
			path = "nametag",
			listener = OnNameChanged
		});
		OnNameChanged();
		capi = entity.World.Api as ICoreClientAPI;
		if (capi != null)
		{
			capi.Event.RegisterRenderer(this, EnumRenderStage.Ortho, "nametag");
		}
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		if (IsSelf && capi.Render.CameraType == EnumCameraMode.FirstPerson)
		{
			return;
		}
		if (nameTagRenderHandler == null || (entity is EntityPlayer && player == null))
		{
			player = (entity as EntityPlayer)?.Player;
			if (player != null || !(entity is EntityPlayer))
			{
				nameTagRenderHandler = capi.ModLoader.GetModSystem<EntityNameTagRendererRegistry>().GetNameTagRenderer(entity);
				OnNameChanged();
			}
		}
		if ((player != null && player.WorldData.CurrentGameMode == EnumGameMode.Spectator) || nameTagTexture == null)
		{
			return;
		}
		IPlayer obj = player;
		if (obj != null && obj.Entity?.ServerControls.Sneak == true)
		{
			return;
		}
		IRenderAPI render = capi.Render;
		EntityPlayer entityPlayer = capi.World.Player.Entity;
		if (!(entity.Properties.Client.Renderer is EntityShapeRenderer entityShapeRenderer))
		{
			return;
		}
		Vec3d vec3d = MatrixToolsd.Project(entityShapeRenderer.getAboveHeadPosition(entityPlayer), render.PerspectiveProjectionMat, render.PerspectiveViewMat, render.FrameWidth, render.FrameHeight);
		if (!(vec3d.Z < 0.0))
		{
			float val = 4f / Math.Max(1f, (float)vec3d.Z);
			float num = Math.Min(1f, val);
			if (num > 0.75f)
			{
				num = 0.75f + (num - 0.75f) / 2f;
			}
			float num2 = 0f;
			double num3 = entityPlayer.Pos.SquareDistanceTo(entity.Pos);
			if (nameTagTexture != null && (!ShowOnlyWhenTargeted || capi.World.Player.CurrentEntitySelection?.Entity == entity) && (double)(renderRange * renderRange) > num3)
			{
				float posX = (float)vec3d.X - num * (float)nameTagTexture.Width / 2f;
				float posY = (float)render.FrameHeight - (float)vec3d.Y - (float)nameTagTexture.Height * Math.Max(0f, num);
				render.Render2DTexture(nameTagTexture.TextureId, posX, posY, num * (float)nameTagTexture.Width, num * (float)nameTagTexture.Height, 20f);
				num2 += (float)nameTagTexture.Height;
			}
		}
	}

	public void Dispose()
	{
		if (nameTagTexture != null)
		{
			nameTagTexture.Dispose();
			nameTagTexture = null;
		}
		if (capi != null)
		{
			capi.Event.UnregisterRenderer(this, EnumRenderStage.Ortho);
		}
	}

	public override string GetName(ref EnumHandling handling)
	{
		handling = EnumHandling.PreventDefault;
		return DisplayName;
	}

	public void SetName(string playername)
	{
		ITreeAttribute treeAttribute = entity.WatchedAttributes.GetTreeAttribute("nametag");
		if (treeAttribute == null)
		{
			entity.WatchedAttributes.SetAttribute("nametag", treeAttribute = new TreeAttribute());
		}
		treeAttribute.SetString("name", playername);
		entity.WatchedAttributes.MarkPathDirty("nametag");
	}

	protected void OnNameChanged()
	{
		if (nameTagRenderHandler != null)
		{
			if (nameTagTexture != null)
			{
				nameTagTexture.Dispose();
				nameTagTexture = null;
			}
			nameTagTexture = nameTagRenderHandler(capi, entity);
		}
	}

	public override void OnEntityDeath(DamageSource damageSourceForDeath)
	{
		Dispose();
	}

	public override void OnEntityDespawn(EntityDespawnData despawn)
	{
		Dispose();
	}

	public override string PropertyName()
	{
		return "displayname";
	}
}
