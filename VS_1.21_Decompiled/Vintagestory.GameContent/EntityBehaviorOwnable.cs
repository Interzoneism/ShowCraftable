using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class EntityBehaviorOwnable : EntityBehavior
{
	public string Group;

	public EntityBehaviorOwnable(Entity entity)
		: base(entity)
	{
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		base.Initialize(properties, attributes);
		Group = attributes["groupCode"].AsString();
		verifyOwnership();
	}

	private void verifyOwnership()
	{
		if (entity.World.Side != EnumAppSide.Server)
		{
			return;
		}
		bool flag = false;
		ITreeAttribute treeAttribute = entity.WatchedAttributes.GetTreeAttribute("ownedby");
		if (treeAttribute != null && entity.World.Api.ModLoader.GetModSystem<ModSystemEntityOwnership>().OwnerShipsByPlayerUid.TryGetValue(treeAttribute.GetString("uid", ""), out var value) && value != null && value.TryGetValue(Group, out var value2))
		{
			if (entity.World.Side == EnumAppSide.Server)
			{
				value2.Pos = entity.ServerPos;
			}
			flag = value2.EntityId == entity.EntityId;
		}
		if (!flag)
		{
			entity.WatchedAttributes.RemoveAttribute("ownedby");
		}
	}

	public override void GetInfoText(StringBuilder infotext)
	{
		base.GetInfoText(infotext);
		ITreeAttribute treeAttribute = entity.WatchedAttributes.GetTreeAttribute("ownedby");
		if (treeAttribute == null)
		{
			return;
		}
		infotext.AppendLine(Lang.Get("Owned by {0}", treeAttribute.GetString("name")));
		if ((entity.World as IClientWorldAccessor).Player.WorldData.CurrentGameMode != EnumGameMode.Creative)
		{
			EntityBehaviorHealth behavior = entity.GetBehavior<EntityBehaviorHealth>();
			if (behavior != null)
			{
				infotext.AppendLine(Lang.Get("ownableentity-health", behavior.Health, behavior.MaxHealth));
			}
		}
	}

	public override string PropertyName()
	{
		return "ownable";
	}

	public bool IsOwner(EntityAgent byEntity)
	{
		if (byEntity is EntityPlayer entityPlayer)
		{
			ITreeAttribute treeAttribute = entity.WatchedAttributes.GetTreeAttribute("ownedby");
			if (treeAttribute == null)
			{
				return true;
			}
			string text = treeAttribute.GetString("uid");
			if (text != null && entityPlayer.PlayerUID == text)
			{
				return true;
			}
		}
		return false;
	}

	public override void OnEntityDespawn(EntityDespawnData despawn)
	{
		base.OnEntityDespawn(despawn);
		if (entity.World.Api is ICoreClientAPI coreClientAPI)
		{
			ITreeAttribute treeAttribute = entity.WatchedAttributes.GetTreeAttribute("ownedby");
			if (treeAttribute != null && coreClientAPI.World.Player.PlayerUID == treeAttribute.GetString("uid", "") && entity.World.Api.ModLoader.GetModSystem<ModSystemEntityOwnership>().SelfOwnerShips.TryGetValue(Group, out var value))
			{
				value.Pos = entity.Pos.Copy();
			}
		}
	}

	public override bool ToleratesDamageFrom(Entity eOther, ref EnumHandling handling)
	{
		if (eOther is EntityAgent byEntity && IsOwner(byEntity))
		{
			handling = EnumHandling.PreventDefault;
			return true;
		}
		return false;
	}
}
