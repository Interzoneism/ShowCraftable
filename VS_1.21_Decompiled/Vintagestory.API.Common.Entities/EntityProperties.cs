using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common.Entities;

public class EntityProperties
{
	public int Id;

	public string Color;

	public AssetLocation Code;

	public OrderedDictionary<string, string> Variant = new OrderedDictionary<string, string>();

	public string Class;

	public EntityTagArray Tags = EntityTagArray.Empty;

	public EnumHabitat Habitat = EnumHabitat.Land;

	public Vec2f CollisionBoxSize = new Vec2f(0.2f, 0.2f);

	public Vec2f DeadCollisionBoxSize = new Vec2f(0.3f, 0.3f);

	public Vec2f SelectionBoxSize;

	public Vec2f DeadSelectionBoxSize;

	public double EyeHeight;

	public double SwimmingEyeHeight;

	public float Weight = 25f;

	public bool CanClimb;

	public bool CanClimbAnywhere;

	public bool FallDamage = true;

	public float FallDamageMultiplier = 1f;

	public float ClimbTouchDistance;

	public bool RotateModelOnClimb;

	public float KnockbackResistance;

	public JsonObject Attributes;

	public EntityClientProperties Client;

	public EntityServerProperties Server;

	public Dictionary<string, AssetLocation> Sounds;

	public Dictionary<string, AssetLocation[]> ResolvedSounds = new Dictionary<string, AssetLocation[]>();

	public float IdleSoundChance = 0.3f;

	public float IdleSoundRange = 24f;

	public BlockDropItemStack[] Drops;

	public byte[] DropsPacket;

	public Cuboidf SpawnCollisionBox => new Cuboidf
	{
		X1 = (0f - CollisionBoxSize.X) / 2f,
		Z1 = (0f - CollisionBoxSize.X) / 2f,
		X2 = CollisionBoxSize.X / 2f,
		Z2 = CollisionBoxSize.X / 2f,
		Y2 = CollisionBoxSize.Y
	};

	public EntityProperties Clone()
	{
		BlockDropItemStack[] array;
		if (Drops == null)
		{
			array = null;
		}
		else
		{
			array = new BlockDropItemStack[Drops.Length];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = Drops[i].Clone();
			}
		}
		Dictionary<string, AssetLocation> dictionary = new Dictionary<string, AssetLocation>();
		foreach (KeyValuePair<string, AssetLocation> sound in Sounds)
		{
			dictionary[sound.Key] = sound.Value.Clone();
		}
		Dictionary<string, AssetLocation[]> dictionary2 = new Dictionary<string, AssetLocation[]>();
		foreach (KeyValuePair<string, AssetLocation[]> resolvedSound in ResolvedSounds)
		{
			AssetLocation[] value = resolvedSound.Value;
			dictionary2[resolvedSound.Key] = new AssetLocation[value.Length];
			for (int j = 0; j < value.Length; j++)
			{
				dictionary2[resolvedSound.Key][j] = value[j].Clone();
			}
		}
		if (!(Attributes is JsonObject_ReadOnly) && Attributes != null)
		{
			Attributes = new JsonObject_ReadOnly(Attributes);
		}
		return new EntityProperties
		{
			Code = Code.Clone(),
			Tags = Tags,
			Class = Class,
			Color = Color,
			Habitat = Habitat,
			CollisionBoxSize = CollisionBoxSize.Clone(),
			DeadCollisionBoxSize = DeadCollisionBoxSize.Clone(),
			SelectionBoxSize = SelectionBoxSize?.Clone(),
			DeadSelectionBoxSize = DeadSelectionBoxSize?.Clone(),
			CanClimb = CanClimb,
			Weight = Weight,
			CanClimbAnywhere = CanClimbAnywhere,
			FallDamage = FallDamage,
			FallDamageMultiplier = FallDamageMultiplier,
			ClimbTouchDistance = ClimbTouchDistance,
			RotateModelOnClimb = RotateModelOnClimb,
			KnockbackResistance = KnockbackResistance,
			Attributes = Attributes,
			Sounds = new Dictionary<string, AssetLocation>(Sounds),
			IdleSoundChance = IdleSoundChance,
			IdleSoundRange = IdleSoundRange,
			Drops = array,
			EyeHeight = EyeHeight,
			SwimmingEyeHeight = SwimmingEyeHeight,
			Client = (Client?.Clone() as EntityClientProperties),
			Server = (Server?.Clone() as EntityServerProperties),
			Variant = new OrderedDictionary<string, string>(Variant)
		};
	}

	public void Initialize(Entity entity, ICoreAPI api)
	{
		if (api.Side.IsClient())
		{
			if (Client == null)
			{
				return;
			}
			Client.loadBehaviors(entity, this, api.World);
		}
		else if (Server != null)
		{
			Server.loadBehaviors(entity, this, api.World);
		}
		Client?.Init(Code, api.World);
		InitSounds(api.Assets);
	}

	public void InitSounds(IAssetManager assetManager)
	{
		if (Sounds == null)
		{
			return;
		}
		foreach (KeyValuePair<string, AssetLocation> sound in Sounds)
		{
			if (sound.Value.Path.EndsWith('*'))
			{
				List<IAsset> manyInCategory = assetManager.GetManyInCategory("sounds", sound.Value.Path.Substring(0, sound.Value.Path.Length - 1), sound.Value.Domain);
				AssetLocation[] array = new AssetLocation[manyInCategory.Count];
				int num = 0;
				foreach (IAsset item in manyInCategory)
				{
					array[num++] = item.Location;
				}
				ResolvedSounds[sound.Key] = array;
			}
			else
			{
				ResolvedSounds[sound.Key] = new AssetLocation[1] { sound.Value.Clone().WithPathPrefix("sounds/") };
			}
		}
	}

	internal void PopulateDrops(IWorldAccessor worldForResolve)
	{
		using (MemoryStream input = new MemoryStream(DropsPacket))
		{
			BinaryReader binaryReader = new BinaryReader(input);
			BlockDropItemStack[] array = new BlockDropItemStack[binaryReader.ReadInt32()];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = new BlockDropItemStack();
				array[i].FromBytes(binaryReader, worldForResolve.ClassRegistry);
				array[i].Resolve(worldForResolve, "decode entity drops for ", Code);
			}
			Drops = array;
		}
		DropsPacket = null;
	}
}
