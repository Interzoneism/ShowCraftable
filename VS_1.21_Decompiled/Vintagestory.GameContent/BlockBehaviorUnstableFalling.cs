using System;
using System.Collections.Generic;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

[DocumentAsJson]
[AddDocumentationProperty("AttachableFaces", "The faces that this block could be attached from which will prevent it from falling.", "System.String[]", "Optional", "None", false)]
[AddDocumentationProperty("AttachmentAreas", "A list of attachment areas per face that determine what blocks can be attached to.", "System.Collections.Generic.Dictionary{System.String,Vintagestory.API.Datastructures.RotatableCube}", "Optional", "None", false)]
[AddDocumentationProperty("AttachmentArea", "A single attachment area that determine what blocks can be attached to. Used if AttachmentAreas is not supplied.", "Vintagestory.API.Mathtools.Cuboidi", "Optional", "None", false)]
[AddDocumentationProperty("AllowUnstablePlacement", "Can this block be placed in an unstable position?", "System.Boolean", "Optional", "False", true)]
[AddDocumentationProperty("IgnorePlaceTest", "(Obsolete) Please use the AllowUnstablePlacement attribute instead.", "System.Boolean", "Obsolete", "", false)]
public class BlockBehaviorUnstableFalling : BlockBehavior
{
	[DocumentAsJson("Optional", "None", false)]
	private AssetLocation[] exceptions;

	[DocumentAsJson("Optional", "False", false)]
	public bool fallSideways;

	[DocumentAsJson("Optional", "0", false)]
	private float dustIntensity;

	[DocumentAsJson("Optional", "0.3", false)]
	private float fallSidewaysChance = 0.3f;

	[DocumentAsJson("Optional", "None", false)]
	private AssetLocation fallSound;

	[DocumentAsJson("Optional", "1", false)]
	private float impactDamageMul;

	private Cuboidi[] attachmentAreas;

	private BlockFacing[] attachableFaces;

	public BlockBehaviorUnstableFalling(Block block)
		: base(block)
	{
	}

	public override void Initialize(JsonObject properties)
	{
		base.Initialize(properties);
		attachableFaces = null;
		if (properties["attachableFaces"].Exists)
		{
			string[] array = properties["attachableFaces"].AsArray<string>();
			attachableFaces = new BlockFacing[array.Length];
			for (int i = 0; i < array.Length; i++)
			{
				attachableFaces[i] = BlockFacing.FromCode(array[i]);
			}
		}
		Dictionary<string, RotatableCube> dictionary = properties["attachmentAreas"].AsObject<Dictionary<string, RotatableCube>>();
		attachmentAreas = new Cuboidi[6];
		if (dictionary != null)
		{
			foreach (KeyValuePair<string, RotatableCube> item in dictionary)
			{
				item.Value.Origin.Set(8.0, 8.0, 8.0);
				BlockFacing blockFacing = BlockFacing.FromFirstLetter(item.Key[0]);
				attachmentAreas[blockFacing.Index] = item.Value.RotatedCopy().ConvertToCuboidi();
			}
		}
		else
		{
			attachmentAreas[4] = properties["attachmentArea"].AsObject<Cuboidi>();
		}
		exceptions = properties["exceptions"].AsObject(Array.Empty<AssetLocation>(), block.Code.Domain);
		fallSideways = properties["fallSideways"].AsBool();
		dustIntensity = properties["dustIntensity"].AsFloat();
		fallSidewaysChance = properties["fallSidewaysChance"].AsFloat(0.3f);
		string text = properties["fallSound"].AsString();
		if (text != null)
		{
			fallSound = AssetLocation.Create(text, block.Code.Domain);
		}
		impactDamageMul = properties["impactDamageMul"].AsFloat(1f);
	}

	public override bool CanPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling, ref string failureCode)
	{
		handling = EnumHandling.PassThrough;
		JsonObject attributes = base.block.Attributes;
		if (attributes != null && attributes["allowUnstablePlacement"].AsBool())
		{
			return true;
		}
		Cuboidi attachmentArea = attachmentAreas[4];
		BlockPos pos = blockSel.Position.DownCopy();
		Block block = world.BlockAccessor.GetBlock(pos);
		if (blockSel != null && !IsAttached(world.BlockAccessor, blockSel.Position) && !block.CanAttachBlockAt(world.BlockAccessor, base.block, pos, BlockFacing.UP, attachmentArea) && !block.WildCardMatch(exceptions))
		{
			handling = EnumHandling.PreventSubsequent;
			failureCode = "requiresolidground";
			return false;
		}
		return true;
	}

	public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ref EnumHandling handling)
	{
		TryFalling(world, blockPos, ref handling);
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos, ref EnumHandling handling)
	{
		base.OnNeighbourBlockChange(world, pos, neibpos, ref handling);
		if (world.Side != EnumAppSide.Client)
		{
			EnumHandling handling2 = EnumHandling.PassThrough;
			TryFalling(world, pos, ref handling2);
		}
	}

	private bool TryFalling(IWorldAccessor world, BlockPos pos, ref EnumHandling handling)
	{
		if (world.Side != EnumAppSide.Server)
		{
			return false;
		}
		if (!fallSideways && IsAttached(world.BlockAccessor, pos))
		{
			return false;
		}
		ICoreServerAPI coreServerAPI = (world as IServerWorldAccessor).Api as ICoreServerAPI;
		if (!coreServerAPI.World.Config.GetBool("allowFallingBlocks"))
		{
			return false;
		}
		if (IsReplacableBeneath(world, pos) || (fallSideways && world.Rand.NextDouble() < (double)fallSidewaysChance && IsReplacableBeneathAndSideways(world, pos)))
		{
			BlockPos ourPos = pos.Copy();
			coreServerAPI.Event.EnqueueMainThreadTask(delegate
			{
				Block block = world.BlockAccessor.GetBlock(ourPos);
				if (base.block == block && world.GetNearestEntity(ourPos.ToVec3d().Add(0.5, 0.5, 0.5), 1f, 1.5f, (Entity e) => e is EntityBlockFalling entityBlockFalling && entityBlockFalling.initialPos.Equals(ourPos)) == null)
				{
					BlockEntity blockEntity = world.BlockAccessor.GetBlockEntity(ourPos);
					EntityBlockFalling entity = new EntityBlockFalling(block, blockEntity, ourPos, fallSound, impactDamageMul, canFallSideways: true, dustIntensity);
					world.SpawnEntity(entity);
				}
			}, "falling");
			handling = EnumHandling.PreventSubsequent;
			return true;
		}
		handling = EnumHandling.PassThrough;
		return false;
	}

	public virtual bool IsAttached(IBlockAccessor blockAccessor, BlockPos pos)
	{
		BlockPos pos2;
		if (attachableFaces == null)
		{
			pos2 = pos.DownCopy();
			return blockAccessor.GetBlock(pos2).CanAttachBlockAt(blockAccessor, block, pos2, BlockFacing.UP, attachmentAreas[5]);
		}
		pos2 = new BlockPos();
		for (int i = 0; i < attachableFaces.Length; i++)
		{
			BlockFacing blockFacing = attachableFaces[i];
			pos2.Set(pos).Add(blockFacing);
			if (blockAccessor.GetBlock(pos2).CanAttachBlockAt(blockAccessor, block, pos2, blockFacing.Opposite, attachmentAreas[blockFacing.Index]))
			{
				return true;
			}
		}
		return false;
	}

	private bool IsReplacableBeneathAndSideways(IWorldAccessor world, BlockPos pos)
	{
		for (int i = 0; i < 4; i++)
		{
			BlockFacing blockFacing = BlockFacing.HORIZONTALS[i];
			Block blockOrNull = world.BlockAccessor.GetBlockOrNull(pos.X + blockFacing.Normali.X, pos.Y + blockFacing.Normali.Y, pos.Z + blockFacing.Normali.Z);
			if (blockOrNull != null && blockOrNull.Replaceable >= 6000)
			{
				blockOrNull = world.BlockAccessor.GetBlockOrNull(pos.X + blockFacing.Normali.X, pos.Y + blockFacing.Normali.Y - 1, pos.Z + blockFacing.Normali.Z);
				if (blockOrNull != null && blockOrNull.Replaceable >= 6000)
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool IsReplacableBeneath(IWorldAccessor world, BlockPos pos)
	{
		return world.BlockAccessor.GetBlockBelow(pos).Replaceable > 6000;
	}
}
