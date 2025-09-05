using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Systems;

namespace Vintagestory.GameContent;

public class EntityBehaviorConversable : EntityBehavior
{
	public static int BeginConvoPacketId = 1213;

	public static int SelectAnswerPacketId = 1214;

	public static int CloseConvoPacketId = 1215;

	public Dictionary<string, DialogueController> ControllerByPlayer = new Dictionary<string, DialogueController>();

	public GuiDialogueDialog Dialog;

	private EntityTalkUtil talkUtilInst;

	private IWorldAccessor world;

	private EntityAgent eagent;

	private DialogueConfig dialogue;

	private AssetLocation dialogueLoc;

	private bool approachPlayer;

	public Action<DialogueController> OnControllerCreated;

	private EntityBehaviorActivityDriven bhActivityDriven;

	private AiTaskGotoEntity gototask;

	private float gotoaccum;

	public const float BeginTalkRangeSq = 9f;

	public const float ApproachRangeSq = 16f;

	public const float StopTalkRangeSq = 25f;

	public string[] remainStationaryAnimations = new string[9] { "sit-idle", "sit-write", "sit-tinker", "sitfloor", "sitedge", "sitchair", "sitchairtable", "eatsittable", "bowl-eatsittable" };

	public EntityTalkUtil TalkUtil
	{
		get
		{
			if (!(entity is ITalkUtil talkUtil))
			{
				return talkUtilInst;
			}
			return talkUtil.TalkUtil;
		}
	}

	public event CanConverseDelegate CanConverse;

	public DialogueController GetOrCreateController(EntityPlayer player)
	{
		if (player == null)
		{
			return null;
		}
		DialogueComponent[] components;
		if (ControllerByPlayer.TryGetValue(player.PlayerUID, out var value))
		{
			components = dialogue.components;
			for (int i = 0; i < components.Length; i++)
			{
				components[i].SetReferences(value, Dialog);
			}
			return value;
		}
		dialogue = loadDialogue(dialogueLoc, player);
		if (dialogue == null)
		{
			return null;
		}
		DialogueController dialogueController = (ControllerByPlayer[player.PlayerUID] = new DialogueController(world.Api, player, entity as EntityAgent, dialogue));
		value = dialogueController;
		value.DialogTriggers += Controller_DialogTriggers;
		OnControllerCreated?.Invoke(value);
		components = dialogue.components;
		for (int i = 0; i < components.Length; i++)
		{
			components[i].SetReferences(value, Dialog);
		}
		return value;
	}

	private int Controller_DialogTriggers(EntityAgent triggeringEntity, string value, JsonObject data)
	{
		if (value == "closedialogue")
		{
			Dialog?.TryClose();
		}
		if (value == "playanimation")
		{
			entity.AnimManager.StartAnimation(data.AsObject<AnimationMetaData>());
		}
		if (value == "giveitemstack" && entity.World.Side == EnumAppSide.Server)
		{
			JsonItemStack jsonItemStack = data.AsObject<JsonItemStack>();
			jsonItemStack.Resolve(entity.World, "conversable giveitem trigger");
			ItemStack resolvedItemstack = jsonItemStack.ResolvedItemstack;
			int stackSize = resolvedItemstack.StackSize;
			if (!triggeringEntity.TryGiveItemStack(resolvedItemstack))
			{
				entity.World.SpawnItemEntity(resolvedItemstack, triggeringEntity.Pos.XYZ);
			}
			entity.Api.World.Logger.Audit("{0} Got from {1} {2}x{3} at {4}.", (triggeringEntity is EntityPlayer entityPlayer) ? entityPlayer.Player.PlayerName : entity.GetName(), entity.GetName(), stackSize, resolvedItemstack.Collectible.Code, entity.SidedPos.AsBlockPos);
		}
		if (value == "spawnentity" && entity.World.Side == EnumAppSide.Server)
		{
			DlgSpawnEntityConfig dlgSpawnEntityConfig = data.AsObject<DlgSpawnEntityConfig>();
			float num = 0f;
			for (int i = 0; i < dlgSpawnEntityConfig.Codes.Length; i++)
			{
				num += dlgSpawnEntityConfig.Codes[i].Weight;
			}
			double num2 = entity.World.Rand.NextDouble() * (double)num;
			for (int j = 0; j < dlgSpawnEntityConfig.Codes.Length; j++)
			{
				if ((num2 -= (double)dlgSpawnEntityConfig.Codes[j].Weight) <= 0.0)
				{
					TrySpawnEntity((triggeringEntity as EntityPlayer)?.Player, dlgSpawnEntityConfig.Codes[j].Code, dlgSpawnEntityConfig.Range, dlgSpawnEntityConfig);
					break;
				}
			}
		}
		if (value == "takefrominventory" && entity.World.Side == EnumAppSide.Server)
		{
			JsonItemStack jsonItemStack2 = data.AsObject<JsonItemStack>();
			jsonItemStack2.Resolve(entity.World, "conversable giveitem trigger");
			ItemStack resolvedItemstack2 = jsonItemStack2.ResolvedItemstack;
			ItemSlot itemSlot = DialogueComponent.FindDesiredItem(triggeringEntity, resolvedItemstack2);
			if (itemSlot != null)
			{
				itemSlot.TakeOut(jsonItemStack2.Quantity);
				itemSlot.MarkDirty();
				entity.Api.World.Logger.Audit("{0} Gave to {1} {2}x{3} at {4}.", (triggeringEntity is EntityPlayer entityPlayer2) ? entityPlayer2.Player.PlayerName : entity.GetName(), entity.GetName(), jsonItemStack2.Quantity, jsonItemStack2.Code, entity.SidedPos.AsBlockPos);
			}
		}
		if ((value == "repairheldtool" || value == "repairheldarmor") && entity.World.Side == EnumAppSide.Server)
		{
			ItemSlot rightHandItemSlot = triggeringEntity.RightHandItemSlot;
			if (!rightHandItemSlot.Empty)
			{
				ItemRepairConfig itemRepairConfig = data.AsObject<ItemRepairConfig>();
				int remainingDurability = rightHandItemSlot.Itemstack.Collectible.GetRemainingDurability(rightHandItemSlot.Itemstack);
				int maxDurability = rightHandItemSlot.Itemstack.Collectible.GetMaxDurability(rightHandItemSlot.Itemstack);
				if (((value == "repairheldtool") ? rightHandItemSlot.Itemstack.Collectible.Tool.HasValue : (rightHandItemSlot.Itemstack.Collectible.FirstCodePart() == "armor")) && remainingDurability < maxDurability)
				{
					rightHandItemSlot.Itemstack.Collectible.SetDurability(rightHandItemSlot.Itemstack, Math.Min(maxDurability, remainingDurability + itemRepairConfig.Amount));
					rightHandItemSlot.MarkDirty();
				}
			}
		}
		if (value == "attack")
		{
			EnumDamageType type = (EnumDamageType)Enum.Parse(typeof(EnumDamageType), data["type"].AsString("BluntAttack"));
			triggeringEntity.ReceiveDamage(new DamageSource
			{
				Source = EnumDamageSource.Entity,
				SourceEntity = entity,
				Type = type
			}, data["damage"].AsInt());
		}
		if (value == "revealname")
		{
			IPlayer player = (triggeringEntity as EntityPlayer)?.Player;
			if (player != null)
			{
				string text = data["selector"].ToString();
				if (text != null && text.StartsWith("e["))
				{
					EntitiesArgParser entitiesArgParser = new EntitiesArgParser("test", world.Api, isMandatoryArg: true);
					TextCommandCallingArgs textCommandCallingArgs = new TextCommandCallingArgs();
					textCommandCallingArgs.Caller = new Caller
					{
						Type = EnumCallerType.Console,
						CallerRole = "admin",
						CallerPrivileges = new string[1] { "*" },
						FromChatGroupId = GlobalConstants.ConsoleGroup,
						Pos = new Vec3d(0.5, 0.5, 0.5)
					};
					textCommandCallingArgs.RawArgs = new CmdArgs(text);
					TextCommandCallingArgs args = textCommandCallingArgs;
					if (entitiesArgParser.TryProcess(args) == EnumParseResult.Good)
					{
						Entity[] array = (Entity[])entitiesArgParser.GetValue();
						for (int k = 0; k < array.Length; k++)
						{
							array[k].GetBehavior<EntityBehaviorNameTag>().SetNameRevealedFor(player.PlayerUID);
						}
					}
					else
					{
						world.Logger.Warning("Conversable trigger: Unable to reveal name, invalid selector - " + text);
					}
				}
				else
				{
					entity.GetBehavior<EntityBehaviorNameTag>().SetNameRevealedFor(player.PlayerUID);
				}
			}
		}
		if (value == "unlockdoor" && triggeringEntity is EntityPlayer player2)
		{
			string doorCode = data["doorcode"].AsString();
			world.Api.ModLoader.GetModSystem<StoryLockableDoor>().Add(doorCode, player2);
		}
		return -1;
	}

	private void TrySpawnEntity(IPlayer forplayer, string entityCode, float range, DlgSpawnEntityConfig cfg)
	{
		EntityProperties entityType = entity.World.GetEntityType(AssetLocation.Create(entityCode, entity.Code.Domain));
		if (entityType == null)
		{
			entity.World.Logger.Warning("Dialogue system, unable to spawn {0}, no such entity exists", entityCode);
			return;
		}
		EntityPos serverPos = entity.ServerPos;
		BlockPos asBlockPos = serverPos.Copy().Add(0f - range, 0.0, 0f - range).AsBlockPos;
		BlockPos asBlockPos2 = serverPos.Copy().Add(range, 0.0, range).AsBlockPos;
		Vec3d vec3d = findSpawnPos(forplayer, entityType, asBlockPos, asBlockPos2, rainheightmap: false, 4);
		if (vec3d == null)
		{
			vec3d = findSpawnPos(forplayer, entityType, asBlockPos, asBlockPos2, rainheightmap: true, 1);
		}
		if (vec3d == null)
		{
			vec3d = findSpawnPos(forplayer, entityType, asBlockPos, asBlockPos2, rainheightmap: true, 1);
		}
		if (!(vec3d != null))
		{
			return;
		}
		Entity spawnentity = entity.Api.ClassRegistry.CreateEntity(entityType);
		spawnentity.ServerPos.SetPos(vec3d);
		entity.World.SpawnEntity(spawnentity);
		if (cfg.GiveStacks == null)
		{
			return;
		}
		JsonItemStack[] giveStacks = cfg.GiveStacks;
		foreach (JsonItemStack stack in giveStacks)
		{
			if (stack.Resolve(entity.World, "spawn entity give stack"))
			{
				entity.Api.Event.EnqueueMainThreadTask(delegate
				{
					spawnentity.TryGiveItemStack(stack.ResolvedItemstack.Clone());
				}, "tradedlggivestack");
			}
		}
	}

	private Vec3d findSpawnPos(IPlayer forplayer, EntityProperties etype, BlockPos minpos, BlockPos maxpos, bool rainheightmap, int mindistance)
	{
		bool spawned = false;
		BlockPos tmp = new BlockPos();
		IBlockAccessor ba = entity.World.BlockAccessor;
		CollisionTester collisionTester = entity.World.CollisionTester;
		ICoreServerAPI sapi = entity.Api as ICoreServerAPI;
		Vec3d okspawnpos = null;
		Vec3d epos = entity.ServerPos.XYZ;
		ba.WalkBlocks(minpos, maxpos, delegate(Block block, int x, int y, int z)
		{
			if (!spawned && !(epos.DistanceTo(x, y, z) < (float)mindistance))
			{
				int num = z % 32;
				int num2 = x % 32;
				IMapChunk mapChunkAtBlockPos = ba.GetMapChunkAtBlockPos(tmp.Set(x, y, z));
				int num3 = (rainheightmap ? mapChunkAtBlockPos.RainHeightMap[num * 32 + num2] : (mapChunkAtBlockPos.WorldGenTerrainHeightMap[num * 32 + num2] + 1));
				Vec3d vec3d = new Vec3d((double)x + 0.5, (double)num3 + 0.1, (double)z + 0.5);
				Cuboidf entityBoxRel = etype.SpawnCollisionBox.OmniNotDownGrowBy(0.1f);
				if (!collisionTester.IsColliding(ba, entityBoxRel, vec3d, alsoCheckTouch: false) && sapi.World.Claims.TestAccess(forplayer, vec3d.AsBlockPos, EnumBlockAccessFlags.BuildOrBreak) == EnumWorldAccessResponse.Granted)
				{
					spawned = true;
					okspawnpos = vec3d;
				}
			}
		}, centerOrder: true);
		return okspawnpos;
	}

	public EntityBehaviorConversable(Entity entity)
		: base(entity)
	{
		world = entity.World;
		eagent = entity as EntityAgent;
		if (world.Side == EnumAppSide.Client && !(entity is ITalkUtil))
		{
			talkUtilInst = new EntityTalkUtil(world.Api as ICoreClientAPI, entity, isMultiSoundVoice: false);
		}
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		base.Initialize(properties, attributes);
		approachPlayer = attributes["approachPlayer"].AsBool(defaultValue: true);
		string text = attributes["dialogue"].AsString();
		dialogueLoc = AssetLocation.Create(text, entity.Code.Domain);
		if (entity.World.Side == EnumAppSide.Server)
		{
			JsonObject[] behaviorsAsJsonObj = properties.Client.BehaviorsAsJsonObj;
			foreach (JsonObject jsonObject in behaviorsAsJsonObj)
			{
				if (jsonObject["code"].ToString() == attributes["code"].ToString() && text != jsonObject["dialogue"].AsString())
				{
					throw new InvalidOperationException(string.Format("Conversable behavior for entity {0}: You must define the same dialogue path on the client as well as the server side, currently they are set to {1} and {2}.", entity.Code, text, jsonObject["dialogue"].AsString()));
				}
			}
		}
		if (dialogueLoc == null)
		{
			world.Logger.Error(string.Concat("entity behavior conversable for entity ", entity.Code, ", dialogue path not set. Won't load dialogue."));
		}
	}

	public override void AfterInitialized(bool onFirstSpawn)
	{
		base.AfterInitialized(onFirstSpawn);
		bhActivityDriven = entity.GetBehavior<EntityBehaviorActivityDriven>();
	}

	public override void OnEntitySpawn()
	{
		setupTaskBlocker();
	}

	public override void OnEntityLoaded()
	{
		setupTaskBlocker();
	}

	private void setupTaskBlocker()
	{
		if (entity.Api.Side != EnumAppSide.Server)
		{
			return;
		}
		EntityBehaviorTaskAI behavior = entity.GetBehavior<EntityBehaviorTaskAI>();
		if (behavior != null)
		{
			behavior.TaskManager.OnShouldExecuteTask += (IAiTask task) => ControllerByPlayer.Count == 0 || task is AiTaskIdle || task is AiTaskSeekEntity || task is AiTaskGotoEntity;
		}
		EntityBehaviorActivityDriven behavior2 = entity.GetBehavior<EntityBehaviorActivityDriven>();
		if (behavior2 != null)
		{
			behavior2.OnShouldRunActivitySystem += () => (ControllerByPlayer.Count != 0 || gototask != null) ? EnumInteruptionType.BeingTalkedTo : EnumInteruptionType.None;
		}
	}

	private DialogueConfig loadDialogue(AssetLocation loc, EntityPlayer forPlayer)
	{
		string value = forPlayer.WatchedAttributes.GetString("characterClass");
		string text = entity.WatchedAttributes.GetString("personality");
		IAsset asset = world.AssetManager.TryGet(loc.Clone().WithPathAppendixOnce($"-{text}-{value}.json"));
		if (asset == null)
		{
			asset = world.AssetManager.TryGet(loc.Clone().WithPathAppendixOnce("-" + text + ".json"));
		}
		if (asset == null)
		{
			asset = world.AssetManager.TryGet(loc.WithPathAppendixOnce(".json"));
		}
		if (asset == null)
		{
			world.Logger.Error(string.Concat("Entitybehavior conversable for entity ", entity.Code, ", dialogue asset ", loc, " not found. Won't load dialogue."));
			return null;
		}
		try
		{
			DialogueConfig dialogueConfig = asset.ToObject<DialogueConfig>();
			dialogueConfig.Init();
			return dialogueConfig;
		}
		catch (Exception e)
		{
			world.Logger.Error("Entitybehavior conversable for entity {0}, dialogue asset is invalid:", entity.Code);
			world.Logger.Error(e);
			return null;
		}
	}

	public override string PropertyName()
	{
		return "conversable";
	}

	public override void OnGameTick(float deltaTime)
	{
		if (gototask != null)
		{
			gotoaccum += deltaTime;
			if (gototask.TargetReached())
			{
				IServerPlayer serverPlayer = (gototask.targetEntity as EntityPlayer)?.Player as IServerPlayer;
				ICoreServerAPI coreServerAPI = entity.World.Api as ICoreServerAPI;
				if (serverPlayer != null && serverPlayer.ConnectionState == EnumClientState.Playing)
				{
					AiTaskLookAtEntity aiTaskLookAtEntity = new AiTaskLookAtEntity(eagent, JsonObject.FromJson("{}"), JsonObject.FromJson("{}"));
					aiTaskLookAtEntity.manualExecute = true;
					aiTaskLookAtEntity.targetEntity = gototask.targetEntity;
					(entity.GetBehavior<EntityBehaviorTaskAI>()?.TaskManager).ExecuteTask(aiTaskLookAtEntity, 1);
					coreServerAPI.Network.SendEntityPacket(serverPlayer, entity.EntityId, BeginConvoPacketId);
					beginConvoServer(serverPlayer);
				}
				gototask = null;
			}
			AiTaskGotoEntity aiTaskGotoEntity = gototask;
			if ((aiTaskGotoEntity != null && aiTaskGotoEntity.Finished) || gotoaccum > 3f)
			{
				gototask = null;
			}
		}
		foreach (KeyValuePair<string, DialogueController> item in ControllerByPlayer)
		{
			IPlayer player = world.PlayerByUid(item.Key);
			EntityPlayer entityPlayer = player.Entity;
			if (!entityPlayer.Alive || entityPlayer.Pos.SquareDistanceTo(entity.Pos) > 25f)
			{
				ControllerByPlayer.Remove(item.Key);
				if (world.Api is ICoreServerAPI coreServerAPI2)
				{
					coreServerAPI2.Network.SendEntityPacket(player as IServerPlayer, entity.EntityId, CloseConvoPacketId);
				}
				else
				{
					Dialog?.TryClose();
				}
				break;
			}
		}
	}

	public override void OnInteract(EntityAgent byEntity, ItemSlot slot, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled)
	{
		if (mode != EnumInteractMode.Interact || !(byEntity is EntityPlayer))
		{
			handled = EnumHandling.PassThrough;
		}
		else
		{
			if (!entity.Alive)
			{
				return;
			}
			if (this.CanConverse != null)
			{
				Delegate[] invocationList = this.CanConverse.GetInvocationList();
				for (int i = 0; i < invocationList.Length; i++)
				{
					if (!((CanConverseDelegate)invocationList[i])(out var errorMessage))
					{
						((byEntity as EntityPlayer)?.Player as IServerPlayer)?.SendIngameError("cantconverse", Lang.Get(errorMessage));
						return;
					}
				}
			}
			GetOrCreateController(byEntity as EntityPlayer);
			handled = EnumHandling.PreventDefault;
			EntityPlayer entityPlayer = byEntity as EntityPlayer;
			world.PlayerByUid(entityPlayer.PlayerUID);
			if (world.Side == EnumAppSide.Client)
			{
				_ = (ICoreClientAPI)world.Api;
				if (entityPlayer.Pos.SquareDistanceTo(entity.Pos) <= 9f)
				{
					GuiDialogueDialog dialog = Dialog;
					if (dialog == null || !dialog.IsOpened())
					{
						beginConvoClient();
					}
				}
				TalkUtil.Talk(EnumTalkType.Meet);
			}
			if (world.Side != EnumAppSide.Server || gototask != null || !(byEntity.Pos.SquareDistanceTo(entity.Pos) <= 16f) || remainStationaryOnCall())
			{
				return;
			}
			AiTaskManager aiTaskManager = entity.GetBehavior<EntityBehaviorTaskAI>()?.TaskManager;
			if (aiTaskManager != null)
			{
				aiTaskManager.StopTask(typeof(AiTaskWander));
				gototask = new AiTaskGotoEntity(eagent, entityPlayer);
				gototask.allowedExtraDistance = 1f;
				if (gototask.TargetReached() || !approachPlayer)
				{
					gotoaccum = 0f;
					gototask = null;
					AiTaskLookAtEntity aiTaskLookAtEntity = new AiTaskLookAtEntity(eagent, JsonObject.FromJson("{}"), JsonObject.FromJson("{}"));
					aiTaskLookAtEntity.manualExecute = true;
					aiTaskLookAtEntity.targetEntity = entityPlayer;
					aiTaskManager.ExecuteTask(aiTaskLookAtEntity, 1);
				}
				else
				{
					aiTaskManager.ExecuteTask(gototask, 1);
					bhActivityDriven?.ActivitySystem.Pause(EnumInteruptionType.AskedToCome);
				}
				entity.AnimManager.TryStartAnimation(new AnimationMetaData
				{
					Animation = "welcome",
					Code = "welcome",
					Weight = 10f,
					EaseOutSpeed = 10000f,
					EaseInSpeed = 10000f
				});
				entity.AnimManager.StopAnimation("idle");
			}
		}
	}

	private bool remainStationaryOnCall()
	{
		EntityAgent entityAgent = entity as EntityAgent;
		if (entityAgent == null || entityAgent.MountedOn == null || !(entityAgent.MountedOn is BlockEntityBed))
		{
			return entityAgent.AnimManager.IsAnimationActive(remainStationaryAnimations);
		}
		return true;
	}

	private bool beginConvoClient()
	{
		ICoreClientAPI coreClientAPI = entity.World.Api as ICoreClientAPI;
		EntityPlayer player = coreClientAPI.World.Player.Entity;
		if (coreClientAPI.Gui.OpenedGuis.FirstOrDefault((GuiDialog dlg) => dlg is GuiDialogueDialog && dlg.IsOpened()) == null)
		{
			Dialog = new GuiDialogueDialog(coreClientAPI, eagent);
			Dialog.OnClosed += Dialog_OnClosed;
			DialogueController orCreateController = GetOrCreateController(player);
			if (orCreateController == null)
			{
				coreClientAPI.TriggerIngameError(this, "errord", Lang.Get("Error when loading dialogue. Check log files."));
				return false;
			}
			Dialog.InitAndOpen();
			orCreateController.ContinueExecute();
			coreClientAPI.Network.SendEntityPacket(entity.EntityId, BeginConvoPacketId);
			return true;
		}
		coreClientAPI.TriggerIngameError(this, "onlyonedialog", Lang.Get("Can only trade with one trader at a time"));
		return false;
	}

	private void Dialog_OnClosed()
	{
		ControllerByPlayer.Clear();
		Dialog = null;
		(world.Api as ICoreClientAPI).Network.SendEntityPacket(entity.EntityId, CloseConvoPacketId);
	}

	public override void OnReceivedClientPacket(IServerPlayer player, int packetid, byte[] data, ref EnumHandling handled)
	{
		base.OnReceivedClientPacket(player, packetid, data, ref handled);
		if (packetid == BeginConvoPacketId)
		{
			beginConvoServer(player);
		}
		if (packetid == SelectAnswerPacketId)
		{
			int id = SerializerUtil.Deserialize<int>(data);
			GetOrCreateController(player.Entity).PlayerSelectAnswerById(id);
		}
		if (packetid == CloseConvoPacketId)
		{
			ControllerByPlayer.Remove(player.PlayerUID);
		}
	}

	private void beginConvoServer(IServerPlayer player)
	{
		GetOrCreateController(player.Entity).ContinueExecute();
	}

	public override void OnReceivedServerPacket(int packetid, byte[] data, ref EnumHandling handled)
	{
		base.OnReceivedServerPacket(packetid, data, ref handled);
		if (packetid == BeginConvoPacketId)
		{
			ICoreClientAPI coreClientAPI = entity.World.Api as ICoreClientAPI;
			if (!(coreClientAPI.World.Player.Entity.Pos.SquareDistanceTo(entity.Pos) > 25f))
			{
				GuiDialogueDialog dialog = Dialog;
				if ((dialog == null || !dialog.IsOpened()) && beginConvoClient())
				{
					goto IL_008b;
				}
			}
			coreClientAPI.Network.SendEntityPacket(entity.EntityId, CloseConvoPacketId);
		}
		goto IL_008b;
		IL_008b:
		if (packetid == CloseConvoPacketId)
		{
			ControllerByPlayer.Clear();
			Dialog?.TryClose();
			Dialog = null;
		}
	}
}
