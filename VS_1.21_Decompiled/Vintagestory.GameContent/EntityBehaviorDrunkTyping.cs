using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityBehaviorDrunkTyping : EntityBehavior
{
	private ICoreAPI api;

	private bool isCommand;

	public EntityBehaviorDrunkTyping(Entity entity)
		: base(entity)
	{
	}

	public override void Initialize(EntityProperties properties, JsonObject typeAttributes)
	{
		api = entity.World.Api;
	}

	public override void OnEntityLoaded()
	{
		if (entity.Api is ICoreClientAPI coreClientAPI && (entity as EntityPlayer)?.PlayerUID == coreClientAPI.Settings.String["playeruid"])
		{
			coreClientAPI.Event.RegisterEventBusListener(onChatKeyDownPre, 1.0, "chatkeydownpre");
			coreClientAPI.Event.RegisterEventBusListener(onChatKeyDownPost, 1.0, "chatkeydownpost");
		}
	}

	public override string PropertyName()
	{
		return "drunktyping";
	}

	private void onChatKeyDownPre(string eventName, ref EnumHandling handling, IAttribute data)
	{
		string value = ((data as TreeAttribute)["text"] as StringAttribute).value;
		isCommand = value.Length > 0 && (value[0] == '.' || value[0] == '/');
	}

	private void onChatKeyDownPost(string eventName, ref EnumHandling handling, IAttribute data)
	{
		TreeAttribute treeAttribute = data as TreeAttribute;
		int value = (treeAttribute["key"] as IntAttribute).value;
		string value2 = (treeAttribute["text"] as StringAttribute).value;
		int caretPos = 0;
		if (isCommand && value2.Length > 0 && value2[0] != '.' && value2[0] != '/')
		{
			string text = value2[0].ToString() ?? "";
			for (int i = 1; i < value2.Length; i++)
			{
				text = slurText(text, ref caretPos);
				text += value2[i];
			}
			value2 = text;
			(treeAttribute["text"] as StringAttribute).value = value2;
		}
		else if (value != 53 && value != 47 && value != 48 && value != 55 && value != 5 && value != 3 && value2.Length > 0 && value2[0] != '.' && value2[0] != '/')
		{
			value2 = slurText(value2, ref caretPos);
			(treeAttribute["text"] as StringAttribute).value = value2;
		}
		treeAttribute.SetInt("deltacaretpos", caretPos);
	}

	private string slurText(string text, ref int caretPos)
	{
		Random rand = api.World.Rand;
		float num = entity.WatchedAttributes.GetFloat("intoxication");
		if (rand.NextDouble() < (double)num)
		{
			switch (rand.Next(9))
			{
			case 0:
			case 1:
				if (text.Length > 1)
				{
					text = text.Substring(0, text.Length - 2) + text[text.Length - 1] + text[text.Length - 2];
				}
				break;
			case 2:
			case 3:
			case 4:
				if (text.Length > 0)
				{
					text += text[text.Length - 1];
					caretPos++;
				}
				break;
			case 5:
			case 6:
			{
				if (text.Length <= 0)
				{
					break;
				}
				string[] array = new string[4] { "1234567890-", "qwertyuiop[", "asdfghjkl;", "zxcvbnm,." };
				char value = text[text.Length - 1];
				for (int i = 0; i < 3; i++)
				{
					int num2 = array[i].IndexOf(value);
					if (num2 >= 0)
					{
						int num3 = rand.Next(2) * 2 - 1;
						text += array[i][GameMath.Clamp(num2 + num3, 0, array[i].Length - 1)];
						caretPos++;
					}
				}
				break;
			}
			}
		}
		return text;
	}
}
