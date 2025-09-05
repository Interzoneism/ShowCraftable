using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityStaticTranslocator : BlockEntityTeleporterBase
{
	public int MinTeleporterRangeInBlocks = 400;

	public int MaxTeleporterRangeInBlocks = 8000;

	public BlockPos tpLocation;

	private BlockStaticTranslocator ownBlock;

	private Vec3d posvec;

	private ICoreServerAPI sapi;

	public int repairState;

	private bool activated;

	private bool canTeleport;

	public bool findNextChunk = true;

	public ILoadedSound translocatingSound;

	private float particleAngle;

	private float translocVolume;

	private float translocPitch;

	private long somebodyIsTeleportingReceivedTotalMs;

	public int RepairInteractionsRequired = 4;

	public bool Activated => true;

	public BlockPos TargetLocation => tpLocation;

	public bool FullyRepaired => repairState >= RepairInteractionsRequired;

	private BlockEntityAnimationUtil animUtil => GetBehavior<BEBehaviorAnimatable>().animUtil;

	public override Vec3d GetTarget(Entity forEntity)
	{
		return tpLocation.ToVec3d().Add(-0.3, 1.0, -0.3);
	}

	public BlockEntityStaticTranslocator()
	{
		TeleportWarmupSec = 4.4f;
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		if (FullyRepaired)
		{
			setupGameTickers();
		}
		ownBlock = base.Block as BlockStaticTranslocator;
		posvec = new Vec3d((double)Pos.X + 0.5, Pos.Y, (double)Pos.Z + 0.5);
		if (api.World.Side == EnumAppSide.Client)
		{
			float rotateY = base.Block.Shape.rotateY;
			animUtil.InitializeAnimator("translocator", null, null, new Vec3f(0f, rotateY, 0f));
			updateSoundState();
		}
	}

	public void updateSoundState()
	{
		if (translocVolume > 0f)
		{
			startSound();
		}
		else
		{
			stopSound();
		}
	}

	public void startSound()
	{
		if (translocatingSound == null)
		{
			ICoreAPI api = Api;
			if (api != null && api.Side == EnumAppSide.Client)
			{
				translocatingSound = (Api as ICoreClientAPI).World.LoadSound(new SoundParams
				{
					Location = new AssetLocation("sounds/effect/translocate-active.ogg"),
					ShouldLoop = true,
					Position = Pos.ToVec3f(),
					RelativePosition = false,
					DisposeOnFinish = false,
					Volume = 0.5f
				});
				translocatingSound.Start();
			}
		}
	}

	public void stopSound()
	{
		if (translocatingSound != null)
		{
			translocatingSound.Stop();
			translocatingSound.Dispose();
			translocatingSound = null;
		}
	}

	public void DoActivate()
	{
		activated = true;
		canTeleport = true;
		MarkDirty(redrawOnClient: true);
	}

	public void DoRepair(IPlayer byPlayer)
	{
		if (FullyRepaired)
		{
			return;
		}
		if (repairState == 1)
		{
			int num = GameMath.RoundRandom(Api.World.Rand, byPlayer.Entity.Stats.GetBlended("temporalGearTLRepairCost") - 1f);
			if (num < 0)
			{
				repairState += -num;
				RepairInteractionsRequired = 4;
			}
			RepairInteractionsRequired += Math.Max(0, num);
		}
		repairState++;
		MarkDirty(redrawOnClient: true);
		if (FullyRepaired)
		{
			activated = true;
			setupGameTickers();
		}
	}

	public void setupGameTickers()
	{
		if (Api.Side == EnumAppSide.Server)
		{
			sapi = Api as ICoreServerAPI;
			RegisterGameTickListener(OnServerGameTick, 250);
		}
		else
		{
			RegisterGameTickListener(OnClientGameTick, 50);
		}
	}

	public override void OnEntityCollide(Entity entity)
	{
		if (FullyRepaired && Activated && canTeleport)
		{
			base.OnEntityCollide(entity);
		}
	}

	private void OnClientGameTick(float dt)
	{
		if (ownBlock == null || Api?.World == null || !canTeleport || !Activated)
		{
			return;
		}
		if (Api.World.ElapsedMilliseconds - somebodyIsTeleportingReceivedTotalMs > 6000)
		{
			somebodyIsTeleporting = false;
		}
		HandleSoundClient(dt);
		int num;
		int num2;
		if (Api.World.ElapsedMilliseconds > 100)
		{
			num = ((Api.World.ElapsedMilliseconds - lastOwnPlayerCollideMs < 100) ? 1 : 0);
			if (num != 0)
			{
				num2 = 1;
				goto IL_009a;
			}
		}
		else
		{
			num = 0;
		}
		num2 = (somebodyIsTeleporting ? 1 : 0);
		goto IL_009a;
		IL_009a:
		bool flag = (byte)num2 != 0;
		bool flag2 = animUtil.activeAnimationsByAnimCode.ContainsKey("teleport");
		if (num == 0 && flag)
		{
			manager.lastTranslocateCollideMsOtherPlayer = Api.World.ElapsedMilliseconds;
		}
		SimpleParticleProperties simpleParticleProperties = (flag2 ? ownBlock.insideParticles : ownBlock.idleParticles);
		if (flag)
		{
			AnimationMetaData meta = new AnimationMetaData
			{
				Animation = "teleport",
				Code = "teleport",
				AnimationSpeed = 1f,
				EaseInSpeed = 1f,
				EaseOutSpeed = 2f,
				Weight = 1f,
				BlendMode = EnumAnimationBlendMode.Add
			};
			animUtil.StartAnimation(meta);
			animUtil.StartAnimation(new AnimationMetaData
			{
				Animation = "idle",
				Code = "idle",
				AnimationSpeed = 1f,
				EaseInSpeed = 1f,
				EaseOutSpeed = 1f,
				Weight = 1f,
				BlendMode = EnumAnimationBlendMode.Average
			});
		}
		else
		{
			animUtil.StopAnimation("teleport");
		}
		if (animUtil.activeAnimationsByAnimCode.Count > 0 && Api.World.ElapsedMilliseconds - lastOwnPlayerCollideMs > 10000 && Api.World.ElapsedMilliseconds - manager.lastTranslocateCollideMsOtherPlayer > 10000)
		{
			animUtil.StopAnimation("idle");
		}
		int num3 = 53;
		int num4 = 221;
		int num5 = 172;
		simpleParticleProperties.Color = (num3 << 16) | (num4 << 8) | num5 | 0x32000000;
		simpleParticleProperties.AddPos.Set(0.0, 0.0, 0.0);
		simpleParticleProperties.BlueEvolve = null;
		simpleParticleProperties.RedEvolve = null;
		simpleParticleProperties.GreenEvolve = null;
		simpleParticleProperties.MinSize = 0.1f;
		simpleParticleProperties.MaxSize = 0.2f;
		simpleParticleProperties.SizeEvolve = null;
		simpleParticleProperties.OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, 100f);
		bool flag3 = base.Block.Variant["side"] == "east" || base.Block.Variant["side"] == "west";
		particleAngle = (flag2 ? (particleAngle + 5f * dt) : 0f);
		double num6 = GameMath.Cos(particleAngle + (flag3 ? ((float)Math.PI / 2f) : 0f)) * 0.35f;
		double num7 = 1.9 + Api.World.Rand.NextDouble() * 0.2;
		double num8 = GameMath.Sin(particleAngle + (flag3 ? ((float)Math.PI / 2f) : 0f)) * 0.35f;
		simpleParticleProperties.LifeLength = GameMath.Sqrt(num6 * num6 + num8 * num8) / 10f;
		simpleParticleProperties.MinPos.Set(posvec.X + num6, posvec.Y + num7, posvec.Z + num8);
		simpleParticleProperties.MinVelocity.Set((0f - (float)num6) / 2f, -1f - (float)Api.World.Rand.NextDouble() / 2f, (0f - (float)num8) / 2f);
		simpleParticleProperties.MinQuantity = (flag2 ? 3f : 0.25f);
		simpleParticleProperties.AddVelocity.Set(0f, 0f, 0f);
		simpleParticleProperties.AddQuantity = 0.5f;
		Api.World.SpawnParticles(simpleParticleProperties);
		simpleParticleProperties.MinPos.Set(posvec.X - num6, posvec.Y + num7, posvec.Z - num8);
		simpleParticleProperties.MinVelocity.Set((float)num6 / 2f, -1f - (float)Api.World.Rand.NextDouble() / 2f, (float)num8 / 2f);
		Api.World.SpawnParticles(simpleParticleProperties);
	}

	protected virtual void HandleSoundClient(float dt)
	{
		ICoreClientAPI coreClientAPI = Api as ICoreClientAPI;
		bool flag = coreClientAPI.World.ElapsedMilliseconds - lastOwnPlayerCollideMs <= 200;
		bool flag2 = coreClientAPI.World.ElapsedMilliseconds - lastEntityCollideMs <= 200;
		if (flag || flag2)
		{
			translocVolume = Math.Min(0.5f, translocVolume + dt / 3f);
			translocPitch = Math.Min(translocPitch + dt / 3f, 2.5f);
			if (flag)
			{
				coreClientAPI.World.AddCameraShake(0.0575f);
			}
		}
		else
		{
			translocVolume = Math.Max(0f, translocVolume - 2f * dt);
			translocPitch = Math.Max(translocPitch - dt, 0.5f);
		}
		updateSoundState();
		if (translocVolume > 0f)
		{
			translocatingSound.SetVolume(translocVolume);
			translocatingSound.SetPitch(translocPitch);
		}
	}

	private void OnServerGameTick(float dt)
	{
		if (findNextChunk)
		{
			findNextChunk = false;
			int num = MaxTeleporterRangeInBlocks - MinTeleporterRangeInBlocks;
			int num2 = (int)((double)MinTeleporterRangeInBlocks + sapi.World.Rand.NextDouble() * (double)num) * (2 * sapi.World.Rand.Next(2) - 1);
			int num3 = (int)((double)MinTeleporterRangeInBlocks + sapi.World.Rand.NextDouble() * (double)num) * (2 * sapi.World.Rand.Next(2) - 1);
			int chunkX = (Pos.X + num2) / 32;
			int chunkZ = (Pos.Z + num3) / 32;
			if (!sapi.World.BlockAccessor.IsValidPos(Pos.X + num2, 1, Pos.Z + num3))
			{
				findNextChunk = true;
				return;
			}
			ChunkPeekOptions options = new ChunkPeekOptions
			{
				OnGenerated = delegate(Dictionary<Vec2i, IServerChunk[]> chunks)
				{
					TestForExitPoint(chunks, chunkX, chunkZ);
				},
				UntilPass = EnumWorldGenPass.TerrainFeatures,
				ChunkGenParams = chunkGenParams()
			};
			sapi.WorldManager.PeekChunkColumn(chunkX, chunkZ, options);
		}
		if (canTeleport && Activated)
		{
			try
			{
				HandleTeleportingServer(dt);
			}
			catch (Exception e)
			{
				Api.Logger.Warning("Exception when ticking Static Translocator at {0}", Pos);
				Api.Logger.Error(e);
			}
		}
	}

	private ITreeAttribute chunkGenParams()
	{
		TreeAttribute treeAttribute = new TreeAttribute();
		TreeAttribute treeAttribute2 = (TreeAttribute)(treeAttribute["structureChanceModifier"] = new TreeAttribute());
		treeAttribute2.SetFloat("gates", 10f);
		treeAttribute2 = (TreeAttribute)(treeAttribute["structureMaxCount"] = new TreeAttribute());
		treeAttribute2.SetInt("gates", 1);
		return treeAttribute;
	}

	private void TestForExitPoint(Dictionary<Vec2i, IServerChunk[]> columnsByChunkCoordinate, int centerCx, int centerCz)
	{
		BlockPos pos = HasExitPoint(columnsByChunkCoordinate, centerCx, centerCz);
		if (pos == null)
		{
			findNextChunk = true;
			return;
		}
		sapi.WorldManager.LoadChunkColumnPriority(centerCx, centerCz, new ChunkLoadOptions
		{
			ChunkGenParams = chunkGenParams(),
			OnLoaded = delegate
			{
				exitChunkLoaded(pos);
			}
		});
	}

	private void exitChunkLoaded(BlockPos exitPos)
	{
		BlockStaticTranslocator blockStaticTranslocator = Api.World.BlockAccessor.GetBlock(exitPos) as BlockStaticTranslocator;
		if (blockStaticTranslocator == null)
		{
			exitPos = HasExitPoint(exitPos);
			if (exitPos != null)
			{
				blockStaticTranslocator = Api.World.BlockAccessor.GetBlock(exitPos) as BlockStaticTranslocator;
			}
		}
		if (blockStaticTranslocator != null && !blockStaticTranslocator.Repaired)
		{
			Api.World.BlockAccessor.SetBlock(ownBlock.Id, exitPos);
			BlockEntityStaticTranslocator blockEntityStaticTranslocator = Api.World.BlockAccessor.GetBlockEntity(exitPos) as BlockEntityStaticTranslocator;
			blockEntityStaticTranslocator.tpLocation = Pos.Copy();
			blockEntityStaticTranslocator.canTeleport = true;
			blockEntityStaticTranslocator.findNextChunk = false;
			blockEntityStaticTranslocator.activated = true;
			if (!blockEntityStaticTranslocator.FullyRepaired)
			{
				blockEntityStaticTranslocator.repairState = 4;
				blockEntityStaticTranslocator.setupGameTickers();
			}
			Api.World.BlockAccessor.MarkBlockEntityDirty(exitPos);
			Api.World.BlockAccessor.MarkBlockDirty(exitPos);
			Api.World.Logger.Debug("Connected translocator at {0} (chunkpos: {2}) to my location: {1}", exitPos, Pos, exitPos / 32);
			MarkDirty(redrawOnClient: true);
			tpLocation = exitPos;
			canTeleport = true;
		}
		else
		{
			Api.World.Logger.Warning("Translocator: Regen chunk but broken translocator is gone. Structure generation perhaps seed not consistent? May also just be pre-v1.10 chunk, so probably nothing to worry about. Searching again...");
			findNextChunk = true;
		}
	}

	private BlockPos HasExitPoint(BlockPos nearpos)
	{
		List<GeneratedStructure> list = (Api.World.BlockAccessor.GetChunkAtBlockPos(nearpos) as IServerChunk)?.MapChunk?.MapRegion?.GeneratedStructures;
		if (list == null)
		{
			return null;
		}
		foreach (GeneratedStructure item in list)
		{
			if (!item.Code.Contains("gates"))
			{
				continue;
			}
			Cuboidi location = item.Location;
			BlockPos foundPos = null;
			Api.World.BlockAccessor.SearchBlocks(location.Start.AsBlockPos, location.End.AsBlockPos, delegate(Block block, BlockPos pos)
			{
				if (block is BlockStaticTranslocator { Repaired: false })
				{
					foundPos = pos.Copy();
					return false;
				}
				return true;
			});
			if (foundPos != null)
			{
				return foundPos;
			}
		}
		return null;
	}

	private BlockPos HasExitPoint(Dictionary<Vec2i, IServerChunk[]> columnsByChunkCoordinate, int centerCx, int centerCz)
	{
		foreach (GeneratedStructure generatedStructure in columnsByChunkCoordinate[new Vec2i(centerCx, centerCz)][0].MapChunk.MapRegion.GeneratedStructures)
		{
			if (generatedStructure.Code.Contains("gates"))
			{
				BlockPos blockPos = FindTranslocator(generatedStructure.Location, columnsByChunkCoordinate, centerCx, centerCz);
				if (blockPos != null)
				{
					return blockPos;
				}
			}
		}
		return null;
	}

	private BlockPos FindTranslocator(Cuboidi location, Dictionary<Vec2i, IServerChunk[]> columnsByChunkCoordinate, int centerCx, int centerCz)
	{
		Vec2i vec2i = new Vec2i(0, 0);
		for (int i = location.X1; i < location.X2; i++)
		{
			for (int j = location.Y1; j < location.Y2; j++)
			{
				for (int k = location.Z1; k < location.Z2; k++)
				{
					int x = i / 32;
					int y = k / 32;
					vec2i.X = x;
					vec2i.Y = y;
					if (columnsByChunkCoordinate.TryGetValue(vec2i, out var value))
					{
						IServerChunk serverChunk = value[j / 32];
						int num = i % 32;
						int num2 = j % 32;
						int num3 = k % 32;
						int index3d = (num2 * 32 + num3) * 32 + num;
						if (Api.World.Blocks[serverChunk.Data[index3d]] is BlockStaticTranslocator { Repaired: false })
						{
							return new BlockPos(i, j, k);
						}
					}
				}
			}
		}
		return null;
	}

	public long MapRegionIndex2D(int regionX, int regionZ)
	{
		return (long)regionZ * (long)(sapi.WorldManager.MapSizeX / sapi.WorldManager.RegionSize) + regionX;
	}

	protected override void didTeleport(Entity entity)
	{
		if (entity is EntityPlayer)
		{
			manager.DidTranslocateServer((entity as EntityPlayer).Player as IServerPlayer);
		}
		activated = false;
		ownBlock.teleportParticles.MinPos.Set(Pos.X, Pos.Y, Pos.Z);
		ownBlock.teleportParticles.AddPos.Set(1.0, 1.8, 1.0);
		ownBlock.teleportParticles.MinVelocity.Set(-1f, -1f, -1f);
		ownBlock.teleportParticles.AddVelocity.Set(2f, 2f, 2f);
		ownBlock.teleportParticles.MinQuantity = 150f;
		ownBlock.teleportParticles.AddQuantity = 0.5f;
		int num = 53;
		int num2 = 221;
		int num3 = 172;
		ownBlock.teleportParticles.Color = (num << 16) | (num2 << 8) | num3 | 0x64000000;
		ownBlock.teleportParticles.BlueEvolve = null;
		ownBlock.teleportParticles.RedEvolve = null;
		ownBlock.teleportParticles.GreenEvolve = null;
		ownBlock.teleportParticles.MinSize = 0.1f;
		ownBlock.teleportParticles.MaxSize = 0.2f;
		ownBlock.teleportParticles.SizeEvolve = null;
		ownBlock.teleportParticles.OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -10f);
		Api.World.SpawnParticles(ownBlock.teleportParticles);
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		if (Api.Side == EnumAppSide.Server)
		{
			(Api as ICoreServerAPI).ModLoader.GetModSystem<TeleporterManager>().DeleteLocation(Pos);
		}
		translocatingSound?.Dispose();
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		translocatingSound?.Dispose();
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		base.FromTreeAttributes(tree, worldAccessForResolve);
		canTeleport = tree.GetBool("canTele");
		repairState = tree.GetInt("repairState");
		findNextChunk = tree.GetBool("findNextChunk", defaultValue: true);
		activated = tree.GetBool("activated");
		tpLocationIsOffset = tree.GetBool("tpLocationIsOffset");
		if (canTeleport)
		{
			tpLocation = new BlockPos(tree.GetInt("teleX"), tree.GetInt("teleY"), tree.GetInt("teleZ"));
			if (tpLocation.X == 0 && tpLocation.Z == 0)
			{
				tpLocation = null;
			}
		}
		if (!findNextChunk && FullyRepaired && !canTeleport && tpLocation == null)
		{
			findNextChunk = true;
		}
		if (worldAccessForResolve == null || worldAccessForResolve.Side != EnumAppSide.Client)
		{
			return;
		}
		somebodyIsTeleportingReceivedTotalMs = worldAccessForResolve.ElapsedMilliseconds;
		if (tree.GetBool("somebodyDidTeleport"))
		{
			worldAccessForResolve.Api.Event.EnqueueMainThreadTask(delegate
			{
				worldAccessForResolve.PlaySoundAt(new AssetLocation("sounds/effect/translocate-breakdimension"), Pos, 0.0, null, randomizePitch: false, 16f);
			}, "playtelesound");
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetBool("canTele", canTeleport);
		tree.SetInt("repairState", repairState);
		tree.SetBool("findNextChunk", findNextChunk);
		tree.SetBool("activated", activated);
		tree.SetBool("tpLocationIsOffset", tpLocationIsOffset);
		if (tpLocation != null)
		{
			tree.SetInt("teleX", tpLocation.X);
			tree.SetInt("teleY", tpLocation.Y);
			tree.SetInt("teleZ", tpLocation.Z);
		}
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		if (animUtil.activeAnimationsByAnimCode.Count > 0 || (animUtil.animator != null && animUtil.animator.ActiveAnimationCount > 0))
		{
			return true;
		}
		if (!FullyRepaired)
		{
			MeshData orCreate = ObjectCacheUtil.GetOrCreate(Api, "statictranslocator-" + repairState + "-" + ownBlock.Shape.rotateY, delegate
			{
				float rotateY = ownBlock.Shape.rotateY;
				_ = Api;
				string text = "normal";
				switch (repairState)
				{
				case 0:
					text = "broken";
					break;
				case 1:
					text = "repairstate1";
					break;
				case 2:
					text = "repairstate2";
					break;
				case 3:
					text = "repairstate3";
					break;
				}
				Shape shape = Shape.TryGet(Api, "shapes/block/machine/statictranslocator/" + text + ".json");
				tessThreadTesselator.TesselateShape(ownBlock, shape, out var modeldata, new Vec3f(0f, rotateY, 0f));
				return modeldata;
			});
			mesher.AddMeshData(orCreate);
			return true;
		}
		return false;
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		if (!FullyRepaired)
		{
			dsc.AppendLine(Lang.Get("Seems to be missing a couple of gears. I think I've seen such gears before."));
		}
		else if (tpLocation == null)
		{
			string[] array = new string[3]
			{
				Lang.Get("Warping spacetime."),
				Lang.Get("Warping spacetime.."),
				Lang.Get("Warping spacetime...")
			};
			dsc.AppendLine(array[(int)((float)Api.World.ElapsedMilliseconds / 1000f) % 3]);
		}
		else if (forPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative)
		{
			BlockPos asBlockPos = Api.World.DefaultSpawnPosition.AsBlockPos;
			BlockPos blockPos = tpLocation.Copy().Sub(asBlockPos.X, 0, asBlockPos.Z);
			if (tpLocationIsOffset)
			{
				blockPos.Add(Pos.X, asBlockPos.Y, asBlockPos.Z);
			}
			dsc.AppendLine(Lang.Get("Teleports to {0}", blockPos));
		}
		else
		{
			dsc.AppendLine(Lang.Get("Spacetime subduction completed."));
		}
	}
}
