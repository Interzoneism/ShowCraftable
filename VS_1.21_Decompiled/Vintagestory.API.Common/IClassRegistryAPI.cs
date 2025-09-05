using System;
using System.Collections.Generic;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Vintagestory.API.Common;

public interface IClassRegistryAPI
{
	Dictionary<string, Type> BlockClassToTypeMapping { get; }

	Dictionary<string, Type> ItemClassToTypeMapping { get; }

	string GetBlockBehaviorClassName(Type blockBehaviorType);

	string GetCollectibleBehaviorClassName(Type blockBehaviorType);

	Block CreateBlock(string blockclass);

	Type GetBlockClass(string blockclass);

	BlockEntity CreateBlockEntity(string blockEntityClass);

	Entity CreateEntity(string entityClass);

	Entity CreateEntity(EntityProperties entityType);

	IMountableSeat GetMountable(TreeAttribute tree);

	BlockBehavior CreateBlockBehavior(Block forBlock, string code);

	CollectibleBehavior CreateCollectibleBehavior(CollectibleObject forCollectible, string code);

	Type GetBlockEntityBehaviorClass(string name);

	BlockEntityBehavior CreateBlockEntityBehavior(BlockEntity blockEntity, string name);

	Type GetBlockBehaviorClass(string code);

	Type GetCollectibleBehaviorClass(string code);

	EntityBehavior CreateEntityBehavior(Entity forEntity, string entityBehaviorName);

	Type GetEntityBehaviorClass(string entityBehaviorName);

	IInventoryNetworkUtil CreateInvNetworkUtil(InventoryBase inv, ICoreAPI api);

	Item CreateItem(string itemclass);

	Type GetItemClass(string itemClass);

	JsonTreeAttribute CreateJsonTreeAttributeFromDict(Dictionary<string, JsonTreeAttribute> attributes);

	Type GetBlockEntity(string bockEntityClass);

	string GetBlockEntityClass(Type type);

	string GetEntityClassName(Type entityType);

	CropBehavior CreateCropBehavior(Block forBlock, string cropBehaviorName);

	void RegisterParticlePropertyProvider(string className, Type ParticleProvider);

	IParticlePropertiesProvider CreateParticlePropertyProvider(Type entityType);

	IParticlePropertiesProvider CreateParticlePropertyProvider(string className);
}
