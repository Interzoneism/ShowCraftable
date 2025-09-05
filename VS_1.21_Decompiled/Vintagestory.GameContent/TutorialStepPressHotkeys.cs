using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class TutorialStepPressHotkeys : TutorialStepBase
{
	private List<string> hotkeysToPress = new List<string>();

	private HashSet<string> hotkeysPressed = new HashSet<string>();

	private ICoreClientAPI capi;

	private HashSet<EnumEntityAction> activeActions = new HashSet<EnumEntityAction>();

	private EnumEntityAction deferredActionTrigger = EnumEntityAction.None;

	private EnumEntityAction deferredActionPreReq = EnumEntityAction.None;

	public static Dictionary<EnumEntityAction, string> actionToHotkeyMapping = new Dictionary<EnumEntityAction, string>
	{
		{
			EnumEntityAction.Forward,
			"walkforward"
		},
		{
			EnumEntityAction.Backward,
			"walkbackward"
		},
		{
			EnumEntityAction.Left,
			"walkleft"
		},
		{
			EnumEntityAction.Right,
			"walkright"
		},
		{
			EnumEntityAction.Sneak,
			"sneak"
		},
		{
			EnumEntityAction.Sprint,
			"sprint"
		},
		{
			EnumEntityAction.Jump,
			"jump"
		},
		{
			EnumEntityAction.FloorSit,
			"sitdown"
		},
		{
			EnumEntityAction.CtrlKey,
			"ctrl"
		},
		{
			EnumEntityAction.ShiftKey,
			"shift"
		}
	};

	public override bool Complete => hotkeysPressed.Count == hotkeysToPress.Count;

	public TutorialStepPressHotkeys(ICoreClientAPI capi, string text, params string[] hotkeys)
	{
		this.capi = capi;
		base.text = text;
		hotkeysToPress.AddRange(hotkeys);
	}

	public override RichTextComponentBase[] GetText(CairoFont font)
	{
		_ = capi.Input.HotKeys;
		List<string> list = new List<string>();
		foreach (string item in hotkeysToPress)
		{
			if (hotkeysPressed.Contains(item))
			{
				list.Add("<font color=\"#99ff99\"><hk>" + item + "</hk></font>");
			}
			else
			{
				list.Add("<hk>" + item + "</hk>");
			}
		}
		object[] obj = new object[2]
		{
			index + 1,
			null
		};
		string key = text;
		object[] args = list.ToArray();
		obj[1] = Lang.Get(key, args);
		string vtmlCode = Lang.Get("tutorialstep-numbered", obj);
		return VtmlUtil.Richtextify(capi, vtmlCode, font);
	}

	public override void Restart()
	{
		hotkeysPressed.Clear();
		deferredActionTrigger = EnumEntityAction.None;
		deferredActionPreReq = EnumEntityAction.None;
	}

	public override void Skip()
	{
		foreach (string item in hotkeysToPress)
		{
			hotkeysPressed.Add(item);
		}
	}

	public override bool OnHotkeyPressed(string hotkeycode, KeyCombination keyComb)
	{
		if (hotkeysToPress.Contains(hotkeycode) && !hotkeysPressed.Contains(hotkeycode))
		{
			hotkeysPressed.Add(hotkeycode);
			return true;
		}
		return false;
	}

	public override bool OnAction(EnumEntityAction action, bool on)
	{
		if (on)
		{
			activeActions.Add(action);
		}
		else
		{
			activeActions.Remove(action);
		}
		EnumEntityAction enumEntityAction = ((action != EnumEntityAction.Sprint) ? EnumEntityAction.None : EnumEntityAction.Forward);
		if (on && actionToHotkeyMapping.TryGetValue(action, out var value))
		{
			if (enumEntityAction != EnumEntityAction.None && !activeActions.Contains(enumEntityAction))
			{
				deferredActionTrigger = enumEntityAction;
				deferredActionPreReq = action;
				return false;
			}
			if (action == deferredActionTrigger && activeActions.Contains(deferredActionPreReq) && actionToHotkeyMapping.TryGetValue(deferredActionPreReq, out var value2))
			{
				deferredActionTrigger = EnumEntityAction.None;
				deferredActionPreReq = EnumEntityAction.None;
				bool flag = OnHotkeyPressed(value2, null);
				return OnHotkeyPressed(value, null) || flag;
			}
			return OnHotkeyPressed(value, null);
		}
		if (action == deferredActionPreReq && !on)
		{
			deferredActionTrigger = EnumEntityAction.None;
			deferredActionPreReq = EnumEntityAction.None;
		}
		return false;
	}

	public override void ToJson(JsonObject job)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Expected O, but got Unknown
		//IL_0026: Expected O, but got Unknown
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Expected O, but got Unknown
		if (hotkeysPressed.Count > 0)
		{
			JToken token = job.Token;
			string obj = code;
			JObject val = new JObject();
			JToken token2 = (JToken)val;
			token[(object)obj] = (JToken)val;
			JToken token3 = new JsonObject(token2).Token;
			object[] array = hotkeysPressed.ToArray();
			token3[(object)"pressed"] = (JToken)new JArray(array);
		}
	}

	public override void FromJson(JsonObject job)
	{
		JToken token = job[code]["pressed"].Token;
		JArray val = (JArray)(object)((token is JArray) ? token : null);
		if (val == null)
		{
			return;
		}
		hotkeysPressed.Clear();
		foreach (JToken item2 in val)
		{
			string item = (string)item2;
			hotkeysPressed.Add(item);
		}
	}
}
