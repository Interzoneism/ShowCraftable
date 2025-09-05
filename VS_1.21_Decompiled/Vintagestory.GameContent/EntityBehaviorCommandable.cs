using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityBehaviorCommandable : EntityBehavior
{
	public bool Sit
	{
		get
		{
			return entity.WatchedAttributes.GetBool("commandSit");
		}
		set
		{
			entity.WatchedAttributes.SetBool("commandSit", value);
		}
	}

	public string GuardedName
	{
		get
		{
			return entity.WatchedAttributes.GetString("guardedName");
		}
		set
		{
			entity.WatchedAttributes.SetString("guardedName", value);
		}
	}

	public EntityBehaviorCommandable(Entity entity)
		: base(entity)
	{
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		base.Initialize(properties, attributes);
		try
		{
			SetGuardedName(0f);
		}
		catch
		{
			entity.Api.Event.RegisterCallback(SetGuardedName, 1500);
		}
	}

	private void SetGuardedName(float dt)
	{
		Entity guardedEntity = GetGuardedEntity();
		if (guardedEntity != null)
		{
			string name = guardedEntity.GetName();
			GuardedName = name ?? "";
		}
		else
		{
			GuardedName = "";
		}
	}

	public override void OnEntitySpawn()
	{
		setupTaskBlocker();
	}

	public override void OnEntityLoaded()
	{
		setupTaskBlocker();
	}

	private void setupTaskBlocker()
	{
		if (entity.Api.Side != EnumAppSide.Server)
		{
			return;
		}
		EntityBehaviorTaskAI behavior = entity.GetBehavior<EntityBehaviorTaskAI>();
		if (behavior != null)
		{
			behavior.TaskManager.OnShouldExecuteTask += (IAiTask task) => !Sit || task is AiTaskIdle || task is AiTaskLookAround;
		}
	}

	public override void OnInteract(EntityAgent byEntity, ItemSlot itemslot, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled)
	{
		Sit = !Sit;
	}

	public override void GetInfoText(StringBuilder infotext)
	{
		base.GetInfoText(infotext);
		if (Sit)
		{
			infotext.AppendLine(Lang.Get("Waits"));
		}
		else
		{
			infotext.AppendLine(Lang.Get("Follows {0}", GuardedName));
		}
		ITreeAttribute treeAttribute = entity.WatchedAttributes.GetTreeAttribute("health");
		if (treeAttribute != null)
		{
			infotext.AppendLine(Lang.Get("commandable-entity-healthpoints", treeAttribute.GetFloat("currenthealth"), treeAttribute.GetFloat("maxhealth")));
		}
	}

	public override string PropertyName()
	{
		return "commandable";
	}

	public Entity GetGuardedEntity()
	{
		string text = entity.WatchedAttributes.GetString("guardedPlayerUid");
		if (text != null)
		{
			return entity.World.PlayerByUid(text)?.Entity;
		}
		long entityId = entity.WatchedAttributes.GetLong("guardedEntityId", 0L);
		return entity.World.GetEntityById(entityId);
	}
}
