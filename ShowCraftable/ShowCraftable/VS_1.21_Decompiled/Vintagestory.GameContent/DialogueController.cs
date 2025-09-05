using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class DialogueController
{
	public EntityPlayer PlayerEntity;

	public EntityAgent NPCEntity;

	private DialogueComponent[] dialogue;

	private DialogueComponent currentDialogueCmp;

	private ICoreAPI api;

	public VariablesModSystem VarSys;

	public event DialogueTriggerDelegate DialogTriggers;

	public DialogueController(ICoreAPI api, EntityPlayer playerEntity, EntityAgent npcEntity, DialogueConfig dialogueConfig)
	{
		this.api = api;
		PlayerEntity = playerEntity;
		NPCEntity = npcEntity;
		dialogue = dialogueConfig.components;
		currentDialogueCmp = dialogue[0];
		VarSys = api.ModLoader.GetModSystem<VariablesModSystem>();
		VarSys.OnControllerInit(playerEntity, npcEntity);
	}

	public int Trigger(EntityAgent triggeringEntity, string value, JsonObject data)
	{
		if (this.DialogTriggers == null)
		{
			return 0;
		}
		int result = 0;
		Delegate[] invocationList = this.DialogTriggers.GetInvocationList();
		for (int i = 0; i < invocationList.Length; i++)
		{
			int num = ((DialogueTriggerDelegate)invocationList[i])(triggeringEntity, value, data);
			if (num != -1)
			{
				result = num;
			}
		}
		return result;
	}

	public void Init()
	{
		ContinueExecute();
	}

	public void PlayerSelectAnswerById(int id)
	{
		if (currentDialogueCmp is DlgTalkComponent dlgTalkComponent)
		{
			dlgTalkComponent.SelectAnswerById(id);
		}
	}

	public void JumpTo(string code)
	{
		currentDialogueCmp = dialogue.FirstOrDefault((DialogueComponent dlgcmp) => dlgcmp.Code == code);
		if (currentDialogueCmp == null)
		{
			(api as ICoreClientAPI)?.TriggerIngameError(this, "dialogueerror", "Invalid chat fragment of code " + code + " found");
		}
		else
		{
			ContinueExecute();
		}
	}

	public void ContinueExecute()
	{
		string nextCode;
		while ((nextCode = currentDialogueCmp.Execute()) != null)
		{
			DialogueComponent dialogueComponent = dialogue.FirstOrDefault((DialogueComponent dlgcmp) => dlgcmp.Code == nextCode);
			if (dialogueComponent == null && nextCode == "next")
			{
				int num = dialogue.IndexOf(currentDialogueCmp) + 1;
				if (num < dialogue.Length)
				{
					dialogueComponent = dialogue[num];
				}
			}
			if (dialogueComponent != null)
			{
				currentDialogueCmp = dialogueComponent;
				continue;
			}
			break;
		}
	}
}
