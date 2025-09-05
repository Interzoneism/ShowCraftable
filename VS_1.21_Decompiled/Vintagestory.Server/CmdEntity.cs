using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.Server;

public class CmdEntity
{
	private ServerMain server;

	public CmdEntity(ServerMain server)
	{
		CmdEntity cmdEntity = this;
		this.server = server;
		IChatCommandApi chatCommands = server.api.ChatCommands;
		CommandArgumentParsers parsers = server.api.ChatCommands.Parsers;
		ServerCoreAPI sapi = server.api;
		chatCommands.GetOrCreate("entity").WithAlias("e").WithDesc("Entity control via entity selector")
			.RequiresPrivilege(Privilege.controlserver)
			.BeginSub("cmd")
			.WithDesc("Issue commands on existing entities")
			.WithArgs(parsers.Entities("target entities"))
			.BeginSub("stopanim")
			.WithDesc("Stop an entity animation")
			.WithArgs(parsers.Word("animation name"))
			.HandleWith((TextCommandCallingArgs args) => CmdUtil.EntityEach(args, delegate(Entity e)
			{
				e.StopAnimation((string)args.LastArg);
				return TextCommandResult.Success("animation stopped");
			}))
			.EndSub()
			.BeginSub("starttask")
			.WithDesc("Start an ai task")
			.WithArgs(parsers.Word("task id"))
			.HandleWith((TextCommandCallingArgs args) => CmdUtil.EntityEach(args, delegate(Entity e)
			{
				e.Notify("starttask", (string)args.LastArg);
				return TextCommandResult.Success("task start executed");
			}))
			.EndSub()
			.BeginSub("stoptask")
			.WithDesc("Stop an ai task")
			.WithArgs(parsers.Word("task id"))
			.HandleWith((TextCommandCallingArgs args) => CmdUtil.EntityEach(args, delegate(Entity e)
			{
				e.Notify("stoptask", (string)args.LastArg);
				return TextCommandResult.Success("task stop executed");
			}))
			.EndSub()
			.BeginSub("setattr")
			.WithDesc("Set entity attributes")
			.WithArgs(parsers.WordRange("datatype", "float", "int", "string", "bool"), parsers.Word("name"), parsers.Word("value"))
			.HandleWith((TextCommandCallingArgs args) => CmdUtil.EntityEach(args, (Entity e) => cmdEntity.entitySetAttr(e, args)))
			.EndSub()
			.BeginSub("attr")
			.WithDesc("Read entity attributes")
			.WithArgs(parsers.Word("name"))
			.HandleWith((TextCommandCallingArgs args) => CmdUtil.EntityEach(args, (Entity e) => cmdEntity.entityReadAttr(e, args)))
			.EndSub()
			.BeginSub("setgen")
			.WithDesc("Set entity generation")
			.WithArgs(parsers.Int("generation"))
			.HandleWith((TextCommandCallingArgs args) => CmdUtil.EntityEach(args, delegate(Entity e)
			{
				e.WatchedAttributes.SetInt("generation", (int)args[1]);
				return TextCommandResult.Success("generation set");
			}))
			.EndSub()
			.BeginSub("rmbh")
			.WithDesc("Remove behavior")
			.WithArgs(parsers.Word("behavior code"))
			.HandleWith((TextCommandCallingArgs args) => CmdUtil.EntityEach(args, delegate(Entity e)
			{
				EntityBehavior behavior = e.GetBehavior((string)args[1]);
				if (behavior == null)
				{
					return TextCommandResult.Error("entity " + e.Code.ToShortString() + " has no such behavior");
				}
				e.RemoveBehavior(behavior);
				return TextCommandResult.Success("Ok, behavior removed");
			}))
			.EndSub()
			.BeginSub("setlact")
			.WithDesc("Set entity lactating")
			.HandleWith((TextCommandCallingArgs args) => CmdUtil.EntityEach(args, delegate(Entity e)
			{
				e.WatchedAttributes.GetTreeAttribute("multiply")?.SetDouble("totalDaysLastBirth", server.api.World.Calendar.TotalDays);
				e.WatchedAttributes.MarkPathDirty("multiply");
				return TextCommandResult.Success("Ok, entity lactating set");
			}))
			.EndSub()
			.BeginSub("move")
			.WithDesc("move a creature")
			.WithArgs(parsers.Double("delta x"), parsers.Double("delta y"), parsers.Double("delta z"))
			.HandleWith((TextCommandCallingArgs args) => CmdUtil.EntityEach(args, delegate(Entity e)
			{
				e.ServerPos.X += (double)args[1];
				e.ServerPos.Y += (double)args[2];
				e.ServerPos.Z += (double)args[3];
				return TextCommandResult.Success("Ok, entity moved");
			}))
			.EndSub()
			.BeginSub("kill")
			.WithDesc("kill a creature")
			.HandleWith((TextCommandCallingArgs args) => CmdUtil.EntityEach(args, delegate(Entity e)
			{
				if (e == args.Caller.Entity)
				{
					return TextCommandResult.Success("Ignoring killing of caller");
				}
				e.Die(EnumDespawnReason.Death, new DamageSource
				{
					Source = EnumDamageSource.Player,
					SourcePos = args.Caller.Pos,
					SourceEntity = args.Caller.Entity
				});
				return TextCommandResult.Success("Ok, entity killed");
			}))
			.EndSub()
			.BeginSub("birth")
			.WithDesc("force a creature to give birth (if it can!)")
			.WithArgs(parsers.OptionalInt("number"))
			.HandleWith((TextCommandCallingArgs args) => CmdUtil.EntityEach(args, delegate(Entity e)
			{
				EntityBehavior? behavior = e.GetBehavior("multiply");
				behavior?.TestCommand(args.Parsers[1].IsMissing ? ((object)1) : args[1]);
				return TextCommandResult.Success((behavior == null) ? (Lang.Get("item-creature-" + e.Code.Path) + " " + Lang.Get("can't bear young!")) : "OK!");
			}))
			.EndSub()
			.EndSub()
			.BeginSub("wipeall")
			.WithDesc("Removes all entities (except players) from all loaded chunks")
			.WithArgs(parsers.OptionalInt("killRadius"))
			.HandleWith(WipeAllHandler)
			.EndSub();
		chatCommands.GetOrCreate("entity").BeginSub("debug").WithDesc("Set entity debug mode")
			.WithArgs(parsers.Bool("on"))
			.HandleWith(delegate(TextCommandCallingArgs args)
			{
				server.Config.EntityDebugMode = (bool)args[0];
				server.ConfigNeedsSaving = true;
				return TextCommandResult.Success(Lang.Get("Ok, entity debug mode is now {0}", server.Config.EntityDebugMode ? Lang.Get("on") : Lang.Get("off")));
			})
			.EndSub()
			.BeginSub("spawndebug")
			.WithDesc("Set entity spawn debug mode")
			.WithArgs(parsers.Bool("on"))
			.HandleWith(delegate(TextCommandCallingArgs args)
			{
				server.SpawnDebug = (bool)args[0];
				return TextCommandResult.Success(Lang.Get("Ok, entity spawn debug mode is now {0}", server.SpawnDebug ? Lang.Get("on") : Lang.Get("off")));
			})
			.EndSub()
			.BeginSub("count")
			.WithDesc("Count entities by code/filter and show a summary")
			.WithArgs(parsers.OptionalEntities("entity filter"))
			.HandleWith((TextCommandCallingArgs args) => cmdEntity.Count(args, grouped: false))
			.EndSub()
			.BeginSub("locateg")
			.WithDesc("Group entities together within the specified range and returns the position and amount. This is to find large groups of entities.")
			.WithArgs(parsers.OptionalEntities("entity filter"), parsers.OptionalInt("range", 100))
			.HandleWith(OnLocate)
			.EndSub()
			.BeginSub("countg")
			.WithDesc("Count entities by code/filter and show a summary grouped by first code part")
			.WithArgs(parsers.OptionalEntities("entity filter"))
			.HandleWith((TextCommandCallingArgs args) => cmdEntity.Count(args, grouped: true))
			.EndSub()
			.BeginSub("spawn")
			.WithAlias("sp")
			.WithDesc("Spawn entities at the callers position")
			.WithArgs(parsers.EntityType("entity type"), parsers.Int("amount"))
			.HandleWith(spawnEntities)
			.EndSub()
			.BeginSub("spawnat")
			.WithDesc("Spawn entities at given position, within a given radius")
			.WithArgs(parsers.EntityType("entity type"), parsers.Int("amount"), parsers.WorldPosition("position"), parsers.Double("spawn radius"))
			.HandleWith(spawnEntitiesAt)
			.EndSub()
			.BeginSub("remove")
			.WithAlias("re")
			.WithDesc("remove selected creatures")
			.WithArgs(parsers.Entities("target entities"))
			.HandleWith((TextCommandCallingArgs args) => CmdUtil.EntityEach(args, delegate(Entity e)
			{
				if (e == args.Caller.Entity)
				{
					return TextCommandResult.Success("Ignoring removal of caller");
				}
				e.Die(EnumDespawnReason.Removed);
				return TextCommandResult.Success("Ok, entity removed");
			}))
			.EndSub()
			.BeginSub("removebyid")
			.WithDesc("remove selected creatures")
			.WithArgs(parsers.Long("id"))
			.HandleWith(delegate(TextCommandCallingArgs args)
			{
				long num = (long)args[0];
				if (num == args.Caller.Entity.EntityId)
				{
					return TextCommandResult.Success("Ignoring removal of caller");
				}
				if (sapi.World.LoadedEntities.TryGetValue(num, out var value))
				{
					value.Die(EnumDespawnReason.Removed);
					return TextCommandResult.Success("Ok, entity removed");
				}
				return TextCommandResult.Success("No entity found");
			})
			.EndSub()
			.BeginSub("set-angle")
			.WithAlias("sa")
			.WithDesc("Set the angle of the entity")
			.WithArgs(parsers.Entities("target entities"), parsers.WordRange("axis", "yaw", "pitch", "roll"), parsers.Float("degrees"))
			.HandleWith((TextCommandCallingArgs args) => CmdUtil.EntityEach(args, (Entity e) => cmdEntity.setEntityAngle(e, args)))
			.EndSub()
			.BeginSub("export")
			.WithDescription("Export a entity spawnat command to server-main log file")
			.WithArgs(parsers.Entities("target entities"))
			.HandleWith((TextCommandCallingArgs args) => CmdUtil.EntityEach(args, (Entity e) => cmdEntity.exportEntity(e, args)))
			.EndSub();
	}

	private TextCommandResult exportEntity(Entity entity, TextCommandCallingArgs args)
	{
		server.api.Logger.Notification($"/entity spawnat {entity.Code} 1 ={entity.ServerPos.X:F2} ={entity.ServerPos.Y:F2} ={entity.ServerPos.Z:F2} 0");
		return TextCommandResult.Success("Ok, entity exported");
	}

	private TextCommandResult spawnEntitiesAt(TextCommandCallingArgs args)
	{
		EntityProperties entityProperties = (EntityProperties)args[0];
		int num = (int)args[1];
		Vec3d vec3d = (Vec3d)args[2];
		double num2 = (double)args[3];
		Random rand = server.api.World.Rand;
		long nextHerdId = server.GetNextHerdId();
		int num3 = num;
		while (num3-- > 0)
		{
			Entity entity = server.api.ClassRegistry.CreateEntity(entityProperties);
			if (entity is EntityAgent)
			{
				(entity as EntityAgent).HerdId = nextHerdId;
			}
			entity.Pos.SetFrom(vec3d);
			entity.Pos.X += rand.NextDouble() * 2.0 * num2 - num2;
			entity.Pos.Z += rand.NextDouble() * 2.0 * num2 - num2;
			entity.Pos.Pitch = 0f;
			entity.Pos.Yaw = 0f;
			entity.ServerPos.SetFrom(entity.Pos);
			server.SpawnEntity(entity);
		}
		return TextCommandResult.Success(Lang.Get("{0}x{1} spawned.", num, entityProperties.Code));
	}

	private TextCommandResult spawnEntities(TextCommandCallingArgs args)
	{
		int num = (int)args[1];
		EntityProperties entityProperties = (EntityProperties)args[0];
		Random rand = server.api.World.Rand;
		long nextHerdId = server.GetNextHerdId();
		int num2 = num;
		while (num2-- > 0)
		{
			Entity entity = server.api.ClassRegistry.CreateEntity(entityProperties);
			if (entity is EntityAgent)
			{
				(entity as EntityAgent).HerdId = nextHerdId;
			}
			entity.Pos.SetFrom(args.Caller.Entity.Pos);
			entity.Pos.X += rand.NextDouble() / 10.0 - 0.05;
			entity.Pos.Z += rand.NextDouble() / 10.0 - 0.05;
			entity.Pos.Pitch = 0f;
			entity.Pos.Yaw = 0f;
			entity.Pos.Motion.Set((0.125 - 0.25 * rand.NextDouble()) / 2.0, (0.1 + 0.1 * rand.NextDouble()) / 2.0, (0.125 - 0.25 * rand.NextDouble()) / 2.0);
			entity.ServerPos.SetFrom(entity.Pos);
			server.SpawnEntity(entity);
		}
		return TextCommandResult.Success(Lang.Get("{0}x{1} spawned.", num, entityProperties.Code));
	}

	private TextCommandResult entitySetAttr(Entity entity, TextCommandCallingArgs args)
	{
		string text = (string)args[1];
		string text2 = (string)args[2];
		string text3 = (string)args[3];
		ITreeAttribute treeAttribute = entity.WatchedAttributes;
		string text4 = null;
		if (text2.Contains("/"))
		{
			string[] array = text2.Split('/');
			text2 = array[^1];
			string[] value = array.RemoveAt(array.Length - 1);
			text4 = string.Join("/", value);
			treeAttribute = entity.WatchedAttributes.GetAttributeByPath(text4) as ITreeAttribute;
			if (treeAttribute == null)
			{
				return TextCommandResult.Error(Lang.Get("No such path - {0}", text4), "nosuchpath");
			}
		}
		if (text4 != null)
		{
			entity.WatchedAttributes.MarkPathDirty(text4);
		}
		switch (text)
		{
		case "float":
		{
			float value4 = text3.ToFloat();
			treeAttribute.SetFloat(text2, value4);
			return TextCommandResult.Success(text2 + " float value set to " + value4);
		}
		case "int":
		{
			int value3 = text3.ToInt();
			treeAttribute.SetInt(text2, value3);
			return TextCommandResult.Success(text2 + " int value set to " + value3);
		}
		case "string":
		{
			string text5 = text3 + args.RawArgs.PopAll();
			treeAttribute.SetString(text2, text5);
			return TextCommandResult.Success(text2 + " string value set to " + text5);
		}
		case "bool":
		{
			bool value2 = text3.ToBool();
			treeAttribute.SetBool(text2, value2);
			return TextCommandResult.Success(text2 + " bool value set to " + value2);
		}
		default:
			return TextCommandResult.Error("Incorrect datatype, choose float, int, string or bool", "wrongdatatype");
		}
	}

	private TextCommandResult entityReadAttr(Entity entity, TextCommandCallingArgs args)
	{
		string text = (string)args[1];
		IAttribute attributeByPath = entity.WatchedAttributes.GetAttributeByPath(text);
		if (attributeByPath == null)
		{
			return TextCommandResult.Error(Lang.Get("No such path - {0}", text), "nosuchpath");
		}
		return TextCommandResult.Success(Lang.Get("Value is: {0}", attributeByPath.GetValue()));
	}

	private TextCommandResult setEntityAngle(Entity entity, TextCommandCallingArgs args)
	{
		string text = (string)args[1];
		float num = (float)args[2];
		switch (text)
		{
		case "yaw":
			entity.ServerPos.Yaw = (float)Math.PI / 180f * num;
			break;
		case "pitch":
			entity.ServerPos.Pitch = (float)Math.PI / 180f * num;
			break;
		case "roll":
			entity.ServerPos.Roll = (float)Math.PI / 180f * num;
			break;
		}
		return TextCommandResult.Success("Entity angle set");
	}

	private TextCommandResult OnLocate(TextCommandCallingArgs args)
	{
		Dictionary<BlockPos, List<Entity>> dictionary = new Dictionary<BlockPos, List<Entity>>();
		int num = (int)args[1];
		List<Entity> list = ((!args.Parsers[0].IsMissing) ? (args[0] as Entity[]).ToList() : server.LoadedEntities.Values.ToList());
		if (list.Count != 0)
		{
			Entity entity = list.First();
			dictionary.Add(entity.Pos.AsBlockPos, new List<Entity> { entity });
		}
		foreach (Entity item in list.Skip(1))
		{
			bool flag = false;
			foreach (var (blockPos2, _) in dictionary)
			{
				if (item.Pos.HorDistanceTo(blockPos2.ToVec3d()) < (double)num)
				{
					dictionary[blockPos2].Add(item);
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				dictionary.Add(item.Pos.AsBlockPos, new List<Entity> { item });
			}
		}
		string message;
		if (dictionary.Count == 0)
		{
			message = "No entities found";
		}
		else
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (KeyValuePair<BlockPos, List<Entity>> item2 in dictionary)
			{
				stringBuilder.AppendLine(item2.Key?.ToString() + " : " + item2.Value.Count);
			}
			message = stringBuilder.ToString();
		}
		return TextCommandResult.Success(message);
	}

	private TextCommandResult Count(TextCommandCallingArgs args, bool grouped)
	{
		int num = 0;
		int num2 = 0;
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		IEnumerable<Entity> enumerable = ((!args.Parsers[0].IsMissing) ? (args[0] as Entity[]) : server.LoadedEntities.Values);
		foreach (Entity item in enumerable)
		{
			string key = item.Code.Path;
			if (grouped)
			{
				key = item.FirstCodePart();
			}
			if (dictionary.ContainsKey(key))
			{
				dictionary[key]++;
			}
			else
			{
				dictionary[key] = 1;
			}
			if (item.State == EnumEntityState.Active)
			{
				num2++;
			}
			num++;
		}
		string message;
		if (dictionary.Count == 0)
		{
			message = "No entities found";
		}
		else
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine(Lang.Get("{0} total entities, of which {1} active.", num, num2));
			foreach (KeyValuePair<string, int> item2 in dictionary)
			{
				stringBuilder.AppendLine(item2.Key + ": " + item2.Value);
			}
			message = stringBuilder.ToString();
		}
		return TextCommandResult.Success(message);
	}

	private bool entityTypeMatches(EntityProperties type, EntityProperties referenceType, string searchCode, bool isWildcard)
	{
		if (isWildcard)
		{
			string text = Regex.Escape(searchCode).Replace("\\*", "(.*)");
			return Regex.IsMatch(type.Code.Path.ToLowerInvariant(), "^" + text + "$");
		}
		return type.Code.Path == referenceType.Code.Path;
	}

	private TextCommandResult WipeAllHandler(TextCommandCallingArgs args)
	{
		int num;
		if (args.Parsers[0].IsMissing)
		{
			num = 0;
		}
		else
		{
			num = (int)args[0];
			num *= num + 1;
		}
		int xInt = args.Caller.Pos.XInt;
		int zInt = args.Caller.Pos.ZInt;
		int num2 = 0;
		foreach (KeyValuePair<long, Entity> loadedEntity in server.LoadedEntities)
		{
			if (!(loadedEntity.Value is EntityPlayer) && (num <= 0 || loadedEntity.Value.Pos.InHorizontalRangeOf(xInt, zInt, num)))
			{
				loadedEntity.Value.Die(EnumDespawnReason.Removed);
				num2++;
			}
		}
		return TextCommandResult.Success("Killed " + num2 + " entities");
	}
}
