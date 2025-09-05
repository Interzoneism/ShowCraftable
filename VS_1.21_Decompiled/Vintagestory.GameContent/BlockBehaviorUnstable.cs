using System.Collections.Generic;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

[DocumentAsJson]
[AddDocumentationProperty("AttachedToFaces", "A list of faces on this block that can be attached to other blocks.", "System.String[]", "Optional", "Down", false)]
[AddDocumentationProperty("AttachmentAreas", "A list of attachment areas per face that determine what blocks can be attached to.", "System.Collections.Generic.Dictionary{System.String,Vintagestory.API.Datastructures.RotatableCube}", "Optional", "None", false)]
public class BlockBehaviorUnstable : BlockBehavior
{
	private BlockFacing[] AttachedToFaces;

	private Dictionary<string, Cuboidi> attachmentAreas;

	public BlockBehaviorUnstable(Block block)
		: base(block)
	{
	}

	public override void Initialize(JsonObject properties)
	{
		base.Initialize(properties);
		AttachedToFaces = new BlockFacing[1] { BlockFacing.DOWN };
		if (properties["attachedToFaces"].Exists)
		{
			string[] array = properties["attachedToFaces"].AsArray<string>();
			AttachedToFaces = new BlockFacing[array.Length];
			for (int i = 0; i < array.Length; i++)
			{
				AttachedToFaces[i] = BlockFacing.FromCode(array[i]);
			}
		}
		Dictionary<string, RotatableCube> dictionary = properties["attachmentAreas"].AsObject<Dictionary<string, RotatableCube>>();
		if (dictionary == null)
		{
			return;
		}
		attachmentAreas = new Dictionary<string, Cuboidi>();
		foreach (KeyValuePair<string, RotatableCube> item in dictionary)
		{
			item.Value.Origin.Set(8.0, 8.0, 8.0);
			attachmentAreas[item.Key] = item.Value.RotatedCopy().ConvertToCuboidi();
		}
	}

	public override bool CanPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling, ref string failureCode)
	{
		if (!IsAttached(world.BlockAccessor, blockSel.Position))
		{
			handling = EnumHandling.PreventSubsequent;
			failureCode = "requireattachable";
			return false;
		}
		return true;
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos, ref EnumHandling handled)
	{
		if (!IsAttached(world.BlockAccessor, pos))
		{
			handled = EnumHandling.PreventDefault;
			world.BlockAccessor.BreakBlock(pos, null);
		}
		else
		{
			base.OnNeighbourBlockChange(world, pos, neibpos, ref handled);
		}
	}

	public virtual bool IsAttached(IBlockAccessor blockAccessor, BlockPos pos)
	{
		for (int i = 0; i < AttachedToFaces.Length; i++)
		{
			BlockFacing blockFacing = AttachedToFaces[i];
			Block obj = blockAccessor.GetBlock(pos.AddCopy(blockFacing));
			Cuboidi value = null;
			attachmentAreas?.TryGetValue(blockFacing.Code, out value);
			if (obj.CanAttachBlockAt(blockAccessor, block, pos.AddCopy(blockFacing), blockFacing.Opposite, value))
			{
				return true;
			}
		}
		return false;
	}
}
