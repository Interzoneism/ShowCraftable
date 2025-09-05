using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public abstract class TutorialBase : ITutorial
{
	protected ICoreClientAPI capi;

	protected JsonObject stepData;

	protected List<TutorialStepBase> steps = new List<TutorialStepBase>();

	public int currentStep;

	public string pageCode;

	public JsonObject StepDataForSaving
	{
		get
		{
			//IL_0009: Unknown result type (might be due to invalid IL or missing references)
			//IL_0013: Expected O, but got Unknown
			if (stepData == null)
			{
				stepData = new JsonObject((JToken)new JObject());
			}
			return stepData;
		}
	}

	public string PageCode => pageCode;

	public bool Complete => steps[steps.Count - 1].Complete;

	public float Progress
	{
		get
		{
			if (steps.Count != 0)
			{
				return (float)steps.Sum((TutorialStepBase t) => t.Complete ? 1 : 0) / (float)steps.Count;
			}
			return 0f;
		}
	}

	protected TutorialBase(ICoreClientAPI capi, string pageCode)
	{
		this.capi = capi;
		this.pageCode = pageCode;
	}

	public void Restart()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Expected O, but got Unknown
		stepData = new JsonObject((JToken)new JObject());
		foreach (TutorialStepBase step in steps)
		{
			step.Restart();
			step.ToJson(StepDataForSaving);
		}
		Save();
	}

	public bool OnStateUpdate(ActionBoolReturn<TutorialStepBase> stepCall)
	{
		bool flag = false;
		bool flag2 = false;
		foreach (TutorialStepBase step in steps)
		{
			if (!step.Complete)
			{
				bool flag3 = stepCall(step);
				flag = flag || flag3;
				if (flag3)
				{
					step.ToJson(StepDataForSaving);
				}
				if (step.Complete)
				{
					flag2 = true;
				}
			}
		}
		if (flag2)
		{
			capi.Gui.PlaySound(new AssetLocation("sounds/tutorialstepsuccess.ogg"));
			Save();
		}
		return flag;
	}

	public void addSteps(params TutorialStepBase[] steps)
	{
		for (int i = 0; i < steps.Length; i++)
		{
			steps[i].index = i;
		}
		this.steps.AddRange(steps);
	}

	public List<TutorialStepBase> GetTutorialSteps(bool skipOld)
	{
		if (steps.Count == 0)
		{
			initTutorialSteps();
		}
		List<TutorialStepBase> list = new List<TutorialStepBase>();
		int num = 1;
		foreach (TutorialStepBase step in steps)
		{
			if (num <= 0)
			{
				break;
			}
			if (stepData != null)
			{
				step.FromJson(stepData);
			}
			list.Add(step);
			if (!step.Complete)
			{
				num--;
			}
		}
		if (skipOld)
		{
			while (list.Count > 1 && list[0].Complete)
			{
				list.RemoveAt(0);
			}
		}
		return list;
	}

	protected abstract void initTutorialSteps();

	public void Skip(int cnt)
	{
		while (cnt-- > 0)
		{
			TutorialStepBase tutorialStepBase = steps.FirstOrDefault((TutorialStepBase s) => !s.Complete);
			if (tutorialStepBase != null)
			{
				tutorialStepBase.Skip();
				tutorialStepBase.ToJson(StepDataForSaving);
			}
		}
		capi.Gui.PlaySound(new AssetLocation("sounds/tutorialstepsuccess.ogg"));
	}

	public void Save()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Expected O, but got Unknown
		JsonObject jsonObject = new JsonObject((JToken)new JObject());
		foreach (TutorialStepBase step in steps)
		{
			step.ToJson(jsonObject);
		}
		capi.StoreModConfig(jsonObject, "tutorial-" + PageCode + ".json");
	}

	public void Load()
	{
		try
		{
			stepData = capi.LoadModConfig("tutorial-" + PageCode + ".json");
		}
		catch (Exception e)
		{
			capi.Logger.Error("Failed to load tutorial-" + PageCode + ".json, the tutorial will be reset.");
			capi.Logger.Error(e);
		}
		if (stepData == null)
		{
			return;
		}
		foreach (TutorialStepBase step in steps)
		{
			step.FromJson(stepData);
		}
	}
}
