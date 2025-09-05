using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class InventoryStoneCoffin : InventoryGeneric
{
	private Vec3d secondaryPos;

	public InventoryStoneCoffin(int size, string invId, ICoreAPI api)
		: base(size, invId, api)
	{
	}

	public override void DropAll(Vec3d pos, int maxStackSize = 0)
	{
		using IEnumerator<ItemSlot> enumerator = GetEnumerator();
		while (enumerator.MoveNext())
		{
			ItemSlot current = enumerator.Current;
			if (current.Itemstack == null)
			{
				continue;
			}
			int stackSize = current.Itemstack.StackSize;
			if (stackSize != 0)
			{
				int i;
				for (i = 0; i + 2 <= stackSize; i += 2)
				{
					ItemStack itemStack = current.Itemstack.Clone();
					itemStack.StackSize = 1;
					Api.World.SpawnItemEntity(itemStack, pos);
					Api.World.SpawnItemEntity(itemStack.Clone(), secondaryPos);
				}
				if (i < stackSize)
				{
					ItemStack itemStack2 = current.Itemstack.Clone();
					itemStack2.StackSize = 1;
					Api.World.SpawnItemEntity(itemStack2, pos);
				}
				current.Itemstack = null;
				current.MarkDirty();
			}
		}
	}

	internal void SetSecondaryPos(BlockPos blockPos)
	{
		secondaryPos = blockPos.ToVec3d();
	}
}
