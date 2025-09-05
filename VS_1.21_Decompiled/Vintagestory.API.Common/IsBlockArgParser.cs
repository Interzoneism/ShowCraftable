using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public class IsBlockArgParser : ArgumentParserBase
{
	private ICoreAPI api;

	private int blockId;

	private Vec3d pos;

	private bool isFluid;

	private Dictionary<string, string> subargs;

	public IsBlockArgParser(string argName, ICoreAPI api, bool isMandatoryArg)
		: base(argName, isMandatoryArg)
	{
		this.api = api;
	}

	public override object GetValue()
	{
		if (pos == null)
		{
			return false;
		}
		BlockPos asBlockPos = pos.AsBlockPos;
		if (api.World.BlockAccessor.GetBlock(asBlockPos, (!isFluid) ? 1 : 2).Id != blockId)
		{
			return false;
		}
		if (subargs == null || subargs.Count == 0)
		{
			return true;
		}
		BlockEntity blockEntity = api.World.BlockAccessor.GetBlockEntity(asBlockPos);
		if (blockEntity == null)
		{
			return false;
		}
		TreeAttribute treeAttribute = new TreeAttribute();
		blockEntity.ToTreeAttributes(treeAttribute);
		foreach (KeyValuePair<string, string> subarg in subargs)
		{
			if (!treeAttribute.HasAttribute(subarg.Key))
			{
				return false;
			}
			if (treeAttribute.GetAttribute(subarg.Key).ToString() != subarg.Value)
			{
				return false;
			}
		}
		return true;
	}

	public override void SetValue(object data)
	{
		throw new NotImplementedException();
	}

	public override EnumParseResult TryProcess(TextCommandCallingArgs args, Action<AsyncParseResults> onReady = null)
	{
		pos = null;
		if (args.RawArgs.Length != 4)
		{
			lastErrorMessage = "Required format: isBlock[code=...] x y z";
			return EnumParseResult.Bad;
		}
		if (args.RawArgs.PopUntil('[') != "isBlock")
		{
			lastErrorMessage = "Required format: isBlock[code=...] x y z";
			return EnumParseResult.Bad;
		}
		string parseErrorMsg;
		string text = args.RawArgs.PopCodeBlock('[', ']', out parseErrorMsg);
		if (parseErrorMsg != null)
		{
			lastErrorMessage = parseErrorMsg;
			return EnumParseResult.Bad;
		}
		if (text.Length > 2)
		{
			subargs = parseSubArgs(text);
			if (!subargs.TryGetValue("code", out var value))
			{
				lastErrorMessage = "Requires [code=...] to be specified";
				return EnumParseResult.Bad;
			}
			Block block = api.World.GetBlock(new AssetLocation(value));
			if (block == null)
			{
				lastErrorMessage = "Code " + value + " is not a valid block";
				return EnumParseResult.Bad;
			}
			blockId = block.Id;
			isFluid = block.ForFluidsLayer;
			subargs.Remove("code");
		}
		Vec3d mapMiddle = new Vec3d(api.World.DefaultSpawnPosition.X, 0.0, api.World.DefaultSpawnPosition.Z);
		pos = args.RawArgs.PopFlexiblePos(args.Caller.Pos, mapMiddle);
		if (pos == null)
		{
			lastErrorMessage = Lang.Get("Invalid position, must be 3 numbers");
			return EnumParseResult.Bad;
		}
		return EnumParseResult.Good;
	}

	public static string Test(ICoreAPI api, Caller caller, string testcmd)
	{
		TextCommandCallingArgs args = new TextCommandCallingArgs
		{
			Caller = caller,
			RawArgs = new CmdArgs(testcmd)
		};
		IsBlockArgParser isBlockArgParser = new IsBlockArgParser("cond", api, isMandatoryArg: true);
		if (isBlockArgParser.TryProcess(args) == EnumParseResult.Bad)
		{
			return isBlockArgParser.LastErrorMessage;
		}
		return isBlockArgParser.TestCond();
	}

	private string TestCond()
	{
		if (pos == null)
		{
			return "No position specified";
		}
		BlockPos asBlockPos = pos.AsBlockPos;
		Block block = api.World.BlockAccessor.GetBlock(asBlockPos, 1);
		Block block2 = api.World.BlockAccessor.GetBlock(asBlockPos, 2);
		StringBuilder stringBuilder = new StringBuilder();
		if (block.Id > 0)
		{
			stringBuilder.AppendLine("Solid: " + block.Code.ToShortString());
		}
		if (block2.Id > 0)
		{
			stringBuilder.AppendLine("Fluid: " + block2.Code.ToShortString());
		}
		BlockEntity blockEntity = api.World.BlockAccessor.GetBlockEntity(asBlockPos);
		if (blockEntity == null)
		{
			stringBuilder.AppendLine("(no BlockEntity here)");
		}
		else
		{
			TreeAttribute treeAttribute = new TreeAttribute();
			blockEntity.ToTreeAttributes(treeAttribute);
			foreach (KeyValuePair<string, IAttribute> item in treeAttribute)
			{
				if (!"posx".Equals(item.Key) && !"posy".Equals(item.Key) && !"posz".Equals(item.Key))
				{
					stringBuilder.AppendLine("  " + item.Key + "=" + item.Value.ToString());
				}
			}
		}
		return stringBuilder.ToString();
	}
}
