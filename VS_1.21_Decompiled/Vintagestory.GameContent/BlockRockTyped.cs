using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockRockTyped : BlockShapeFromAttributes
{
	public Dictionary<string, ClutterTypeProps> clutterByCode = new Dictionary<string, ClutterTypeProps>();

	public override string ClassType => "rocktyped-" + Variant["cover"];

	public override IEnumerable<IShapeTypeProps> AllTypes => clutterByCode.Values;

	public override IShapeTypeProps GetTypeProps(string code, ItemStack stack, BEBehaviorShapeFromAttributes be)
	{
		if (code == null)
		{
			return null;
		}
		clutterByCode.TryGetValue(code, out var value);
		return value;
	}

	public override void LoadTypes()
	{
		ClutterTypeProps[] array = Attributes["types"].AsObject<ClutterTypeProps[]>();
		StandardWorldProperty standardWorldProperty = api.Assets.Get("worldproperties/block/rock.json").ToObject<StandardWorldProperty>();
		List<JsonItemStack> list = new List<JsonItemStack>();
		ModelTransform defaults = ModelTransform.BlockDefaultGui();
		ModelTransform defaults2 = ModelTransform.BlockDefaultFp();
		ModelTransform defaults3 = ModelTransform.BlockDefaultTp();
		ModelTransform defaults4 = ModelTransform.BlockDefaultGround();
		ClutterTypeProps[] array2 = array;
		foreach (ClutterTypeProps clutterTypeProps in array2)
		{
			if (clutterTypeProps.GuiTf != null)
			{
				clutterTypeProps.GuiTransform = new ModelTransform(clutterTypeProps.GuiTf, defaults);
			}
			if (clutterTypeProps.FpTf != null)
			{
				clutterTypeProps.FpTtransform = new ModelTransform(clutterTypeProps.FpTf, defaults2);
			}
			if (clutterTypeProps.TpTf != null)
			{
				clutterTypeProps.TpTransform = new ModelTransform(clutterTypeProps.TpTf, defaults3);
			}
			if (clutterTypeProps.GroundTf != null)
			{
				clutterTypeProps.GroundTransform = new ModelTransform(clutterTypeProps.GroundTf, defaults4);
			}
			clutterTypeProps.ShapePath.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");
			WorldPropertyVariant[] variants = standardWorldProperty.Variants;
			foreach (WorldPropertyVariant worldPropertyVariant in variants)
			{
				ClutterTypeProps clutterTypeProps2 = clutterTypeProps.Clone();
				clutterTypeProps2.Code = clutterTypeProps2.Code + "-" + worldPropertyVariant.Code.Path;
				clutterByCode[clutterTypeProps2.Code] = clutterTypeProps2;
				foreach (CompositeTexture value in clutterTypeProps2.Textures.Values)
				{
					value.FillPlaceholder("{rock}", worldPropertyVariant.Code.Path);
				}
				if (clutterTypeProps2.Drops != null)
				{
					BlockDropItemStack[] drops = clutterTypeProps2.Drops;
					foreach (BlockDropItemStack blockDropItemStack in drops)
					{
						blockDropItemStack.Code.Path = blockDropItemStack.Code.Path.Replace("{rock}", worldPropertyVariant.Code.Path);
						blockDropItemStack.Resolve(api.World, "rock typed block drop", Code);
					}
				}
				JsonItemStack jsonItemStack = new JsonItemStack();
				jsonItemStack.Code = Code;
				jsonItemStack.Type = EnumItemClass.Block;
				jsonItemStack.Attributes = new JsonObject(JToken.Parse("{ \"type\": \"" + clutterTypeProps2.Code + "\", \"rock\": \"" + worldPropertyVariant.Code.Path + "\" }"));
				JsonItemStack jsonItemStack2 = jsonItemStack;
				jsonItemStack2.Resolve(api.World, ClassType + " type");
				list.Add(jsonItemStack2);
			}
		}
		if (Variant["cover"] != "snow")
		{
			CreativeInventoryStacks = new CreativeTabAndStackList[1]
			{
				new CreativeTabAndStackList
				{
					Stacks = list.ToArray(),
					Tabs = new string[2] { "general", "terrain" }
				}
			};
		}
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		BEBehaviorShapeFromAttributes bEBehavior = GetBEBehavior<BEBehaviorShapeFromAttributes>(pos);
		return GetTypeProps(bEBehavior?.Type, null, bEBehavior)?.Drops?.Select((BlockDropItemStack drop) => drop.GetNextItemStack(dropQuantityMultiplier)).ToArray() ?? Array.Empty<ItemStack>();
	}

	public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		BlockDropItemStack[] dropsForHandbook = base.GetDropsForHandbook(handbookStack, forPlayer);
		dropsForHandbook[0] = dropsForHandbook[0].Clone();
		dropsForHandbook[0].ResolvedItemstack.SetFrom(handbookStack);
		return dropsForHandbook;
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		string rockType = getRockType(inSlot.Itemstack.Attributes.GetString("type"));
		dsc.AppendLine(Lang.Get("rock-" + rockType));
	}

	public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
	{
		BEBehaviorShapeFromAttributes bEBehavior = GetBEBehavior<BEBehaviorShapeFromAttributes>(pos);
		if (bEBehavior != null && bEBehavior.Type != null)
		{
			return Lang.Get("rock-" + getRockType(bEBehavior.Type));
		}
		return base.GetPlacedBlockInfo(world, pos, forPlayer);
	}

	private string getRockType(string type)
	{
		string[] array = type.Split('-');
		if (array.Length < 3)
		{
			return "unknown";
		}
		return array[2];
	}
}
