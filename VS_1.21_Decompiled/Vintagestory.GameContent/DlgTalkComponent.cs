using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class DlgTalkComponent : DialogueComponent
{
	public DialogeTextElement[] Text;

	private HashSet<int> usedAnswers;

	private bool IsPlayer => Owner == "player";

	public override string Execute()
	{
		setVars();
		RichTextComponentBase[] array = genText(!IsPlayer);
		if (array.Length != 0)
		{
			dialog?.EmitDialogue(array);
		}
		if (IsPlayer)
		{
			return null;
		}
		if (JumpTo == null)
		{
			return "next";
		}
		return JumpTo;
	}

	protected RichTextComponentBase[] genText(bool selectRandom)
	{
		List<RichTextComponentBase> list = new List<RichTextComponentBase>();
		ICoreAPI api = controller.NPCEntity.Api;
		if (api.Side != EnumAppSide.Client)
		{
			return list.ToArray();
		}
		CairoFont cairoFont = CairoFont.WhiteSmallText().WithLineHeightMultiplier(1.2);
		if (IsPlayer)
		{
			list.Add(new RichTextComponent(api as ICoreClientAPI, "\r\n", cairoFont));
		}
		int num = 1;
		List<DialogeTextElement> list2 = new List<DialogeTextElement>();
		for (int i = 0; i < Text.Length; i++)
		{
			if (!selectRandom || conditionsMet(Text[i].Conditions))
			{
				list2.Add(Text[i]);
			}
		}
		int num2 = api.World.Rand.Next(list2.Count);
		for (int j = 0; j < list2.Count; j++)
		{
			if ((!selectRandom && !conditionsMet(Text[j].Conditions)) || (selectRandom && j != num2))
			{
				continue;
			}
			string text = Lang.Get(list2[j].Value).Replace("{characterclass}", Lang.Get("characterclass-" + controller.PlayerEntity.WatchedAttributes.GetString("characterClass"))).Replace("{playername}", controller.PlayerEntity.GetBehavior<EntityBehaviorNameTag>()?.DisplayName)
				.Replace("{npcname}", controller.NPCEntity.GetBehavior<EntityBehaviorNameTag>()?.DisplayName);
			if (IsPlayer)
			{
				int id = list2[j].Id;
				LinkTextComponent linkTextComponent = new LinkTextComponent(api as ICoreClientAPI, num + ". " + text, cairoFont, delegate
				{
					SelectAnswerById(id);
				});
				list.Add(linkTextComponent);
				list.Add(new RichTextComponent(api as ICoreClientAPI, "\r\n", cairoFont));
				CairoFont font = linkTextComponent.Font;
				HashSet<int> hashSet = usedAnswers;
				font.WithColor((hashSet != null && hashSet.Contains(id)) ? GuiStyle.ColorParchment : GuiStyle.ColorTime1).WithOrientation(EnumTextOrientation.Right);
				num++;
			}
			else
			{
				list.AddRange(VtmlUtil.Richtextify(api as ICoreClientAPI, text + "\r\n", cairoFont));
			}
		}
		return list.ToArray();
	}

	private bool conditionsMet(ConditionElement[] conds)
	{
		if (conds == null)
		{
			return true;
		}
		foreach (ConditionElement cond in conds)
		{
			if (!isConditionMet(cond))
			{
				return false;
			}
		}
		return true;
	}

	private bool isConditionMet(ConditionElement cond)
	{
		if (IsConditionMet(cond.Variable, cond.IsValue, cond.Invert))
		{
			return true;
		}
		return false;
	}

	public void SelectAnswerById(int id)
	{
		ICoreAPI api = controller.NPCEntity.Api;
		DialogeTextElement dialogeTextElement = Text.FirstOrDefault((DialogeTextElement elem) => elem.Id == id);
		if (dialogeTextElement == null || !conditionsMet(dialogeTextElement.Conditions))
		{
			api.Logger.Warning($"Got invalid answer index: {id} for {controller.NPCEntity.Code}");
			return;
		}
		if (IsPlayer)
		{
			if (usedAnswers == null)
			{
				usedAnswers = new HashSet<int>();
			}
			usedAnswers.Add(id);
		}
		if (api is ICoreClientAPI coreClientAPI)
		{
			coreClientAPI.Network.SendEntityPacket(controller.NPCEntity.EntityId, EntityBehaviorConversable.SelectAnswerPacketId, SerializerUtil.Serialize(id));
		}
		dialog?.ClearDialogue();
		jumpTo(dialogeTextElement.JumpTo);
	}

	private void jumpTo(string code)
	{
		controller.JumpTo(code);
	}

	public override void Init(ref int uniqueIdCounter)
	{
		DialogeTextElement[] text = Text;
		for (int i = 0; i < text.Length; i++)
		{
			text[i].Id = uniqueIdCounter++;
		}
	}
}
