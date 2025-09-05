using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common.Entities;

public class EntityClientProperties : EntitySidedProperties
{
	public EntityRenderer Renderer;

	public string RendererName;

	public IDictionary<string, CompositeTexture> Textures = new FastSmallDictionary<string, CompositeTexture>(0);

	public int TexturesAlternatesCount;

	public int GlowLevel;

	public bool PitchStep = true;

	public CompositeShape Shape;

	public Shape LoadedShape;

	public Shape[] LoadedAlternateShapes;

	public Shape LoadedShapeForEntity;

	public CompositeShape ShapeForEntity;

	public float Size = 1f;

	public float SizeGrowthFactor;

	public AnimationMetaData[] Animations;

	public Dictionary<string, AnimationMetaData> AnimationsByMetaCode = new Dictionary<string, AnimationMetaData>(StringComparer.OrdinalIgnoreCase);

	public Dictionary<uint, AnimationMetaData> AnimationsByCrc32 = new Dictionary<uint, AnimationMetaData>();

	public virtual CompositeTexture Texture
	{
		get
		{
			if (Textures != null && Textures.Count != 0)
			{
				return Textures.First().Value;
			}
			return null;
		}
	}

	public CompositeTexture FirstTexture
	{
		get
		{
			if (Textures != null && Textures.Count != 0)
			{
				return Textures.First().Value;
			}
			return null;
		}
	}

	public EntityClientProperties(JsonObject[] behaviors, Dictionary<string, JsonObject> commonConfigs)
		: base(behaviors, commonConfigs)
	{
	}

	public void DetermineLoadedShape(long forEntityId)
	{
		if (LoadedAlternateShapes != null && LoadedAlternateShapes.Length != 0)
		{
			int num = GameMath.MurmurHash3Mod(0, 0, (int)forEntityId, 1 + LoadedAlternateShapes.Length);
			if (num == 0)
			{
				LoadedShapeForEntity = LoadedShape;
				ShapeForEntity = Shape;
			}
			else
			{
				LoadedShapeForEntity = LoadedAlternateShapes[num - 1];
				ShapeForEntity = Shape.Alternates[num - 1];
			}
		}
		else
		{
			LoadedShapeForEntity = LoadedShape;
			ShapeForEntity = Shape;
		}
	}

	public void Init(AssetLocation entityTypeCode, IWorldAccessor world)
	{
		if (Animations != null)
		{
			for (int i = 0; i < Animations.Length; i++)
			{
				AnimationMetaData animationMetaData = Animations[i];
				animationMetaData.Init();
				if (animationMetaData.Animation != null)
				{
					AnimationsByMetaCode[animationMetaData.Code] = animationMetaData;
				}
				if (animationMetaData.Animation != null)
				{
					AnimationsByCrc32[animationMetaData.CodeCrc32] = animationMetaData;
				}
			}
		}
		if (world != null)
		{
			EntityClientProperties entityClientProperties = world.EntityTypes.FirstOrDefault((EntityProperties et) => et.Code.Equals(entityTypeCode))?.Client;
			LoadedShape = entityClientProperties?.LoadedShape;
			LoadedAlternateShapes = entityClientProperties?.LoadedAlternateShapes;
		}
	}

	public override EntitySidedProperties Clone()
	{
		Dictionary<string, AnimationMetaData> dictionary = new Dictionary<string, AnimationMetaData>(StringComparer.OrdinalIgnoreCase);
		Dictionary<uint, AnimationMetaData> dictionary2 = new Dictionary<uint, AnimationMetaData>();
		AnimationMetaData[] array = null;
		if (Animations != null)
		{
			AnimationMetaData[] animations = Animations;
			array = new AnimationMetaData[animations.Length];
			for (int i = 0; i < array.Length; i++)
			{
				AnimationMetaData animationMetaData = (array[i] = animations[i].Clone());
				if (AnimationsByMetaCode.ContainsKey(animationMetaData.Code))
				{
					dictionary[animationMetaData.Code] = animationMetaData;
					dictionary2[animationMetaData.CodeCrc32] = animationMetaData;
				}
			}
		}
		return new EntityClientProperties(BehaviorsAsJsonObj, null)
		{
			Textures = ((Textures == null) ? null : new FastSmallDictionary<string, CompositeTexture>(Textures)),
			TexturesAlternatesCount = TexturesAlternatesCount,
			RendererName = RendererName,
			GlowLevel = GlowLevel,
			PitchStep = PitchStep,
			Size = Size,
			SizeGrowthFactor = SizeGrowthFactor,
			Shape = Shape?.Clone(),
			LoadedAlternateShapes = LoadedAlternateShapes,
			Animations = array,
			AnimationsByMetaCode = dictionary,
			AnimationsByCrc32 = dictionary2
		};
	}

	public virtual void FreeRAMServer()
	{
		CompositeTexture[] array = FirstTexture?.Alternates;
		TexturesAlternatesCount = ((array != null) ? array.Length : 0);
		Textures = null;
		if (Animations != null)
		{
			AnimationMetaData[] animations = Animations;
			for (int i = 0; i < animations.Length; i++)
			{
				animations[i].DeDuplicate();
			}
		}
	}
}
