using System;
using System.Collections.Generic;
using Vintagestory.API.Datastructures;

namespace Vintagestory.API.Common;

public interface ICoreAPICommon
{
	Dictionary<string, object> ObjectCache { get; }

	string DataBasePath { get; }

	T RegisterRecipeRegistry<T>(string recipeRegistryCode) where T : RecipeRegistryBase;

	void RegisterColorMap(ColorMap map);

	void RegisterEntity(string className, Type entity);

	void RegisterEntityBehaviorClass(string className, Type entityBehavior);

	void RegisterBlockClass(string className, Type blockType);

	void RegisterCropBehavior(string className, Type type);

	void RegisterBlockEntityClass(string className, Type blockentityType);

	void RegisterItemClass(string className, Type itemType);

	void RegisterCollectibleBehaviorClass(string className, Type blockBehaviorType);

	void RegisterBlockBehaviorClass(string className, Type blockBehaviorType);

	void RegisterBlockEntityBehaviorClass(string className, Type blockEntityBehaviorType);

	void RegisterMountable(string className, GetMountableDelegate mountableInstancer);

	string GetOrCreateDataPath(string foldername);

	void StoreModConfig<T>(T jsonSerializeableData, string filename);

	void StoreModConfig(JsonObject jobj, string filename);

	T LoadModConfig<T>(string filename);

	JsonObject LoadModConfig(string filename);
}
