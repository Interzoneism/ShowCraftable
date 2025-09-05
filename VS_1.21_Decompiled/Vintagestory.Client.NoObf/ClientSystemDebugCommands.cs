using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common.Database;

namespace Vintagestory.Client.NoObf;

internal class ClientSystemDebugCommands : ClientSystem
{
	public override string Name => "debmc";

	private WireframeModes wfmodes => game.api.renderapi.WireframeDebugRender;

	public ClientSystemDebugCommands(ClientMain game)
		: base(game)
	{
		IChatCommandApi chatCommands = game.api.ChatCommands;
		CommandArgumentParsers parsers = game.api.ChatCommands.Parsers;
		chatCommands.GetOrCreate("debug").WithDescription("Debug and Developer utilities").RequiresPrivilege(Privilege.controlserver)
			.BeginSubCommand("clobjc")
			.WithDescription("clobjc")
			.HandleWith(OnCmdClobjc)
			.EndSubCommand()
			.BeginSubCommand("self")
			.WithDescription("self")
			.RequiresPrivilege(Privilege.chat)
			.HandleWith(OnCmdSelfDebugInfo)
			.EndSubCommand()
			.BeginSubCommand("talk")
			.WithDescription("talk")
			.WithArgs(parsers.OptionalWordRange("talk", Enum.GetNames<EnumTalkType>()))
			.HandleWith(OnCmdTalk)
			.EndSubCommand()
			.BeginSubCommand("normalview")
			.WithDescription("normalview")
			.HandleWith(OnCmdNormalview)
			.EndSubCommand()
			.BeginSubCommand("perceptioneffect")
			.WithAlias("pc")
			.WithDescription("perceptioneffect")
			.WithArgs(parsers.OptionalWord("effectname"), parsers.OptionalFloat("intensity", 1f))
			.HandleWith(OnCmdPerceptioneffect)
			.EndSubCommand()
			.BeginSubCommand("debdc")
			.WithDescription("debdc")
			.HandleWith(OnCmdDebdc)
			.EndSubCommand()
			.BeginSubCommand("tofb")
			.WithDescription("tofb")
			.WithArgs(parsers.OptionalBool("enable"))
			.HandleWith(OnCmdTofb)
			.EndSubCommand()
			.BeginSubCommand("cmr")
			.WithDescription("cmr")
			.HandleWith(OnCmdCmr)
			.EndSubCommand()
			.BeginSubCommand("us")
			.WithDescription("us")
			.HandleWith(OnCmdUs)
			.EndSubCommand()
			.BeginSubCommand("gl")
			.WithDescription("gl")
			.WithArgs(parsers.OptionalBool("GlDebugMode"))
			.HandleWith(OnCmdGl)
			.EndSubCommand()
			.BeginSubCommand("plranims")
			.RequiresPrivilege(Privilege.chat)
			.WithDescription("plranims")
			.HandleWith(OnCmdPlranims)
			.EndSubCommand()
			.BeginSubCommand("uiclick")
			.WithDescription("uiclick")
			.HandleWith(OnCmdUiclick)
			.EndSubCommand()
			.BeginSubCommand("discovery")
			.WithDescription("discovery")
			.WithArgs(parsers.All("text"))
			.HandleWith(OnCmdDiscovery)
			.EndSubCommand()
			.BeginSubCommand("soundsummary")
			.WithDescription("soundsummary")
			.HandleWith(OnCmdSoundsummary)
			.EndSubCommand()
			.BeginSubCommand("meshsummary")
			.RequiresPrivilege(Privilege.chat)
			.WithDescription("meshsummary")
			.HandleWith(OnCmdMeshsummary)
			.EndSubCommand()
			.BeginSubCommand("chunksummary")
			.RequiresPrivilege(Privilege.chat)
			.WithDescription("chunksummary")
			.HandleWith(OnCmdChunksummary)
			.EndSubCommand()
			.BeginSubCommand("logticks")
			.RequiresPrivilege(Privilege.chat)
			.WithDescription("logticks")
			.WithArgs(parsers.OptionalInt("ticksThreshold", 40))
			.HandleWith(OnCmdLogticks)
			.EndSubCommand()
			.BeginSubCommand("renderers")
			.RequiresPrivilege(Privilege.chat)
			.WithDescription("renderers")
			.WithArgs(parsers.OptionalBool("print"))
			.HandleWith(OnCmdRenderers)
			.EndSubCommand()
			.BeginSubCommand("exptexatlas")
			.WithDescription("exptexatlas")
			.WithArgs(parsers.OptionalWordRange("atlas", "block", "item", "entity"))
			.HandleWith(OnCmdExptexatlas)
			.EndSubCommand()
			.BeginSubCommand("liquidselectable")
			.WithDescription("liquidselectable")
			.WithArgs(parsers.OptionalBool("forceLiquidSelectable"))
			.HandleWith(OnCmdLiquidselectable)
			.EndSubCommand()
			.BeginSubCommand("relightchunk")
			.WithDescription("relightchunk")
			.HandleWith(OnCmdRelightchunk)
			.EndSubCommand()
			.BeginSubCommand("fog")
			.WithDescription("fog")
			.WithArgs(parsers.OptionalFloat("density"), parsers.OptionalFloat("min", 1f))
			.HandleWith(OnCmdFog)
			.EndSubCommand()
			.BeginSubCommand("fov")
			.WithDescription("fov")
			.WithArgs(parsers.OptionalInt("fov"))
			.HandleWith(OnCmdFov)
			.EndSubCommand()
			.BeginSubCommand("wgen")
			.WithDescription("wgen")
			.HandleWith(OnWgenCommand)
			.EndSubCommand()
			.BeginSubCommand("redrawall")
			.WithDescription("redrawall")
			.HandleWith(OnRedrawAll)
			.EndSubCommand()
			.BeginSubCommand("ci")
			.RequiresPrivilege(Privilege.chat)
			.WithDescription("ci")
			.HandleWith(OnChunkInfo)
			.EndSubCommand()
			.BeginSubCommand("plrattr")
			.WithDescription("plrattr")
			.WithArgs(parsers.All("path"))
			.HandleWith(OnCmdPlrattr)
			.EndSubCommand()
			.BeginSubCommand("crw")
			.WithDescription("crw")
			.HandleWith(OnCmdCrw)
			.EndSubCommand()
			.BeginSubCommand("shake")
			.WithDescription("shake")
			.WithArgs(parsers.OptionalFloat("strength", 0.5f))
			.HandleWith(OnCmdShake)
			.EndSubCommand()
			.BeginSubCommand("recalctrav")
			.WithDescription("recalctrav")
			.HandleWith(OnCmdRecalctrav)
			.EndSubCommand()
			.BeginSubCommand("wireframe")
			.WithDescription("View wireframes showing various game elements")
			.BeginSubCommand("scene")
			.WithDescription("GUI elements converted to wireframe triangles")
			.HandleWith(OnCmdScene)
			.EndSubCommand()
			.BeginSubCommand("ambsounds")
			.WithDescription("Show the current sources of ambient sounds")
			.HandleWith(OnCmdAmbsounds)
			.EndSubCommand()
			.BeginSubCommand("entity")
			.WithDescription("For every entity, the collision box (red) and selection box (blue)")
			.HandleWith(OnCmdEntity)
			.EndSubCommand()
			.BeginSubCommand("chunk")
			.RequiresPrivilege(Privilege.chat)
			.WithDescription("The boundaries of the current chunk")
			.HandleWith(OnCmdChunk)
			.EndSubCommand()
			.BeginSubCommand("inside")
			.RequiresPrivilege(Privilege.chat)
			.WithDescription("The block(s) the player is currently 'inside'")
			.HandleWith(OnCmdInside)
			.EndSubCommand()
			.BeginSubCommand("serverchunk")
			.RequiresPrivilege(Privilege.chat)
			.WithDescription("The boundaries of the current serverchunk")
			.HandleWith(OnCmdServerchunk)
			.EndSubCommand()
			.BeginSubCommand("region")
			.RequiresPrivilege(Privilege.chat)
			.WithDescription("The boundaries of the current MapRegion")
			.HandleWith(OnCmdRegion)
			.EndSubCommand()
			.BeginSubCommand("blockentity")
			.WithDescription("All the BlockEntities")
			.HandleWith(OnCmdBlockentity)
			.EndSubCommand()
			.BeginSubCommand("landclaim")
			.WithDescription("All the LandClaims in the current Map region")
			.HandleWith(OnCmdLandClaim)
			.EndSubCommand()
			.BeginSubCommand("structures")
			.WithDescription("All the Structures in the current mapregion")
			.HandleWith(OnCmdStructure)
			.EndSubCommand()
			.EndSubCommand()
			.BeginSubCommand("find")
			.WithDescription("find")
			.WithArgs(parsers.Word("searchString"))
			.HandleWith(OnCmdFind)
			.EndSubCommand()
			.BeginSubCommand("dumpanimstate")
			.WithDescription("Dump animation state into log file")
			.WithArgs(parsers.Entities("target entity"))
			.HandleWith((TextCommandCallingArgs args) => CmdUtil.EntityEach(args, (Entity e) => handleDumpAnimState(e, args)))
			.EndSubCommand();
	}

	private TextCommandResult handleDumpAnimState(Entity e, TextCommandCallingArgs args)
	{
		game.Logger.Notification(e.AnimManager?.Animator?.DumpCurrentState());
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdSelfDebugInfo(TextCommandCallingArgs args)
	{
		game.EntityPlayer.UpdateDebugAttributes();
		StringBuilder stringBuilder = new StringBuilder();
		foreach (KeyValuePair<string, IAttribute> debugAttribute in game.EntityPlayer.DebugAttributes)
		{
			stringBuilder.AppendLine(debugAttribute.Key + ": " + debugAttribute.Value.ToString());
		}
		return TextCommandResult.Success(stringBuilder.ToString());
	}

	private TextCommandResult OnCmdFind(TextCommandCallingArgs args)
	{
		if (game.EntityPlayer.Player.WorldData.CurrentGameMode == EnumGameMode.Creative)
		{
			if (args.Parsers[0].IsMissing)
			{
				return TextCommandResult.Success(Lang.Get("Specify all or part of the name of a block to find"));
			}
			game.FindCmd(args[0] as string);
			return TextCommandResult.Success();
		}
		return TextCommandResult.Success(Lang.Get("Need to be in Creative mode to use the command .debug find [blockname]"));
	}

	private TextCommandResult OnCmdBlockentity(TextCommandCallingArgs args)
	{
		return WireframeCommon(ref wfmodes.BlockEntity, "Block entity wireframes");
	}

	private TextCommandResult OnCmdStructure(TextCommandCallingArgs args)
	{
		return WireframeCommon(ref wfmodes.Structures, "Structure wireframes");
	}

	private TextCommandResult OnCmdRegion(TextCommandCallingArgs args)
	{
		return WireframeCommon(ref wfmodes.Region, "Region wireframe");
	}

	private TextCommandResult OnCmdLandClaim(TextCommandCallingArgs args)
	{
		return WireframeCommon(ref wfmodes.LandClaim, "Land claim wireframe");
	}

	private TextCommandResult OnCmdServerchunk(TextCommandCallingArgs args)
	{
		return WireframeCommon(ref wfmodes.ServerChunk, "Server chunk wireframe");
	}

	private TextCommandResult OnCmdChunk(TextCommandCallingArgs args)
	{
		return WireframeCommon(ref wfmodes.Chunk, "Chunk wireframe");
	}

	private TextCommandResult OnCmdEntity(TextCommandCallingArgs args)
	{
		return WireframeCommon(ref wfmodes.Entity, "Entity wireframes");
	}

	private TextCommandResult OnCmdInside(TextCommandCallingArgs args)
	{
		return WireframeCommon(ref wfmodes.Inside, "Inside block wireframe");
	}

	private TextCommandResult OnCmdAmbsounds(TextCommandCallingArgs args)
	{
		return WireframeCommon(ref wfmodes.AmbientSounds, "Ambient sounds wireframes");
	}

	private TextCommandResult WireframeCommon(ref bool toggle, string name)
	{
		toggle = !toggle;
		return TextCommandResult.Success(Lang.Get(name + " now {0}", toggle ? Lang.Get("on") : Lang.Get("off")));
	}

	private TextCommandResult OnCmdScene(TextCommandCallingArgs args)
	{
		game.Platform.GLWireframes(wfmodes.Vertex = !wfmodes.Vertex);
		return TextCommandResult.Success(Lang.Get("Scene wireframes now {0}", wfmodes.Vertex ? Lang.Get("on") : Lang.Get("off")));
	}

	private TextCommandResult OnCmdRecalctrav(TextCommandCallingArgs args)
	{
		foreach (KeyValuePair<long, ClientChunk> chunk in game.WorldMap.chunks)
		{
			ChunkPos item = game.WorldMap.ChunkPosFromChunkIndex3D(chunk.Key);
			if (item.Dimension == 0)
			{
				lock (game.chunkPositionsLock)
				{
					game.chunkPositionsForRegenTrav.Add(item);
				}
			}
		}
		return TextCommandResult.Success("Ok queued all chunks to recalc their traverseability");
	}

	private TextCommandResult OnCmdCrw(TextCommandCallingArgs args)
	{
		BlockPos asBlockPos = game.EntityPlayer.Pos.AsBlockPos;
		game.WorldMap.MarkChunkDirty(asBlockPos.X / 32, asBlockPos.Y / 32, asBlockPos.Z / 32);
		return TextCommandResult.Success("Ok, chunk marked dirty for redraw");
	}

	private TextCommandResult OnCmdPlrattr(TextCommandCallingArgs args)
	{
		string path = args[0] as string;
		IAttribute attributeByPath = game.EntityPlayer.WatchedAttributes.GetAttributeByPath(path);
		if (attributeByPath == null)
		{
			return TextCommandResult.Success("No such path found");
		}
		return TextCommandResult.Success(Lang.Get("Value is: {0}", attributeByPath.GetValue()));
	}

	private TextCommandResult OnCmdRenderers(TextCommandCallingArgs args)
	{
		if (game.eventManager == null)
		{
			return TextCommandResult.Error("Client already shutting down");
		}
		List<RenderHandler>[] renderersByStage = game.eventManager.renderersByStage;
		StringBuilder stringBuilder = new StringBuilder();
		bool flag = (bool)args[0];
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		for (int i = 0; i < renderersByStage.Length; i++)
		{
			EnumRenderStage enumRenderStage = (EnumRenderStage)i;
			stringBuilder.AppendLine(enumRenderStage.ToString() + ": " + renderersByStage[i].Count);
			if (!flag)
			{
				continue;
			}
			foreach (RenderHandler item in renderersByStage[i])
			{
				string key = item.Renderer.GetType()?.ToString() ?? "";
				if (dictionary.ContainsKey(key))
				{
					dictionary[key]++;
				}
				else
				{
					dictionary[key] = 1;
				}
			}
		}
		game.ShowChatMessage("Renderers:");
		game.ShowChatMessage(stringBuilder.ToString());
		if (flag)
		{
			game.Logger.Notification("Renderer summary:");
			foreach (KeyValuePair<string, int> item2 in dictionary)
			{
				game.Logger.Notification(item2.Value + "x " + item2.Key);
			}
			game.ShowChatMessage("Summary printed to client log file");
		}
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdLogticks(TextCommandCallingArgs args)
	{
		ScreenManager.FrameProfiler.PrintSlowTicks = !ScreenManager.FrameProfiler.PrintSlowTicks;
		ScreenManager.FrameProfiler.Enabled = ScreenManager.FrameProfiler.PrintSlowTicks;
		ScreenManager.FrameProfiler.PrintSlowTicksThreshold = (int)args[0];
		ScreenManager.FrameProfiler.Begin(null);
		game.ShowChatMessage("Client Tick Profiling now " + (ScreenManager.FrameProfiler.PrintSlowTicks ? ("on, threshold " + ScreenManager.FrameProfiler.PrintSlowTicksThreshold + " ms") : "off"));
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdChunksummary(TextCommandCallingArgs args)
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		foreach (KeyValuePair<long, ClientChunk> chunk in game.WorldMap.chunks)
		{
			num++;
			if (chunk.Value.IsPacked())
			{
				num2++;
			}
			if (chunk.Value.Empty)
			{
				num4++;
			}
			else
			{
				num3++;
			}
		}
		game.ShowChatMessage($"{num} Total chunks ({num3} with data and {num4} empty)\n{num2} of which are packed");
		ClientChunkDataPool chunkDataPool = game.WorldMap.chunkDataPool;
		game.ShowChatMessage($"Free pool objects {chunkDataPool.CountFree()}");
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdMeshsummary(TextCommandCallingArgs args)
	{
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		game.Logger.Debug("==== Mesh summary ====");
		int num = 0;
		MeshData[] blockModelDatas = game.TesselatorManager.blockModelDatas;
		for (int i = 0; i < blockModelDatas.Length; i++)
		{
			MeshData meshData = blockModelDatas[i];
			if (meshData == null)
			{
				continue;
			}
			num++;
			Block block = game.Blocks[i];
			int num2 = meshData.SizeInBytes();
			dictionary.TryGetValue(block.FirstCodePart(), out var value);
			value += num2;
			MeshData[] array = game.TesselatorManager.altblockModelDatasLod1[i];
			int num3 = 0;
			while (array != null && num3 < array.Length)
			{
				MeshData meshData2 = array[num3];
				if (meshData2 != null)
				{
					value += meshData2.SizeInBytes();
				}
				num3++;
			}
			MeshData[][] altblockModelDatasLod = game.TesselatorManager.altblockModelDatasLod0;
			array = ((altblockModelDatasLod != null) ? altblockModelDatasLod[i] : null);
			int num4 = 0;
			while (array != null && num4 < array.Length)
			{
				MeshData meshData3 = array[num4];
				if (meshData3 != null)
				{
					value += meshData3.SizeInBytes();
				}
				num4++;
			}
			MeshData[][] altblockModelDatasLod2 = game.TesselatorManager.altblockModelDatasLod2;
			array = ((altblockModelDatasLod2 != null) ? altblockModelDatasLod2[i] : null);
			int num5 = 0;
			while (array != null && num5 < array.Length)
			{
				MeshData meshData4 = array[num5];
				if (meshData4 != null)
				{
					value += meshData4.SizeInBytes();
				}
				num5++;
			}
			dictionary[block.FirstCodePart()] = value;
		}
		int num6 = 0;
		foreach (KeyValuePair<string, int> item in dictionary)
		{
			int num7 = item.Value / 1024;
			num6 += num7;
			if (num7 > 99)
			{
				game.Logger.Debug("{0}: {1} kB", item.Key, num7);
			}
		}
		string text = $"{num} of {blockModelDatas.Length} meshes loaded, using {num6} kB";
		game.Logger.Debug("   " + text);
		return TextCommandResult.Success(text);
	}

	private TextCommandResult OnCmdSoundsummary(TextCommandCallingArgs args)
	{
		int num = 0;
		int count = ScreenManager.soundAudioData.Count;
		int num2 = 0;
		game.Logger.Debug("==== Sound summary ====");
		foreach (KeyValuePair<AssetLocation, AudioData> soundAudioDatum in ScreenManager.soundAudioData)
		{
			if (soundAudioDatum.Value.Loaded > 1)
			{
				num++;
				int num3 = (soundAudioDatum.Value as AudioMetaData).Pcm.Length / 1024;
				num2 += num3;
				if (num3 > 99)
				{
					game.Logger.Debug("{0}: {1} kB", soundAudioDatum.Key, num3);
				}
			}
		}
		string text = $"{num} of {count} sounds loaded, using {num2} kB";
		game.Logger.Debug("   " + text);
		return TextCommandResult.Success(text);
	}

	private TextCommandResult OnCmdDiscovery(TextCommandCallingArgs args)
	{
		string text = args[0] as string;
		game.eventManager?.TriggerIngameDiscovery(this, "no", text);
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdUiclick(TextCommandCallingArgs args)
	{
		GuiManager.DEBUG_PRINT_INTERACTIONS = !GuiManager.DEBUG_PRINT_INTERACTIONS;
		return TextCommandResult.Success("UI Debug pring interactions now " + (GuiManager.DEBUG_PRINT_INTERACTIONS ? "on" : "off"));
	}

	private TextCommandResult OnCmdPlranims(TextCommandCallingArgs args)
	{
		IAnimationManager animManager = game.player.Entity.AnimManager;
		string text = "";
		int num = 0;
		foreach (string key in animManager.ActiveAnimationsByAnimCode.Keys)
		{
			if (num++ > 0)
			{
				text += ",";
			}
			text += key;
		}
		num = 0;
		StringBuilder stringBuilder = new StringBuilder();
		RunningAnimation[] animations = animManager.Animator.Animations;
		foreach (RunningAnimation runningAnimation in animations)
		{
			if (runningAnimation.Active)
			{
				if (num++ > 0)
				{
					stringBuilder.Append(",");
				}
				stringBuilder.Append(runningAnimation.Animation.Code);
			}
		}
		game.ShowChatMessage("Active Animations: " + ((text.Length > 0) ? text : "-"));
		return TextCommandResult.Success("Running Animations: " + ((stringBuilder.Length > 0) ? stringBuilder.ToString() : "-"));
	}

	private TextCommandResult OnCmdGl(TextCommandCallingArgs args)
	{
		ScreenManager.Platform.GlDebugMode = (bool)args[0];
		return TextCommandResult.Success("OpenGL debug mode now " + (ScreenManager.Platform.GlDebugMode ? "on" : "off"));
	}

	private TextCommandResult OnCmdUs(TextCommandCallingArgs args)
	{
		game.unbindSamplers = !game.unbindSamplers;
		return TextCommandResult.Success("Unpind samplers mode now " + (game.unbindSamplers ? "on" : "off"));
	}

	private TextCommandResult OnCmdCmr(TextCommandCallingArgs args)
	{
		float[] colorMapRects = game.shUniforms.ColorMapRects4;
		for (int i = 0; i < colorMapRects.Length; i += 4)
		{
			game.Logger.Notification("x: {0}, y: {1}, w: {2}, h: {3}", colorMapRects[i], colorMapRects[i + 1], colorMapRects[i + 2], colorMapRects[i + 3]);
		}
		return TextCommandResult.Success("Color map rects printed to client-main.log");
	}

	private TextCommandResult OnCmdTofb(TextCommandCallingArgs args)
	{
		ScreenManager.Platform.ToggleOffscreenBuffer((bool)args[0]);
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdDebdc(TextCommandCallingArgs args)
	{
		ScreenManager.debugDrawCallNextFrame = true;
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdPerceptioneffect(TextCommandCallingArgs args)
	{
		PerceptionEffects perceptionEffects = game.api.Render.PerceptionEffects;
		if (args.Parsers[0].IsMissing)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("Missing effect name argument. Available: ");
			int num = 0;
			foreach (string registeredEffect in perceptionEffects.RegisteredEffects)
			{
				if (num > 0)
				{
					stringBuilder.Append(", ");
				}
				num++;
				stringBuilder.Append(registeredEffect);
			}
			return TextCommandResult.Success(stringBuilder.ToString());
		}
		string text = args[0] as string;
		if (perceptionEffects.RegisteredEffects.Contains(text))
		{
			perceptionEffects.TriggerEffect(text, (float)args[1]);
			return TextCommandResult.Success();
		}
		return TextCommandResult.Success("No such effect registered.");
	}

	private TextCommandResult OnCmdNormalview(TextCommandCallingArgs args)
	{
		ShaderRegistry.NormalView = !ShaderRegistry.NormalView;
		bool flag = ShaderRegistry.ReloadShaders();
		bool flag2 = game.eventManager != null && game.eventManager.TriggerReloadShaders();
		flag = flag && flag2;
		return TextCommandResult.Success("Shaders reloaded" + (flag ? "" : ". errors occured, please check client log"));
	}

	private TextCommandResult OnCmdTalk(TextCommandCallingArgs args)
	{
		if (args.Parsers[0].IsMissing)
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (object value in Enum.GetValues(typeof(EnumTalkType)))
			{
				if (stringBuilder.Length > 0)
				{
					stringBuilder.Append(", ");
				}
				stringBuilder.Append(value);
			}
			return TextCommandResult.Success(stringBuilder.ToString());
		}
		if (Enum.TryParse<EnumTalkType>(args[0] as string, ignoreCase: true, out var result))
		{
			game.api.World.Player.Entity.talkUtil.Talk(result);
		}
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdClobjc(TextCommandCallingArgs args)
	{
		game.api.ObjectCache.Clear();
		return TextCommandResult.Success("Ok, cleared");
	}

	private TextCommandResult OnCmdFog(TextCommandCallingArgs args)
	{
		if (args.Parsers[0].IsMissing && args.Parsers[1].IsMissing)
		{
			return TextCommandResult.Success("Current fog density = " + game.AmbientManager.Base.FogDensity.Value + ", fog min= " + game.AmbientManager.Base.FogMin.Value);
		}
		float density = (float)args[0];
		float min = (float)args[1];
		game.AmbientManager.SetFogRange(density, min);
		return TextCommandResult.Success("Fog set to density=" + density + ", min=" + min);
	}

	private TextCommandResult OnCmdFov(TextCommandCallingArgs args)
	{
		int num = (int)args[0];
		int num2 = 1;
		int num3 = 179;
		if (!game.IsSingleplayer)
		{
			num2 = 60;
		}
		if (num < num2 || num > num3)
		{
			return TextCommandResult.Success($"Valid field of view: {num2}-{num3}");
		}
		float fov = (float)Math.PI * 2f * ((float)num / 360f);
		game.MainCamera.Fov = fov;
		game.OnResize();
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdShake(TextCommandCallingArgs args)
	{
		float num = (float)args[0];
		game.MainCamera.CameraShakeStrength += num;
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdLiquidselectable(TextCommandCallingArgs args)
	{
		if (!args.Parsers[0].IsMissing)
		{
			game.forceLiquidSelectable = (bool)args[0];
		}
		else
		{
			game.forceLiquidSelectable = !game.forceLiquidSelectable;
		}
		return TextCommandResult.Success("Forced Liquid selectable now " + (game.LiquidSelectable ? "on" : "off"));
	}

	private TextCommandResult OnCmdRelightchunk(TextCommandCallingArgs args)
	{
		BlockPos blockPos = game.EntityPlayer.Pos.AsBlockPos / 32;
		ClientChunk clientChunk = game.WorldMap.GetClientChunk(blockPos.X, blockPos.Y, blockPos.Z);
		game.terrainIlluminator.SunRelightChunk(clientChunk, blockPos.X, blockPos.Y, blockPos.Z);
		long index3d = game.WorldMap.ChunkIndex3D(blockPos.X, blockPos.Y, blockPos.Z);
		game.WorldMap.SetChunkDirty(index3d, priority: true);
		return TextCommandResult.Success("Chunk sunlight recaculated and queued for redrawing");
	}

	private TextCommandResult OnCmdExptexatlas(TextCommandCallingArgs args)
	{
		if (!(args[0] is string text))
		{
			return TextCommandResult.Success();
		}
		TextureAtlasManager textureAtlasManager = null;
		string text2 = "";
		switch (text)
		{
		case "block":
			textureAtlasManager = game.BlockAtlasManager;
			text2 = "Block";
			break;
		case "item":
			textureAtlasManager = game.ItemAtlasManager;
			text2 = "Item";
			break;
		case "entity":
			textureAtlasManager = game.EntityAtlasManager;
			text2 = "Entity";
			break;
		}
		if (textureAtlasManager == null)
		{
			return TextCommandResult.Success("Usage: /exptexatlas [block, item or entity]");
		}
		for (int i = 0; i < textureAtlasManager.Atlasses.Count; i++)
		{
			textureAtlasManager.Atlasses[i].Export(text + "Atlas-" + i, game, textureAtlasManager.AtlasTextures[i].TextureId);
		}
		return TextCommandResult.Success(text2 + " atlas(ses) exported");
	}

	private TextCommandResult OnChunkInfo(TextCommandCallingArgs textCommandCallingArgs)
	{
		BlockPos asBlockPos = game.EntityPlayer.Pos.AsBlockPos;
		ClientChunk chunkAtBlockPos = game.WorldMap.GetChunkAtBlockPos(asBlockPos.X, asBlockPos.Y, asBlockPos.Z);
		if (chunkAtBlockPos == null)
		{
			game.ShowChatMessage("Not loaded yet");
		}
		else
		{
			string text = "no";
			if (chunkAtBlockPos.centerModelPoolLocations != null)
			{
				text = "center";
			}
			if (chunkAtBlockPos.edgeModelPoolLocations != null)
			{
				text = ((chunkAtBlockPos.centerModelPoolLocations != null) ? "yes" : "edge");
			}
			game.ShowChatMessage($"Loaded: {chunkAtBlockPos.loadedFromServer}, Rendering: {text}, #Drawn: {chunkAtBlockPos.quantityDrawn}, #Relit: {chunkAtBlockPos.quantityRelit}, Queued4Redraw: {chunkAtBlockPos.enquedForRedraw}, Queued4Upload: {chunkAtBlockPos.queuedForUpload}, Packed: {chunkAtBlockPos.IsPacked()}, Empty: {chunkAtBlockPos.Empty}");
			game.ShowChatMessage("Traversability: " + Convert.ToString(chunkAtBlockPos.traversability, 2));
		}
		return TextCommandResult.Success();
	}

	private TextCommandResult OnRedrawAll(TextCommandCallingArgs textCommandCallingArgs)
	{
		game.RedrawAllBlocks();
		return TextCommandResult.Success("Ok, will redraw all chunks, might take some time to take effect.");
	}

	private TextCommandResult OnWgenCommand(TextCommandCallingArgs textCommandCallingArgs)
	{
		BlockPos asBlockPos = game.EntityPlayer.Pos.AsBlockPos;
		int climate = game.WorldMap.GetClimate(asBlockPos.X, asBlockPos.Z);
		int rainFall = Climate.GetRainFall((climate >> 8) & 0xFF, asBlockPos.Y);
		int adjustedTemperature = Climate.GetAdjustedTemperature((climate >> 16) & 0xFF, asBlockPos.Y - ClientWorldMap.seaLevel);
		game.ShowChatMessage("Rain=" + rainFall + ", temp=" + adjustedTemperature);
		return TextCommandResult.Success();
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Misc;
	}
}
