using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common.Entities;

public abstract class Entity : RegistryObject
{
	public static WaterSplashParticles SplashParticleProps;

	public static AdvancedParticleProperties[] FireParticleProps;

	public static FloatingSedimentParticles FloatingSedimentParticles;

	public static AirBubbleParticles AirBubbleParticleProps;

	public static SimpleParticleProperties bioLumiParticles;

	public static NormalizedSimplexNoise bioLumiNoise;

	protected int HurtColor = ColorUtil.ToRgba(255, 255, 100, 100);

	public IWorldAccessor World;

	public ICoreAPI Api;

	public EntityTagArray Tags = EntityTagArray.Empty;

	internal bool tagsDirty;

	public PhysicsTickDelegate PhysicsUpdateWatcher;

	public Dictionary<string, long> ActivityTimers = new Dictionary<string, long>();

	public EntityPos Pos = new EntityPos();

	public EntityPos ServerPos = new EntityPos();

	public EntityPos PreviousServerPos = new EntityPos();

	public Vec3d PositionBeforeFalling = new Vec3d();

	public long InChunkIndex3d;

	public Cuboidf CollisionBox;

	public Cuboidf OriginCollisionBox;

	public Cuboidf SelectionBox;

	public Cuboidf OriginSelectionBox;

	public bool Teleporting;

	public long EntityId;

	public int SimulationRange;

	public BlockFacing ClimbingOnFace;

	public BlockFacing ClimbingIntoFace;

	public Cuboidf ClimbingOnCollBox;

	public bool OnGround;

	public bool FeetInLiquid;

	protected bool resetLightHsv;

	public bool InLava;

	public long InLavaBeginTotalMs;

	public long OnFireBeginTotalMs;

	public bool Swimming;

	public bool CollidedVertically;

	public bool CollidedHorizontally;

	public EnumEntityState State = EnumEntityState.Despawned;

	public EntityDespawnData DespawnReason;

	public SyncedTreeAttribute WatchedAttributes = new SyncedTreeAttribute();

	public SyncedTreeAttribute DebugAttributes = new SyncedTreeAttribute();

	public SyncedTreeAttribute Attributes = new SyncedTreeAttribute();

	public bool IsRendered;

	public bool IsShadowRendered;

	public EntityStats Stats;

	protected float fireDamageAccum;

	public double touchDistance;

	public double touchDistanceSq;

	[Obsolete("Unused but retained for mod API backwards compatibility")]
	public bool hasRepulseBehavior;

	[Obsolete("Unused but retained for mod API backwards compatibility")]
	public bool customRepulseBehavior;

	public EntityBehavior BHRepulseAgents;

	public Action AfterPhysicsTick;

	public byte IsTracked;

	public bool PositionTicked;

	public bool IsTeleport;

	public bool trickleDownRayIntersects;

	public bool requirePosesOnServer;

	public object packet;

	public EntityBehavior[] ServerBehaviorsMainThread;

	public EntityBehavior[] ServerBehaviorsThreadsafe;

	private Dictionary<string, string> codeRemaps;

	protected bool alive = true;

	public float NearestPlayerDistance;

	private int prevInvulnerableTime;

	protected bool shapeFresh;

	public virtual bool IsCreature => false;

	public virtual bool CanStepPitch => Properties.Habitat != EnumHabitat.Air;

	public virtual bool CanSwivel
	{
		get
		{
			if (Properties.Habitat != EnumHabitat.Air)
			{
				if (Properties.Habitat == EnumHabitat.Land)
				{
					return !Swimming;
				}
				return true;
			}
			return false;
		}
	}

	public virtual bool CanSwivelNow => OnGround;

	public virtual IAnimationManager AnimManager { get; set; }

	public bool IsOnFire
	{
		get
		{
			return WatchedAttributes.GetBool("onFire");
		}
		set
		{
			if (value != WatchedAttributes.GetBool("onFire"))
			{
				WatchedAttributes.SetBool("onFire", value);
			}
		}
	}

	public EntityProperties Properties { get; protected set; }

	public EntitySidedProperties SidedProperties
	{
		get
		{
			if (Properties == null)
			{
				return null;
			}
			if (World.Side.IsClient())
			{
				return Properties.Client;
			}
			return Properties.Server;
		}
	}

	public virtual bool IsInteractable => true;

	public virtual double SwimmingOffsetY => (double)SelectionBox.Y1 + (double)SelectionBox.Y2 * 0.66;

	public bool Collided
	{
		get
		{
			if (!CollidedVertically)
			{
				return CollidedHorizontally;
			}
			return true;
		}
	}

	public EntityPos SidedPos
	{
		get
		{
			if (World.Side != EnumAppSide.Server)
			{
				return Pos;
			}
			return ServerPos;
		}
	}

	public virtual Vec3d LocalEyePos { get; set; } = new Vec3d();

	public virtual bool ApplyGravity
	{
		get
		{
			if (Properties.Habitat != EnumHabitat.Land)
			{
				if (Properties.Habitat == EnumHabitat.Sea || Properties.Habitat == EnumHabitat.Underwater)
				{
					return !Swimming;
				}
				return false;
			}
			return true;
		}
	}

	public virtual float MaterialDensity => 3000f;

	public virtual byte[] LightHsv { get; set; }

	public virtual bool ShouldDespawn => !Alive;

	public virtual bool StoreWithChunk => true;

	public virtual bool AllowOutsideLoadedRange => false;

	public virtual bool AlwaysActive { get; set; }

	public virtual bool Alive
	{
		get
		{
			return alive;
		}
		set
		{
			WatchedAttributes.SetInt("entityDead", (!value) ? 1 : 0);
			alive = value;
		}
	}

	public virtual bool AdjustCollisionBoxToAnimation
	{
		get
		{
			if (alive)
			{
				return AnimManager.AdjustCollisionBoxToAnimation;
			}
			return true;
		}
	}

	public float IdleSoundChanceModifier
	{
		get
		{
			return WatchedAttributes.GetFloat("idleSoundChanceModifier", 1f);
		}
		set
		{
			WatchedAttributes.SetFloat("idleSoundChanceModifier", value);
		}
	}

	public int RenderColor { get; set; } = -1;

	public virtual double LadderFixDelta => 0.0;

	public virtual float ImpactBlockUpdateChance { get; set; }

	public bool ShapeFresh => shapeFresh;

	public virtual double FrustumSphereRadius => Math.Max(3f, Math.Max(SelectionBox?.XSize ?? 1f, SelectionBox?.YSize ?? 1f));

	public event Action OnInitialized;

	static Entity()
	{
		SplashParticleProps = new WaterSplashParticles();
		FireParticleProps = new AdvancedParticleProperties[3];
		FloatingSedimentParticles = new FloatingSedimentParticles();
		AirBubbleParticleProps = new AirBubbleParticles();
		FireParticleProps[0] = new AdvancedParticleProperties
		{
			HsvaColor = new NatFloat[4]
			{
				NatFloat.createUniform(30f, 20f),
				NatFloat.createUniform(255f, 50f),
				NatFloat.createUniform(255f, 50f),
				NatFloat.createUniform(255f, 0f)
			},
			GravityEffect = NatFloat.createUniform(0f, 0f),
			Velocity = new NatFloat[3]
			{
				NatFloat.createUniform(0.2f, 0.05f),
				NatFloat.createUniform(0.5f, 0.1f),
				NatFloat.createUniform(0.2f, 0.05f)
			},
			Size = NatFloat.createUniform(0.25f, 0f),
			Quantity = NatFloat.createUniform(0.25f, 0f),
			VertexFlags = 128,
			SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -0.5f),
			SelfPropelled = true
		};
		FireParticleProps[1] = new AdvancedParticleProperties
		{
			HsvaColor = new NatFloat[4]
			{
				NatFloat.createUniform(30f, 20f),
				NatFloat.createUniform(255f, 50f),
				NatFloat.createUniform(255f, 50f),
				NatFloat.createUniform(255f, 0f)
			},
			OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -16f),
			GravityEffect = NatFloat.createUniform(0f, 0f),
			Velocity = new NatFloat[3]
			{
				NatFloat.createUniform(0f, 0.02f),
				NatFloat.createUniform(0f, 0.02f),
				NatFloat.createUniform(0f, 0.02f)
			},
			Size = NatFloat.createUniform(0.3f, 0.05f),
			Quantity = NatFloat.createUniform(0.25f, 0f),
			VertexFlags = 128,
			SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, 1f),
			LifeLength = NatFloat.createUniform(0.5f, 0f),
			ParticleModel = EnumParticleModel.Quad
		};
		FireParticleProps[2] = new AdvancedParticleProperties
		{
			HsvaColor = new NatFloat[4]
			{
				NatFloat.createUniform(0f, 0f),
				NatFloat.createUniform(0f, 0f),
				NatFloat.createUniform(40f, 30f),
				NatFloat.createUniform(220f, 50f)
			},
			OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -16f),
			GravityEffect = NatFloat.createUniform(0f, 0f),
			Velocity = new NatFloat[3]
			{
				NatFloat.createUniform(0f, 0.05f),
				NatFloat.createUniform(0.2f, 0.3f),
				NatFloat.createUniform(0f, 0.05f)
			},
			Size = NatFloat.createUniform(0.3f, 0.05f),
			Quantity = NatFloat.createUniform(0.25f, 0f),
			SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, 1.5f),
			LifeLength = NatFloat.createUniform(1.5f, 0f),
			ParticleModel = EnumParticleModel.Quad,
			SelfPropelled = true
		};
		bioLumiParticles = new SimpleParticleProperties
		{
			Color = ColorUtil.ToRgba(255, 0, 230, 142),
			MinSize = 0.02f,
			MaxSize = 0.07f,
			MinQuantity = 1f,
			GravityEffect = 0f,
			LifeLength = 1f,
			ParticleModel = EnumParticleModel.Quad,
			ShouldDieInAir = true,
			VertexFlags = 255,
			MinPos = new Vec3d(),
			AddPos = new Vec3d()
		};
		bioLumiParticles.ShouldDieInAir = true;
		bioLumiParticles.OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, -150f);
		bioLumiParticles.MinSize = 0.02f;
		bioLumiParticles.MaxSize = 0.07f;
		bioLumiNoise = new NormalizedSimplexNoise(new double[2] { 1.0, 0.5 }, new double[2] { 5.0, 10.0 }, 97901L);
	}

	public Entity()
	{
		SimulationRange = GlobalConstants.DefaultSimulationRange;
		AnimManager = new AnimationManager();
		Stats = new EntityStats(this);
		WatchedAttributes.SetAttribute("animations", new TreeAttribute());
		WatchedAttributes.SetAttribute("extraInfoText", new TreeAttribute());
	}

	protected Entity(int trackingRange)
	{
		SimulationRange = trackingRange;
		WatchedAttributes.SetAttribute("extraInfoText", new TreeAttribute());
	}

	public virtual void OnHurt(DamageSource dmgSource, float damage)
	{
	}

	public virtual void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
	{
		World = api.World;
		Api = api;
		Properties = properties;
		Class = properties.Class;
		this.InChunkIndex3d = InChunkIndex3d;
		Tags = properties.Tags;
		Stats.Initialize(api);
		alive = WatchedAttributes.GetInt("entityDead") == 0;
		WatchedAttributes.SetFloat("onHurt", 0f);
		int onHurtCounter = WatchedAttributes.GetInt("onHurtCounter");
		WatchedAttributes.RegisterModifiedListener("onHurt", delegate
		{
			float num2 = WatchedAttributes.GetFloat("onHurt");
			if (num2 != 0f)
			{
				int num3 = WatchedAttributes.GetInt("onHurtCounter");
				if (num3 != onHurtCounter)
				{
					onHurtCounter = num3;
					if (Attributes.GetInt("dmgkb") == 0)
					{
						Attributes.SetInt("dmgkb", 1);
					}
					if ((double)num2 > 0.05)
					{
						SetActivityRunning("invulnerable", 500);
						if (World.Side == EnumAppSide.Client)
						{
							OnHurt(null, WatchedAttributes.GetFloat("onHurt"));
						}
					}
				}
			}
		});
		WatchedAttributes.RegisterModifiedListener("onFire", updateOnFire);
		WatchedAttributes.RegisterModifiedListener("entityDead", updateColSelBoxes);
		if (World.Side == EnumAppSide.Client && Properties.Client.SizeGrowthFactor != 0f)
		{
			WatchedAttributes.RegisterModifiedListener("grow", delegate
			{
				if (World != null && Properties != null)
				{
					float sizeGrowthFactor = Properties.Client.SizeGrowthFactor;
					if (sizeGrowthFactor != 0f)
					{
						EntityClientProperties client = World.GetEntityType(Code).Client;
						Properties.Client.Size = client.Size + WatchedAttributes.GetTreeAttribute("grow").GetFloat("age") * sizeGrowthFactor;
					}
				}
			});
		}
		if (Properties.CollisionBoxSize != null || properties.SelectionBoxSize != null)
		{
			updateColSelBoxes();
		}
		DoInitialActiveCheck(api);
		if (api.Side == EnumAppSide.Server && properties.Client != null && properties.Client.TexturesAlternatesCount > 0 && !WatchedAttributes.HasAttribute("textureIndex"))
		{
			WatchedAttributes.SetInt("textureIndex", World.Rand.Next(properties.Client.TexturesAlternatesCount + 1));
		}
		Properties.Initialize(this, api);
		Properties.Client.DetermineLoadedShape(EntityId);
		if (api.Side == EnumAppSide.Server)
		{
			AnimManager.LoadAnimator(api, this, properties.Client.LoadedShapeForEntity, null, requirePosesOnServer, "head");
			AnimManager.OnServerTick(0f);
		}
		else
		{
			AnimManager.Init(api, this);
		}
		LocalEyePos.Y = Properties.EyeHeight;
		float num = Math.Max(0f, 0.75f - CollisionBox.Height);
		ImpactBlockUpdateChance = Attributes.GetFloat("impactBlockUpdateChance", 0.2f - num);
		TriggerOnInitialized();
	}

	public virtual void AfterInitialized(bool onFirstSpawn)
	{
		touchDistance = GetTouchDistance();
		foreach (EntityBehavior behavior in SidedProperties.Behaviors)
		{
			behavior.AfterInitialized(onFirstSpawn);
		}
		if (Api is ICoreServerAPI)
		{
			CacheServerBehaviors();
		}
	}

	protected void TriggerOnInitialized()
	{
		this.OnInitialized?.Invoke();
	}

	protected void DoInitialActiveCheck(ICoreAPI api)
	{
		if (AlwaysActive || api.Side == EnumAppSide.Client)
		{
			State = EnumEntityState.Active;
			return;
		}
		State = EnumEntityState.Inactive;
		IPlayer[] allOnlinePlayers = World.AllOnlinePlayers;
		for (int i = 0; i < allOnlinePlayers.Length; i++)
		{
			EntityPlayer entity = allOnlinePlayers[i].Entity;
			if (entity != null && Pos.InRangeOf(entity.Pos, SimulationRange * SimulationRange))
			{
				State = EnumEntityState.Active;
				break;
			}
		}
	}

	public virtual bool InRangeOf(Vec3d position, float horRangeSq, float vertRange)
	{
		return SidedPos.InRangeOf(position, horRangeSq, vertRange);
	}

	protected void updateColSelBoxes()
	{
		if (WatchedAttributes.GetInt("entityDead") == 0 || Properties.DeadCollisionBoxSize == null)
		{
			SetCollisionBox(Properties.CollisionBoxSize.X, Properties.CollisionBoxSize.Y);
			Vec2f vec2f = Properties.SelectionBoxSize ?? Properties.CollisionBoxSize;
			SetSelectionBox(vec2f.X, vec2f.Y);
		}
		else
		{
			SetCollisionBox(Properties.DeadCollisionBoxSize.X, Properties.DeadCollisionBoxSize.Y);
			Vec2f vec2f2 = Properties.DeadSelectionBoxSize ?? Properties.DeadCollisionBoxSize;
			SetSelectionBox(vec2f2.X, vec2f2.Y);
		}
		foreach (EntityBehavior behavior in SidedProperties.Behaviors)
		{
			behavior.UpdateColSelBoxes();
		}
		double num = (touchDistance = GetTouchDistance());
		touchDistanceSq = num * num;
	}

	protected void updateOnFire()
	{
		bool isOnFire = IsOnFire;
		if (isOnFire)
		{
			OnFireBeginTotalMs = World.ElapsedMilliseconds;
		}
		if (isOnFire && LightHsv == null)
		{
			LightHsv = new byte[3] { 5, 7, 10 };
			resetLightHsv = true;
		}
		if (!isOnFire && resetLightHsv)
		{
			LightHsv = null;
		}
	}

	public virtual bool TryGiveItemStack(ItemStack itemstack)
	{
		EnumHandling handling = EnumHandling.PassThrough;
		bool flag = false;
		foreach (EntityBehavior behavior in SidedProperties.Behaviors)
		{
			flag |= behavior.TryGiveItemStack(itemstack, ref handling);
			if (handling == EnumHandling.PreventSubsequent)
			{
				return flag;
			}
		}
		return flag;
	}

	public virtual ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer)
	{
		EnumHandling handling = EnumHandling.PassThrough;
		ItemStack[] result = null;
		foreach (EntityBehavior behavior in SidedProperties.Behaviors)
		{
			result = behavior.GetDrops(world, pos, byPlayer, ref handling);
			if (handling == EnumHandling.PreventSubsequent)
			{
				return result;
			}
		}
		if (handling == EnumHandling.PreventDefault)
		{
			return result;
		}
		if (Properties.Drops == null)
		{
			return null;
		}
		List<ItemStack> list = new List<ItemStack>();
		float num = 1f;
		JsonObject attributes = Properties.Attributes;
		if ((attributes == null || !attributes["isMechanical"].AsBool()) && byPlayer?.Entity != null)
		{
			num = 1f + byPlayer.Entity.Stats.GetBlended("animalLootDropRate");
		}
		for (int i = 0; i < Properties.Drops.Length; i++)
		{
			BlockDropItemStack blockDropItemStack = Properties.Drops[i];
			float num2 = 1f;
			if (blockDropItemStack.DropModbyStat != null && byPlayer?.Entity != null)
			{
				num2 = byPlayer.Entity.Stats.GetBlended(blockDropItemStack.DropModbyStat);
			}
			ItemStack itemStack = blockDropItemStack.GetNextItemStack(num * num2);
			if (itemStack != null)
			{
				if (itemStack.Collectible is IResolvableCollectible resolvableCollectible)
				{
					DummySlot dummySlot = new DummySlot(itemStack);
					resolvableCollectible.Resolve(dummySlot, world);
					itemStack = dummySlot.Itemstack;
				}
				list.Add(itemStack);
				if (blockDropItemStack.LastDrop)
				{
					break;
				}
			}
		}
		return list.ToArray();
	}

	public virtual void TeleportToDouble(double x, double y, double z, Action onTeleported = null)
	{
		Teleporting = true;
		if (World.Api is ICoreServerAPI coreServerAPI)
		{
			coreServerAPI.WorldManager.LoadChunkColumnPriority((int)x / 32, (int)z / 32, new ChunkLoadOptions
			{
				OnLoaded = delegate
				{
					IsTeleport = true;
					Pos.SetPos(x, y, z);
					ServerPos.SetPos(x, y, z);
					PositionBeforeFalling.Set(x, y, z);
					Pos.Motion.Set(0.0, 0.0, 0.0);
					onTeleported?.Invoke();
					Teleporting = false;
				}
			});
		}
	}

	public virtual void TeleportTo(int x, int y, int z)
	{
		TeleportToDouble(x, y, z);
	}

	public virtual void TeleportTo(Vec3d position)
	{
		TeleportToDouble(position.X, position.Y, position.Z);
	}

	public virtual void TeleportTo(BlockPos position)
	{
		TeleportToDouble(position.X, position.Y, position.Z);
	}

	public virtual void TeleportTo(EntityPos position, Action onTeleported = null)
	{
		Pos.Yaw = position.Yaw;
		Pos.Pitch = position.Pitch;
		Pos.Roll = position.Roll;
		ServerPos.Yaw = position.Yaw;
		ServerPos.Pitch = position.Pitch;
		ServerPos.Roll = position.Roll;
		TeleportToDouble(position.X, position.Y, position.Z, onTeleported);
	}

	public virtual bool ReceiveDamage(DamageSource damageSource, float damage)
	{
		if ((!Alive || (IsActivityRunning("invulnerable") && !damageSource.IgnoreInvFrames)) && damageSource.Type != EnumDamageType.Heal)
		{
			return false;
		}
		if (ShouldReceiveDamage(damageSource, damage))
		{
			foreach (EntityBehavior behavior in SidedProperties.Behaviors)
			{
				behavior.OnEntityReceiveDamage(damageSource, ref damage);
			}
			if (damageSource.Type != EnumDamageType.Heal && damage > 0f)
			{
				WatchedAttributes.SetInt("onHurtCounter", WatchedAttributes.GetInt("onHurtCounter") + 1);
				WatchedAttributes.SetFloat("onHurt", damage);
				if (damage > 0.05f)
				{
					AnimManager.StartAnimation("hurt");
				}
			}
			if (damageSource.GetSourcePosition() != null)
			{
				bool flag = false;
				if (damageSource.GetAttackAngle(Pos.XYZ, out var _, out var attackPitch))
				{
					flag = Math.Abs(attackPitch) > 1.3962633609771729 || Math.Abs(attackPitch) < 0.1745329201221466;
				}
				Vec3d vec3d = (SidedPos.XYZ - damageSource.GetSourcePosition()).Normalize();
				if (flag)
				{
					vec3d.Y = 0.05000000074505806;
					vec3d.Normalize();
				}
				else
				{
					vec3d.Y = 0.699999988079071;
				}
				vec3d.Y /= damageSource.YDirKnockbackDiv;
				float num = damageSource.KnockbackStrength * GameMath.Clamp((1f - Properties.KnockbackResistance) / 10f, 0f, 1f);
				WatchedAttributes.SetFloat("onHurtDir", (float)Math.Atan2(vec3d.X, vec3d.Z));
				WatchedAttributes.SetDouble("kbdirX", vec3d.X * (double)num);
				WatchedAttributes.SetDouble("kbdirY", vec3d.Y * (double)num);
				WatchedAttributes.SetDouble("kbdirZ", vec3d.Z * (double)num);
			}
			else
			{
				WatchedAttributes.SetDouble("kbdirX", 0.0);
				WatchedAttributes.SetDouble("kbdirY", 0.0);
				WatchedAttributes.SetDouble("kbdirZ", 0.0);
				WatchedAttributes.SetFloat("onHurtDir", -999f);
			}
			return damage > 0f;
		}
		return false;
	}

	public virtual bool ShouldReceiveDamage(DamageSource damageSource, float damage)
	{
		return true;
	}

	public virtual void OnGameTick(float dt)
	{
		IWorldAccessor world = World;
		if (world.EntityDebugMode)
		{
			UpdateDebugAttributes();
			DebugAttributes.MarkAllDirty();
		}
		if (world.Side == EnumAppSide.Client)
		{
			int num = RemainingActivityTime("invulnerable");
			if (prevInvulnerableTime != num)
			{
				RenderColor = ColorUtil.ColorOverlay(HurtColor, -1, 1f - (float)num / 500f);
				prevInvulnerableTime = num;
			}
			alive = WatchedAttributes.GetInt("entityDead") == 0;
			if (world.FrameProfiler.Enabled)
			{
				world.FrameProfiler.Enter("behaviors");
				foreach (EntityBehavior behavior in Properties.Client.Behaviors)
				{
					behavior.OnGameTick(dt);
					world.FrameProfiler.Mark(behavior.ProfilerName);
				}
				world.FrameProfiler.Leave();
			}
			else
			{
				foreach (EntityBehavior behavior2 in Properties.Client.Behaviors)
				{
					behavior2.OnGameTick(dt);
				}
			}
			if (world.Rand.NextDouble() < (double)(IdleSoundChanceModifier * Properties.IdleSoundChance) / 100.0 && Alive)
			{
				PlayEntitySound("idle", null, randomizePitch: true, Properties.IdleSoundRange);
			}
		}
		else
		{
			if (requirePosesOnServer && !shapeFresh)
			{
				CompositeShape shape = Properties.Client.Shape;
				Shape entityShape = Properties.Client.LoadedShapeForEntity;
				if (entityShape != null)
				{
					OnTesselation(ref entityShape, shape.Base.ToString());
					OnTesselated();
				}
			}
			if (Properties.Server.Behaviors.Count != ServerBehaviorsMainThread.Length + ServerBehaviorsThreadsafe.Length)
			{
				CacheServerBehaviors();
			}
			EntityBehavior[] serverBehaviorsMainThread = ServerBehaviorsMainThread;
			if (world.FrameProfiler.Enabled)
			{
				FrameProfilerUtil frameProfiler = world.FrameProfiler;
				frameProfiler.Enter("behaviors");
				foreach (EntityBehavior entityBehavior in serverBehaviorsMainThread)
				{
					entityBehavior.OnGameTick(dt);
					frameProfiler.Mark(entityBehavior.ProfilerName);
				}
				frameProfiler.Leave();
			}
			else
			{
				for (int j = 0; j < serverBehaviorsMainThread.Length; j++)
				{
					serverBehaviorsMainThread[j].OnGameTick(dt);
				}
			}
			if (InLava)
			{
				Ignite();
			}
		}
		if (IsOnFire)
		{
			Block block = world.BlockAccessor.GetBlock(Pos.AsBlockPos, 2);
			if (((block.IsLiquid() && block.LiquidCode != "lava") || world.ElapsedMilliseconds - OnFireBeginTotalMs > 12000) && !InLava)
			{
				IsOnFire = false;
			}
			else
			{
				if (world.Side == EnumAppSide.Client)
				{
					int num2 = Math.Min(FireParticleProps.Length - 1, Api.World.Rand.Next(FireParticleProps.Length + 1));
					AdvancedParticleProperties advancedParticleProperties = FireParticleProps[num2];
					advancedParticleProperties.basePos.Set(Pos.X, Pos.InternalY + (double)(SelectionBox.YSize / 2f), Pos.Z);
					advancedParticleProperties.PosOffset[0].var = SelectionBox.XSize / 2f;
					advancedParticleProperties.PosOffset[1].var = SelectionBox.YSize / 2f;
					advancedParticleProperties.PosOffset[2].var = SelectionBox.ZSize / 2f;
					advancedParticleProperties.Velocity[0].avg = (float)Pos.Motion.X * 10f;
					advancedParticleProperties.Velocity[1].avg = (float)Pos.Motion.Y * 5f;
					advancedParticleProperties.Velocity[2].avg = (float)Pos.Motion.Z * 10f;
					advancedParticleProperties.Quantity.avg = GameMath.Sqrt(advancedParticleProperties.PosOffset[0].var + advancedParticleProperties.PosOffset[1].var + advancedParticleProperties.PosOffset[2].var) * num2 switch
					{
						1 => 3f, 
						0 => 0.5f, 
						_ => 1.25f, 
					};
					Api.World.SpawnParticles(advancedParticleProperties);
				}
				else
				{
					ApplyFireDamage(dt);
				}
				if (!alive && InLava && !(this is EntityPlayer))
				{
					DieInLava();
				}
			}
		}
		if (world.Side == EnumAppSide.Server && State == EnumEntityState.Active)
		{
			try
			{
				AnimManager.OnServerTick(dt);
			}
			catch (Exception)
			{
				world.Logger.Error("Error ticking animations for entity " + Code.ToShortString() + " at " + SidedPos.AsBlockPos);
				throw;
			}
			world.FrameProfiler.Mark("entity-animation-ticking");
		}
	}

	protected void ApplyFireDamage(float dt)
	{
		fireDamageAccum += dt;
		if (fireDamageAccum > 1f)
		{
			ReceiveDamage(new DamageSource
			{
				Source = EnumDamageSource.Internal,
				Type = EnumDamageType.Fire
			}, 0.5f);
			fireDamageAccum = 0f;
		}
	}

	protected void DieInLava()
	{
		float quantity = GameMath.Clamp(SelectionBox.XSize * SelectionBox.YSize * SelectionBox.ZSize * 150f, 10f, 150f);
		Api.World.SpawnParticles(quantity, ColorUtil.ColorFromRgba(20, 20, 20, 255), new Vec3d(ServerPos.X + (double)SelectionBox.X1, ServerPos.InternalY + (double)SelectionBox.Y1, ServerPos.Z + (double)SelectionBox.Z1), new Vec3d(ServerPos.X + (double)SelectionBox.X2, ServerPos.InternalY + (double)SelectionBox.Y2, ServerPos.Z + (double)SelectionBox.Z2), new Vec3f(-1f, -1f, -1f), new Vec3f(2f, 2f, 2f), 2f, 1f, 1f, EnumParticleModel.Cube);
		Die(EnumDespawnReason.Combusted);
	}

	public virtual void OnAsyncParticleTick(float dt, IAsyncParticleManager manager)
	{
	}

	public virtual void Ignite()
	{
		IsOnFire = true;
	}

	public virtual ITexPositionSource GetTextureSource()
	{
		if (Api.Side != EnumAppSide.Client)
		{
			return null;
		}
		ITexPositionSource result = null;
		List<EntityBehavior> list = Properties.Client?.Behaviors;
		EnumHandling handling = EnumHandling.PassThrough;
		if (list != null)
		{
			foreach (EntityBehavior item in list)
			{
				result = item.GetTextureSource(ref handling);
				if (handling == EnumHandling.PreventSubsequent)
				{
					return result;
				}
			}
		}
		if (handling == EnumHandling.PreventDefault)
		{
			return result;
		}
		int altTextureNumber = WatchedAttributes.GetInt("textureIndex");
		return (Api as ICoreClientAPI).Tesselator.GetTextureSource(this, null, altTextureNumber);
	}

	public virtual void MarkShapeModified()
	{
		shapeFresh = false;
	}

	public virtual void OnTesselation(ref Shape entityShape, string shapePathForLogging)
	{
		shapeFresh = true;
		bool shapeIsCloned = false;
		OnTesselation(ref entityShape, shapePathForLogging, ref shapeIsCloned);
		if (shapeIsCloned && entityShape.Animations != null)
		{
			Animation[] animations = entityShape.Animations;
			for (int i = 0; i < animations.Length; i++)
			{
				animations[i].PrevNextKeyFrameByFrame = null;
			}
		}
	}

	protected virtual void OnTesselation(ref Shape entityShape, string shapePathForLogging, ref bool shapeIsCloned)
	{
		shapeFresh = true;
		CompositeShape shape = Properties.Client.Shape;
		if (shape?.Overlays != null && shape.Overlays.Length != 0)
		{
			shapeIsCloned = true;
			entityShape = entityShape.Clone();
			IDictionary<string, CompositeTexture> textures = Properties.Client.Textures;
			CompositeShape[] overlays = shape.Overlays;
			foreach (CompositeShape compositeShape in overlays)
			{
				Shape shape2 = Api.Assets.TryGet(compositeShape.Base.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json"))?.ToObject<Shape>();
				if (shape2 == null)
				{
					Api.Logger.Error("Entity {0} defines a shape overlay {1}, but no such file found. Will ignore.", Code, compositeShape.Base);
					continue;
				}
				string texturePrefixCode = null;
				JsonObject attributes = Properties.Attributes;
				if (attributes != null && attributes["wearableTexturePrefixCode"].Exists)
				{
					texturePrefixCode = Properties.Attributes["wearableTexturePrefixCode"].AsString();
				}
				entityShape.StepParentShape(shape2, compositeShape.Base.ToShortString(), shapePathForLogging, Api.Logger, delegate(string texcode, AssetLocation tloc)
				{
					if (Api is ICoreClientAPI coreClientAPI && (texturePrefixCode != null || !textures.ContainsKey(texcode)))
					{
						CompositeTexture compositeTexture = (textures[texturePrefixCode + "-" + texcode] = new CompositeTexture(tloc));
						CompositeTexture compositeTexture3 = compositeTexture;
						compositeTexture3.Bake(Api.Assets);
						coreClientAPI.EntityTextureAtlas.GetOrInsertTexture(compositeTexture3.Baked.TextureFilenames[0], out var textureSubId, out var _);
						compositeTexture3.Baked.TextureSubId = textureSubId;
					}
				});
			}
		}
		string[] willDeleteElements = null;
		JsonObject attributes2 = Properties.Attributes;
		if (attributes2 != null && attributes2["disableElements"].Exists)
		{
			willDeleteElements = Properties.Attributes["disableElements"].AsArray<string>();
		}
		List<EntityBehavior> list = ((World.Side != EnumAppSide.Server) ? Properties.Client?.Behaviors : Properties.Server?.Behaviors);
		EnumHandling enumHandling = EnumHandling.PassThrough;
		if (list != null)
		{
			foreach (EntityBehavior item in list)
			{
				item.OnTesselation(ref entityShape, shapePathForLogging, ref shapeIsCloned, ref willDeleteElements);
				if (enumHandling == EnumHandling.PreventSubsequent)
				{
					break;
				}
			}
		}
		if (willDeleteElements != null && willDeleteElements.Length != 0)
		{
			if (!shapeIsCloned)
			{
				Shape shape3 = entityShape.Clone();
				entityShape = shape3;
				shapeIsCloned = true;
			}
			entityShape.RemoveElements(willDeleteElements);
		}
		if (shapeIsCloned)
		{
			AnimManager.LoadAnimator(World.Api, this, entityShape, AnimManager.Animator?.Animations, requirePosesOnServer, "head");
		}
		else
		{
			AnimManager.LoadAnimatorCached(World.Api, this, entityShape, AnimManager.Animator?.Animations, requirePosesOnServer, "head");
		}
	}

	public virtual void OnTesselated()
	{
		List<EntityBehavior> list = ((World.Side != EnumAppSide.Server) ? Properties.Client?.Behaviors : Properties.Server?.Behaviors);
		if (list == null)
		{
			return;
		}
		foreach (EntityBehavior item in list)
		{
			item.OnTesselated();
		}
	}

	public virtual void OnFallToGround(double motionY)
	{
		foreach (EntityBehavior behavior in SidedProperties.Behaviors)
		{
			behavior.OnFallToGround(PositionBeforeFalling, motionY);
		}
	}

	public virtual void OnCollided()
	{
	}

	public virtual void OnCollideWithLiquid()
	{
		if (World.Side == EnumAppSide.Server)
		{
			return;
		}
		EntityPos sidedPos = SidedPos;
		float num = (float)Math.Abs(PositionBeforeFalling.Y - sidedPos.Y);
		double num2 = SelectionBox.XSize;
		double num3 = SelectionBox.YSize;
		double num4 = (double)(2f * GameMath.Sqrt(num2 * num3)) + sidedPos.Motion.Length() * 10.0;
		if (!(num4 < 0.4000000059604645) && !(num < 0.25f))
		{
			string domainAndPath = (new string[3] { "sounds/environment/smallsplash", "sounds/environment/mediumsplash", "sounds/environment/largesplash" })[(int)GameMath.Clamp(num4 / 1.6, 0.0, 2.0)];
			num4 = Math.Min(10.0, num4);
			float num5 = GameMath.Sqrt(num2 * num3);
			World.PlaySoundAt(new AssetLocation(domainAndPath), (float)sidedPos.X, (float)sidedPos.InternalY, (float)sidedPos.Z);
			BlockPos asBlockPos = sidedPos.AsBlockPos;
			Vec3d pos = new Vec3d(Pos.X, (double)asBlockPos.InternalY + 1.02, Pos.Z);
			World.SpawnCubeParticles(asBlockPos, pos, SelectionBox.XSize, (int)((double)(num5 * 8f) * num4), 0.75f);
			World.SpawnCubeParticles(asBlockPos, pos, SelectionBox.XSize, (int)((double)(num5 * 8f) * num4), 0.25f);
			if (num4 >= 2.0)
			{
				SplashParticleProps.BasePos.Set(sidedPos.X - num2 / 2.0, sidedPos.Y - 0.75, sidedPos.Z - num2 / 2.0);
				SplashParticleProps.AddPos.Set(num2, 0.75, num2);
				SplashParticleProps.AddVelocity.Set((float)GameMath.Clamp(sidedPos.Motion.X * 30.0, -2.0, 2.0), 1f, (float)GameMath.Clamp(sidedPos.Motion.Z * 30.0, -2.0, 2.0));
				SplashParticleProps.QuantityMul = (float)(num4 - 1.0) * num5;
				World.SpawnParticles(SplashParticleProps);
			}
			SpawnWaterMovementParticles((float)Math.Min(0.25, num4 / 10.0), 0.0, -0.5);
		}
	}

	protected virtual void SpawnWaterMovementParticles(float quantityMul, double offx = 0.0, double offy = 0.0, double offz = 0.0)
	{
		if (World.Side == EnumAppSide.Server)
		{
			return;
		}
		ClimateCondition selfClimateCond = (Api as ICoreClientAPI).World.Player.Entity.selfClimateCond;
		if (selfClimateCond == null)
		{
			return;
		}
		float num = Math.Max(0f, (28f - selfClimateCond.Temperature) / 6f) + Math.Max(0f, (0.8f - selfClimateCond.Rainfall) * 3f);
		double num2 = bioLumiNoise.Noise(SidedPos.X / 300.0, SidedPos.Z / 300.0) * 2.0 - 1.0 - (double)num;
		if (!(num2 < 0.0))
		{
			if (this is EntityPlayer && Swimming)
			{
				bioLumiParticles.MinPos.Set(SidedPos.X + (double)(2f * SelectionBox.X1), SidedPos.Y + offy + 0.5 + (double)(1.25f * SelectionBox.Y1), SidedPos.Z + (double)(2f * SelectionBox.Z1));
				bioLumiParticles.AddPos.Set(3f * SelectionBox.XSize, 0.5f * SelectionBox.YSize, 3f * SelectionBox.ZSize);
			}
			else
			{
				bioLumiParticles.MinPos.Set(SidedPos.X + (double)(1.25f * SelectionBox.X1), SidedPos.Y + offy + (double)(1.25f * SelectionBox.Y1), SidedPos.Z + (double)(1.25f * SelectionBox.Z1));
				bioLumiParticles.AddPos.Set(1.5f * SelectionBox.XSize, 1.5f * SelectionBox.YSize, 1.5f * SelectionBox.ZSize);
			}
			bioLumiParticles.MinQuantity = Math.Min(200f, 100f * quantityMul * (float)num2);
			bioLumiParticles.MinVelocity.Set(-0.2f + 2f * (float)Pos.Motion.X, -0.2f + 2f * (float)Pos.Motion.Y, -0.2f + 2f * (float)Pos.Motion.Z);
			bioLumiParticles.AddVelocity.Set(0.4f + 2f * (float)Pos.Motion.X, 0.4f + 2f * (float)Pos.Motion.Y, 0.4f + 2f * (float)Pos.Motion.Z);
			World.SpawnParticles(bioLumiParticles);
		}
	}

	public virtual void OnEntityLoaded()
	{
		foreach (EntityBehavior behavior in SidedProperties.Behaviors)
		{
			behavior.OnEntityLoaded();
		}
		Properties.Client.Renderer?.OnEntityLoaded();
		MarkShapeModified();
	}

	public virtual void OnEntitySpawn()
	{
		foreach (EntityBehavior behavior in SidedProperties.Behaviors)
		{
			behavior.OnEntitySpawn();
		}
		Properties.Client.Renderer?.OnEntityLoaded();
		MarkShapeModified();
	}

	public virtual void OnEntityDespawn(EntityDespawnData despawn)
	{
		if (SidedProperties == null)
		{
			return;
		}
		foreach (EntityBehavior behavior in SidedProperties.Behaviors)
		{
			behavior.OnEntityDespawn(despawn);
		}
		AnimManager.Dispose();
		WatchedAttributes.OnModified.Clear();
	}

	public virtual void OnExitedLiquid()
	{
	}

	public virtual void OnInteract(EntityAgent byEntity, ItemSlot itemslot, Vec3d hitPosition, EnumInteractMode mode)
	{
		EnumHandling handled = EnumHandling.PassThrough;
		foreach (EntityBehavior behavior in SidedProperties.Behaviors)
		{
			behavior.OnInteract(byEntity, itemslot, hitPosition, mode, ref handled);
			if (handled == EnumHandling.PreventSubsequent)
			{
				break;
			}
		}
	}

	public virtual WorldInteraction[] GetInteractionHelp(IClientWorldAccessor world, EntitySelection es, IClientPlayer player)
	{
		EnumHandling handled = EnumHandling.PassThrough;
		List<WorldInteraction> list = new List<WorldInteraction>();
		foreach (EntityBehavior behavior in SidedProperties.Behaviors)
		{
			WorldInteraction[] interactionHelp = behavior.GetInteractionHelp(world, es, player, ref handled);
			if (interactionHelp != null)
			{
				list.AddRange(interactionHelp);
			}
			if (handled == EnumHandling.PreventSubsequent)
			{
				break;
			}
		}
		return list.ToArray();
	}

	public virtual void OnReceivedServerPos(bool isTeleport)
	{
		EnumHandling handled = EnumHandling.PassThrough;
		foreach (EntityBehavior behavior in SidedProperties.Behaviors)
		{
			behavior.OnReceivedServerPos(isTeleport, ref handled);
			if (handled == EnumHandling.PreventSubsequent)
			{
				break;
			}
		}
		if (handled == EnumHandling.PassThrough && GetBehavior("entityinterpolation") == null)
		{
			Pos.SetFrom(ServerPos);
		}
	}

	public virtual void OnReceivedClientPacket(IServerPlayer player, int packetid, byte[] data)
	{
		EnumHandling handled = EnumHandling.PassThrough;
		foreach (EntityBehavior behavior in SidedProperties.Behaviors)
		{
			behavior.OnReceivedClientPacket(player, packetid, data, ref handled);
			if (handled == EnumHandling.PreventSubsequent)
			{
				break;
			}
		}
	}

	public virtual void OnReceivedServerPacket(int packetid, byte[] data)
	{
		if (packetid == 1)
		{
			Vec3d vec3d = SerializerUtil.Deserialize<Vec3d>(data);
			if (Api is ICoreClientAPI coreClientAPI && coreClientAPI.World.Player.Entity.EntityId == EntityId)
			{
				Pos.SetPosWithDimension(vec3d);
				((EntityPlayer)this).UpdatePartitioning();
			}
			ServerPos.SetPosWithDimension(vec3d);
			World.BlockAccessor.MarkBlockDirty(vec3d.AsBlockPos);
			return;
		}
		EnumHandling handled = EnumHandling.PassThrough;
		foreach (EntityBehavior behavior in SidedProperties.Behaviors)
		{
			behavior.OnReceivedServerPacket(packetid, data, ref handled);
			if (handled == EnumHandling.PreventSubsequent)
			{
				break;
			}
		}
	}

	public virtual void OnReceivedServerAnimations(int[] activeAnimations, int activeAnimationsCount, float[] activeAnimationSpeeds)
	{
		AnimManager.OnReceivedServerAnimations(activeAnimations, activeAnimationsCount, activeAnimationSpeeds);
	}

	public virtual ItemStack OnCollected(Entity byEntity)
	{
		return null;
	}

	public virtual void OnStateChanged(EnumEntityState beforeState)
	{
		EnumHandling handling = EnumHandling.PassThrough;
		foreach (EntityBehavior behavior in Properties.Server.Behaviors)
		{
			behavior.OnStateChanged(beforeState, ref handling);
			if (handling == EnumHandling.PreventSubsequent)
			{
				break;
			}
		}
	}

	public virtual void SetCollisionBox(float length, float height)
	{
		CollisionBox = new Cuboidf
		{
			X1 = (0f - length) / 2f,
			Z1 = (0f - length) / 2f,
			X2 = length / 2f,
			Z2 = length / 2f,
			Y2 = height
		};
		OriginCollisionBox = CollisionBox.Clone();
	}

	public virtual void SetSelectionBox(float length, float height)
	{
		SelectionBox = new Cuboidf
		{
			X1 = (0f - length) / 2f,
			Z1 = (0f - length) / 2f,
			X2 = length / 2f,
			Z2 = length / 2f,
			Y2 = height
		};
		OriginSelectionBox = SelectionBox.Clone();
	}

	public virtual void AddBehavior(EntityBehavior behavior)
	{
		SidedProperties.Behaviors.Add(behavior);
		if (Api is ICoreServerAPI)
		{
			CacheServerBehaviors();
		}
	}

	public virtual void RemoveBehavior(EntityBehavior behavior)
	{
		SidedProperties.Behaviors.Remove(behavior);
		if (Api is ICoreServerAPI)
		{
			CacheServerBehaviors();
		}
	}

	public void CacheServerBehaviors()
	{
		int num = 0;
		List<EntityBehavior> behaviors = SidedProperties.Behaviors;
		for (int i = 0; i < behaviors.Count; i++)
		{
			if (behaviors[i].ThreadSafe)
			{
				num++;
			}
		}
		ServerBehaviorsMainThread = new EntityBehavior[behaviors.Count - num];
		ServerBehaviorsThreadsafe = new EntityBehavior[num];
		int num2 = 0;
		int num3 = 0;
		for (int j = 0; j < behaviors.Count; j++)
		{
			EntityBehavior entityBehavior = behaviors[j];
			if (entityBehavior.ThreadSafe)
			{
				ServerBehaviorsThreadsafe[num3++] = entityBehavior;
			}
			else
			{
				ServerBehaviorsMainThread[num2++] = entityBehavior;
			}
		}
	}

	public virtual bool HasBehavior(string behaviorName)
	{
		List<EntityBehavior> behaviors = SidedProperties.Behaviors;
		for (int i = 0; i < behaviors.Count; i++)
		{
			if (behaviors[i].PropertyName().Equals(behaviorName))
			{
				return true;
			}
		}
		return false;
	}

	public virtual bool HasBehavior<T>() where T : EntityBehavior
	{
		List<EntityBehavior> behaviors = SidedProperties.Behaviors;
		for (int i = 0; i < behaviors.Count; i++)
		{
			if (behaviors[i] is T)
			{
				return true;
			}
		}
		return false;
	}

	public virtual EntityBehavior? GetBehavior(string name)
	{
		List<EntityBehavior> behaviors = SidedProperties.Behaviors;
		for (int i = 0; i < behaviors.Count; i++)
		{
			if (behaviors[i].PropertyName().Equals(name))
			{
				return behaviors[i];
			}
		}
		return null;
	}

	public virtual T? GetBehavior<T>() where T : EntityBehavior
	{
		List<EntityBehavior> behaviors = SidedProperties.Behaviors;
		for (int i = 0; i < behaviors.Count; i++)
		{
			if (behaviors[i] is T result)
			{
				return result;
			}
		}
		return null;
	}

	public virtual List<T> GetInterfaces<T>() where T : class
	{
		List<T> list = new List<T>();
		if (this is T item)
		{
			list.Add(item);
		}
		List<EntityBehavior> behaviors = SidedProperties.Behaviors;
		for (int i = 0; i < behaviors.Count; i++)
		{
			if (behaviors[i] is T item2)
			{
				list.Add(item2);
			}
		}
		return list;
	}

	public virtual T GetInterface<T>() where T : class
	{
		if (this is T)
		{
			return this as T;
		}
		List<EntityBehavior> behaviors = SidedProperties.Behaviors;
		for (int i = 0; i < behaviors.Count; i++)
		{
			if (behaviors[i] is T result)
			{
				return result;
			}
		}
		return null;
	}

	public virtual bool IsActivityRunning(string key)
	{
		ActivityTimers.TryGetValue(key, out var value);
		return value > World.ElapsedMilliseconds;
	}

	public virtual int RemainingActivityTime(string key)
	{
		ActivityTimers.TryGetValue(key, out var value);
		return (int)Math.Max(0L, value - World.ElapsedMilliseconds);
	}

	public virtual void SetActivityRunning(string key, int milliseconds)
	{
		ActivityTimers[key] = World.ElapsedMilliseconds + milliseconds;
	}

	public virtual void UpdateDebugAttributes()
	{
		if (World.Side == EnumAppSide.Client)
		{
			DebugAttributes.SetString("Entity Id", EntityId.ToString() ?? "");
			DebugAttributes.SetString("Yaw, Pitch", $"{Pos.Yaw * (180f / (float)Math.PI):0.##}, {Pos.Pitch * (180f / (float)Math.PI):0.##}");
			if (AnimManager != null)
			{
				UpdateAnimationDebugAttributes();
			}
		}
	}

	protected virtual void UpdateAnimationDebugAttributes()
	{
		string text = "";
		int num = 0;
		foreach (string key in AnimManager.ActiveAnimationsByAnimCode.Keys)
		{
			if (num++ > 0)
			{
				text += ",";
			}
			text += key;
		}
		num = 0;
		StringBuilder stringBuilder = new StringBuilder();
		if (AnimManager.Animator == null)
		{
			return;
		}
		RunningAnimation[] animations = AnimManager.Animator.Animations;
		foreach (RunningAnimation runningAnimation in animations)
		{
			if (runningAnimation.Running)
			{
				if (num++ > 0)
				{
					stringBuilder.Append(',');
				}
				stringBuilder.Append(runningAnimation.Animation.Code);
			}
		}
		DebugAttributes.SetString("Active Animations", (text.Length > 0) ? text : "-");
		DebugAttributes.SetString("Running Animations", (stringBuilder.Length > 0) ? stringBuilder.ToString() : "-");
	}

	public virtual void FromBytes(BinaryReader reader, bool isSync, Dictionary<string, string> serversideRemaps)
	{
		codeRemaps = serversideRemaps;
		FromBytes(reader, isSync);
		codeRemaps = null;
	}

	public virtual void FromBytes(BinaryReader reader, bool isSync)
	{
		string version = "";
		if (!isSync)
		{
			version = reader.ReadString().DeDuplicate();
		}
		EntityId = reader.ReadInt64();
		WatchedAttributes.FromBytes(reader);
		if (!WatchedAttributes.HasAttribute("extraInfoText"))
		{
			WatchedAttributes["extraInfoText"] = new TreeAttribute();
		}
		if (this is EntityPlayer && GameVersion.IsLowerVersionThan(version, "1.7.0"))
		{
			WatchedAttributes.GetTreeAttribute("health")?.SetFloat("basemaxhealth", 15f);
		}
		ServerPos.FromBytes(reader);
		GetHeadPositionFromWatchedAttributes();
		Pos.SetFrom(ServerPos);
		PositionBeforeFalling.X = reader.ReadDouble();
		PositionBeforeFalling.Y = reader.ReadDouble();
		PositionBeforeFalling.Z = reader.ReadDouble();
		string text = reader.ReadString().DeDuplicate();
		if (codeRemaps != null && codeRemaps.TryGetValue(text, out var value))
		{
			text = value;
		}
		Code = new AssetLocation(text);
		if (!isSync)
		{
			Attributes.FromBytes(reader);
		}
		if (isSync || GameVersion.IsAtLeastVersion(version, "1.8.0-pre.1"))
		{
			TreeAttribute treeAttribute = new TreeAttribute();
			treeAttribute.FromBytes(reader);
			AnimManager?.FromAttributes(treeAttribute, version);
			if (Properties?.Server?.Behaviors != null)
			{
				foreach (EntityBehavior behavior in Properties.Server.Behaviors)
				{
					behavior.FromBytes(isSync);
				}
			}
		}
		if (this is EntityPlayer && GameVersion.IsLowerVersionThan(version, "1.10-dev.2"))
		{
			WatchedAttributes.GetTreeAttribute("hunger")?.SetFloat("maxsaturation", 1500f);
		}
		Stats.FromTreeAttributes(WatchedAttributes);
		if (isSync)
		{
			Tags = EntityTagArray.FromBytes(reader);
			EntityControls obj = ((SidedProperties == null) ? null : GetInterface<IMountable>()?.ControllingControls);
			int flagsInt = reader.ReadInt32();
			obj?.FromInt(flagsInt);
		}
	}

	public virtual void ToBytes(BinaryWriter writer, bool forClient)
	{
		if (Properties?.Server?.Behaviors != null)
		{
			foreach (EntityBehavior behavior in Properties.Server.Behaviors)
			{
				behavior.ToBytes(forClient);
			}
		}
		if (!forClient)
		{
			writer.Write("1.21.0");
		}
		writer.Write(EntityId);
		SetHeadPositionToWatchedAttributes();
		Stats.ToTreeAttributes(WatchedAttributes, forClient);
		WatchedAttributes.ToBytes(writer);
		ServerPos.ToBytes(writer);
		writer.Write(PositionBeforeFalling.X);
		writer.Write(PositionBeforeFalling.Y);
		writer.Write(PositionBeforeFalling.Z);
		if (Code == null)
		{
			World.Logger.Error("Entity.ToBytes(): entityType.Code is null?! Entity will probably be incorrectly saved to disk");
		}
		writer.Write(Code?.ToShortString());
		if (!forClient)
		{
			Attributes.ToBytes(writer);
		}
		if (AnimManager is AnimationManager animationManager)
		{
			animationManager.ToAttributeBytes(writer, forClient);
			TreeAttribute.TerminateWrite(writer);
		}
		else if (AnimManager == null || AnimManager is NoAnimationManager)
		{
			TreeAttribute.TerminateWrite(writer);
		}
		else
		{
			TreeAttribute treeAttribute = new TreeAttribute();
			AnimManager.ToAttributes(treeAttribute, forClient);
			treeAttribute.ToBytes(writer);
		}
		if (forClient)
		{
			Tags.ToBytes(writer);
			writer.Write(((SidedProperties == null) ? null : GetInterface<IMountable>()?.ControllingControls)?.ToInt() ?? 0);
		}
	}

	protected virtual void SetHeadPositionToWatchedAttributes()
	{
	}

	protected virtual void GetHeadPositionFromWatchedAttributes()
	{
	}

	public virtual void Revive()
	{
		Alive = true;
		ReceiveDamage(new DamageSource
		{
			Source = EnumDamageSource.Revive,
			Type = EnumDamageType.Heal
		}, 9999f);
		AnimManager?.StopAnimation("die");
		IsOnFire = false;
		foreach (EntityBehavior behavior in SidedProperties.Behaviors)
		{
			behavior.OnEntityRevive();
		}
	}

	public virtual void Die(EnumDespawnReason reason = EnumDespawnReason.Death, DamageSource damageSourceForDeath = null)
	{
		if (!Alive)
		{
			return;
		}
		Alive = false;
		if (reason == EnumDespawnReason.Death)
		{
			Api.Event.TriggerEntityDeath(this, damageSourceForDeath);
			ItemStack[] drops = GetDrops(World, Pos.AsBlockPos, null);
			if (drops != null)
			{
				for (int i = 0; i < drops.Length; i++)
				{
					World.SpawnItemEntity(drops[i], SidedPos.XYZ.Add(0.0, 0.25, 0.0));
				}
			}
			AnimManager.ActiveAnimationsByAnimCode.Clear();
			AnimManager.StartAnimation("die");
			WatchedAttributes.SetDouble("deathTotalHours", Api.World.Calendar.TotalHours);
			if (reason == EnumDespawnReason.Death && damageSourceForDeath != null && World.Side == EnumAppSide.Server)
			{
				WatchedAttributes.SetInt("deathReason", (int)damageSourceForDeath.Source);
				WatchedAttributes.SetInt("deathDamageType", (int)damageSourceForDeath.Type);
				Entity causeEntity = damageSourceForDeath.GetCauseEntity();
				if (causeEntity != null)
				{
					WatchedAttributes.SetString("deathByEntityLangCode", "prefixandcreature-" + causeEntity.Code.Path.Replace("-", ""));
					WatchedAttributes.SetString("deathByEntity", causeEntity.Code.ToString());
				}
				if (causeEntity is EntityPlayer entityPlayer)
				{
					WatchedAttributes.SetString("deathByPlayer", entityPlayer.Player?.PlayerName);
				}
			}
			foreach (EntityBehavior behavior in SidedProperties.Behaviors)
			{
				behavior.OnEntityDeath(damageSourceForDeath);
			}
		}
		DespawnReason = new EntityDespawnData
		{
			Reason = reason,
			DamageSourceForDeath = damageSourceForDeath
		};
	}

	public virtual void PlayEntitySound(string type, IPlayer dualCallByPlayer = null, bool randomizePitch = true, float range = 24f)
	{
		if (Properties.ResolvedSounds != null && Properties.ResolvedSounds.TryGetValue(type, out var value) && value.Length != 0)
		{
			World.PlaySoundAt(value[World.Rand.Next(value.Length)], (float)SidedPos.X, (float)SidedPos.InternalY, (float)SidedPos.Z, dualCallByPlayer, randomizePitch, range);
		}
	}

	public virtual bool CanCollect(Entity byEntity)
	{
		return false;
	}

	public virtual void Notify(string key, object data)
	{
		foreach (EntityBehavior behavior in SidedProperties.Behaviors)
		{
			behavior.Notify(key, data);
		}
	}

	public virtual void WillExport(BlockPos startPos)
	{
		ServerPos.X -= startPos.X;
		ServerPos.Y -= startPos.Y;
		ServerPos.Z -= startPos.Z;
		Pos.X -= startPos.X;
		Pos.Y -= startPos.Y;
		Pos.Z -= startPos.Z;
		PositionBeforeFalling.X -= startPos.X;
		PositionBeforeFalling.Y -= startPos.Y;
		PositionBeforeFalling.Z -= startPos.Z;
	}

	public virtual void DidImportOrExport(BlockPos startPos)
	{
		ServerPos.X += startPos.X;
		ServerPos.Y += startPos.Y;
		ServerPos.Z += startPos.Z;
		ServerPos.Dimension = startPos.dimension;
		Pos.X += startPos.X;
		Pos.Y += startPos.Y;
		Pos.Z += startPos.Z;
		Pos.Dimension = startPos.dimension;
		PositionBeforeFalling.X += startPos.X;
		PositionBeforeFalling.Y += startPos.Y;
		PositionBeforeFalling.Z += startPos.Z;
	}

	public virtual void OnStoreCollectibleMappings(Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
	{
		foreach (EntityBehavior behavior in Properties.Server.Behaviors)
		{
			behavior.OnStoreCollectibleMappings(blockIdMapping, itemIdMapping);
		}
	}

	public virtual void OnLoadCollectibleMappings(IWorldAccessor worldForNewMappings, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed, bool resolveImports)
	{
		foreach (EntityBehavior behavior in Properties.Server.Behaviors)
		{
			behavior.OnLoadCollectibleMappings(worldForNewMappings, oldBlockIdMapping, oldItemIdMapping, resolveImports);
		}
	}

	public virtual string GetName()
	{
		string text = null;
		foreach (EntityBehavior behavior in SidedProperties.Behaviors)
		{
			EnumHandling handling = EnumHandling.PassThrough;
			string name = behavior.GetName(ref handling);
			switch (handling)
			{
			case EnumHandling.PreventSubsequent:
				return text;
			case EnumHandling.PreventDefault:
				text = name;
				break;
			}
		}
		if (text != null)
		{
			return text;
		}
		if (!Alive)
		{
			return Lang.GetMatching(Code.Domain + ":item-dead-creature-" + Code.Path);
		}
		return Lang.GetMatching(Code.Domain + ":item-creature-" + Code.Path);
	}

	public virtual string GetInfoText()
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (EntityBehavior behavior in SidedProperties.Behaviors)
		{
			behavior.GetInfoText(stringBuilder);
		}
		int num = WatchedAttributes.GetInt("generation");
		if (num > 0)
		{
			stringBuilder.AppendLine(Lang.Get("Generation: {0}", num));
		}
		if (!Alive && WatchedAttributes.HasAttribute("deathByPlayer"))
		{
			stringBuilder.AppendLine(Lang.Get("Killed by Player: {0}", WatchedAttributes.GetString("deathByPlayer")));
		}
		if (WatchedAttributes.HasAttribute("extraInfoText"))
		{
			foreach (KeyValuePair<string, IAttribute> item in WatchedAttributes.GetTreeAttribute("extraInfoText").SortedCopy())
			{
				stringBuilder.AppendLine(item.Value.ToString());
			}
		}
		if (Api is ICoreClientAPI coreClientAPI && coreClientAPI.Settings.Bool["extendedDebugInfo"])
		{
			stringBuilder.AppendLine("<font color=\"#bbbbbb\">Id:" + EntityId + "</font>");
			stringBuilder.AppendLine(string.Concat("<font color=\"#bbbbbb\">Code: ", Code, "</font>"));
			IEnumerable<string> source = Tags.ToArray().Select(Api.TagRegistry.EntityTagIdToTag).Order();
			if (source.Any())
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(35, 1, stringBuilder2);
				handler.AppendLiteral("<font color=\"#bbbbbb\">Tags: ");
				handler.AppendFormatted(source.Aggregate((string first, string second) => first + ", " + second));
				handler.AppendLiteral("</font>");
				stringBuilder2.AppendLine(ref handler);
			}
		}
		return stringBuilder.ToString();
	}

	public virtual void StartAnimation(string code)
	{
		AnimManager.StartAnimation(code);
	}

	public virtual void StopAnimation(string code)
	{
		AnimManager.StopAnimation(code);
	}

	public virtual bool IntersectsRay(Ray ray, AABBIntersectionTest interesectionTester, out double intersectionDistance, ref int selectionBoxIndex)
	{
		if (trickleDownRayIntersects)
		{
			EnumHandling handled = EnumHandling.PassThrough;
			bool flag = false;
			bool flag2 = false;
			intersectionDistance = 0.0;
			foreach (EntityBehavior behavior in SidedProperties.Behaviors)
			{
				flag2 |= behavior.IntersectsRay(ray, interesectionTester, out intersectionDistance, ref selectionBoxIndex, ref handled);
				flag = flag || handled == EnumHandling.PreventDefault;
				if (handled == EnumHandling.PreventSubsequent)
				{
					return flag2;
				}
			}
			if (flag)
			{
				return flag2;
			}
		}
		if (interesectionTester.RayIntersectsWithCuboid(SelectionBox, SidedPos.X, SidedPos.InternalY, SidedPos.Z))
		{
			intersectionDistance = Pos.SquareDistanceTo(ray.origin);
			return true;
		}
		intersectionDistance = 0.0;
		return false;
	}

	public virtual double GetTouchDistance()
	{
		Cuboidf selectionBox = SelectionBox;
		float num = ((selectionBox != null) ? (selectionBox.XSize / 2f) : 0.25f);
		foreach (EntityBehavior behavior in SidedProperties.Behaviors)
		{
			EnumHandling handling = EnumHandling.PassThrough;
			float num2 = behavior.GetTouchDistance(ref handling);
			if (handling != EnumHandling.PassThrough)
			{
				num = num2;
			}
			if (handling == EnumHandling.PreventSubsequent)
			{
				break;
			}
		}
		return num;
	}

	public void MarkTagsDirty()
	{
		tagsDirty = true;
	}

	public bool IsFirstTick()
	{
		if (PreviousServerPos.X == 0.0 && PreviousServerPos.Y == 0.0)
		{
			return PreviousServerPos.Z == 0.0;
		}
		return false;
	}
}
