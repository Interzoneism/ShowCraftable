using System;
using System.Linq;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace Vintagestory.API.Common;

public class CollectibleArgParser : ArgumentParserBase
{
	private EnumItemClass itemclass;

	private ICoreAPI api;

	private ItemStack stack;

	public CollectibleArgParser(string argName, ICoreAPI api, EnumItemClass itemclass, bool isMandatoryArg)
		: base(argName, isMandatoryArg)
	{
		this.api = api;
		this.itemclass = itemclass;
	}

	public override string GetSyntaxExplanation(string indent)
	{
		return indent + Lang.Get("{0} is a precise {1} name, optionally immediately followed by braces {{}} containing attributes for the {1}", GetSyntax(), itemclass);
	}

	public override string[] GetValidRange(CmdArgs args)
	{
		string text = args.PeekWord();
		if (text.Length == 0)
		{
			return null;
		}
		if (text.Contains("{"))
		{
			text = text.Split(new string[1] { "{" }, 1, StringSplitOptions.None)[0];
		}
		if (itemclass == EnumItemClass.Block)
		{
			return (from b in api.World.SearchBlocks(new AssetLocation(text))
				select b.Code.ToShortString()).ToArray();
		}
		return (from i in api.World.SearchItems(new AssetLocation(text))
			select i.Code.ToShortString()).ToArray();
	}

	public override object GetValue()
	{
		return stack;
	}

	public override void SetValue(object data)
	{
		stack = (ItemStack)data;
	}

	public override void PreProcess(TextCommandCallingArgs args)
	{
		base.PreProcess(args);
		stack = null;
	}

	public override EnumParseResult TryProcess(TextCommandCallingArgs args, Action<AsyncParseResults> onReady = null)
	{
		string text = args.RawArgs.PeekWord();
		if (text == null)
		{
			lastErrorMessage = "Missing";
			return EnumParseResult.Bad;
		}
		string text2 = null;
		if (text.Contains("{"))
		{
			text = args.RawArgs.PopUntil('{');
			if (text.Length == 0)
			{
				lastErrorMessage = "Missing Item/Block Code";
				return EnumParseResult.Bad;
			}
			text2 = args.RawArgs.PopCodeBlock('{', '}', out var parseErrorMsg);
			if (parseErrorMsg != null)
			{
				lastErrorMessage = parseErrorMsg;
				return EnumParseResult.Bad;
			}
		}
		else
		{
			args.RawArgs.PopWord();
		}
		if (itemclass == EnumItemClass.Block)
		{
			Block block = api.World.GetBlock(new AssetLocation(text));
			if (block == null)
			{
				lastErrorMessage = "No such block exists";
				return EnumParseResult.Bad;
			}
			stack = new ItemStack(block);
		}
		else
		{
			Item item = api.World.GetItem(new AssetLocation(text));
			if (item == null)
			{
				lastErrorMessage = "No such item exists";
				return EnumParseResult.Bad;
			}
			stack = new ItemStack(item);
		}
		if (text2 != null)
		{
			try
			{
				stack.Attributes = JsonObject.FromJson(text2).ToAttribute() as ITreeAttribute;
			}
			catch (Exception ex)
			{
				lastErrorMessage = "Unable to json parse attributes, likely invalid syntax: " + ex.Message;
				return EnumParseResult.Bad;
			}
		}
		return EnumParseResult.Good;
	}
}
