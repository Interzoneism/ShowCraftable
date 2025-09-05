using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;

namespace Vintagestory.GameContent;

public class EntityBehaviorErelBoss : EntityBehaviorBoss
{
	public override string BossName
	{
		get
		{
			if (entity.Pos.Dimension != 0)
			{
				return Lang.Get(entity.Code.Domain + ":erel-boss-name-past");
			}
			return Lang.Get(entity.Code.Domain + ":erel-boss-name-present");
		}
	}

	public EntityBehaviorErelBoss(Entity entity)
		: base(entity)
	{
	}
}
