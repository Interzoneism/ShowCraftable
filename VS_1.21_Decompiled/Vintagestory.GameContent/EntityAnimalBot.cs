using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.Essentials;

namespace Vintagestory.GameContent;

public class EntityAnimalBot : EntityAgent
{
	public string Name;

	public List<INpcCommand> Commands = new List<INpcCommand>();

	public Queue<INpcCommand> ExecutingCommands = new Queue<INpcCommand>();

	protected bool commandQueueActive;

	public bool LoopCommands;

	public PathTraverserBase linepathTraverser;

	public PathTraverserBase wppathTraverser;

	public override bool StoreWithChunk => true;

	public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
	{
		base.Initialize(properties, api, InChunkIndex3d);
		linepathTraverser = new StraightLineTraverser(this);
		wppathTraverser = new WaypointsTraverser(this);
	}

	public void StartExecuteCommands(bool enqueue = true)
	{
		if (enqueue)
		{
			foreach (INpcCommand command in Commands)
			{
				ExecutingCommands.Enqueue(command);
			}
		}
		if (ExecutingCommands.Count > 0)
		{
			INpcCommand npcCommand = ExecutingCommands.Peek();
			npcCommand.Start();
			WatchedAttributes.SetString("currentCommand", npcCommand.Type);
		}
		commandQueueActive = true;
	}

	public void StopExecuteCommands()
	{
		if (ExecutingCommands.Count > 0)
		{
			ExecutingCommands.Peek().Stop();
		}
		ExecutingCommands.Clear();
		commandQueueActive = false;
		WatchedAttributes.SetString("currentCommand", "");
	}

	public override void OnGameTick(float dt)
	{
		base.OnGameTick(dt);
		linepathTraverser.OnGameTick(dt);
		wppathTraverser.OnGameTick(dt);
		if (commandQueueActive)
		{
			if (ExecutingCommands.Count > 0)
			{
				if (ExecutingCommands.Peek().IsFinished())
				{
					WatchedAttributes.SetString("currentCommand", "");
					ExecutingCommands.Dequeue();
					if (ExecutingCommands.Count > 0)
					{
						ExecutingCommands.Peek().Start();
					}
					else if (LoopCommands)
					{
						StartExecuteCommands();
					}
					else
					{
						commandQueueActive = false;
					}
				}
			}
			else if (LoopCommands)
			{
				StartExecuteCommands();
			}
			else
			{
				commandQueueActive = false;
			}
		}
		World.FrameProfiler.Mark("entityAnimalBot-pathfinder-and-commands");
	}

	public override void ToBytes(BinaryWriter writer, bool forClient)
	{
		if (!forClient)
		{
			ITreeAttribute treeAttribute = new TreeAttribute();
			WatchedAttributes["commandQueue"] = treeAttribute;
			ITreeAttribute treeAttribute2 = (ITreeAttribute)(treeAttribute["commands"] = new TreeAttribute());
			int num = 0;
			foreach (INpcCommand command in Commands)
			{
				ITreeAttribute treeAttribute3 = new TreeAttribute();
				command.ToAttribute(treeAttribute3);
				treeAttribute3.SetString("type", command.Type);
				treeAttribute2["cmd" + num] = treeAttribute3;
				num++;
			}
			WatchedAttributes.SetBool("loop", LoopCommands);
		}
		base.ToBytes(writer, forClient);
	}

	public override void FromBytes(BinaryReader reader, bool forClient)
	{
		base.FromBytes(reader, forClient);
		if (forClient)
		{
			return;
		}
		ITreeAttribute treeAttribute = WatchedAttributes.GetTreeAttribute("commandQueue");
		if (treeAttribute == null)
		{
			return;
		}
		ITreeAttribute treeAttribute2 = treeAttribute.GetTreeAttribute("commands");
		if (treeAttribute2 == null)
		{
			return;
		}
		foreach (KeyValuePair<string, IAttribute> item in treeAttribute2)
		{
			ITreeAttribute treeAttribute3 = item.Value as ITreeAttribute;
			string text = treeAttribute3.GetString("type");
			INpcCommand npcCommand = null;
			switch (text)
			{
			case "tp":
				npcCommand = new NpcTeleportCommand(this, null);
				break;
			case "goto":
				npcCommand = new NpcGotoCommand(this, null, astar: false, null, 0f);
				break;
			case "anim":
				npcCommand = new NpcPlayAnimationCommand(this, null, 1f);
				break;
			case "lookat":
				npcCommand = new NpcLookatCommand(this, 0f);
				break;
			}
			npcCommand.FromAttribute(treeAttribute3);
			Commands.Add(npcCommand);
		}
		LoopCommands = WatchedAttributes.GetBool("loop");
	}
}
